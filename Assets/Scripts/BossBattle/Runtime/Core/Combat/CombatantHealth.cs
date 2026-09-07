using System;
using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    public sealed class CombatantHealth : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float currentHealth = 100f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;

        public event Action<float, float> HealthChanged;

        public void Configure(float configuredMaxHealth)
        {
            maxHealth = Mathf.Max(1f, configuredMaxHealth);
            currentHealth = maxHealth;
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ReceiveDamage(float damage)
        {
            if (IsDead || damage <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - damage);
            HealthChanged?.Invoke(currentHealth, maxHealth);
            Debug.Log($"{name} received {damage:F0} damage. Health: {currentHealth:F0}/{maxHealth:F0}");
        }
    }
}
