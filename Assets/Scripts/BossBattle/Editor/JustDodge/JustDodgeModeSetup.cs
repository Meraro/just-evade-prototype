using System;
using RPGGame.BossBattle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RPGGame.BossBattle.Editor
{
    public static class JustDodgeModeSetup
    {
        private const string ScenePath = "Assets/Scenes/BossBattlePrototype.unity";
        private const string BossPath = "BB001_Stage/Boss";

        [MenuItem("Tools/Boss Battle/Apply Simple Just Dodge")]
        public static void ApplySimpleJustDodge()
        {
            ApplyEvaluationMode(JustDodgeEvaluationMode.SimpleCollider);
        }

        [MenuItem("Tools/Boss Battle/Apply Attack-Timing Window Just Dodge")]
        public static void ApplyAttackTimingWindowJustDodge()
        {
            ApplyEvaluationMode(JustDodgeEvaluationMode.AttackTiming);
        }

        private static void ApplyEvaluationMode(JustDodgeEvaluationMode mode)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException("Exit Play Mode before changing the saved dodge mode.");
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }

                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            GameObject boss = GameObject.Find(BossPath);
            BossAttackThreatTracker tracker = boss == null ? null : boss.GetComponent<BossAttackThreatTracker>();
            if (tracker == null)
            {
                throw new InvalidOperationException("BossAttackThreatTracker was not found in the Boss Battle scene.");
            }

            Undo.RecordObject(tracker, "Change Just Dodge Mode");
            tracker.SetEvaluationMode(mode);
            EditorUtility.SetDirty(tracker);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Just Dodge evaluation mode set to {mode}.");
        }
    }
}
