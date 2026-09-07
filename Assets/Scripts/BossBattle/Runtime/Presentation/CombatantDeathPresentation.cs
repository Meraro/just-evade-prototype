using System;
using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    public sealed class CombatantDeathPresentation : MonoBehaviour
    {
        private static readonly int DeathTrigger = Animator.StringToHash("Death");

        [SerializeField] private CombatantHealth health;
        [SerializeField] private Animator animator;
        [SerializeField] private Behaviour[] disableOnDeath;

        private PlayerMotor playerMotor;
        private BossMotor bossMotor;
        private bool hasPlayedDeath;
        private bool missingTriggerWarningIssued;

        public void Configure(
            CombatantHealth targetHealth,
            Animator targetAnimator,
            Behaviour[] behavioursToDisable,
            PlayerMotor targetPlayerMotor,
            BossMotor targetBossMotor)
        {
            Unsubscribe();
            health = targetHealth;
            animator = targetAnimator;
            disableOnDeath = behavioursToDisable;
            playerMotor = targetPlayerMotor;
            bossMotor = targetBossMotor;
            Subscribe();

            if (health != null && health.IsDead)
            {
                PlayDeath();
            }
        }

        private void Awake()
        {
            health ??= GetComponent<CombatantHealth>();
            animator ??= GetComponentInChildren<Animator>();
            playerMotor ??= GetComponent<PlayerMotor>();
            bossMotor ??= GetComponent<BossMotor>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (health == null)
            {
                return;
            }

            health.HealthChanged -= OnHealthChanged;
            health.HealthChanged += OnHealthChanged;
        }

        private void Unsubscribe()
        {
            if (health != null)
            {
                health.HealthChanged -= OnHealthChanged;
            }
        }

        private void OnHealthChanged(float currentHealth, float maximumHealth)
        {
            if (currentHealth <= 0f)
            {
                PlayDeath();
            }
        }

        private void PlayDeath()
        {
            if (hasPlayedDeath)
            {
                return;
            }

            hasPlayedDeath = true;

            if (disableOnDeath != null)
            {
                foreach (Behaviour behaviour in disableOnDeath)
                {
                    if (behaviour != null && behaviour != this)
                    {
                        behaviour.enabled = false;
                    }
                }
            }

            // Disabling a dodge can restore movement; lock it after cleanup.
            playerMotor?.SetMovementEnabled(false);
            bossMotor?.Stop();

            if (animator == null)
            {
                return;
            }

            if (!HasDeathTrigger())
            {
                if (!missingTriggerWarningIssued)
                {
                    missingTriggerWarningIssued = true;
                    Debug.LogWarning($"{name} cannot play its death animation because its Animator has no Death trigger.", this);
                }

                return;
            }

            // Pending attack triggers must not interrupt the death state.
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    animator.ResetTrigger(parameter.nameHash);
                }
            }

            animator.speed = 1f;
            animator.SetTrigger(DeathTrigger);
        }

        private bool HasDeathTrigger()
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.nameHash == DeathTrigger)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
