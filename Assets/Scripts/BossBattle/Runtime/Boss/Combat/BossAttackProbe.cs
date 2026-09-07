using System.Collections.Generic;
using UnityEngine;

namespace RPGGame.BossBattle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BossAttackProbe : MonoBehaviour
    {
        [SerializeField] private BossAttackThreatTracker threatTracker;
        [SerializeField] private SphereCollider triggerCollider;

        private readonly HashSet<PlayerJustDodgeController> actualContacts = new HashSet<PlayerJustDodgeController>();
        private readonly HashSet<PlayerJustDodgeController> sensorContacts = new HashSet<PlayerJustDodgeController>();

        public Vector3 WorldCenter => triggerCollider == null
            ? transform.position
            : transform.TransformPoint(triggerCollider.center);
        public float Radius => triggerCollider == null ? 0f : triggerCollider.radius;
        public bool IsActive => triggerCollider != null && triggerCollider.enabled;

        public void Configure(BossAttackThreatTracker targetThreatTracker)
        {
            threatTracker = targetThreatTracker;
            triggerCollider ??= GetComponent<SphereCollider>();
            Rigidbody body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeAll;
            triggerCollider.isTrigger = true;
        }

        public void Activate(BossAttackProfile profile, float centerHeight)
        {
            if (profile == null)
            {
                Deactivate();
                return;
            }

            Activate(profile.HitRange, profile.HitArc, centerHeight);
        }

        public void Activate(BossAttackStrikeDefinition strike, float centerHeight)
        {
            if (strike == null)
            {
                return;
            }

            Activate(strike.HitRange, strike.HitArc, centerHeight);
        }

        private void Activate(float hitRange, float hitArc, float centerHeight)
        {
            triggerCollider ??= GetComponent<SphereCollider>();
            bool isRadialAttack = hitArc >= 270f;
            float radius = isRadialAttack ? hitRange : hitRange * 0.5f;
            triggerCollider.radius = Mathf.Max(0.01f, radius);
            triggerCollider.center = new Vector3(0f, centerHeight, isRadialAttack ? 0f : radius);
            triggerCollider.enabled = true;
            Physics.SyncTransforms();
        }

        public void Deactivate()
        {
            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }

            actualContacts.Clear();
            sensorContacts.Clear();
        }

        public bool IsSensorOverlapping(PlayerJustDodgeController player)
        {
            return player != null && sensorContacts.Contains(player);
        }

        public bool IsActualOverlapping(PlayerJustDodgeController player)
        {
            return player != null && actualContacts.Contains(player);
        }

        private void Awake()
        {
            Configure(threatTracker);
            Deactivate();
        }

        private void OnTriggerEnter(Collider other)
        {
            UpdateContact(other, true);
        }

        private void OnTriggerExit(Collider other)
        {
            UpdateContact(other, false);
        }

        private void UpdateContact(Collider other, bool isEntering)
        {
            PlayerDodgeCollisionVolume volume = other.GetComponent<PlayerDodgeCollisionVolume>();
            PlayerJustDodgeController player = volume == null ? null : volume.Owner;
            if (player == null)
            {
                return;
            }

            HashSet<PlayerJustDodgeController> contacts = volume.Type == PlayerDodgeCollisionVolume.VolumeType.ActualHurtbox
                ? actualContacts
                : sensorContacts;
            if (isEntering)
            {
                contacts.Add(player);
                if (volume.Type == PlayerDodgeCollisionVolume.VolumeType.JustDodgeSensor)
                {
                    threatTracker?.NotifySensorContact(player);
                }
            }
            else
            {
                contacts.Remove(player);
            }
        }
    }
}
