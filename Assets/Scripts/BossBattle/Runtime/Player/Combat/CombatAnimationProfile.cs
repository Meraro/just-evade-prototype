using System;
using UnityEngine;

namespace RPGGame.BossBattle
{
    [CreateAssetMenu(menuName = "Boss Battle/Combat Animation Profile", fileName = "CombatAnimationProfile")]
    public sealed class CombatAnimationProfile : ScriptableObject
    {
        [SerializeField] private CombatAttackDefinition[] attacks = Array.Empty<CombatAttackDefinition>();

        public int AttackCount => attacks == null ? 0 : attacks.Length;

        public CombatAttackDefinition GetAttack(int oneBasedStep)
        {
            int index = oneBasedStep - 1;
            return index < 0 || index >= attacks.Length ? null : attacks[index];
        }

        public void EnsureAttackDefinitions(
            AnimationClip[] clips,
            float[] defaultHitTimes,
            bool initializeValues)
        {
            if (attacks == null || attacks.Length != clips.Length)
            {
                attacks = new CombatAttackDefinition[clips.Length];
                initializeValues = true;
            }

            for (int index = 0; index < clips.Length; index++)
            {
                if (attacks[index] == null)
                {
                    attacks[index] = new CombatAttackDefinition();
                    initializeValues = true;
                }

                attacks[index].EnsureDefaults(
                    clips[index],
                    index < defaultHitTimes.Length ? defaultHitTimes[index] : 0.45f,
                    initializeValues);
            }
        }

        public void SetPlaybackSpeedDefaults(float[] playbackSpeeds)
        {
            if (attacks == null || playbackSpeeds == null)
            {
                return;
            }

            int count = Mathf.Min(attacks.Length, playbackSpeeds.Length);
            for (int index = 0; index < count; index++)
            {
                attacks[index]?.SetPlaybackSpeed(playbackSpeeds[index]);
            }
        }
    }

    [Serializable]
    public sealed class CombatAttackDefinition
    {
        [SerializeField] private AnimationClip animationClip;
        [SerializeField, Range(0f, 1f)] private float hitNormalizedTime = 0.45f;
        [SerializeField, Min(0f)] private float damage = 50f;
        [SerializeField, Min(0f)] private float range = 2.2f;
        [SerializeField, Range(0f, 180f)] private float arc = 110f;
        [SerializeField, Range(0.1f, 2f)] private float playbackSpeed = 1f;

        public AnimationClip AnimationClip => animationClip;
        public float HitNormalizedTime => hitNormalizedTime;
        public float Damage => damage;
        public float Range => range;
        public float Arc => arc;
        public float PlaybackSpeed => playbackSpeed;

        public void EnsureDefaults(AnimationClip clip, float defaultHitTime, bool initializeValues)
        {
            if (animationClip == null)
            {
                animationClip = clip;
            }

            if (!initializeValues)
            {
                return;
            }

            hitNormalizedTime = Mathf.Clamp01(defaultHitTime);
            damage = 50f;
            range = 2.2f;
            arc = 110f;
            playbackSpeed = 1f;
        }

        public void SetPlaybackSpeed(float targetPlaybackSpeed)
        {
            playbackSpeed = Mathf.Clamp(targetPlaybackSpeed, 0.1f, 2f);
        }
    }
}
