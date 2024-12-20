﻿using Modding;
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
            explosiveFiller = BlockBehaviour.AddSlider("Explosive Filler", "explosive-filler", 5, 0, 100, "", "kg");
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

        private float PreviewHEPenetration(float distance)
        {
            return (int)ArmorHelper.GetHeParticlePenetration(explosiveFiller.Value, distance);
        }

        private float PreviewHEATPenetration(float angle)
        {
            return ArmorHelper.PreviewHEATPenetration(angle, explosiveFiller.Value);
        }

        private void Update()
        {
            if (BlockMapper.CurrentInstance && BlockMapper.CurrentInstance.Block == BlockBehaviour)
            {
                string preview = "N/A";

                if (mode.Value == (int)Mode.HESH)
                    preview = ($"{(int)(Mod.Config.Shells.HESH.PenetrationValue * Mathf.Pow(explosiveFiller.Value, Mod.Config.Shells.HESH.PenetrationPower))}mm ({PreviewHEPenetration(0)}mm (0m), {PreviewHEPenetration(10)}mm (10m))");
                else if (mode.Value == (int)Mode.HE)
                    preview = ($"{PreviewHEPenetration(0)}mm (0m), {PreviewHEPenetration(10)}mm (10m), {PreviewHEPenetration(25)}mm (25m)");
                else if (mode.Value == (int)Mode.HEAT)
                    preview = ($"{PreviewHEATPenetration(0)}mm (0°), {PreviewHEATPenetration(30)}mm (30°), {PreviewHEATPenetration(60)}mm (60°)");
                else if (mode.Value == (int)Mode.Nuclear)
                    preview = ($"Total annihilation");

                BlockMapper.CurrentInstance.SetBlockName(preview);
            }
        }

        private void Detonate(Collision collision)
        {
            if (_detonated)
                return;
            _detonated = true;

            StartCoroutine(WaitForProjectiles());
            GetComponentInChildren<SphereCollider>().isTrigger = true;
            Vector3 center = transform.position + transform.forward * 0.5F * transform.localScale.z;
            if (mode.Value == (int)Mode.HE)
                Spawner.SpawnHighExplosion(center, explosiveFiller.Value);
            if (mode.Value == (int)Mode.HEAT)
                Spawner.SpawnHeatExplosion(center, transform.forward, explosiveFiller.Value);
            if (mode.Value == (int)Mode.HESH)
            {
                Spawner.SpawnHighExplosion(center, explosiveFiller.Value);
                if (collision != null)          
                {
                    Vector3 point = collision.contacts[0].point;
                    Vector3 normal = collision.contacts[0].normal;
                    Vector3 enter = point + normal * 0.1F;
                    BuildSurface surface = collision.collider.attachedRigidbody?.GetComponent<BuildSurface>();
                    Spawner.SpawnHeshSpalling(collision.collider, enter, normal, explosiveFiller.Value, surface);
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
                    SpawnNuclearExplosionEffect();
                    if (StatMaster.isHosting)
                        Networking.SpawnNuclearExplosionEffect();
                }
            }
        }

        private IEnumerator WaitForProjectiles()
        {
            Rigidbody.isKinematic = true;
            yield return new WaitForFixedUpdate();
            Rigidbody.isKinematic = false;
        }

        public static void SpawnNuclearExplosionEffect()
        {
            if (LocalMachine.Active().SimulationMachine == null)
                return;
            GameObject gameObject = new GameObject("Nuclear Explosion Effect");
            gameObject.AddComponent<NuclearExplosionEffect>();
            gameObject.transform.parent = LocalMachine.Active().SimulationMachine;
        }
    }
}