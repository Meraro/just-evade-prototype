using UnityEngine;

namespace RPGGame.BossBattle
{
    /// <summary>
    /// Receives Animation Events on the Animator GameObject and forwards them
    /// to the Player gameplay root, where combo intent is owned.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAnimationEventRelay : MonoBehaviour
    {
        [SerializeField] private PlayerComboAttackController comboController;

        public void Configure(PlayerComboAttackController targetComboController)
        {
            comboController = targetComboController;
        }

        // Animation Event: authored connection/end point in an attack clip.
        public void AttackEnd()
        {
            if (comboController != null && comboController.isActiveAndEnabled)
            {
                comboController.OnAttackEndAnimationEvent();
            }
        }

        // This event is an authored clip marker used by the Animator setup to find
        // the appropriate entry point for the independent 1H04 attack. It has no
        // gameplay action at runtime, but must be received by the Animator object.
        public void AttackStart()
        {
        }
    }
}
