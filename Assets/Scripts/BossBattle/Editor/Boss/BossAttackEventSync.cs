using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.BossBattle;
using UnityEditor;
using UnityEngine;

namespace RPGGame.BossBattle.Editor
{
    public static class BossAttackEventSync
    {
        private static readonly string[] ProfilePaths =
        {
            "Assets/BossBattle/Animation/BossAttackCombo3Profile.asset",
            "Assets/BossBattle/Animation/BossAttackCombo7Profile.asset"
        };

        [MenuItem("Tools/Boss Battle/Sync Data-Driven Boss Attacks")]
        public static void SyncDataDrivenBossAttacks()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Exit Play Mode before synchronizing animation assets.");
            }

            var profiles = new List<BossAttackProfile>();
            foreach (string profilePath in ProfilePaths)
            {
                BossAttackProfile profile = AssetDatabase.LoadAssetAtPath<BossAttackProfile>(profilePath);
                if (profile == null)
                {
                    throw new InvalidOperationException($"Boss attack profile was not found: {profilePath}");
                }

                ValidateProfile(profile);
                profiles.Add(profile);
            }

            foreach (BossAttackProfile profile in profiles)
            {
                SyncProfile(profile);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Data-driven boss attack events synchronized.");
        }

        private static void ValidateProfile(BossAttackProfile profile)
        {
            AnimationClip clip = profile.AnimationClip;
            if (clip == null)
            {
                throw new InvalidOperationException($"{profile.name} has no animation clip.");
            }

            BossAttackStrikeDefinition[] strikes = profile.Strikes;
            if (strikes == null || strikes.Length == 0 || strikes.Any(strike => strike == null))
            {
                throw new InvalidOperationException($"{profile.name} has incomplete strike data.");
            }

            if (!AssetDatabase.GetAssetPath(clip).EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"{profile.name} must use a working .anim clip, not an imported source clip.");
            }

            for (int index = 0; index < strikes.Length; index++)
            {
                BossAttackStrikeDefinition strike = strikes[index];
                float open = strike.JustDodgeWindowOpenNormalizedTime;
                float close = strike.JustDodgeWindowCloseNormalizedTime;
                float hit = strike.HitNormalizedTime;
                if (strike.StrikeIndex != index + 1 ||
                    !(0f <= open && open <= close && close <= hit && hit <= profile.FallbackEndNormalizedTime) ||
                    (index > 0 && open <= strikes[index - 1].HitNormalizedTime))
                {
                    throw new InvalidOperationException($"{profile.name}: invalid timing or strike index at strike {index + 1}.");
                }

                if (profile.JustDodgeWindows != null && profile.JustDodgeWindows.Length == strikes.Length &&
                    (!profile.TryGetJustDodgeWindow(index, out JustDodgeWindowDefinition window) ||
                     window.StrikeIndex != strike.StrikeIndex))
                {
                    throw new InvalidOperationException($"{profile.name}: window {index} must reference strike {strike.StrikeIndex}.");
                }
            }
        }

        private static void SyncProfile(BossAttackProfile profile)
        {
            AnimationClip clip = profile.AnimationClip;
            BossAttackStrikeDefinition[] strikes = profile.Strikes;
            Undo.RecordObjects(new UnityEngine.Object[] { profile, clip }, "Sync Boss Attack Events");
            profile.EnsureJustDodgeWindows(strikes.Length);
            List<AnimationEvent> events = AnimationUtility.GetAnimationEvents(clip)
                .Where(animationEvent => !IsGeneratedAttackEvent(animationEvent.functionName))
                .ToList();

            foreach (BossAttackStrikeDefinition strike in strikes)
            {
                int windowId = strike.StrikeIndex - 1;
                events.Add(CreateEvent(clip, "JustDodgeWindowOpen", windowId,
                    strike.JustDodgeWindowOpenNormalizedTime));
                events.Add(CreateEvent(clip, "JustDodgeWindowClose", windowId,
                    strike.JustDodgeWindowCloseNormalizedTime));
                events.Add(CreateEvent(clip, "AttackHitStrike", strike.StrikeIndex,
                    strike.HitNormalizedTime));
            }

            events.Add(CreateEvent(clip, "AttackEnd", 0, profile.FallbackEndNormalizedTime));
            AnimationUtility.SetAnimationEvents(clip, events.OrderBy(animationEvent => animationEvent.time).ToArray());
            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(clip);
        }

        private static bool IsGeneratedAttackEvent(string functionName)
        {
            return functionName == "AttackHit" ||
                functionName == "AttackHitStrike" ||
                functionName == "AttackEnd" ||
                functionName == "JustDodgeWindowOpen" ||
                functionName == "JustDodgeWindowClose";
        }

        private static AnimationEvent CreateEvent(
            AnimationClip clip,
            string functionName,
            int intParameter,
            float normalizedTime)
        {
            return new AnimationEvent
            {
                functionName = functionName,
                intParameter = intParameter,
                time = clip.length * Mathf.Clamp01(normalizedTime)
            };
        }
    }
}
