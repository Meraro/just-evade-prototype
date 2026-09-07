using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    public sealed class BossCombatBrain : MonoBehaviour
    {
        public enum CombatState
        {
            Idle,
            Approach,
            Face,
            Reposition,
            Telegraph,
            Attack,
            Recovery,
            Defeated,
            Retreat
        }

        private enum ExplorationAction
        {
            None,
            Idle,
            Strafe,
            AttackApproach
        }

        [SerializeField] private Transform player;
        [SerializeField] private BossMotor motor;
        [SerializeField] private BossAttackController attackController;
        [SerializeField] private CombatantHealth health;
        [SerializeField] private CombatState currentState;
        [SerializeField] private bool orbitClockwise = true;

        [Header("Runtime Exploration Debug")]
        [SerializeField] private string currentExplorationAction;

        [Header("Combat Distance And Exploration")]
        [SerializeField, Min(0f)] private float minimumCombatDistance = 1.65f;
        [SerializeField, Min(0f)] private float preferredCombatDistance = 2.05f;
        // Boss approaches until this range, then idles or strafes instead of closing into attack range.
        [SerializeField, Min(0f)] private float maximumCombatDistance = 5f;
        [SerializeField, Range(0f, 1f)] private float closeRangeOrbitSpeedMultiplier = 0.3f;

        [Header("Exploration")]
        [SerializeField, Min(0f)] private float idleExploreDuration = 0.55f;
        [SerializeField, Min(0f)] private float strafeMinimumDuration = 0.5f;
        [SerializeField, Min(0f)] private float strafeMaximumDuration = 2f;
        [SerializeField, Range(0f, 1f)] private float strafeSelectionChanceWithoutAttack = 0.5f;

        private ExplorationAction explorationAction;
        private float explorationEndTime = float.NegativeInfinity;

        public CombatState CurrentState => currentState;

        public void Configure(
            Transform targetPlayer,
            BossMotor targetMotor,
            BossAttackController targetAttackController,
            CombatantHealth targetHealth)
        {
            player = targetPlayer;
            motor = targetMotor;
            attackController = targetAttackController;
            health = targetHealth;
        }

        private void Awake()
        {
            if (motor == null)
            {
                motor = GetComponent<BossMotor>();
            }

            if (attackController == null)
            {
                attackController = GetComponent<BossAttackController>();
            }

            if (health == null)
            {
                health = GetComponent<CombatantHealth>();
            }
        }

        private void OnValidate()
        {
            minimumCombatDistance = Mathf.Max(0f, minimumCombatDistance);
            preferredCombatDistance = Mathf.Max(minimumCombatDistance, preferredCombatDistance);
            maximumCombatDistance = Mathf.Max(preferredCombatDistance, maximumCombatDistance);
            closeRangeOrbitSpeedMultiplier = Mathf.Clamp01(closeRangeOrbitSpeedMultiplier);
            idleExploreDuration = Mathf.Max(0f, idleExploreDuration);
            strafeMinimumDuration = Mathf.Max(0f, strafeMinimumDuration);
            strafeMaximumDuration = Mathf.Max(strafeMinimumDuration, strafeMaximumDuration);
            strafeSelectionChanceWithoutAttack = Mathf.Clamp01(strafeSelectionChanceWithoutAttack);
        }

        private void Update()
        {
            if (health != null && health.IsDead)
            {
                currentState = CombatState.Defeated;
                motor?.Stop();
                return;
            }

            if (player == null || attackController == null || motor == null || !attackController.HasConfiguredAttacks)
            {
                currentState = CombatState.Idle;
                motor?.Stop();
                return;
            }

            if (attackController.IsBusy)
            {
                currentState = attackController.IsTelegraphing
                    ? CombatState.Telegraph
                    : attackController.IsAttacking ? CombatState.Attack : CombatState.Recovery;
                motor.Stop();
                ResetExploration();
                return;
            }

            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance > maximumCombatDistance)
            {
                currentState = CombatState.Approach;
                motor.MoveTowards(player, maximumCombatDistance);
                ResetExploration();
                return;
            }

            float angle = motor.FaceTowards(player, attackController.PreparationTurnSpeed);
            if (angle > attackController.AttackStartAngle)
            {
                currentState = CombatState.Face;
                motor.Stop();
                ResetExploration();
                return;
            }

            if (distance < minimumCombatDistance)
            {
                currentState = CombatState.Retreat;
                motor.StrafeAround(player, orbitClockwise, closeRangeOrbitSpeedMultiplier);
                ResetExploration();
                return;
            }

            UpdateExploration(distance);
        }

        private void UpdateExploration(float distance)
        {
            if (Time.time >= explorationEndTime)
            {
                bool hasReadyAttack = attackController.CanBeginAttack;
                explorationAction = SelectExplorationAction(hasReadyAttack);
                currentExplorationAction = explorationAction.ToString();
                orbitClockwise = Random.value > 0.5f;
                explorationEndTime = Time.time + GetActionDuration(explorationAction);
            }

            if (explorationAction == ExplorationAction.AttackApproach)
            {
                if (!attackController.CanBeginAttack)
                {
                    ResetExploration();
                    motor.Stop();
                    return;
                }

                if (distance > attackController.AttackStartDistance)
                {
                    currentState = CombatState.Approach;
                    motor.MoveTowards(player, attackController.AttackStartDistance);
                    return;
                }

                motor.Stop();
                if (attackController.BeginAttack())
                {
                    currentState = CombatState.Attack;
                    ResetExploration();
                }

                return;
            }

            if (explorationAction == ExplorationAction.Strafe)
            {
                currentState = CombatState.Reposition;
                motor.StrafeAround(player, orbitClockwise, closeRangeOrbitSpeedMultiplier);
                return;
            }

            currentState = CombatState.Idle;
            motor.Stop();
        }

        private void ResetExploration()
        {
            explorationAction = ExplorationAction.None;
            currentExplorationAction = explorationAction.ToString();
            explorationEndTime = float.NegativeInfinity;
        }

        private ExplorationAction SelectExplorationAction(bool hasReadyAttack)
        {
            if (hasReadyAttack)
            {
                // A ready attack and a strafe have equal priority, but a ready attack prevents repeated strafing.
                return explorationAction == ExplorationAction.Strafe || Random.value < 0.5f
                    ? ExplorationAction.AttackApproach
                    : ExplorationAction.Strafe;
            }

            return Random.value < strafeSelectionChanceWithoutAttack
                ? ExplorationAction.Strafe
                : ExplorationAction.Idle;
        }

        private float GetActionDuration(ExplorationAction action)
        {
            switch (action)
            {
                case ExplorationAction.Strafe:
                    return Random.Range(strafeMinimumDuration, strafeMaximumDuration);
                case ExplorationAction.AttackApproach:
                    // Once an attack approach is selected, keep full-speed forward motion until the attack range is reached.
                    return float.PositiveInfinity;
                default:
                    return idleExploreDuration;
            }
        }
    }
}
