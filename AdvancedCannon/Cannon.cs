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
            "AP", "APHE", "HE", "APFSDS"
        };

        enum Mode { AP, APHE, HE, APFSDS };

        public MKey fire;
        public MSlider velocity;
        public MSlider caliber;
        public MSlider mass;
        public MSlider spread;
        public MSlider explosiveFiller;
        public MSlider explosiveDistance;

        public MToggle apCap;
        public MToggle bCap;

        public MMenu mode;

        public override void SafeAwake()
        {
            fire = AddKey("Fire", "fire", KeyCode.C);
            velocity = BlockBehaviour.AddSlider("Velocity", "velocity", 950, 300, 2000, "", "ms");
            caliber = BlockBehaviour.AddSlider("Caliber", "caliber", 45, 20, 200, "", "mm");
            mass = BlockBehaviour.AddSlider("Mass", "mass", 3F, 0.5F, 20F, "", "kg");
            spread = BlockBehaviour.AddSlider("Spread", "spread", 0.5F, 0, 5, "", "°");
            explosiveFiller = BlockBehaviour.AddSlider("Explosive Filler", "explosive-filler", 0, 0, 2F, "", "kg");
            explosiveDistance = BlockBehaviour.AddSlider("Explosive Distance", "explosive-distance", 0, 0, 2F, "", "m");

            apCap = AddToggle("AP Cap", "ap-cap", false);
            bCap = AddToggle("B Cap", "b-cap", false);
            mode = AddMenu("mode", 0, MODES);

            mode.ValueChanged += Mode_ValueChanged;
            Mode_ValueChanged(0);
        }

        private void Mode_ValueChanged(int value)
        {
            Mode projMode = (Mode)value;

            apCap.DisplayInMapper = true;
            bCap.DisplayInMapper = true;
            explosiveFiller.DisplayInMapper = true;
            explosiveDistance.DisplayInMapper = true;

            if (projMode == Mode.AP)
            {
                explosiveFiller.DisplayInMapper = false;
                explosiveDistance.DisplayInMapper = false;
            }

            if (projMode == Mode.APHE)
            {

            }

            if (projMode == Mode.HE)
            {

            }

            if (projMode == Mode.APFSDS)
            {
                explosiveFiller.DisplayInMapper = false;
                explosiveDistance.DisplayInMapper = false;
                apCap.DisplayInMapper = false;
                bCap.DisplayInMapper = false;
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

            Projectile projectile = Mod.SpawnProjectile(transform.position + transform.forward, Color.red);

            projectile.body.mass = mass.Value + explosiveFiller.Value;

            projectile.body.velocity = Mod.RandomSpread(transform.forward * velocity.Value, spread.Value);
            projectile.body.drag = apCap.IsActive ? Mod.Config.ArmorPiercingCapDrag : bCap.IsActive ? Mod.Config.BallisticCapDrag : Mod.Config.BaseProjectileDrag;
            projectile.arCap = apCap.IsActive;
            projectile.ballisticCap = bCap.IsActive;
            projectile.highExplosive = projMode == Mode.HE;
            projectile.timeToLive = Mod.Config.ShellTimeToLive;
            projectile.accurateRaycasting = true;
            projectile.shell = true;
            projectile.fsds = projMode == Mode.APFSDS;

            if (projMode == Mode.APFSDS)
                projectile.caliber = caliber.Value / 4;
            else
                projectile.caliber = caliber.Value;

            if (projMode == Mode.APHE || projMode == Mode.HE)
            {
                projectile.explosiveFiller = explosiveFiller.Value;
                projectile.explosiveDistance = explosiveDistance.Value;
            }
        }
    }
}
