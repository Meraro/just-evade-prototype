using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    public sealed class BossAttackController : MonoBehaviour
    {
        private static readonly int TelegraphTrigger = Animator.StringToHash("BossTelegraph");
        private static readonly int AttackEndTrigger = Animator.StringToHash("BossAttackEnd");
        private static readonly int RecoveryEndTrigger = Animator.StringToHash("BossRecoveryEnd");
        private static readonly int RecoveryState = Animator.StringToHash("BossRecovery");

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private BossMotor motor;
        [SerializeField] private Hurtbox targetHurtbox;
        [SerializeField] private BossAttackThreatTracker threatTracker;
        [SerializeField] private BossAttackProfile[] attackProfiles;
        [SerializeField] private BossAttackSelectionPolicy attackSelection = new BossAttackSelectionPolicy();

        [Header("Weapon Pose Layer")]
        [SerializeField] private bool useWeaponHandsGrip = true;
        [SerializeField] private string weaponHandsGripLayerName = "WeaponHandsGrip";
        [SerializeField, Min(0f)] private float weaponHandsBlendSpeed = 12f;

        private bool isTelegraphing;
        private bool isAttacking;
        private bool isRecovering;
        private bool attackEndRequested;
        private int processedHitCount;
        private bool recoveryTimerStarted;
        private bool recoveryExitRequested;
        private float attackStartTime;
        private float telegraphEndTime;
        private float recoveryEndTime;
        private int weaponHandsGripLayerIndex = -1;
        private int activeAttackIndex = -1;
        private readonly HashSet<int> processedStrikeIndices = new HashSet<int>();

        public bool IsBusy => isTelegraphing || isAttacking || isRecovering;
        public bool IsTelegraphing => isTelegraphing;
        public bool IsAttacking => isAttacking;
        public bool IsRecovering => isRecovering;
        public bool HitApplied => processedHitCount > 0;
        public int ProcessedHitCount => processedHitCount;
        public event Action AttackCommitted;
        public event Action AttackFinished;
        public bool HasConfiguredAttacks => attackProfiles != null && attackProfiles.Length > 0;
        public bool CanBeginAttack => !IsBusy && attackSelection != null && attackSelection.CanSelectAttack(attackProfiles);
        public float RemainingPostAttackDecisionDelay => attackSelection == null
            ? 0f
            : attackSelection.RemainingPostAttackDecisionDelay;
        public BossAttackProfile AttackProfile => HasConfiguredAttacks && activeAttackIndex >= 0 && activeAttackIndex < attackProfiles.Length
            ? attackProfiles[activeAttackIndex]
            : HasConfiguredAttacks ? attackProfiles[0] : null;
        public float ApproachStopDistance => GetLargestProfileValue(profile => profile.ApproachStopDistance);
        public float AttackStartDistance => GetLargestProfileValue(profile => profile.AttackStartDistance);
        public float AttackStartAngle => GetLargestProfileValue(profile => profile.AttackStartAngle);
        public float PreparationTurnSpeed => GetLargestProfileValue(profile => profile.PreparationTurnSpeed);

        public void Configure(
            Animator targetAnimator,
            BossMotor targetMotor,
            Hurtbox targetPlayerHurtbox,
            BossAttackProfile[] profiles)
        {
            animator = targetAnimator;
            motor = targetMotor;
            targetHurtbox = targetPlayerHurtbox;
            attackProfiles = profiles;
            EnsureAttackSelection();
            attackSelection.Configure(attackProfiles);
            FindWeaponHandsGripLayer();
            threatTracker ??= GetComponent<BossAttackThreatTracker>();
        }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (motor == null)
            {
                motor = GetComponent<BossMotor>();
            }

            threatTracker ??= GetComponent<BossAttackThreatTracker>();

            EnsureAttackSelection();
            attackSelection.Configure(attackProfiles);
            FindWeaponHandsGripLayer();
        }

        private void OnDisable()
        {
            isTelegraphing = false;
            isAttacking = false;
            isRecovering = false;
            threatTracker?.EndThreat();
            motor?.Stop();
        }

        private void Update()
        {
            UpdateWeaponHandsGrip();

            if (isTelegraphing && Time.time >= telegraphEndTime)
            {
                BeginCommittedAttack();
            }

            if (isAttacking && !attackEndRequested && HasReachedFallbackEnd())
            {
                RequestAttackEnd();
            }

            UpdateRecovery();
            attackSelection?.RefreshDebug(attackProfiles);
        }

        public bool BeginAttack()
        {
            if (!CanBeginAttack || animator == null || targetHurtbox == null ||
                targetHurtbox.Health == null || targetHurtbox.Health.IsDead)
            {
                return false;
            }

            activeAttackIndex = attackSelection.SelectRandomAttackIndex(attackProfiles);
            BossAttackProfile attackProfile = AttackProfile;
            if (activeAttackIndex < 0 || attackProfile == null || attackProfile.AnimationClip == null ||
                string.IsNullOrWhiteSpace(attackProfile.AnimatorTrigger))
            {
                activeAttackIndex = -1;
                return false;
            }

            motor?.Stop();
            isTelegraphing = true;
            isAttacking = false;
            attackEndRequested = false;
            processedHitCount = 0;
            processedStrikeIndices.Clear();
            telegraphEndTime = Time.time + attackProfile.TelegraphDuration;
            ResetAttackTriggers();
            animator.ResetTrigger(AttackEndTrigger);
            animator.ResetTrigger(RecoveryEndTrigger);
            animator.SetTrigger(TelegraphTrigger);
            return true;
        }

        // Animation Event: an authored blade contact frame.
        public void OnAttackHitAnimationEvent()
        {
            OnAttackHitAnimationEvent(processedHitCount + 1);
        }

        // Animation Event: authored blade contact frame with an explicit data strike index.
        public void OnAttackHitAnimationEvent(int strikeIndex)
        {
            BossAttackProfile attackProfile = AttackProfile;
            if (!isAttacking || attackProfile == null || strikeIndex < 1 || strikeIndex > attackProfile.StrikeCount ||
                processedStrikeIndices.Contains(strikeIndex))
            {
                return;
            }

            // Each event consumes one strike even if the target is currently outside the hit volume.
            processedStrikeIndices.Add(strikeIndex);
            processedHitCount = processedStrikeIndices.Count;
            BossAttackStrikeDefinition strike = attackProfile.GetStrike(strikeIndex);
            if (targetHurtbox == null || targetHurtbox.Health == null || targetHurtbox.Health.IsDead)
            {
                return;
            }

            if (threatTracker != null)
            {
                threatTracker.ResolveAttackHit(targetHurtbox, strike.Damage, strikeIndex);
                return;
            }

            if (BossAttackGeometry.IsWithinAttackShape(
                    transform,
                    targetHurtbox.transform.position,
                    strike.HitRange,
                    strike.HitArc))
            {
                targetHurtbox.ReceiveDamage(strike.Damage);
            }
        }

        // Animation Event: authored point at which the attack may recover.
        public void OnAttackEndAnimationEvent()
        {
            if (isAttacking)
            {
                RequestAttackEnd();
            }
        }

        // Animation Event: AttackTiming mode input window open.
        public void OnJustDodgeWindowOpenAnimationEvent(int windowId)
        {
            if (isAttacking)
            {
                threatTracker?.OpenJustDodgeWindow(windowId);
            }
        }

        // Animation Event: AttackTiming mode input window close.
        public void OnJustDodgeWindowCloseAnimationEvent(int windowId)
        {
            if (isAttacking)
            {
                threatTracker?.CloseJustDodgeWindow(windowId);
            }
        }

        private void UpdateRecovery()
        {
            if (!isRecovering || animator == null)
            {
                return;
            }

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (!recoveryTimerStarted)
            {
                if (state.shortNameHash != RecoveryState)
                {
                    return;
                }

                recoveryTimerStarted = true;
                recoveryEndTime = Time.time + (AttackProfile == null ? 0f : AttackProfile.RecoveryDuration);
            }

            if (!recoveryExitRequested && Time.time >= recoveryEndTime)
            {
                recoveryExitRequested = true;
                animator.SetTrigger(RecoveryEndTrigger);
                return;
            }

            if (recoveryExitRequested && state.shortNameHash != RecoveryState)
            {
                isRecovering = false;
                recoveryTimerStarted = false;
                recoveryExitRequested = false;
                activeAttackIndex = -1;
            }
        }

        private bool HasReachedFallbackEnd()
        {
            BossAttackProfile attackProfile = AttackProfile;
            if (attackProfile == null || attackProfile.AnimationClip == null)
            {
                return false;
            }

            float duration = attackProfile.AnimationClip.length / Mathf.Max(0.1f, attackProfile.PlaybackSpeed);
            return Time.time >= attackStartTime + duration * attackProfile.FallbackEndNormalizedTime;
        }

        private void BeginCommittedAttack()
        {
            BossAttackProfile attackProfile = AttackProfile;
            if (!isTelegraphing || animator == null || attackProfile == null)
            {
                return;
            }

            isTelegraphing = false;
            isAttacking = true;
            attackStartTime = Time.time;
            threatTracker?.BeginThreat(attackProfile);
            animator.SetTrigger(attackProfile.AnimatorTrigger);
            AttackCommitted?.Invoke();
        }

        private void RequestAttackEnd()
        {
            if (attackEndRequested || animator == null)
            {
                return;
            }

            attackEndRequested = true;
            attackSelection?.BeginAttackCooldown(activeAttackIndex, AttackProfile);
            isAttacking = false;
            isTelegraphing = false;
            isRecovering = true;
            recoveryTimerStarted = false;
            recoveryExitRequested = false;
            threatTracker?.EndThreat();
            animator.SetTrigger(AttackEndTrigger);
            AttackFinished?.Invoke();
        }

        private void UpdateWeaponHandsGrip()
        {
            if (animator == null || weaponHandsGripLayerIndex < 0)
            {
                return;
            }

            float targetWeight = useWeaponHandsGrip && !IsBusy ? 1f : 0f;
            float currentWeight = animator.GetLayerWeight(weaponHandsGripLayerIndex);
            animator.SetLayerWeight(
                weaponHandsGripLayerIndex,
                Mathf.MoveTowards(currentWeight, targetWeight, weaponHandsBlendSpeed * Time.deltaTime));
        }

        private void FindWeaponHandsGripLayer()
        {
            weaponHandsGripLayerIndex = animator == null ? -1 : animator.GetLayerIndex(weaponHandsGripLayerName);
        }

        private float GetLargestProfileValue(Func<BossAttackProfile, float> selector)
        {
            if (!HasConfiguredAttacks)
            {
                return 0f;
            }

            float largest = 0f;
            foreach (BossAttackProfile profile in attackProfiles)
            {
                if (profile != null)
                {
                    largest = Mathf.Max(largest, selector(profile));
                }
            }

            return largest;
        }

        private void ResetAttackTriggers()
        {
            foreach (BossAttackProfile profile in attackProfiles)
            {
                if (profile != null && !string.IsNullOrWhiteSpace(profile.AnimatorTrigger))
                {
                    animator.ResetTrigger(profile.AnimatorTrigger);
                }
            }
        }

        private void EnsureAttackSelection()
        {
            if (attackSelection == null)
            {
                attackSelection = new BossAttackSelectionPolicy();
            }
        }
    }
}
