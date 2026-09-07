using UnityEngine;

namespace RPGGame.BossBattle
{
    public enum JustDodgeEvaluationMode
    {
        SimpleCollider,
        AttackTiming
    }

    [DisallowMultipleComponent]
    public sealed class BossAttackThreatTracker : MonoBehaviour
    {
        [SerializeField] private BossAttackProbe attackProbe;
        [SerializeField, Min(0f)] private float probeCenterHeight = 0.9f;

        [Header("Just Dodge Evaluation")]
        [SerializeField] private JustDodgeEvaluationMode evaluationMode = JustDodgeEvaluationMode.AttackTiming;

        private bool isThreatActive;
        private float candidateExpiresAt;
        private int activeWindowId = -1;
        private int candidateWindowId = -1;

        private BossAttackProfile activeProfile;
        private PlayerJustDodgeController candidate;

        public bool IsThreatActive => isThreatActive;
        public JustDodgeEvaluationMode EvaluationMode => evaluationMode;

        public void SetEvaluationMode(JustDodgeEvaluationMode mode)
        {
            evaluationMode = mode;
            ClearCandidate();
            activeWindowId = -1;
        }

        private void Awake()
        {
            attackProbe ??= GetComponentInChildren<BossAttackProbe>(true);
        }

        public void BeginThreat(BossAttackProfile profile)
        {
            activeProfile = profile;
            isThreatActive = profile != null && profile.AllowJustDodge;
            ClearCandidate();
            activeWindowId = -1;
            if (profile != null)
            {
                attackProbe?.Activate(profile, probeCenterHeight);
            }
        }

        public void EndThreat()
        {
            isThreatActive = false;
            activeProfile = null;
            ClearCandidate();
            activeWindowId = -1;
            attackProbe?.Deactivate();
        }

        public bool TryRegisterCandidate(PlayerJustDodgeController player)
        {
            if (!isThreatActive || activeProfile == null || player == null || !player.IsDodging)
            {
                return false;
            }

            bool mayRegister = evaluationMode == JustDodgeEvaluationMode.SimpleCollider
                ? attackProbe != null && attackProbe.IsSensorOverlapping(player)
                : activeWindowId >= 0 && attackProbe != null && attackProbe.IsActualOverlapping(player);
            if (!mayRegister)
            {
                return false;
            }

            candidate = player;
            candidateWindowId = evaluationMode == JustDodgeEvaluationMode.AttackTiming ? activeWindowId : -1;
            candidateExpiresAt = evaluationMode == JustDodgeEvaluationMode.SimpleCollider
                ? Time.time + activeProfile.JustDodgeCandidateDuration
                : 0f;
            return true;
        }

        public void NotifySensorContact(PlayerJustDodgeController player)
        {
            if (evaluationMode == JustDodgeEvaluationMode.SimpleCollider && candidate == null && player != null && player.IsDodging)
            {
                TryRegisterCandidate(player);
            }
        }

        public void OpenJustDodgeWindow(int windowId)
        {
            if (!isThreatActive || activeProfile == null ||
                !activeProfile.TryGetJustDodgeWindow(windowId, out _))
            {
                return;
            }

            activeProfile.TryGetJustDodgeWindow(windowId, out JustDodgeWindowDefinition window);
            BossAttackStrikeDefinition strike = activeProfile.GetStrike(window.StrikeIndex);
            attackProbe?.Activate(strike, probeCenterHeight);
            if (evaluationMode == JustDodgeEvaluationMode.AttackTiming)
            {
                activeWindowId = windowId;
            }
        }

        public void CloseJustDodgeWindow(int windowId)
        {
            if (evaluationMode != JustDodgeEvaluationMode.AttackTiming || activeWindowId != windowId)
            {
                return;
            }

            activeWindowId = -1;
        }

        public void ResolveAttackHit(Hurtbox targetHurtbox, float damage, int strikeIndex)
        {
            if (targetHurtbox == null)
            {
                return;
            }

            PlayerJustDodgeController targetPlayer = targetHurtbox.GetComponent<PlayerJustDodgeController>();
            bool timingWindowMatchesStrike = evaluationMode != JustDodgeEvaluationMode.AttackTiming ||
                activeProfile != null && activeProfile.TryGetJustDodgeWindow(candidateWindowId, out JustDodgeWindowDefinition window) &&
                window.StrikeIndex == strikeIndex;

            bool isCandidateForTarget = candidate != null && candidate == targetPlayer && candidate.IsDodging &&
                (evaluationMode != JustDodgeEvaluationMode.SimpleCollider || Time.time <= candidateExpiresAt) &&
                timingWindowMatchesStrike;
            bool overlapsActualHurtbox = attackProbe != null && attackProbe.IsActualOverlapping(targetPlayer);
            bool damageWouldApply = overlapsActualHurtbox && !targetHurtbox.IsInvulnerable;

            if (isCandidateForTarget && !damageWouldApply)
            {
                ClearCandidate();
                targetPlayer.NotifyJustDodgeSucceeded();
                return;
            }

            if (candidate != null && (evaluationMode == JustDodgeEvaluationMode.SimpleCollider || timingWindowMatchesStrike))
            {
                ClearCandidate();
            }

            if (damageWouldApply)
            {
                targetHurtbox.ReceiveDamage(damage);
            }
        }

        private void ClearCandidate()
        {
            candidate = null;
            candidateExpiresAt = 0f;
            candidateWindowId = -1;
        }
    }
}
