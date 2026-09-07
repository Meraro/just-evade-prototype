using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerStrafeHeadLookIK : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform characterRoot;
        [SerializeField, Range(0f, 1f)] private float headWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float strafeInputThreshold = 0.25f;
        [SerializeField, Range(-90f, 90f)] private float forwardYawOffset = 55f;
        [SerializeField, Range(-90f, 90f)] private float backwardYawOffset = 55f;
        [SerializeField, Range(-90f, 90f)] private float leftStrafeYawOffset = 55f;
        [SerializeField, Range(-90f, 90f)] private float rightStrafeYawOffset = 55f;
        [SerializeField, Range(-90f, 90f)] private float forwardLeftYawOffset = 55f;
        [SerializeField, Range(-90f, 90f)] private float forwardRightYawOffset = 55f;
        [SerializeField, Range(-90f, 90f)] private float backwardLeftYawOffset = 55f;
        [SerializeField, Range(-90f, 90f)] private float backwardRightYawOffset = 55f;

        private Transform headBone;

        public void Configure(Animator targetAnimator, Transform targetCharacterRoot)
        {
            animator = targetAnimator;
            characterRoot = targetCharacterRoot;
            headBone = animator == null
                ? null
                : animator.GetBoneTransform(HumanBodyBones.Head);
        }

        public void ApplySettings(
            bool isEnabled,
            float targetHeadWeight,
            float targetStrafeInputThreshold,
            float targetForwardYawOffset,
            float targetBackwardYawOffset,
            float targetLeftStrafeYawOffset,
            float targetRightStrafeYawOffset,
            float targetForwardLeftYawOffset,
            float targetForwardRightYawOffset,
            float targetBackwardLeftYawOffset,
            float targetBackwardRightYawOffset)
        {
            enabled = isEnabled;
            headWeight = Mathf.Clamp01(targetHeadWeight);
            strafeInputThreshold = Mathf.Clamp01(targetStrafeInputThreshold);
            forwardYawOffset = Mathf.Clamp(targetForwardYawOffset, -90f, 90f);
            backwardYawOffset = Mathf.Clamp(targetBackwardYawOffset, -90f, 90f);
            leftStrafeYawOffset = Mathf.Clamp(targetLeftStrafeYawOffset, -90f, 90f);
            rightStrafeYawOffset = Mathf.Clamp(targetRightStrafeYawOffset, -90f, 90f);
            forwardLeftYawOffset = Mathf.Clamp(targetForwardLeftYawOffset, -90f, 90f);
            forwardRightYawOffset = Mathf.Clamp(targetForwardRightYawOffset, -90f, 90f);
            backwardLeftYawOffset = Mathf.Clamp(targetBackwardLeftYawOffset, -90f, 90f);
            backwardRightYawOffset = Mathf.Clamp(targetBackwardRightYawOffset, -90f, 90f);
        }

        private void Reset()
        {
            animator = GetComponent<Animator>();
        }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            headBone = animator == null
                ? null
                : animator.GetBoneTransform(HumanBodyBones.Head);
        }

        private void LateUpdate()
        {
            if (animator == null || characterRoot == null || headBone == null)
            {
                return;
            }

            float moveX = animator.GetFloat("MoveX");
            float moveY = animator.GetFloat("MoveY");
            float speed = animator.GetFloat("Speed");
            if (speed <= 0.01f || headWeight <= 0f ||
                !TryGetDirectionalYawOffset(moveX, moveY, out float yawOffset))
            {
                return;
            }

            Quaternion correction = Quaternion.AngleAxis(
                yawOffset * headWeight,
                characterRoot.up);
            headBone.rotation = correction * headBone.rotation;
        }

        private bool TryGetDirectionalYawOffset(float moveX, float moveY, out float yawOffset)
        {
            bool isLeft = moveX <= -strafeInputThreshold;
            bool isRight = moveX >= strafeInputThreshold;
            bool isForward = moveY >= strafeInputThreshold;
            bool isBackward = moveY <= -strafeInputThreshold;

            if (isForward && isLeft)
            {
                yawOffset = forwardLeftYawOffset;
                return true;
            }

            if (isForward && isRight)
            {
                yawOffset = forwardRightYawOffset;
                return true;
            }

            if (isBackward && isLeft)
            {
                yawOffset = backwardLeftYawOffset;
                return true;
            }

            if (isBackward && isRight)
            {
                yawOffset = backwardRightYawOffset;
                return true;
            }

            if (isForward)
            {
                yawOffset = forwardYawOffset;
                return true;
            }

            if (isBackward)
            {
                yawOffset = backwardYawOffset;
                return true;
            }

            if (isLeft)
            {
                yawOffset = leftStrafeYawOffset;
                return true;
            }

            if (isRight)
            {
                yawOffset = rightStrafeYawOffset;
                return true;
            }

            yawOffset = 0f;
            return false;
        }
    }
}
