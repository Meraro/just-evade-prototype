using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class PlayerDodgeCollisionVolume : MonoBehaviour
    {
        public enum VolumeType
        {
            ActualHurtbox,
            JustDodgeSensor
        }

        [SerializeField] private PlayerJustDodgeController owner;
        [SerializeField] private VolumeType volumeType;

        public PlayerJustDodgeController Owner => owner;
        public VolumeType Type => volumeType;

        public void Configure(PlayerJustDodgeController targetOwner, VolumeType targetType, float radius, Vector3 center)
        {
            owner = targetOwner;
            volumeType = targetType;
            SphereCollider sphere = GetComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = Mathf.Max(0.01f, radius);
            sphere.center = center;
        }
    }
}
