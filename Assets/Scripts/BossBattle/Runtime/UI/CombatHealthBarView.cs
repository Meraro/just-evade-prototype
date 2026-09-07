using UnityEngine;
using UnityEngine.UI;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    public sealed class CombatHealthBarView : MonoBehaviour
    {
        [Header("Layer References")]
        [SerializeField] private Image immediateFill;
        [SerializeField] private Image delayedDamageFill;

        [Header("Damage Presentation")]
        [SerializeField, Min(0f)] private float damageDelay = 0.45f;
        [SerializeField, Min(0.01f)] private float delayedDrainSpeed = 1.6f;
        [SerializeField] private bool useUnscaledTime = true;

        private float immediateRatio = 1f;
        private float delayedRatio = 1f;
        private float remainingDelay;
        private RectTransform immediateFillRect;
        private RectTransform delayedDamageFillRect;

        public void Configure(Image targetImmediateFill, Image targetDelayedDamageFill)
        {
            immediateFill = targetImmediateFill;
            delayedDamageFill = targetDelayedDamageFill;
            immediateFillRect = immediateFill == null ? null : immediateFill.rectTransform;
            delayedDamageFillRect = delayedDamageFill == null ? null : delayedDamageFill.rectTransform;
            ApplyFillAmounts();
        }

        public void SetHealth(float currentHealth, float maximumHealth)
        {
            float targetRatio = maximumHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maximumHealth);
            bool tookDamage = targetRatio < immediateRatio;
            immediateRatio = targetRatio;

            if (tookDamage)
            {
                remainingDelay = damageDelay;
            }
            else
            {
                delayedRatio = targetRatio;
                remainingDelay = 0f;
            }

            ApplyFillAmounts();
        }

        private void Update()
        {
            if (delayedRatio <= immediateRatio)
            {
                return;
            }

            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (remainingDelay > 0f)
            {
                remainingDelay = Mathf.Max(0f, remainingDelay - deltaTime);
                return;
            }

            delayedRatio = Mathf.MoveTowards(delayedRatio, immediateRatio, delayedDrainSpeed * deltaTime);
            ApplyFillAmounts();
        }

        private void ApplyFillAmounts()
        {
            ApplyWidth(immediateFill, immediateFillRect, immediateRatio);
            ApplyWidth(delayedDamageFill, delayedDamageFillRect, delayedRatio);
        }

        private static void ApplyWidth(Image fill, RectTransform fillRect, float ratio)
        {
            if (fill == null)
            {
                return;
            }

            ratio = Mathf.Clamp01(ratio);
            fill.type = Image.Type.Simple;
            fill.fillAmount = ratio;
            (fillRect ?? fill.rectTransform).anchorMax = new Vector2(ratio, 1f);
        }
    }
}
