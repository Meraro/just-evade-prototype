using System;
using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BossBattleInputReader))]
    [RequireComponent(typeof(Hurtbox))]
    public sealed class PlayerDodgeController : MonoBehaviour
    {
        private static readonly int DodgeTrigger = Animator.StringToHash("Dodge");

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private BossBattleInputReader inputReader;
        [SerializeField] private PlayerMotor playerMotor;
        [SerializeField] private PlayerComboAttackController comboController;
        [SerializeField] private Hurtbox hurtbox;

        [Header("Eight Direction Root Motion Dodge")]
        [SerializeField, Min(0.05f)] private float dodgeDuration = 0.8f;
        [SerializeField, Min(0f)] private float rootMotionDistanceMultiplier = 1f;
        [SerializeField] private bool constrainRootMotionToDodgeDirection = true;
        [SerializeField, Min(0f)] private float recoveryDuration = 0.12f;

        [Header("Invulnerability")]
        [SerializeField, Min(0f)] private float invulnerabilityStartTime = 0.08f;
        [SerializeField, Min(0f)] private float invulnerabilityEndTime = 0.52f;

        [Header("Runtime Diagnostics")]
        [SerializeField] private bool isDodging;
        [SerializeField] private bool isRecovering;
        [SerializeField] private bool isInvulnerable;
        [SerializeField] private float normalizedDodgeTime;
        [SerializeField] private bool enableDodgeDebugLogging = true;

        private Vector3 dodgeDirection;
        private Vector3 dodgeStartPosition;
        private float dodgeStartTime;

        public bool IsDodging => isDodging;
        public bool IsInvulnerable => isInvulnerable;
        public bool IsApplyingDodgeRootMotion => isDodging && !isRecovering;
        public float RootMotionDistanceMultiplier => rootMotionDistanceMultiplier;
        public bool ConstrainRootMotionToDodgeDirection => constrainRootMotionToDodgeDirection;
        public Vector3 DodgeDirection => dodgeDirection;

        public event Action DodgeStarted;
        public event Action DodgeEnded;

        public void SetDodgeDuration(float targetDodgeDuration)
        {
            dodgeDuration = Mathf.Max(0.05f, targetDodgeDuration);
        }

        public void Configure(
            Animator targetAnimator,
            BossBattleInputReader targetInputReader,
            PlayerMotor targetPlayerMotor,
            PlayerComboAttackController targetComboController,
            Hurtbox targetHurtbox)
        {
            animator = targetAnimator;
            inputReader = targetInputReader;
            playerMotor = targetPlayerMotor;
            comboController = targetComboController;
            hurtbox = targetHurtbox;
        }

        private void Awake()
        {
            inputReader ??= GetComponent<BossBattleInputReader>();
            playerMotor ??= GetComponent<PlayerMotor>();
            comboController ??= GetComponent<PlayerComboAttackController>();
            hurtbox ??= GetComponent<Hurtbox>();
            animator ??= GetComponentInChildren<Animator>();
        }

        private void OnDisable()
        {
            EndDodge();
        }

        private void Update()
        {
            if (!isDodging)
            {
                if (inputReader != null && inputReader.DodgePressed)
                {
                    BeginDodge();
                }

                return;
            }

            UpdateDodge();
        }

        private void OnValidate()
        {
            dodgeDuration = Mathf.Max(0.05f, dodgeDuration);
            rootMotionDistanceMultiplier = Mathf.Max(0f, rootMotionDistanceMultiplier);
            recoveryDuration = Mathf.Max(0f, recoveryDuration);
            invulnerabilityStartTime = Mathf.Max(0f, invulnerabilityStartTime);
            invulnerabilityEndTime = Mathf.Max(invulnerabilityStartTime, invulnerabilityEndTime);
        }

        private void BeginDodge()
        {
            if (animator == null ||
                (comboController != null && !comboController.TryPrepareForDodge()))
            {
                return;
            }

            dodgeDirection = playerMotor == null
                ? Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized
                : playerMotor.GetEightWayWorldDirection(inputReader == null ? Vector2.zero : inputReader.Move);
            if (dodgeDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                dodgeDirection = Vector3.forward;
            }

            isDodging = true;
            isRecovering = false;
            isInvulnerable = false;
            normalizedDodgeTime = 0f;
            dodgeStartPosition = transform.position;
            dodgeStartTime = Time.time;
            playerMotor?.SetMovementEnabled(false);
            playerMotor?.FaceDirectionImmediately(dodgeDirection);
            animator.ResetTrigger(DodgeTrigger);
            animator.SetTrigger(DodgeTrigger);
            if (enableDodgeDebugLogging)
            {
                Debug.Log($"[BB-006 Dodge Start] position:{FormatVector(dodgeStartPosition)} " +
                    $"direction:{FormatVector(dodgeDirection)} duration:{dodgeDuration:F2}s " +
                    $"iFrame:{invulnerabilityStartTime:F2}-{invulnerabilityEndTime:F2}s");
            }

            DodgeStarted?.Invoke();
        }

        private void UpdateDodge()
        {
            float elapsedTime = Time.time - dodgeStartTime;
            normalizedDodgeTime = Mathf.Clamp01(elapsedTime / dodgeDuration);
            if (normalizedDodgeTime >= 1f)
            {
                isRecovering = true;
            }

            bool shouldBeInvulnerable = elapsedTime >= invulnerabilityStartTime &&
                elapsedTime < invulnerabilityEndTime;
            if (isInvulnerable != shouldBeInvulnerable)
            {
                isInvulnerable = shouldBeInvulnerable;
                hurtbox?.SetInvulnerable(isInvulnerable);
            }

            if (elapsedTime >= dodgeDuration + recoveryDuration)
            {
                EndDodge();
            }
        }

        private void EndDodge()
        {
            if (!isDodging && !isInvulnerable)
            {
                return;
            }

            bool wasDodging = isDodging;
            float elapsedTime = Time.time - dodgeStartTime;
            Vector3 dodgeEndPosition = transform.position;

            isDodging = false;
            isRecovering = false;
            normalizedDodgeTime = 0f;
            if (isInvulnerable)
            {
                isInvulnerable = false;
                hurtbox?.SetInvulnerable(false);
            }

            playerMotor?.SetMovementEnabled(true);
            playerMotor?.FaceLockOnTarget();
            if (wasDodging && enableDodgeDebugLogging)
            {
                Debug.Log($"[BB-006 Dodge End] start:{FormatVector(dodgeStartPosition)} " +
                    $"end:{FormatVector(dodgeEndPosition)} displacement:{FormatVector(dodgeEndPosition - dodgeStartPosition)} " +
                    $"elapsed:{elapsedTime:F2}s");
            }

            DodgeEnded?.Invoke();
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = isDodging ? Color.cyan : new Color(0f, 0.8f, 1f, 0.4f);
            Vector3 direction = Application.isPlaying && dodgeDirection.sqrMagnitude > 0f
                ? dodgeDirection
                : transform.forward;
            Gizmos.DrawLine(transform.position, transform.position + direction * 2f);
        }
    }
}
