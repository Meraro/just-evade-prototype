using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class BossMotor : MonoBehaviour
    {
        public enum LocomotionMode
        {
            Idle,
            Approach,
            Strafe
        }

        [SerializeField, Min(0f)] private float turnSpeed = 360f;
        [SerializeField] private Animator animator;

        [Header("Gravity")]
        [SerializeField, Min(0f)] private float gravityMultiplier = 1f;
        [SerializeField, Min(0f)] private float groundedDownwardSpeed = 2f;
        [SerializeField, Min(0f)] private float maximumFallSpeed = 25f;

        [Header("Runtime Grounding")]
        [SerializeField] private bool isGrounded;
        [SerializeField] private float verticalVelocity;

        [Header("Runtime Locomotion Debug")]
        [SerializeField] private LocomotionMode currentLocomotionMode;
        [SerializeField] private float currentLocomotionPlaybackSpeed;

        private CharacterController characterController;

        public bool IsApplyingLocomotionRootMotion { get; private set; }
        public LocomotionMode CurrentLocomotionMode => currentLocomotionMode;
        public float CurrentLocomotionPlaybackSpeed => currentLocomotionPlaybackSpeed;

        public void Configure(Animator targetAnimator)
        {
            animator = targetAnimator;
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            ApplyGravity();
        }

        public float MoveTowards(Transform target, float stopDistance)
        {
            if (target == null)
            {
                Stop();
                return 0f;
            }

            Vector3 offset = target.position - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance <= stopDistance || distance <= Mathf.Epsilon)
            {
                Stop();
                return distance;
            }

            Vector3 direction = offset / distance;
            FaceDirection(direction, turnSpeed);
            SetApproachAnimation();
            return distance;
        }

        public float FaceTowards(Transform target, float maximumTurnSpeed)
        {
            if (target == null)
            {
                return 0f;
            }

            Vector3 direction = target.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return 0f;
            }

            float angle = Vector3.Angle(transform.forward, direction);
            FaceDirection(direction.normalized, maximumTurnSpeed);
            return angle;
        }

        public void StrafeAround(Transform target, bool clockwise, float speedMultiplier)
        {
            if (target == null)
            {
                Stop();
                return;
            }

            FaceTowards(target, turnSpeed);
            SetLocomotionAnimation(speedMultiplier, clockwise ? -1f : 1f, 0f);
        }

        public void Stop()
        {
            IsApplyingLocomotionRootMotion = false;
            currentLocomotionMode = LocomotionMode.Idle;
            currentLocomotionPlaybackSpeed = 0f;
            animator?.SetBool("BossApproaching", false);
            animator?.SetFloat("Speed", 0f);
            animator?.SetFloat("LocomotionPlaybackSpeed", 1f);
            animator?.SetFloat("MoveX", 0f);
            animator?.SetFloat("MoveY", 0f);
        }

        private void FaceDirection(Vector3 direction, float maximumTurnSpeed)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                Mathf.Max(0f, maximumTurnSpeed) * Time.deltaTime);
        }

        private void SetLocomotionAnimation(float speed, float moveX, float moveY)
        {
            IsApplyingLocomotionRootMotion = speed > Mathf.Epsilon;
            currentLocomotionMode = IsApplyingLocomotionRootMotion ? LocomotionMode.Strafe : LocomotionMode.Idle;
            currentLocomotionPlaybackSpeed = IsApplyingLocomotionRootMotion ? speed : 0f;
            animator?.SetBool("BossApproaching", false);
            animator?.SetFloat("Speed", IsApplyingLocomotionRootMotion ? 1f : 0f);
            animator?.SetFloat("LocomotionPlaybackSpeed", Mathf.Max(0.01f, speed));
            animator?.SetFloat("MoveX", moveX);
            animator?.SetFloat("MoveY", moveY);
        }

        private void SetApproachAnimation()
        {
            IsApplyingLocomotionRootMotion = true;
            currentLocomotionMode = LocomotionMode.Approach;
            currentLocomotionPlaybackSpeed = 1f;
            if (animator == null)
            {
                return;
            }

            // Approach is never a slow-motion exploration action. Keep the global Animator rate at normal speed
            // as well as the locomotion state's speed multiplier, so it cannot inherit a prior strafe rate.
            animator.speed = 1f;
            animator.SetBool("BossApproaching", true);
            animator.SetFloat("Speed", 1f);
            animator.SetFloat("LocomotionPlaybackSpeed", 1f);
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveY", 1f);
        }

        private void ApplyGravity()
        {
            if (characterController == null)
            {
                return;
            }

            isGrounded = characterController.isGrounded;
            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -groundedDownwardSpeed;
            }
            else
            {
                verticalVelocity = Mathf.Max(
                    verticalVelocity + Physics.gravity.y * gravityMultiplier * Time.deltaTime,
                    -maximumFallSpeed);
            }

            characterController.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
            isGrounded = characterController.isGrounded;
        }
    }
}
