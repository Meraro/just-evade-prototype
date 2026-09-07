using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    public sealed class ThirdPersonCameraController : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform lockOnTarget;
        [SerializeField] private BossBattleInputReader inputReader;
        [SerializeField] private PlayerMotor playerMotor;
        [SerializeField, Min(0.1f)] private float distance = 6.5f;
        [SerializeField, Min(0.1f)] private float lockOnDistance = 5f;
        [SerializeField] private float lockOnHeightOffset = 1.2f;
        [SerializeField] private float pivotHeight = 1.5f;
        [SerializeField] private float lookSensitivity = 0.15f;
        [SerializeField, Range(-80f, 0f)] private float minPitch = -35f;
        [SerializeField, Range(0f, 80f)] private float maxPitch = 60f;
        [SerializeField, Min(0f)] private float followSharpness = 14f;

        private float yaw;
        private float pitch = 15f;

        public bool IsLockedOn { get; private set; }

        public void Configure(
            Transform playerTransform,
            Transform bossTransform,
            BossBattleInputReader reader,
            PlayerMotor motor)
        {
            player = playerTransform;
            lockOnTarget = bossTransform;
            inputReader = reader;
            playerMotor = motor;
            Vector3 euler = transform.rotation.eulerAngles;
            yaw = euler.y;
            pitch = NormalizePitch(euler.x);
        }

        private void LateUpdate()
        {
            if (player == null || inputReader == null)
            {
                return;
            }

            if (inputReader.LockOnPressed && lockOnTarget != null)
            {
                IsLockedOn = !IsLockedOn;
                playerMotor?.SetLockOnTarget(IsLockedOn ? lockOnTarget : null);
            }

            if (IsLockedOn && lockOnTarget != null)
            {
                UpdateLockOnCamera();
                return;
            }

            UpdateFreeCamera();
        }

        private void UpdateFreeCamera()
        {
            Vector2 look = inputReader.Look;
            yaw += look.x * lookSensitivity;
            pitch = Mathf.Clamp(pitch - look.y * lookSensitivity, minPitch, maxPitch);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = player.position + Vector3.up * pivotHeight;
            MoveCamera(pivot - rotation * Vector3.forward * distance, rotation);
        }

        private void UpdateLockOnCamera()
        {
            Vector3 playerPivot = player.position + Vector3.up * pivotHeight;
            Vector3 targetPivot = lockOnTarget.position + Vector3.up * pivotHeight;
            Vector3 focus = Vector3.Lerp(playerPivot, targetPivot, 0.4f);
            Vector3 horizontalDirection = targetPivot - playerPivot;
            horizontalDirection.y = 0f;
            if (horizontalDirection.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            Quaternion baseRotation = Quaternion.LookRotation(horizontalDirection.normalized, Vector3.up);
            Vector3 desiredPosition = playerPivot + Vector3.up * lockOnHeightOffset
                - baseRotation * Vector3.forward * lockOnDistance;
            Quaternion desiredRotation = Quaternion.LookRotation(focus - desiredPosition, Vector3.up);
            MoveCamera(desiredPosition, desiredRotation);
        }

        private void MoveCamera(Vector3 position, Quaternion rotation)
        {
            float blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, position, blend),
                Quaternion.Slerp(transform.rotation, rotation, blend));
        }

        private static float NormalizePitch(float value)
        {
            return value > 180f ? value - 360f : value;
        }
    }
}
