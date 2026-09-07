using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerComboAttackController comboController;
        [SerializeField] private PlayerDodgeController dodgeController;

        [Header("Recovery")]
        [SerializeField, Min(0f)] private float endPoseHoldDuration = 0.05f;

        [Header("Weapon Pose Layer")]
        [SerializeField] private bool useWeaponHandsGrip = true;
        [SerializeField] private string weaponHandsGripLayerName = "WeaponHandsGrip";
        [SerializeField, Min(0f)] private float weaponHandsBlendSpeed = 12f;

        private int weaponHandsGripLayerIndex = -1;
        private CombatantHealth health;

        public Animator Animator => animator;
        public void Configure(Animator targetAnimator, PlayerComboAttackController targetComboController)
        {
            animator = targetAnimator;
            comboController = targetComboController;
            FindWeaponHandsGripLayer();
            ApplySettings();
        }

        public void ConfigureDodge(PlayerDodgeController targetDodgeController)
        {
            dodgeController = targetDodgeController;
        }

        private void Awake()
        {
            health = GetComponent<CombatantHealth>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (comboController == null)
            {
                comboController = GetComponent<PlayerComboAttackController>();
            }

            if (dodgeController == null)
            {
                dodgeController = GetComponent<PlayerDodgeController>();
            }

            FindWeaponHandsGripLayer();
            ApplySettings();
        }

        private void Update()
        {
            if (animator == null || weaponHandsGripLayerIndex < 0)
            {
                return;
            }

            bool isUsingAttackMotion = comboController != null &&
                (comboController.IsAttacking || comboController.IsRecovering);
            bool isUsingDodgeMotion = dodgeController != null && dodgeController.IsDodging;
            bool isDead = health != null && health.IsDead;
            float targetWeight = useWeaponHandsGrip && !isUsingAttackMotion && !isUsingDodgeMotion && !isDead ? 1f : 0f;
            float currentWeight = animator.GetLayerWeight(weaponHandsGripLayerIndex);
            animator.SetLayerWeight(
                weaponHandsGripLayerIndex,
                Mathf.MoveTowards(currentWeight, targetWeight, weaponHandsBlendSpeed * Time.deltaTime));
        }

        private void OnValidate()
        {
            endPoseHoldDuration = Mathf.Max(0f, endPoseHoldDuration);

            if (Application.isPlaying)
            {
                ApplySettings();
            }
        }

        [ContextMenu("Apply Animation Settings")]
        public void ApplySettings()
        {
            comboController?.SetEndPoseHoldDuration(endPoseHoldDuration);
        }

        private void FindWeaponHandsGripLayer()
        {
            weaponHandsGripLayerIndex = animator == null
                ? -1
                : animator.GetLayerIndex(weaponHandsGripLayerName);
        }
    }
}
