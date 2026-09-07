using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class BossAttackRootMotionDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private BossMotor motor;
        [SerializeField] private BossAttackController attackController;

        [Header("Runtime Diagnostics")]
        [SerializeField] private Vector3 lastAppliedPositionDelta;
        [SerializeField] private float lastAppliedYawDelta;

        public void Configure(
            Animator targetAnimator,
            CharacterController targetCharacterController,
            BossMotor targetMotor,
            BossAttackController targetAttackController)
        {
            animator = targetAnimator;
            characterController = targetCharacterController;
            motor = targetMotor;
            attackController = targetAttackController;
            if (animator != null)
            {
                animator.applyRootMotion = true;
            }
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

            if (characterController == null)
            {
                characterController = GetComponentInParent<CharacterController>();
            }

            if (motor == null)
            {
                motor = GetComponentInParent<BossMotor>();
            }

            if (attackController == null)
            {
                attackController = GetComponentInParent<BossAttackController>();
            }
        }

        private void OnAnimatorMove()
        {
            if (animator == null || characterController == null || attackController == null)
            {
                return;
            }

            bool appliesAttackRootMotion = attackController.IsAttacking;
            bool appliesLocomotionRootMotion = motor != null && motor.IsApplyingLocomotionRootMotion;
            if (!appliesAttackRootMotion && !appliesLocomotionRootMotion)
            {
                return;
            }

            Vector3 deltaPosition = animator.deltaPosition;
            deltaPosition.y = 0f;
            characterController.Move(deltaPosition);

            Quaternion deltaRotation = animator.deltaRotation;
            if (appliesAttackRootMotion)
            {
                characterController.transform.rotation *= deltaRotation;
            }

            lastAppliedPositionDelta = deltaPosition;
            lastAppliedYawDelta = Mathf.DeltaAngle(0f, deltaRotation.eulerAngles.y);
        }
    }
}
