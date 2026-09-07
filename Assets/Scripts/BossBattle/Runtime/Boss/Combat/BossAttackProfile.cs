using UnityEngine;

namespace RPGGame.BossBattle
{
    [CreateAssetMenu(menuName = "Boss Battle/Boss Attack Profile", fileName = "BossAttackProfile")]
    public sealed class BossAttackProfile : ScriptableObject
    {
        [Header("Animation")]
        [SerializeField] private string animatorTrigger = "BossAttackCombo3";
        [SerializeField] private AnimationClip animationClip;
        [SerializeField, Range(0.1f, 2f)] private float playbackSpeed = 1f;
        [SerializeField, Range(0f, 1f)] private float fallbackEndNormalizedTime = 0.95f;

        [Header("Positioning")]
        [SerializeField, Min(0f)] private float attackStartDistance = 2.4f;
        [SerializeField, Min(0f)] private float approachStopDistance = 2f;
        [SerializeField, Range(0f, 180f)] private float attackStartAngle = 25f;
        [SerializeField, Min(0f)] private float preparationTurnSpeed = 180f;
        [SerializeField, Min(0f)] private float telegraphDuration = 0.65f;

        [Header("Hit")]
        [SerializeField, Min(1)] private int hitCount = 3;
        [SerializeField, Min(0f)] private float damage = 20f;
        [SerializeField, Min(0f)] private float hitRange = 2.6f;
        [SerializeField, Range(0f, 360f)] private float hitArc = 120f;

        [Header("Simple Just Dodge")]
        [SerializeField] private bool allowJustDodge = true;
        [SerializeField, Min(0.05f)] private float justDodgeCandidateDuration = 0.6f;

        [Header("Attack Timing Just Dodge")]
        [SerializeField] private JustDodgeWindowDefinition[] justDodgeWindows = System.Array.Empty<JustDodgeWindowDefinition>();

        [Header("Data-Driven Strikes")]
        [SerializeField] private BossAttackStrikeDefinition[] strikes = System.Array.Empty<BossAttackStrikeDefinition>();

        [Header("Recovery")]
        [SerializeField, Min(0f)] private float recoveryDuration = 0.7f;

        [Header("Cooldown")]
        [SerializeField, Min(0f)] private float cooldownDuration = 8f;

        public AnimationClip AnimationClip => animationClip;
        public string AnimatorTrigger => animatorTrigger;
        public float PlaybackSpeed => playbackSpeed;
        public float FallbackEndNormalizedTime => fallbackEndNormalizedTime;
        public float AttackStartDistance => attackStartDistance;
        public float ApproachStopDistance => approachStopDistance;
        public float AttackStartAngle => attackStartAngle;
        public float PreparationTurnSpeed => preparationTurnSpeed;
        public float TelegraphDuration => telegraphDuration;
        public int HitCount => hitCount;
        public float Damage => damage;
        public float HitRange => hitRange;
        public float HitArc => hitArc;
        public bool AllowJustDodge => allowJustDodge;
        public float JustDodgeCandidateDuration => justDodgeCandidateDuration;
        public JustDodgeWindowDefinition[] JustDodgeWindows => justDodgeWindows;
        public BossAttackStrikeDefinition[] Strikes => strikes;
        public int StrikeCount => strikes == null || strikes.Length == 0 ? hitCount : strikes.Length;
        public float RecoveryDuration => recoveryDuration;
        public float CooldownDuration => cooldownDuration;

        public void SetPlaybackSpeed(float targetPlaybackSpeed)
        {
            playbackSpeed = Mathf.Clamp(targetPlaybackSpeed, 0.1f, 2f);
        }

        public bool TryGetJustDodgeWindow(int windowId, out JustDodgeWindowDefinition window)
        {
            foreach (JustDodgeWindowDefinition definition in justDodgeWindows)
            {
                if (definition != null && definition.WindowId == windowId)
                {
                    window = definition;
                    return true;
                }
            }

            window = null;
            return false;
        }

        public BossAttackStrikeDefinition GetStrike(int strikeIndex)
        {
            int arrayIndex = strikeIndex - 1;
            if (strikes != null && arrayIndex >= 0 && arrayIndex < strikes.Length && strikes[arrayIndex] != null)
            {
                return strikes[arrayIndex];
            }

            return new BossAttackStrikeDefinition(
                Mathf.Max(1, strikeIndex),
                0f,
                damage,
                hitRange,
                hitArc,
                0f,
                0f);
        }

        public void EnsureJustDodgeWindows(int targetCount)
        {
            targetCount = Mathf.Max(0, targetCount);
            if (justDodgeWindows != null && justDodgeWindows.Length == targetCount)
            {
                return;
            }

            justDodgeWindows = new JustDodgeWindowDefinition[targetCount];
            for (int index = 0; index < targetCount; index++)
            {
                justDodgeWindows[index] = new JustDodgeWindowDefinition(index, index + 1, $"Strike {index + 1}");
            }
        }

        public void EnsureStrikeDefinitions(int targetCount)
        {
            targetCount = Mathf.Max(0, targetCount);
            if (strikes != null && strikes.Length == targetCount)
            {
                return;
            }

            strikes = new BossAttackStrikeDefinition[targetCount];
            float[] defaultHitTimes = { 0.24f, 0.49f, 0.74f };
            for (int index = 0; index < targetCount; index++)
            {
                float hitTime = index < defaultHitTimes.Length
                    ? defaultHitTimes[index]
                    : (index + 1f) / (targetCount + 1f);
                strikes[index] = new BossAttackStrikeDefinition(
                    index + 1,
                    hitTime,
                    damage,
                    hitRange,
                    hitArc,
                    Mathf.Max(0f, hitTime - 0.06f),
                    Mathf.Max(0f, hitTime - 0.002f));
            }
        }

        public void ConfigureStrike(
            int strikeIndex,
            float targetHitNormalizedTime,
            float targetDamage,
            float targetRange,
            float targetArc,
            float targetWindowOpenNormalizedTime,
            float targetWindowCloseNormalizedTime)
        {
            EnsureStrikeDefinitions(Mathf.Max(hitCount, strikeIndex));
            strikes[strikeIndex - 1].Configure(
                strikeIndex,
                targetHitNormalizedTime,
                targetDamage,
                targetRange,
                targetArc,
                targetWindowOpenNormalizedTime,
                targetWindowCloseNormalizedTime);
        }

        public void ConfigureDefaults(AnimationClip clip, string triggerName)
        {
            animationClip = clip;
            animatorTrigger = triggerName;
            playbackSpeed = 1f;
            fallbackEndNormalizedTime = 0.95f;
            attackStartDistance = 2.4f;
            approachStopDistance = 2f;
            attackStartAngle = 25f;
            preparationTurnSpeed = 180f;
            telegraphDuration = 0.65f;
            hitCount = 3;
            damage = 20f;
            hitRange = 2.6f;
            hitArc = 120f;
            allowJustDodge = true;
            justDodgeCandidateDuration = 0.6f;
            EnsureJustDodgeWindows(hitCount);
            EnsureStrikeDefinitions(hitCount);
            recoveryDuration = 0.7f;
            cooldownDuration = 8f;
        }

        public void EnsureAnimationBinding(AnimationClip clip, string triggerName)
        {
            animationClip = clip;
            animatorTrigger = triggerName;
        }

        public void ConfigureGameplayDefaults(
            int targetHitCount,
            float targetDamage,
            float targetHitRange,
            float targetHitArc,
            float targetTelegraphDuration,
            float targetRecoveryDuration,
            float targetCooldownDuration)
        {
            hitCount = Mathf.Max(1, targetHitCount);
            damage = Mathf.Max(0f, targetDamage);
            hitRange = Mathf.Max(0f, targetHitRange);
            hitArc = Mathf.Clamp(targetHitArc, 0f, 360f);
            telegraphDuration = Mathf.Max(0f, targetTelegraphDuration);
            recoveryDuration = Mathf.Max(0f, targetRecoveryDuration);
            cooldownDuration = Mathf.Max(0f, targetCooldownDuration);
            EnsureJustDodgeWindows(hitCount);
            EnsureStrikeDefinitions(hitCount);
        }

    }

    [System.Serializable]
    public sealed class JustDodgeWindowDefinition
    {
        [SerializeField] private int windowId;
        [SerializeField, Min(1)] private int strikeIndex;
        [SerializeField] private string label;

        public int WindowId => windowId;
        public int StrikeIndex => strikeIndex;
        public string Label => label;

        public JustDodgeWindowDefinition(int targetWindowId, int targetStrikeIndex, string targetLabel)
        {
            windowId = targetWindowId;
            strikeIndex = Mathf.Max(1, targetStrikeIndex);
            label = targetLabel;
        }
    }

    [System.Serializable]
    public sealed class BossAttackStrikeDefinition
    {
        [SerializeField, Min(1)] private int strikeIndex;
        [SerializeField, Range(0f, 1f)] private float hitNormalizedTime;
        [SerializeField, Min(0f)] private float damage;
        [SerializeField, Min(0f)] private float hitRange;
        [SerializeField, Range(0f, 360f)] private float hitArc;
        [SerializeField, Range(0f, 1f)] private float justDodgeWindowOpenNormalizedTime;
        [SerializeField, Range(0f, 1f)] private float justDodgeWindowCloseNormalizedTime;

        public int StrikeIndex => strikeIndex;
        public float HitNormalizedTime => hitNormalizedTime;
        public float Damage => damage;
        public float HitRange => hitRange;
        public float HitArc => hitArc;
        public float JustDodgeWindowOpenNormalizedTime => justDodgeWindowOpenNormalizedTime;
        public float JustDodgeWindowCloseNormalizedTime => justDodgeWindowCloseNormalizedTime;

        public BossAttackStrikeDefinition(
            int targetStrikeIndex,
            float targetHitNormalizedTime,
            float targetDamage,
            float targetHitRange,
            float targetHitArc,
            float targetWindowOpenNormalizedTime,
            float targetWindowCloseNormalizedTime)
        {
            Configure(
                targetStrikeIndex,
                targetHitNormalizedTime,
                targetDamage,
                targetHitRange,
                targetHitArc,
                targetWindowOpenNormalizedTime,
                targetWindowCloseNormalizedTime);
        }

        public void Configure(
            int targetStrikeIndex,
            float targetHitNormalizedTime,
            float targetDamage,
            float targetHitRange,
            float targetHitArc,
            float targetWindowOpenNormalizedTime,
            float targetWindowCloseNormalizedTime)
        {
            strikeIndex = Mathf.Max(1, targetStrikeIndex);
            hitNormalizedTime = Mathf.Clamp01(targetHitNormalizedTime);
            damage = Mathf.Max(0f, targetDamage);
            hitRange = Mathf.Max(0f, targetHitRange);
            hitArc = Mathf.Clamp(targetHitArc, 0f, 360f);
            justDodgeWindowOpenNormalizedTime = Mathf.Clamp(targetWindowOpenNormalizedTime, 0f, hitNormalizedTime);
            justDodgeWindowCloseNormalizedTime = Mathf.Clamp(targetWindowCloseNormalizedTime,
                justDodgeWindowOpenNormalizedTime,
                hitNormalizedTime);
        }
    }
}
