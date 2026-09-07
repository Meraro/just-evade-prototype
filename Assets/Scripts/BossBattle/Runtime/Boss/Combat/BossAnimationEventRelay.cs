using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    public sealed class BossAnimationEventRelay : MonoBehaviour
    {
        [SerializeField] private BossAttackController attackController;

        public void Configure(BossAttackController targetAttackController)
        {
            attackController = targetAttackController;
        }

        // Animation Event: authored blade contact frame.
        public void AttackHit()
        {
            if (attackController != null && attackController.isActiveAndEnabled)
                attackController.OnAttackHitAnimationEvent();
        }

        // Data-driven Animation Event: authored strike index from BossAttackProfile.
        public void AttackHitStrike(int strikeIndex)
        {
            if (attackController != null && attackController.isActiveAndEnabled)
                attackController.OnAttackHitAnimationEvent(strikeIndex);
        }

        // Animation Event: authored end of the committed attack.
        public void AttackEnd()
        {
            if (attackController != null && attackController.isActiveAndEnabled)
                attackController.OnAttackEndAnimationEvent();
        }

        // Animation Event: opens one attack-specific just-dodge input window.
        public void JustDodgeWindowOpen(int windowId)
        {
            if (attackController != null && attackController.isActiveAndEnabled)
                attackController.OnJustDodgeWindowOpenAnimationEvent(windowId);
        }

        // Animation Event: stops new inputs for one just-dodge window.
        public void JustDodgeWindowClose(int windowId)
        {
            if (attackController != null && attackController.isActiveAndEnabled)
                attackController.OnJustDodgeWindowCloseAnimationEvent(windowId);
        }
    }
}
