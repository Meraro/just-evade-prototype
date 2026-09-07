using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    public sealed class CombatTimeController : MonoBehaviour
    {
        [SerializeField, Range(0.01f, 1f)] private float activeTimeScale = 1f;
        [SerializeField] private float effectEndTime;

        public void PlaySlowMotion(float timeScale, float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            float clampedScale = Mathf.Clamp(timeScale, 0.01f, 1f);
            activeTimeScale = Mathf.Min(activeTimeScale, clampedScale);
            effectEndTime = Mathf.Max(effectEndTime, Time.unscaledTime + duration);
            Time.timeScale = activeTimeScale;
        }

        private void Update()
        {
            if (effectEndTime <= 0f || Time.unscaledTime < effectEndTime)
            {
                return;
            }

            effectEndTime = 0f;
            activeTimeScale = 1f;
            Time.timeScale = 1f;
        }

        private void OnDisable()
        {
            effectEndTime = 0f;
            activeTimeScale = 1f;
            Time.timeScale = 1f;
        }
    }
}
