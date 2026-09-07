using System.Collections.Generic;
using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    public sealed class JustDodgeFocusPresentationController : MonoBehaviour
    {
        [SerializeField] private PlayerJustDodgeController justDodgeController;
        [SerializeField] private Transform stageRoot;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Transform bossRoot;

        [Header("Environment Focus")]
        [SerializeField, Range(0f, 1f)] private float environmentBrightness = 0.25f;
        [SerializeField, Min(0f)] private float duration = 1f;
        [SerializeField, Min(0f)] private float fadeInDuration = 0.08f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.12f;

        private readonly List<RendererSlot> affectedSlots = new List<RendererSlot>();
        private float effectStartTime;
        private float effectEndTime;
        private bool isPlaying;

        public void Configure(
            PlayerJustDodgeController targetJustDodgeController,
            Transform targetStageRoot,
            Transform targetPlayerRoot,
            Transform targetBossRoot)
        {
            Unsubscribe();
            justDodgeController = targetJustDodgeController;
            stageRoot = targetStageRoot;
            playerRoot = targetPlayerRoot;
            bossRoot = targetBossRoot;
            Subscribe();
        }

        private void Awake()
        {
            justDodgeController ??= FindFirstObjectByType<PlayerJustDodgeController>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            RestoreEnvironment();
        }

        private void Update()
        {
            if (!isPlaying)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now >= effectEndTime)
            {
                RestoreEnvironment();
                return;
            }

            float progress = Mathf.Clamp01((now - effectStartTime) / Mathf.Max(0.001f, duration));
            float intensity = EvaluateIntensity(progress);
            ApplyEnvironmentBrightness(Mathf.Lerp(1f, environmentBrightness, intensity));
        }

        private void Subscribe()
        {
            if (justDodgeController == null)
            {
                return;
            }

            justDodgeController.JustDodgeSucceeded -= PlayFocus;
            justDodgeController.JustDodgeSucceeded += PlayFocus;
        }

        private void Unsubscribe()
        {
            if (justDodgeController != null)
            {
                justDodgeController.JustDodgeSucceeded -= PlayFocus;
            }
        }

        private void PlayFocus()
        {
            if (stageRoot == null)
            {
                return;
            }

            if (!isPlaying)
            {
                CacheAffectedRenderers();
                isPlaying = true;
            }

            effectStartTime = Time.unscaledTime;
            effectEndTime = effectStartTime + duration;
        }

        private float EvaluateIntensity(float progress)
        {
            float fadeInEnd = Mathf.Clamp01(fadeInDuration / Mathf.Max(0.001f, duration));
            float fadeOutStart = Mathf.Clamp01(1f - fadeOutDuration / Mathf.Max(0.001f, duration));
            if (progress < fadeInEnd)
            {
                return Mathf.SmoothStep(0f, 1f, progress / Mathf.Max(0.001f, fadeInEnd));
            }

            if (progress > fadeOutStart)
            {
                return Mathf.SmoothStep(1f, 0f, (progress - fadeOutStart) / Mathf.Max(0.001f, 1f - fadeOutStart));
            }

            return 1f;
        }

        private void CacheAffectedRenderers()
        {
            affectedSlots.Clear();
            foreach (Renderer renderer in stageRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || IsExcluded(renderer.transform))
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    Material material = materials[index];
                    if (material == null || (!material.HasProperty("_BaseColor") && !material.HasProperty("_Color")))
                    {
                        continue;
                    }

                    MaterialPropertyBlock originalBlock = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(originalBlock, index);
                    affectedSlots.Add(new RendererSlot(renderer, index, material, originalBlock));
                }
            }
        }

        private bool IsExcluded(Transform transformToCheck)
        {
            return (playerRoot != null && transformToCheck.IsChildOf(playerRoot)) ||
                (bossRoot != null && transformToCheck.IsChildOf(bossRoot));
        }

        private void ApplyEnvironmentBrightness(float brightness)
        {
            foreach (RendererSlot slot in affectedSlots)
            {
                if (slot.Renderer == null || slot.Material == null)
                {
                    continue;
                }

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                if (slot.Material.HasProperty("_BaseColor"))
                {
                    block.SetColor("_BaseColor", slot.Material.GetColor("_BaseColor") * brightness);
                }

                if (slot.Material.HasProperty("_Color"))
                {
                    block.SetColor("_Color", slot.Material.GetColor("_Color") * brightness);
                }

                slot.Renderer.SetPropertyBlock(block, slot.MaterialIndex);
            }
        }

        private void RestoreEnvironment()
        {
            if (!isPlaying)
            {
                return;
            }

            foreach (RendererSlot slot in affectedSlots)
            {
                if (slot.Renderer != null)
                {
                    slot.Renderer.SetPropertyBlock(slot.OriginalBlock, slot.MaterialIndex);
                }
            }

            affectedSlots.Clear();
            isPlaying = false;
            effectStartTime = 0f;
            effectEndTime = 0f;
        }

        private readonly struct RendererSlot
        {
            public RendererSlot(Renderer renderer, int materialIndex, Material material, MaterialPropertyBlock originalBlock)
            {
                Renderer = renderer;
                MaterialIndex = materialIndex;
                Material = material;
                OriginalBlock = originalBlock;
            }

            public Renderer Renderer { get; }
            public int MaterialIndex { get; }
            public Material Material { get; }
            public MaterialPropertyBlock OriginalBlock { get; }
        }
    }
}
