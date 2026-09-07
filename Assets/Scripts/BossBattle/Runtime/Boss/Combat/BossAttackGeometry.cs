using UnityEngine;

namespace RPGGame.BossBattle
{
    public static class BossAttackGeometry
    {
        public static bool IsWithinAttackShape(
            Transform attacker,
            Vector3 targetPosition,
            float range,
            float arc)
        {
            if (attacker == null)
            {
                return false;
            }

            Vector3 offset = targetPosition - attacker.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= Mathf.Epsilon || offset.sqrMagnitude > range * range)
            {
                return false;
            }

            return Vector3.Angle(attacker.forward, offset) <= arc * 0.5f;
        }
    }
}
