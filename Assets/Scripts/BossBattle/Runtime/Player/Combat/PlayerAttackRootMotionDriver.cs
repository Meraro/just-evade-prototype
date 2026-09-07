using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerAttackRootMotionDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private PlayerComboAttackController comboController;
        [SerializeField] private PlayerDodgeController dodgeController;

        [Header("Runtime Diagnostics")]
        [SerializeField] private Vector3 lastAppliedPositionDelta;
        [SerializeField] private float lastAppliedYawDelta;

        public void Configure(
            Animator targetAnimator,
            CharacterController targetCharacterController,
            PlayerComboAttackController targetComboController)
        {
            animator = targetAnimator;
            characterController = targetCharacterController;
            comboController = targetComboController;
            if (animator != null)
            {
                animator.applyRootMotion = true;
            }
        }

        public void ConfigureDodge(PlayerDodgeController targetDodgeController)
        {
            dodgeController = targetDodgeController;
        }

        private void Reset()
        {
            animator = GetComponent<Animator>();
        }

        private void OnAnimatorMove()
        {
            if (animator == null || characterController == null)
            {
                return;
            }

            if (dodgeController != null && dodgeController.IsApplyingDodgeRootMotion)
            {
                ApplyDodgeRootMotion();
                return;
            }

            if (comboController == null || !comboController.IsApplyingAttackRootMotion)
            {
                return;
            }

            Vector3 deltaPosition = animator.deltaPosition;
            deltaPosition.y = 0f;
            characterController.Move(deltaPosition);

            Quaternion deltaRotation = animator.deltaRotation;
            characterController.transform.rotation *= deltaRotation;

            lastAppliedPositionDelta = deltaPosition;
            lastAppliedYawDelta = Mathf.DeltaAngle(0f, deltaRotation.eulerAngles.y);
        }

        private void ApplyDodgeRootMotion()
        {
            Vector3 deltaPosition = animator.deltaPosition;
            deltaPosition.y = 0f;
            if (dodgeController.ConstrainRootMotionToDodgeDirection)
            {
                Vector3 dodgeDirection = Vector3.ProjectOnPlane(dodgeController.DodgeDirection, Vector3.up).normalized;
                if (dodgeDirection.sqrMagnitude > Mathf.Epsilon)
                {
                    deltaPosition = dodgeDirection * Vector3.Dot(deltaPosition, dodgeDirection);
                }
            }

            Vector3 appliedDelta = deltaPosition * dodgeController.RootMotionDistanceMultiplier;
            characterController.Move(appliedDelta);
            lastAppliedPositionDelta = appliedDelta;
            lastAppliedYawDelta = 0f;
        }
    }
}
