using UnityEngine;
using UnityEngine.UI;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class CombatHUDController : MonoBehaviour
    {
        private const string PlayerPath = "BB001_Stage/Player";
        private const string BossPath = "BB001_Stage/Boss";

        [Header("Combatants")]
        [SerializeField] private CombatantHealth playerHealth;
        [SerializeField] private CombatantHealth bossHealth;
        [SerializeField] private bool resolveMissingCombatants = true;

        [Header("Presentation")]
        [SerializeField] private string bossDisplayName = "XY BOT";

        [Header("Generated View References")]
        [SerializeField] private CombatHealthBarView playerHealthBar;
        [SerializeField] private CombatHealthBarView bossHealthBar;
        [SerializeField] private Text bossNameText;

        private bool missingReferenceWarningIssued;
        private float lastPlayerHealth = float.NaN;
        private float lastPlayerMaximum = float.NaN;
        private float lastBossHealth = float.NaN;
        private float lastBossMaximum = float.NaN;

        public void Configure(
            CombatantHealth targetPlayerHealth,
            CombatantHealth targetBossHealth,
            bool rebuildView = true)
        {
            Unsubscribe();
            playerHealth = targetPlayerHealth;
            bossHealth = targetBossHealth;
            if (rebuildView)
            {
                RebuildView();
            }
            else
            {
                EnsureView();
            }

            Subscribe();
            RefreshHealthViews();
        }

        public void RebuildView()
        {
            ClearChildren();
            playerHealthBar = null;
            bossHealthBar = null;
            bossNameText = null;
            EnsureView();
        }

        private void Awake()
        {
            EnsureView();
        }

        private void OnEnable()
        {
            ResolveMissingCombatants();
            Subscribe();
            RefreshHealthViews();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            // HealthChanged is the primary path. Polling only acts as a lifecycle-safe
            // fallback when a scene reload or enable order bypasses the subscription.
            RefreshHealthViews(false);
        }

        private void Subscribe()
        {
            ResolveMissingCombatants();
            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= OnPlayerHealthChanged;
                playerHealth.HealthChanged += OnPlayerHealthChanged;
            }

            if (bossHealth != null)
            {
                bossHealth.HealthChanged -= OnBossHealthChanged;
                bossHealth.HealthChanged += OnBossHealthChanged;
            }
        }

        private void Unsubscribe()
        {
            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= OnPlayerHealthChanged;
            }

            if (bossHealth != null)
            {
                bossHealth.HealthChanged -= OnBossHealthChanged;
            }
        }

        private void ResolveMissingCombatants()
        {
            if (!resolveMissingCombatants)
            {
                return;
            }

            if (playerHealth == null)
            {
                GameObject player = GameObject.Find(PlayerPath);
                playerHealth = player == null ? null : player.GetComponent<CombatantHealth>();
            }

            if (bossHealth == null)
            {
                GameObject boss = GameObject.Find(BossPath);
                bossHealth = boss == null ? null : boss.GetComponent<CombatantHealth>();
            }

            if ((playerHealth == null || bossHealth == null) && !missingReferenceWarningIssued)
            {
                missingReferenceWarningIssued = true;
                Debug.LogWarning("Combat HUD could not resolve Player or Boss CombatantHealth.", this);
            }
        }

        private void OnPlayerHealthChanged(float current, float maximum)
        {
            SetPlayerHealth(current, maximum, true);
        }

        private void OnBossHealthChanged(float current, float maximum)
        {
            SetBossHealth(current, maximum, true);
        }

        private void RefreshHealthViews()
        {
            RefreshHealthViews(true);
        }

        private void RefreshHealthViews(bool force)
        {
            if (playerHealth != null)
            {
                SetPlayerHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth, force);
            }

            if (bossHealth != null)
            {
                SetBossHealth(bossHealth.CurrentHealth, bossHealth.MaxHealth, force);
            }
        }

        private void SetPlayerHealth(float current, float maximum, bool force)
        {
            if (!force && Mathf.Approximately(current, lastPlayerHealth) && Mathf.Approximately(maximum, lastPlayerMaximum))
            {
                return;
            }

            lastPlayerHealth = current;
            lastPlayerMaximum = maximum;
            playerHealthBar?.SetHealth(current, maximum);
        }

        private void SetBossHealth(float current, float maximum, bool force)
        {
            if (!force && Mathf.Approximately(current, lastBossHealth) && Mathf.Approximately(maximum, lastBossMaximum))
            {
                return;
            }

            lastBossHealth = current;
            lastBossMaximum = maximum;
            bossHealthBar?.SetHealth(current, maximum);
        }

        private void EnsureView()
        {
            ConfigureCanvas();
            if (playerHealthBar != null && bossHealthBar != null)
            {
                return;
            }

            ClearChildren();
            CreatePlayerHealthPanel();
            CreateBossHealthPanel();
        }

        private void ConfigureCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void CreatePlayerHealthPanel()
        {
            RectTransform panel = CreateRect("PlayerHealth", transform, new Vector2(42f, -42f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(430f, 42f));
            playerHealthBar = CreateHealthBar("Bar", panel, Vector2.zero, new Vector2(420f, 26f),
                new Color(0.48f, 0.08f, 0.11f, 1f), new Color(0.60f, 0.30f, 0.28f, 1f));
            CreateOrnament(panel, new Vector2(0f, -11f), new Vector2(420f, 2f));
        }

        private void CreateBossHealthPanel()
        {
            RectTransform panel = CreateRect("BossHealth", transform, new Vector2(0f, 46f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(920f, 86f));
            bossNameText = CreateLabel("Name", panel, bossDisplayName, 23, TextAnchor.MiddleCenter, new Vector2(0f, 48f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(560f, 30f), new Color(0.78f, 0.70f, 0.58f, 1f));
            bossHealthBar = CreateHealthBar("Bar", panel, new Vector2(0f, 11f), new Vector2(900f, 28f),
                new Color(0.43f, 0.06f, 0.08f, 1f), new Color(0.52f, 0.25f, 0.23f, 1f));
            CreateOrnament(panel, new Vector2(0f, 3f), new Vector2(900f, 2f));
        }

        private static CombatHealthBarView CreateHealthBar(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 size,
            Color immediateColor,
            Color delayedColor)
        {
            RectTransform frame = CreateRect(name, parent, position, Vector2.zero, Vector2.zero,
                new Vector2(0.5f, 0.5f), size);
            Image frameImage = frame.gameObject.AddComponent<Image>();
            frameImage.color = new Color(0.025f, 0.024f, 0.027f, 0.95f);

            RectTransform inner = CreateRect("Inner", frame, Vector2.zero, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), new Vector2(-6f, -6f));
            Image innerImage = inner.gameObject.AddComponent<Image>();
            innerImage.color = new Color(0.12f, 0.09f, 0.09f, 1f);

            Image delayed = CreateFill("DelayedDamage", inner, delayedColor);
            Image immediate = CreateFill("CurrentHealth", inner, immediateColor);
            CombatHealthBarView view = frame.gameObject.AddComponent<CombatHealthBarView>();
            view.Configure(immediate, delayed);
            return view;
        }

        private static Image CreateFill(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.type = Image.Type.Simple;
            image.fillAmount = 1f;
            return image;
        }

        private static void CreateOrnament(Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform ornament = CreateRect("Ornament", parent, position, Vector2.zero, Vector2.zero,
                new Vector2(0.5f, 0.5f), size);
            Image image = ornament.gameObject.AddComponent<Image>();
            image.color = new Color(0.57f, 0.47f, 0.34f, 0.65f);
        }

        private static Text CreateLabel(
            string name,
            Transform parent,
            string content,
            int fontSize,
            TextAnchor alignment,
            Vector2 position,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 size,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent, position, anchorMin, anchorMax,
                new Vector2(0.5f, 0.5f), size);
            Text label = rect.gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = content;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 position,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 size)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private void ClearChildren()
        {
            for (int index = transform.childCount - 1; index >= 0; index--)
            {
                GameObject child = transform.GetChild(index).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }
    }
}
