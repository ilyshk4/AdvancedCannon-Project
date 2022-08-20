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

        public MToggle apCap;
        public MToggle bCap;
        public MToggle triggered;

        public MMenu mode;

        public override void SafeAwake()
        {
            fire = AddKey("Fire", "fire", KeyCode.C);
            velocity = BlockBehaviour.AddSlider("Velocity", "velocity", 950, 300, 2000, "", "ms");
            caliber = BlockBehaviour.AddSlider("Caliber", "caliber", 45, 20, 200, "", "mm");
            mass = BlockBehaviour.AddSlider("Mass", "mass", 3F, 0.5F, 30F, "", "kg");
            spread = BlockBehaviour.AddSlider("Spread", "spread", 0.5F, 0, 5, "", "°");
            explosiveFiller = BlockBehaviour.AddSlider("Explosive Filler", "explosive-filler", 0, 0, 2F, "", "kg");
            explosiveDistance = BlockBehaviour.AddSlider("Explosive Distance", "explosive-distance", 0, 0, 2F, "", "m");
            explosiveDelay = BlockBehaviour.AddSlider("Fuse Delay", "explosive-delay", 5, 0, 100F, "", "mm");
            
            apCap = AddToggle("AP Cap", "ap-cap", false);
            bCap = AddToggle("B Cap", "b-cap", false);

            mode = AddMenu("mode", 0, MODES);

            mode.ValueChanged += Mode_ValueChanged; 

            caliber.ValueChanged += Caliber_ValueChanged;
        }

        private void Start()
        {
            Mode_ValueChanged(mode.Value);
        }

        private float GetDefaultPenetration(float angle)
        {
            return Mathf.RoundToInt(ServerProjectile.CalculatePenetration(angle * Mathf.Deg2Rad, 
                velocity.Value, mass.Value + explosiveFiller.Value, caliber.Value, apCap.IsActive, false, Mod.Config.Shells.AP.ArmorResistanceFactor));
        }

        private float GetAPFSDSPenetration(float angle)
        {
            return Mathf.RoundToInt(ServerProjectile.CalculatePenetration(angle * Mathf.Deg2Rad, 
                velocity.Value, mass.Value + explosiveFiller.Value, caliber.Value * Mod.Config.Shells.APFSDS.CaliberScale, true, true));
        }
        private float GetHEPenetration(float angle)
        {
            return Mathf.RoundToInt(ServerProjectile.CalculatePenetration(angle * Mathf.Deg2Rad, 
                Mod.Config.Shells.HE.BaseVelocity + explosiveFiller.Value * Mod.Config.Shells.HE.VelocityPerKilo, 
                Mod.Config.Shells.HE.FragmentMass, Mod.Config.Shells.HE.FragmentCaliber, false, false));
        }
        private float GetHEATPenetration(float angle)
        {   
            return Mathf.RoundToInt(ServerProjectile.CalculatePenetration(angle * Mathf.Deg2Rad, 
                Mod.Config.Shells.HEAT.VelocityPerKilo * explosiveFiller.Value, Mod.Config.Shells.HEAT.FragmentMass, 10, false, false));
        }

        private void Update()
        {
            if (BlockMapper.CurrentInstance && BlockMapper.CurrentInstance.Block == BlockBehaviour)
            {
                if (mode.Value == (int)Mode.APFSDS)
                {
                    BlockMapper.CurrentInstance.SetBlockName($"{GetAPFSDSPenetration(0)}mm (0°), {GetAPFSDSPenetration(30)}mm (30°), {GetAPFSDSPenetration(60)}mm (60°)");
                }
                else if (mode.Value == (int)Mode.HESH)
                {
                    BlockMapper.CurrentInstance.SetBlockName($"{explosiveFiller.Value * Mod.Config.Shells.HESH.PenetrationPerKilo}mm");
                }
                else if (mode.Value == (int)Mode.HE)
                {
                    BlockMapper.CurrentInstance.SetBlockName($"{GetHEPenetration(0)}mm (0°), {GetHEPenetration(30)}mm (30°), {GetHEPenetration(60)}mm (60°)");
                }
                else if (mode.Value == (int)Mode.HEAT)
                {
                    BlockMapper.CurrentInstance.SetBlockName($"{GetHEATPenetration(0)}mm (0°), {GetHEATPenetration(30)}mm (30°), {GetHEATPenetration(60)}mm (60°)");
                }
                else
                {
                    BlockMapper.CurrentInstance.SetBlockName($"{GetDefaultPenetration(0)}mm (0°), {GetDefaultPenetration(30)}mm (30°), {GetDefaultPenetration(60)}mm (60°)");
                }
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

            if (projMode == Mode.AP)
            {
                explosiveFiller.DisplayInMapper = false;
                explosiveDistance.DisplayInMapper = false;
                explosiveDelay.DisplayInMapper = false;
                SetAssets(Mod.AP);
            }

            if (projMode == Mode.APHE)
            {
                SetAssets(Mod.APHE);
            }

            if (projMode == Mode.HE)
            {
                explosiveDistance.DisplayInMapper = false;
                explosiveDelay.DisplayInMapper = false;
                apCap.DisplayInMapper = false;
                bCap.DisplayInMapper = false;
                SetAssets(Mod.HE);
            }

            if (projMode == Mode.APFSDS)
            {
                explosiveFiller.DisplayInMapper = false;
                explosiveDistance.DisplayInMapper = false;
                explosiveDelay.DisplayInMapper = false;
                apCap.DisplayInMapper = false;
                bCap.DisplayInMapper = false;
                SetAssets(Mod.APFSDS);
            }

            if (projMode == Mode.HESH)
            {
                explosiveDistance.DisplayInMapper = false;
                explosiveDelay.DisplayInMapper = false;
                apCap.DisplayInMapper = false;
                bCap.DisplayInMapper = false;
                SetAssets(Mod.HESH);
            }

            if (projMode == Mode.HEAT)
            {
                explosiveDistance.DisplayInMapper = false;
                explosiveDelay.DisplayInMapper = false;
                apCap.DisplayInMapper = false;
                bCap.DisplayInMapper = false;
                SetAssets(Mod.HEAT);
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

            ServerProjectile projectile = Mod.SpawnProjectile(transform.position + transform.forward, Color.red, true, BlockBehaviour);

            projectile.body.mass = mass.Value + explosiveFiller.Value;

            projectile.body.velocity = Mod.RandomSpread(transform.forward * velocity.Value, spread.Value);

            projectile.body.drag = 
                (0.8F + caliber.Value * 0.002F) 
                * (bCap.IsActive ? Mod.Config.BallisticCap.Drag : Mod.Config.Shell.BaseDrag);
            
            projectile.arCap = apCap.IsActive || projMode == Mode.APFSDS;
            projectile.ballisticCap = bCap.IsActive || projMode == Mode.APFSDS;
           
            projectile.timeToLive = Mod.Config.Shell.TimeToLive;

            projectile.accurateRaycasting = true;
            projectile.shell = true;

            projectile.highExplosive = projMode == Mode.HE;
            projectile.hesh = projMode == Mode.HESH;
            projectile.heat = projMode == Mode.HEAT;
            projectile.explosive = projMode == Mode.APHE;

            if (projMode == Mode.AP || projMode == Mode.APHE)
                projectile.velocityDivider = Mod.Config.Shells.AP.ArmorResistanceFactor;

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
            BlockBehaviour.VisualController.MeshFilter.mesh = assets.mesh;
            BlockBehaviour.MeshRenderer.material.mainTexture = assets.texture;
            BlockBehaviour.MeshRenderer.material.SetFloat("_Glossiness", 0F);
            BlockBehaviour.MeshRenderer.material.SetFloat("_Metallic", 0F);
        }
    }
}
