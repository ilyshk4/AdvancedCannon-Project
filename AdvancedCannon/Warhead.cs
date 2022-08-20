using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LocalMachine = Machine;

namespace AdvancedCannon
{
    public class Warhead : BlockScript
    {
        public static readonly List<string> MODES = new List<string>()
        {
            "HE", "HEAT", "HESH", "Nuclear"
        };

        public enum Mode { HE, HEAT, HESH, Nuclear }

        public MMenu mode;
        public MSlider triggerVelocity;
        public MSlider explosiveFiller;
        public MKey detonate;

        private bool _detonated;

        public override void SafeAwake()
        {
            mode = AddMenu("mode", 0, MODES);
            triggerVelocity = BlockBehaviour.AddSlider("Trigger Velocity", "trigger-velocity", 100, 0, 1000, "", "ms");
            explosiveFiller = BlockBehaviour.AddSlider("Explosive Filler", "explosive-filler", 5, 0, 30, "", "kg");
            detonate = AddKey("Detonate", "detonate", KeyCode.None);

            if (Rigidbody)
            {
                Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }
        public override void SimulateUpdateHost()
        {
            if (detonate.IsPressed)
                Detonate(null);
        }

        public override void KeyEmulationUpdate()
        {
            if (detonate.EmulationPressed())
                Detonate(null);
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude > triggerVelocity.Value)
            {
                Detonate(collision);
            }
        }

        private void Detonate(Collision collision)
        {
            if (_detonated)
                return;
            _detonated = true;

            StartCoroutine(WaitForProjectiles());
            GetComponentInChildren<SphereCollider>().isTrigger = true;
            Vector3 center = transform.position + transform.forward * 0.5F;
            if (mode.Value == (int)Mode.HE)
                Projectile.SpawnHighExplosion(center, transform.forward, explosiveFiller.Value);
            if (mode.Value == (int)Mode.HEAT)
                Projectile.SpawnHeatExplosion(center, transform.forward, explosiveFiller.Value);
            if (mode.Value == (int)Mode.HESH)
            {
                if (collision == null)
                    Projectile.SpawnHighExplosion(center, transform.forward, explosiveFiller.Value);
                else
                {
                    Vector3 point = collision.contacts[0].point;
                    Vector3 normal = collision.contacts[0].normal;
                    Vector3 enter = point + normal * 0.1F;
                    BuildSurface surface = collision.collider.attachedRigidbody?.GetComponent<BuildSurface>();
                    Projectile.HeshPenetration(collision.collider, enter, normal, surface, explosiveFiller.Value);
                }
            }
            if (mode.Value == (int)Mode.Nuclear)
            {
                if (StatMaster.isHosting || !StatMaster.isMP || StatMaster.isLocalSim)
                {
                    foreach (var collider in Physics.OverlapSphere(transform.position, 1000))
                    {
                        collider.attachedRigidbody?.AddExplosionForce(100000, transform.position, 1000);
                        BlockBehaviour block = collider.attachedRigidbody?.GetComponent<BlockBehaviour>();
                        if (block)
                        {
                            block.fireTag?.Ignite();
                            block.BlockHealth?.DamageBlock(1000);
                        }
                    }
                    SpawnNuclearExplosionEffect(transform.position);
                    if (StatMaster.isHosting)
                        ModNetworking.SendToAll(Mod.SpawnNuclearExplosionEffect.CreateMessage(transform.position));
                }
            }
        }

        private IEnumerator WaitForProjectiles()
        {
            Rigidbody.isKinematic = true;
            yield return new WaitForFixedUpdate();
            Rigidbody.isKinematic = false;
        }

        public static void SpawnNuclearExplosionEffect(Vector3 point)
        {
            if (LocalMachine.Active().SimulationMachine == null)
                return;
            GameObject gameObject = new GameObject("Nuclear Explosion Effect");
            gameObject.AddComponent<NuclearExplosionEffect>();
            gameObject.transform.position = point;
            gameObject.transform.parent = LocalMachine.Active().SimulationMachine;
        }
    }
}