using UnityEngine;
using UnityEngine.InputSystem;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    public sealed class BossBattleInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;

        private InputActionMap combatMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction attackAction;
        private InputAction dodgeAction;
        private InputAction lockOnAction;

        public Vector2 Move => moveAction == null ? Vector2.zero : moveAction.ReadValue<Vector2>();
        public Vector2 Look => lookAction == null ? Vector2.zero : lookAction.ReadValue<Vector2>();
        public bool AttackPressed => attackAction != null && attackAction.WasPressedThisFrame();
        public bool DodgePressed => dodgeAction != null && dodgeAction.WasPressedThisFrame();
        public bool LockOnPressed => lockOnAction != null && lockOnAction.WasPressedThisFrame();

        public void Configure(InputActionAsset inputActions)
        {
            actions = inputActions;
            InitializeActions();
        }

        private void OnEnable()
        {
            InitializeActions();
        }

        private void OnDisable()
        {
            combatMap?.Disable();
        }

        private void InitializeActions()
        {
            if (actions == null)
            {
                return;
            }

            combatMap = actions.FindActionMap("Combat", true);
            moveAction = combatMap.FindAction("Move", true);
            lookAction = combatMap.FindAction("Look", true);
            attackAction = combatMap.FindAction("Attack", true);
            dodgeAction = combatMap.FindAction("Dodge", true);
            lockOnAction = combatMap.FindAction("LockOn", true);
            combatMap.Enable();
        }
    }
}
