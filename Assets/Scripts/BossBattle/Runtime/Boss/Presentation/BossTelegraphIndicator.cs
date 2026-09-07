using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    public sealed class BossTelegraphIndicator : MonoBehaviour
    {
        [SerializeField] private BossAttackController attackController;
        [SerializeField] private GameObject ringObject;

        public void Configure(BossAttackController targetAttackController, GameObject targetRingObject)
        {
            attackController = targetAttackController;
            ringObject = targetRingObject;
            RefreshVisibility();
        }

        private void Update()
        {
            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            if (ringObject != null)
            {
                ringObject.SetActive(attackController != null && attackController.IsTelegraphing);
            }
        }
    }
}
