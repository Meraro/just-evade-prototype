using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    public sealed class PlayerMotionController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 6.75f;

        [Header("Strafe Head Look")]
        [SerializeField] private bool strafeHeadLookEnabled = true;
        [SerializeField, Range(0f, 1f)] private float headWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float strafeInputThreshold = 0.25f;

        [Header("Directional Head Yaw Offsets")]
        [SerializeField, Range(-90f, 90f)] private float forwardYawOffset = 55f;
        [SerializeField, Range(-90f, 90f)] private float backwardYawOffset = 55f;
        [SerializeField, Range(-90f, 90f)] private float leftStrafeYawOffset = 55f;
        [SerializeField, Range(-90f, 90f)] private float rightStrafeYawOffset = 55f;
        [SerializeField, Range(-90f, 90f)] private float forwardLeftYawOffset = 55f;
        [SerializeField, Range(-90f, 90f)] private float forwardRightYawOffset = 55f;
        [SerializeField, Range(-90f, 90f)] private float backwardLeftYawOffset = 55f;
        [SerializeField, Range(-90f, 90f)] private float backwardRightYawOffset = 55f;

        [Header("References")]
        [SerializeField] private PlayerMotor playerMotor;
        [SerializeField] private PlayerStrafeHeadLookIK strafeHeadLook;

        public void Configure(PlayerMotor targetPlayerMotor, PlayerStrafeHeadLookIK targetStrafeHeadLook)
        {
            playerMotor = targetPlayerMotor;
            strafeHeadLook = targetStrafeHeadLook;
            ApplySettings();
        }

        public void SetDirectionalYawOffsets(
            float forward,
            float backward,
            float leftStrafe,
            float rightStrafe,
            float forwardLeft,
            float forwardRight,
            float backwardLeft,
            float backwardRight)
        {
            forwardYawOffset = forward;
            backwardYawOffset = backward;
            leftStrafeYawOffset = leftStrafe;
            rightStrafeYawOffset = rightStrafe;
            forwardLeftYawOffset = forwardLeft;
            forwardRightYawOffset = forwardRight;
            backwardLeftYawOffset = backwardLeft;
            backwardRightYawOffset = backwardRight;
            ApplySettings();
        }

        private void Awake()
        {
            ApplySettings();
        }

        private void OnValidate()
        {
            ApplySettings();
        }

        private void ApplySettings()
        {
            if (playerMotor != null)
            {
                playerMotor.SetMoveSpeed(moveSpeed);
            }

            if (strafeHeadLook != null)
            {
                strafeHeadLook.ApplySettings(
                    strafeHeadLookEnabled,
                    headWeight,
                    strafeInputThreshold,
                    forwardYawOffset,
                    backwardYawOffset,
                    leftStrafeYawOffset,
                    rightStrafeYawOffset,
                    forwardLeftYawOffset,
                    forwardRightYawOffset,
                    backwardLeftYawOffset,
                    backwardRightYawOffset);
            }
        }
    }
}
