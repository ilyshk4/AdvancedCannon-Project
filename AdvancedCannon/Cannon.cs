using System;
using System.Collections.Generic;
using Modding;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AdvancedCannon
{
    public class Cannon : BlockScript
    {
        public static readonly List<string> MODES = new List<string>()
        {
            "AP", "APHE", "HE", "APFSDS", "HESH", "HEATFS"
        };

        enum Mode { AP, APHE, HE, APFSDS, HESH, HEAT };

        public MKey fire;
        public MSlider velocity;
        public MSlider caliber;
        public MSlider mass;
        public MSlider spread;
        public MSlider explosiveFiller;
        public MSlider explosiveDistance;
        public MSlider explosiveDelay;
        public MSlider proxFuseRadius;
        public MSlider proxFuseDistance;

        public MToggle apCap;
        public MToggle bCap;
        public MToggle triggered;
        public MToggle proxFuse;

        public MMenu mode;

        public override void SafeAwake()
        {
            fire = AddKey("Fire", "fire", KeyCode.C);

            velocity = BlockBehaviour.AddSlider("Velocity", "velocity", 950, 300, 2000, "", "ms");
            caliber = BlockBehaviour.AddSlider("Caliber", "caliber", 45, 5, 200, "", "mm");
            mass = BlockBehaviour.AddSlider("Mass", "mass", 3F, 0.5F, 30F, "", "kg");
            spread = BlockBehaviour.AddSlider("Spread", "spread", 0.5F, 0, 5, "", "°");
            explosiveFiller = BlockBehaviour.AddSlider("Explosive Filler", "explosive-filler", 0, 0, 10F, "", "kg");
            explosiveDistance = BlockBehaviour.AddSlider("Explosive Distance", "explosive-distance", 0, 0, 2F, "", "m");
            explosiveDelay = BlockBehaviour.AddSlider("Fuse Delay", "explosive-delay", 5, 0, 100F, "", "mm");
            proxFuseRadius = BlockBehaviour.AddSlider("Prox. Fuse Radius", "prox-fuse-radius", 2.5F, 0, 5, "", "m");
            proxFuseDistance = BlockBehaviour.AddSlider("Prox. Fuse Distance", "prox-fuse-distance", 5, 0, 20, "", "m");
            
            apCap = AddToggle("AP Cap", "ap-cap", false);
            bCap = AddToggle("B Cap", "b-cap", false);
            proxFuse = AddToggle("Prox. Fuse", "prox-fuse", false);

            proxFuse.Toggled += ProxFuse_Toggled;

            mode = AddMenu("mode", 0, MODES);

            mode.ValueChanged += Mode_ValueChanged; 

            caliber.ValueChanged += Caliber_ValueChanged;
        }

        private void ProxFuse_Toggled(bool isActive)
        {
            proxFuseRadius.DisplayInMapper = isActive;
            proxFuseDistance.DisplayInMapper = isActive;
        }

        private void Start()
        {
            Mode_ValueChanged(mode.Value);

            if (IsSimulating)
            {
                BlockBehaviour.GetComponentInChildren<CapsuleCollider>().isTrigger = true;

                 if (BlockBehaviour.iJointTo != null)
                    foreach (var joint in BlockBehaviour.iJointTo)
                        joint.breakForce = joint.breakTorque = float.PositiveInfinity;
            }
        }
            
        private float PreviewDefaultPenetration(float angle)
        {
            return ArmorHelper.PreviewDefaultPenetration(angle, velocity.Value, mass.Value, explosiveFiller.Value, caliber.Value, apCap.IsActive);
        }

        private float PreviewAPFSDSPenetration(float angle)
        {
            return ArmorHelper.PreviewAPFSDSPenetration(angle, velocity.Value, mass.Value, explosiveFiller.Value, caliber.Value);
        }

        private float PreviewHEPenetration(float angle)
        {
            return ArmorHelper.PreviewHEPenetration(angle, explosiveFiller.Value);
        }

        private float PreviewHEATPenetration(float angle)
        {
            return ArmorHelper.PreviewHEATPenetration(angle, explosiveFiller.Value);
        }

        private void Update()
        {
            if (BlockMapper.CurrentInstance && BlockMapper.CurrentInstance.Block == BlockBehaviour)
            {
                string preview;

                if (mode.Value == (int)Mode.APFSDS)
                    preview = ($"{PreviewAPFSDSPenetration(0)}mm (0°), {PreviewAPFSDSPenetration(30)}mm (30°), {PreviewAPFSDSPenetration(60)}mm (60°)");
                else if (mode.Value == (int)Mode.HESH)
                    preview = ($"{explosiveFiller.Value * Mod.Config.Shells.HESH.PenetrationPerKilo}mm");
                else if (mode.Value == (int)Mode.HE)
                    preview = ($"{PreviewHEPenetration(0)}mm (0°), {PreviewHEPenetration(30)}mm (30°), {PreviewHEPenetration(60)}mm (60°)");
                else if (mode.Value == (int)Mode.HEAT)
                    preview = ($"{PreviewHEATPenetration(0)}mm (0°), {PreviewHEATPenetration(30)}mm (30°), {PreviewHEATPenetration(60)}mm (60°)");
                else
                    preview = ($"{PreviewDefaultPenetration(0)}mm (0°), {PreviewDefaultPenetration(30)}mm (30°), {PreviewDefaultPenetration(60)}mm (60°)");

                BlockMapper.CurrentInstance.SetBlockName(preview);
            }
        }

        private void Caliber_ValueChanged(float value)
        {
            transform.localScale = Vector3.one * value * 0.01F * Mod.Config.Shell.Scale;
        }

        private void Mode_ValueChanged(int value)
        {
            Mode projMode = (Mode)value;

            apCap.DisplayInMapper = true;
            bCap.DisplayInMapper = true;
            explosiveFiller.DisplayInMapper = true;
            explosiveDistance.DisplayInMapper = true;
            explosiveDelay.DisplayInMapper = true;
            proxFuse.DisplayInMapper = false;
            proxFuseRadius.DisplayInMapper = false;
            proxFuseDistance.DisplayInMapper = false;

            if (projMode == Mode.AP)
            {
                explosiveFiller.DisplayInMapper = false;
                explosiveDistance.DisplayInMapper = false;
                explosiveDelay.DisplayInMapper = false;
                SetAssets(Assets.AP);
            }

            if (projMode == Mode.APHE)
            {
                SetAssets(Assets.APHE);
            }

            if (projMode == Mode.HE)
            {
                explosiveDistance.DisplayInMapper = false;
                explosiveDelay.DisplayInMapper = false;
                apCap.DisplayInMapper = false;
                bCap.DisplayInMapper = false;
                proxFuse.DisplayInMapper = true;

                SetAssets(Assets.HE);
            }

            if (projMode == Mode.APFSDS)
            {
                explosiveFiller.DisplayInMapper = false;
                explosiveDistance.DisplayInMapper = false;
                explosiveDelay.DisplayInMapper = false;
                apCap.DisplayInMapper = false;
                bCap.DisplayInMapper = false;
                SetAssets(Assets.APFSDS);
            }

            if (projMode == Mode.HESH)
            {
                explosiveDistance.DisplayInMapper = false;
                explosiveDelay.DisplayInMapper = false;
                apCap.DisplayInMapper = false;
                bCap.DisplayInMapper = false;
                SetAssets(Assets.HESH);
            }

            if (projMode == Mode.HEAT)
            {
                explosiveDistance.DisplayInMapper = false;
                explosiveDelay.DisplayInMapper = false;
                apCap.DisplayInMapper = false;
                bCap.DisplayInMapper = false;
                SetAssets(Assets.HEAT);
            }
        }

        public override void OnSimulateStart()
        {
            Joint joint = GetComponent<Joint>();
            if (joint != null)
                joint.breakForce = joint.breakTorque = float.PositiveInfinity;

            Collider collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
        }

        public override void SimulateUpdateHost()
        {
            if (fire.IsPressed)
                Fire();
        }

        public override void KeyEmulationUpdate()
        {
            if (fire.EmulationPressed())
                Fire();
        }

        private void Fire()
        {
            Mode projMode = (Mode)mode.Value;

            ServerProjectile projectile = Spawner.SpawnSingleProjectile(new ProjectileSpawnSettings()
            {
                position = transform.position + transform.forward,
                color = Color.red,
                cannon = BlockBehaviour
            });

            projectile.body.mass = mass.Value + explosiveFiller.Value;

            projectile.body.velocity = Utilities.RandomSpread(transform.forward * velocity.Value, spread.Value);

            projectile.body.drag = 
                (0.8F + caliber.Value * 0.002F) 
                * (bCap.IsActive ? Mod.Config.BallisticCap.Drag : Mod.Config.Shell.BaseDrag);
            
            projectile.arCap = apCap.IsActive || projMode == Mode.APFSDS;
           
            projectile.timeToLive = Mod.Config.Shell.TimeToLive;

            projectile.accurateRaycasting = true;
            projectile.shell = true;

            projectile.he = projMode == Mode.HE;
            projectile.hesh = projMode == Mode.HESH;
            projectile.heat = projMode == Mode.HEAT;
            projectile.explosive = projMode == Mode.APHE;
            projectile.proxFuse = projMode == Mode.HE && proxFuse.IsActive;
            projectile.proxFuseDistance = proxFuseDistance.Value;
            projectile.proxFuseRadius = proxFuseRadius.Value;

            if (projMode == Mode.AP || projMode == Mode.APHE)
                projectile.armorResistanceFactor = Mod.Config.Shells.AP.ArmorResistanceFactor;

            if (projMode == Mode.APFSDS)
            {
                projectile.caliber = caliber.Value * Mod.Config.Shells.APFSDS.CaliberScale;
                projectile.fsds = true;
            }
            else
                projectile.caliber = caliber.Value;

            if (projMode == Mode.HESH || projMode == Mode.HEAT)
                projectile.explosiveFiller = explosiveFiller.Value;

            if (projMode == Mode.APHE || projMode == Mode.HE)
            {
                projectile.explosiveFiller = explosiveFiller.Value;
                projectile.explosiveDistance = explosiveDistance.Value;
                projectile.explosiveDelay = explosiveDelay.Value;
            }
        }

        public void SetAssets(ShellAssets assets)
        {
            MeshRenderer meshRenderer = BlockBehaviour.MeshRenderer;

            BlockBehaviour.VisualController.MeshFilter.mesh = assets.mesh;
            meshRenderer.material.mainTexture = assets.texture;
            meshRenderer.material.SetFloat("_Glossiness", 0F);
            meshRenderer.material.SetFloat("_Metallic", 0F);
        }
    }
}
