using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(BossBattleInputReader))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 4.5f;
        [SerializeField, Min(0f)] private float rotationSpeed = 720f;
        [SerializeField] private Animator animator;

        [Header("Gravity")]
        [SerializeField, Min(0f)] private float gravityMultiplier = 1f;
        [SerializeField, Min(0f)] private float groundedDownwardSpeed = 2f;
        [SerializeField, Min(0f)] private float maximumFallSpeed = 25f;

        [Header("Runtime Grounding")]
        [SerializeField] private bool isGrounded;
        [SerializeField] private float verticalVelocity;

        private CharacterController characterController;
        private BossBattleInputReader inputReader;
        private Transform cameraTransform;
        private Transform lockOnTarget;
        private bool movementEnabled = true;

        public void Configure(Animator targetAnimator)
        {
            animator = targetAnimator;
        }

        public void SetMoveSpeed(float targetMoveSpeed)
        {
            moveSpeed = Mathf.Max(0f, targetMoveSpeed);
        }

        public void SetLockOnTarget(Transform target)
        {
            lockOnTarget = target;
        }

        public void SetMovementEnabled(bool isEnabled)
        {
            movementEnabled = isEnabled;
        }

        public Vector3 GetEightWayWorldDirection(Vector2 input)
        {
            if (input.sqrMagnitude <= 0.0001f)
            {
                return Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            }

            Vector3 forward = GetPlanarCameraForward();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float inputAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
            float snappedAngle = Mathf.Round(inputAngle / 45f) * 45f;
            float radians = snappedAngle * Mathf.Deg2Rad;
            return (forward * Mathf.Cos(radians) + right * Mathf.Sin(radians)).normalized;
        }

        public void FaceDirectionImmediately(Vector3 direction)
        {
            direction = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (direction.sqrMagnitude > Mathf.Epsilon)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }

        public void FaceLockOnTarget()
        {
            if (lockOnTarget != null)
            {
                transform.rotation = GetLockOnRotation();
            }
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            inputReader = GetComponent<BossBattleInputReader>();
            cameraTransform = Camera.main == null ? null : Camera.main.transform;
        }

        private void Update()
        {
            ApplyGravity();

            Vector2 input = movementEnabled ? inputReader.Move : Vector2.zero;
            Vector3 direction = GetWorldMoveDirection(input);

            float speedRatio = Mathf.Clamp01(direction.magnitude);
            if (speedRatio > 0f)
            {
                direction.Normalize();
                characterController.Move(direction * (moveSpeed * Time.deltaTime));
                Quaternion targetRotation = lockOnTarget == null
                    ? Quaternion.LookRotation(direction, Vector3.up)
                    : GetLockOnRotation();
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime);
            }

            animator?.SetFloat("Speed", speedRatio);
            animator?.SetFloat("MoveX", input.x);
            animator?.SetFloat("MoveY", input.y);
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

        private Quaternion GetLockOnRotation()
        {
            Vector3 direction = lockOnTarget.position - transform.position;
            direction.y = 0f;
            return direction.sqrMagnitude <= Mathf.Epsilon
                ? transform.rotation
                : Quaternion.LookRotation(direction, Vector3.up);
        }

        private Vector3 GetWorldMoveDirection(Vector2 input)
        {
            if (input.sqrMagnitude <= 0.0001f)
            {
                return Vector3.zero;
            }

            Vector3 forward = GetPlanarCameraForward();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            return right * input.x + forward * input.y;
        }

        private Vector3 GetPlanarCameraForward()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            Vector3 fallback = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (cameraTransform == null)
            {
                return fallback.sqrMagnitude > Mathf.Epsilon ? fallback : Vector3.forward;
            }

            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            return forward.sqrMagnitude > Mathf.Epsilon ? forward : fallback;
        }
    }
}
