using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    public sealed class CombatBootstrap : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Transform boss;

        public Transform Player => player;
        public Transform Boss => boss;

        private void Awake()
        {
            ConfigureDeathPresentation(player, true);
            ConfigureDeathPresentation(boss, false);
            if (GetComponent<BattleEscapeMenu>() == null)
            {
                gameObject.AddComponent<BattleEscapeMenu>();
            }
        }

        public void Configure(Transform playerTransform, Transform bossTransform)
        {
            player = playerTransform;
            boss = bossTransform;
        }

        private static void ConfigureDeathPresentation(Transform combatant, bool isPlayer)
        {
            if (combatant == null)
            {
                return;
            }

            CombatantHealth health = combatant.GetComponent<CombatantHealth>();
            Animator animator = combatant.GetComponentInChildren<Animator>();
            CombatantDeathPresentation presentation = combatant.GetComponent<CombatantDeathPresentation>();
            if (presentation == null)
            {
                presentation = combatant.gameObject.AddComponent<CombatantDeathPresentation>();
            }

            Behaviour[] disabledBehaviours = isPlayer
                ? new Behaviour[]
                {
                    combatant.GetComponent<PlayerComboAttackController>(),
                    combatant.GetComponent<PlayerDodgeController>(),
                    combatant.GetComponent<PlayerJustDodgeController>(),
                    animator == null ? null : animator.GetComponent<PlayerAttackRootMotionDriver>()
                }
                : new Behaviour[]
                {
                    combatant.GetComponent<BossCombatBrain>(),
                    combatant.GetComponent<BossAttackController>(),
                    animator == null ? null : animator.GetComponent<BossAttackRootMotionDriver>()
                };

            presentation.Configure(
                health,
                animator,
                disabledBehaviours,
                isPlayer ? combatant.GetComponent<PlayerMotor>() : null,
                isPlayer ? null : combatant.GetComponent<BossMotor>());
        }
    }
}
