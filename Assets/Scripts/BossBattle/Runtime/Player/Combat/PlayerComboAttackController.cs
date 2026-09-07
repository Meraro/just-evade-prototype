using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    public sealed class PlayerComboAttackController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private BossBattleInputReader inputReader;
        [SerializeField] private PlayerMotor playerMotor;
        [SerializeField] private PlayerDodgeController dodgeController;
        [SerializeField] private Hurtbox targetHurtbox;
        [SerializeField] private CombatAnimationProfile attackProfile;

        [Header("Combo Timing")]
        [SerializeField, Min(0f)] private float postAttackRecoveryDuration = 0.15f;

        [Header("Optional Finisher")]
        [Tooltip("When enabled, the third attack uses the experimental two-handed finisher from Combat Melee Animations Pro.")]
        [SerializeField] private bool enableTwoHandedFinisher;

        // Retained only so pre-profile scenes remain functional.
        [SerializeField, HideInInspector, Range(0f, 1f)] private float hitTime = 0.45f;
        [SerializeField, HideInInspector, Min(0f)] private float damage = 50f;
        [SerializeField, HideInInspector, Min(0f)] private float attackRange = 2.2f;
        [SerializeField, HideInInspector, Range(0f, 180f)] private float attackArc = 110f;

        private int activeStep;
        private int pendingStep;
        private bool queuedNextStep;
        private bool hitApplied;
        private bool activeStepReachedEndEvent;
        private bool isRecovering;
        private bool recoveryTimerStarted;
        private bool exitRecoveryRequested;
        private float recoveryEndTime = float.NegativeInfinity;

        private static readonly int[] RecoveryStateHashes =
        {
            Animator.StringToHash("Attack1Recovery"),
            Animator.StringToHash("Attack2Recovery"),
            Animator.StringToHash("Attack3Recovery")
        };

        public bool IsAttacking => activeStep > 0 || pendingStep > 0;
        public bool IsRecovering => isRecovering;
        public int ActiveStep => activeStep;
        public float RemainingRecoveryLockDuration => !recoveryTimerStarted
            ? postAttackRecoveryDuration
            : Mathf.Max(0f, recoveryEndTime - Time.time);

        // Recovery keeps movement locked but intentionally does not consume animation root motion.
        public bool IsApplyingAttackRootMotion => IsAttacking && !isRecovering;

        private int MaximumComboStep
        {
            get
            {
                int profileAttackCount = attackProfile == null ? 0 : attackProfile.AttackCount;
                int configuredMaximum = enableTwoHandedFinisher ? 3 : 2;
                return profileAttackCount > 0
                    ? Mathf.Min(configuredMaximum, profileAttackCount)
                    : configuredMaximum;
            }
        }

        public void Configure(
            Animator targetAnimator,
            BossBattleInputReader targetInputReader,
            PlayerMotor targetPlayerMotor,
            Hurtbox target,
            CombatAnimationProfile targetAttackProfile)
        {
            animator = targetAnimator;
            inputReader = targetInputReader;
            playerMotor = targetPlayerMotor;
            targetHurtbox = target;
            attackProfile = targetAttackProfile;
        }

        public void SetEndPoseHoldDuration(float targetPostAttackRecoveryDuration)
        {
            postAttackRecoveryDuration = Mathf.Max(0f, targetPostAttackRecoveryDuration);
        }

        public void ConfigureDodge(PlayerDodgeController targetDodgeController)
        {
            dodgeController = targetDodgeController;
        }

        public bool TryPrepareForDodge()
        {
            if (IsAttacking && !isRecovering)
            {
                return false;
            }

            if (isRecovering)
            {
                EndCombo();
            }

            return true;
        }

        private void Update()
        {
            if (animator == null || inputReader == null)
            {
                return;
            }

            if (dodgeController != null && dodgeController.IsDodging)
            {
                return;
            }

            if (isRecovering)
            {
                if (inputReader.AttackPressed)
                {
                    BeginStep(1);
                    return;
                }

                if (inputReader.Move.sqrMagnitude > 0.0001f)
                {
                    RequestRecoveryExit(true);
                    return;
                }

                UpdateRecovery();
                return;
            }

            if (activeStep == 0 && pendingStep == 0)
            {
                if (inputReader.AttackPressed)
                {
                    BeginStep(1);
                }

                return;
            }

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (pendingStep > 0)
            {
                if (state.shortNameHash == Animator.StringToHash($"Attack{pendingStep}"))
                {
                    activeStep = pendingStep;
                    pendingStep = 0;
                    queuedNextStep = false;
                    hitApplied = false;
                }

                return;
            }

            if (state.shortNameHash != Animator.StringToHash($"Attack{activeStep}"))
            {
                EndCombo();
                return;
            }

            float normalizedTime = state.normalizedTime;
            if (!hitApplied && normalizedTime >= GetCurrentHitNormalizedTime())
            {
                hitApplied = true;
                TryApplyHit();
            }

            if (!queuedNextStep && activeStep < MaximumComboStep && inputReader.AttackPressed)
            {
                queuedNextStep = true;
            }

            if (activeStepReachedEndEvent || normalizedTime >= 1f)
            {
                ResolveAttackEnd();
                return;
            }
        }

        /// <summary>
        /// Animation Event receiver. The event is relayed from the Animator GameObject,
        /// then resolves the buffered next step at the clip's authored connection point.
        /// </summary>
        public void OnAttackEndAnimationEvent()
        {
            if (activeStep <= 0 || pendingStep > 0 || isRecovering)
            {
                return;
            }

            activeStepReachedEndEvent = true;
        }

        private void BeginStep(int step)
        {
            playerMotor?.SetMovementEnabled(false);
            pendingStep = step;
            isRecovering = false;
            activeStepReachedEndEvent = false;
            recoveryTimerStarted = false;
            exitRecoveryRequested = false;
            recoveryEndTime = float.NegativeInfinity;
            animator.ResetTrigger("Attack1");
            animator.ResetTrigger("Attack2");
            animator.ResetTrigger("Attack3");
            animator.ResetTrigger("EnterRecovery1");
            animator.ResetTrigger("EnterRecovery2");
            animator.ResetTrigger("EnterRecovery3");
            animator.ResetTrigger("ExitRecovery");
            animator.ResetTrigger("ExitRecoveryToMove");
            animator.SetTrigger($"Attack{step}");
        }

        private void BeginRecovery()
        {
            isRecovering = true;
            queuedNextStep = false;
            activeStepReachedEndEvent = false;
            recoveryTimerStarted = false;
            recoveryEndTime = float.NegativeInfinity;
            animator.SetTrigger($"EnterRecovery{activeStep}");
        }

        private void UpdateRecovery()
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (!recoveryTimerStarted)
            {
                if (!IsRecoveryState(state))
                {
                    return;
                }

                recoveryTimerStarted = true;
                recoveryEndTime = Time.time + postAttackRecoveryDuration;
            }

            if (!exitRecoveryRequested && Time.time >= recoveryEndTime)
            {
                RequestRecoveryExit(false);
                return;
            }

            if (exitRecoveryRequested && !IsRecoveryState(state))
            {
                EndCombo();
            }
        }

        private void RequestRecoveryExit(bool moveToLocomotion)
        {
            if (exitRecoveryRequested)
            {
                return;
            }

            exitRecoveryRequested = true;
            playerMotor?.SetMovementEnabled(true);
            animator.SetTrigger(moveToLocomotion ? "ExitRecoveryToMove" : "ExitRecovery");
        }

        private static bool IsRecoveryState(AnimatorStateInfo state)
        {
            foreach (int recoveryStateHash in RecoveryStateHashes)
            {
                if (state.shortNameHash == recoveryStateHash)
                {
                    return true;
                }
            }

            return false;
        }

        private void EndCombo()
        {
            activeStep = 0;
            pendingStep = 0;
            queuedNextStep = false;
            hitApplied = false;
            activeStepReachedEndEvent = false;
            isRecovering = false;
            recoveryTimerStarted = false;
            exitRecoveryRequested = false;
            recoveryEndTime = float.NegativeInfinity;
            animator?.ResetTrigger("EnterRecovery1");
            animator?.ResetTrigger("EnterRecovery2");
            animator?.ResetTrigger("EnterRecovery3");
            animator?.ResetTrigger("ExitRecovery");
            animator?.ResetTrigger("ExitRecoveryToMove");
            playerMotor?.SetMovementEnabled(true);
        }

        private void TryApplyHit()
        {
            CombatAttackDefinition attack = attackProfile == null
                ? null
                : attackProfile.GetAttack(activeStep);
            if (targetHurtbox == null || targetHurtbox.Health == null || targetHurtbox.Health.IsDead)
            {
                return;
            }

            Vector3 offset = targetHurtbox.transform.position - transform.position;
            offset.y = 0f;
            float range = attack == null ? attackRange : attack.Range;
            if (offset.sqrMagnitude > range * range || offset.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            float arc = attack == null ? attackArc : attack.Arc;
            if (Vector3.Angle(transform.forward, offset) > arc * 0.5f)
            {
                return;
            }

            targetHurtbox.ReceiveDamage(attack == null ? damage : attack.Damage);
        }

        private float GetCurrentHitNormalizedTime()
        {
            CombatAttackDefinition attack = attackProfile == null
                ? null
                : attackProfile.GetAttack(activeStep);
            return attack == null ? hitTime : attack.HitNormalizedTime;
        }

        private void ResolveAttackEnd()
        {
            if (queuedNextStep && activeStep < MaximumComboStep)
            {
                BeginStep(activeStep + 1);
                return;
            }

            BeginRecovery();
        }
    }
}
