using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    public sealed class JustDodgePresentationController : MonoBehaviour
    {
        [SerializeField] private PlayerJustDodgeController justDodgeController;
        [SerializeField] private CombatTimeController combatTimeController;

        [Header("Simple Just Dodge Feedback")]
        [SerializeField, Range(0.05f, 1f)] private float slowMotionScale = 0.3f;
        [SerializeField, Min(0f)] private float slowMotionDuration = 1f;

        public void Configure(PlayerJustDodgeController targetJustDodgeController, CombatTimeController targetCombatTimeController)
        {
            Unsubscribe();
            justDodgeController = targetJustDodgeController;
            combatTimeController = targetCombatTimeController;
            Subscribe();
        }

        public void ConfigureFeedback(float targetSlowMotionScale, float targetSlowMotionDuration)
        {
            slowMotionScale = Mathf.Clamp(targetSlowMotionScale, 0.05f, 1f);
            slowMotionDuration = Mathf.Max(0f, targetSlowMotionDuration);
        }

        private void Awake()
        {
            justDodgeController ??= GetComponent<PlayerJustDodgeController>();
            combatTimeController ??= FindFirstObjectByType<CombatTimeController>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (justDodgeController == null)
            {
                return;
            }

            justDodgeController.JustDodgeSucceeded -= PlayFeedback;
            justDodgeController.JustDodgeSucceeded += PlayFeedback;
        }

        private void Unsubscribe()
        {
            if (justDodgeController != null)
            {
                justDodgeController.JustDodgeSucceeded -= PlayFeedback;
            }
        }

        private void PlayFeedback()
        {
            combatTimeController?.PlaySlowMotion(slowMotionScale, slowMotionDuration);
        }
    }
}
