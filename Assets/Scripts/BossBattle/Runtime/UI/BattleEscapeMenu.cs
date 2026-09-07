using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    public sealed class BattleEscapeMenu : MonoBehaviour
    {
        private const float MenuWidth = 230f;
        private const float MenuHeight = 130f;

        private bool isOpen;
        private float previousTimeScale = 1f;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SetOpen(!isOpen);
            }
        }

        private void OnDisable()
        {
            SetOpen(false);
        }

        private void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            float x = (Screen.width - MenuWidth) * 0.5f;
            float y = (Screen.height - MenuHeight) * 0.5f;
            GUI.Box(new Rect(x, y, MenuWidth, MenuHeight), string.Empty);

            if (GUI.Button(new Rect(x + 20f, y + 20f, MenuWidth - 40f, 36f), "Restart"))
            {
                RestartBattle();
            }

            if (GUI.Button(new Rect(x + 20f, y + 74f, MenuWidth - 40f, 36f), "Quit"))
            {
                QuitGame();
            }
        }

        private void SetOpen(bool open)
        {
            if (isOpen == open)
            {
                return;
            }

            isOpen = open;
            if (isOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = previousTimeScale;
            }
        }

        private void RestartBattle()
        {
            Time.timeScale = 1f;
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(activeScene.name);
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            Debug.Log("Quit is available in a standalone build.");
#else
            Application.Quit();
#endif
        }
    }
}
