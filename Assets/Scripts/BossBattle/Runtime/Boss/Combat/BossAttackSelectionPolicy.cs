using System;
using UnityEngine;

namespace RPGGame.BossBattle
{
    /// <summary>
    /// Owns attack availability, per-attack cooldowns, and the random selection rule.
    /// It is intentionally independent from Animator and hit detection.
    /// </summary>
    [Serializable]
    public sealed class BossAttackSelectionPolicy
    {
        [SerializeField, Min(0f)] private float postAttackDecisionDelay = 1f;

        [Header("Runtime Cooldown Debug")]
        [SerializeField] private float remainingPostAttackDecisionDelay;
        [SerializeField] private BossAttackCooldownDebugEntry[] attackCooldownDebugEntries;

        private float nextAttackAllowedTime = float.NegativeInfinity;
        private float[] attackCooldownEndTimes = Array.Empty<float>();
        private int lastAttackIndex = -1;

        public float RemainingPostAttackDecisionDelay => Mathf.Max(0f, nextAttackAllowedTime - Time.time);

        public void Configure(BossAttackProfile[] profiles)
        {
            int profileCount = profiles == null ? 0 : profiles.Length;
            attackCooldownEndTimes = new float[profileCount];
            attackCooldownDebugEntries = new BossAttackCooldownDebugEntry[profileCount];
            for (int index = 0; index < profileCount; index++)
            {
                attackCooldownDebugEntries[index] = new BossAttackCooldownDebugEntry();
            }

            nextAttackAllowedTime = float.NegativeInfinity;
            lastAttackIndex = -1;
            RefreshDebug(profiles);
        }

        public bool CanSelectAttack(BossAttackProfile[] profiles)
        {
            return Time.time >= nextAttackAllowedTime && CountAvailableAttacks(profiles) > 0;
        }

        public int SelectRandomAttackIndex(BossAttackProfile[] profiles)
        {
            int availableCount = CountAvailableAttacks(profiles);
            if (availableCount == 0)
            {
                return -1;
            }

            bool excludeLastAttack = availableCount > 1 && IsAttackAvailable(lastAttackIndex, profiles);
            int selectionOrder = UnityEngine.Random.Range(0, availableCount - (excludeLastAttack ? 1 : 0));
            int currentOrder = 0;
            for (int index = 0; index < profiles.Length; index++)
            {
                if (!IsAttackAvailable(index, profiles) || (excludeLastAttack && index == lastAttackIndex))
                {
                    continue;
                }

                if (currentOrder == selectionOrder)
                {
                    lastAttackIndex = index;
                    return index;
                }

                currentOrder++;
            }

            return -1;
        }

        public void BeginAttackCooldown(int attackIndex, BossAttackProfile profile)
        {
            if (attackIndex >= 0 && attackIndex < attackCooldownEndTimes.Length && profile != null)
            {
                attackCooldownEndTimes[attackIndex] = Time.time + profile.CooldownDuration;
            }

            nextAttackAllowedTime = Time.time + postAttackDecisionDelay;
        }

        public void RefreshDebug(BossAttackProfile[] profiles)
        {
            remainingPostAttackDecisionDelay = RemainingPostAttackDecisionDelay;
            if (profiles == null || attackCooldownDebugEntries == null)
            {
                return;
            }

            for (int index = 0; index < profiles.Length && index < attackCooldownDebugEntries.Length; index++)
            {
                BossAttackProfile profile = profiles[index];
                BossAttackCooldownDebugEntry entry = attackCooldownDebugEntries[index];
                entry.Set(
                    profile == null ? "Missing Profile" : profile.name,
                    index < attackCooldownEndTimes.Length
                        ? Mathf.Max(0f, attackCooldownEndTimes[index] - Time.time)
                        : 0f,
                    IsAttackAvailable(index, profiles));
            }
        }

        private int CountAvailableAttacks(BossAttackProfile[] profiles)
        {
            if (profiles == null)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < profiles.Length; index++)
            {
                if (IsAttackAvailable(index, profiles))
                {
                    count++;
                }
            }

            return count;
        }

        private bool IsAttackAvailable(int index, BossAttackProfile[] profiles)
        {
            return index >= 0 && profiles != null && index < profiles.Length &&
                index < attackCooldownEndTimes.Length && profiles[index] != null &&
                profiles[index].AnimationClip != null && Time.time >= attackCooldownEndTimes[index];
        }
    }

    [Serializable]
    public sealed class BossAttackCooldownDebugEntry
    {
        [SerializeField] private string attackName;
        [SerializeField] private float remainingCooldown;
        [SerializeField] private bool isAvailable;

        public void Set(string targetAttackName, float targetRemainingCooldown, bool targetIsAvailable)
        {
            attackName = targetAttackName;
            remainingCooldown = targetRemainingCooldown;
            isAvailable = targetIsAvailable;
        }
    }
}
