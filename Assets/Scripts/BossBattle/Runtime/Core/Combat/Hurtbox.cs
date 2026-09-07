using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CombatantHealth))]
    public sealed class Hurtbox : MonoBehaviour
    {
        [SerializeField] private CombatantHealth health;
        [SerializeField] private bool isInvulnerable;

        public CombatantHealth Health => health;
        public bool IsInvulnerable => isInvulnerable;

        public void SetInvulnerable(bool value)
        {
            isInvulnerable = value;
        }

        public void ReceiveDamage(float damage)
        {
            if (isInvulnerable)
            {
                return;
            }

            health?.ReceiveDamage(damage);
        }

        private void Reset()
        {
            health = GetComponent<CombatantHealth>();
        }

        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<CombatantHealth>();
            }
        }
    }
}
