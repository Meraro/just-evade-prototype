using EEjanaiTeam.SwordSlashProject;
using UnityEngine;

namespace RPGGame.BossBattle
{
    /// <summary>
    /// Turns the SwordSlashVFX trail on only for the committed portion of a boss attack.
    /// SlashStart and SlashEnd are authored under the actual weapon visual.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossWeaponTrailPresentation : MonoBehaviour
    {
        [SerializeField] private BossAttackController attackController;
        [SerializeField] private SwordSlashController weaponTrail;
        [SerializeField] private bool enableWeaponTrail;

        public void Configure(BossAttackController targetAttackController, SwordSlashController targetWeaponTrail)
        {
            Unsubscribe();
            attackController = targetAttackController;
            weaponTrail = targetWeaponTrail;
            weaponTrail?.SetSlashActive(false);
            Subscribe();
        }

        private void Awake()
        {
            attackController ??= GetComponent<BossAttackController>();
        }

        private void OnEnable()
        {
            Subscribe();
            if (!enableWeaponTrail)
            {
                weaponTrail?.SetSlashActive(false);
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
            weaponTrail?.SetSlashActive(false);
        }

        private void Subscribe()
        {
            if (attackController == null)
            {
                return;
            }

            attackController.AttackCommitted -= BeginWeaponTrail;
            attackController.AttackCommitted += BeginWeaponTrail;
            attackController.AttackFinished -= EndWeaponTrail;
            attackController.AttackFinished += EndWeaponTrail;
        }

        private void Unsubscribe()
        {
            if (attackController == null)
            {
                return;
            }

            attackController.AttackCommitted -= BeginWeaponTrail;
            attackController.AttackFinished -= EndWeaponTrail;
        }

        private void BeginWeaponTrail()
        {
            if (enableWeaponTrail)
            {
                weaponTrail?.SetSlashActive(true);
            }
        }

        private void EndWeaponTrail()
        {
            weaponTrail?.SetSlashActive(false);
        }
    }
}
