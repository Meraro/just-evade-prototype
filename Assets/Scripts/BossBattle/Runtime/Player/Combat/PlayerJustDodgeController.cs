using System;
using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerDodgeController))]
    public sealed class PlayerJustDodgeController : MonoBehaviour
    {
        [SerializeField] private PlayerDodgeController dodgeController;
        [SerializeField] private BossAttackThreatTracker threatTracker;

        private bool succeededThisDodge;

        public bool IsDodging => dodgeController != null && dodgeController.IsDodging;
        public event Action JustDodgeSucceeded;

        public void Configure(PlayerDodgeController targetDodgeController, BossAttackThreatTracker targetThreatTracker)
        {
            Unsubscribe();
            dodgeController = targetDodgeController;
            threatTracker = targetThreatTracker;
            Subscribe();
        }

        private void Awake()
        {
            dodgeController ??= GetComponent<PlayerDodgeController>();
            threatTracker ??= FindFirstObjectByType<BossAttackThreatTracker>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            succeededThisDodge = false;
        }

        private void Subscribe()
        {
            if (dodgeController == null)
            {
                return;
            }

            dodgeController.DodgeStarted -= OnDodgeStarted;
            dodgeController.DodgeStarted += OnDodgeStarted;
            dodgeController.DodgeEnded -= OnDodgeEnded;
            dodgeController.DodgeEnded += OnDodgeEnded;
        }

        private void Unsubscribe()
        {
            if (dodgeController == null)
            {
                return;
            }

            dodgeController.DodgeStarted -= OnDodgeStarted;
            dodgeController.DodgeEnded -= OnDodgeEnded;
        }

        private void OnDodgeStarted()
        {
            succeededThisDodge = false;
            threatTracker?.TryRegisterCandidate(this);
        }

        private void OnDodgeEnded()
        {
            succeededThisDodge = false;
        }

        public void NotifyJustDodgeSucceeded()
        {
            if (succeededThisDodge || !IsDodging)
            {
                return;
            }

            succeededThisDodge = true;
            JustDodgeSucceeded?.Invoke();
        }
    }
}
