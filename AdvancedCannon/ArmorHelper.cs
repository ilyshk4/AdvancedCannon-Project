using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedCannon
{
    public static class ArmorHelper
    {
        private const string ARMOR_THICKNESS_NAME = "Armor Thickness";
        private const string ARMOR_THICKNESS_KEY = "armor-thickness";
        private const string ARMOR_TYPE_KEY = "armor-type";
        public const int REACTIVE_INDEX = 6;

        private static readonly Dictionary<string, float> ArmorTypes = new Dictionary<string, float>()
        {
            { "RHA", 1.00F },
            { "CHA", 0.94F },
            { "HHRA", 1.25F },
            { "Struct. Steel", 0.45F },
            { "Tracks", 0.75F },
            { "Aluminium", 0.25F },
            { "Reactive", 1F },
        };

        private static List<string> ArmorTypesKeys;
        private static List<float> ArmorTypesValues;


        public static void OnLoad()
        {
            ArmorTypesKeys = ArmorTypes.Keys.ToList();
            ArmorTypesValues = ArmorTypes.Values.ToList();
        }
        public static void GetSurfaceArmor(BuildSurface surface, out float thickness, out int type)
        {
            GetMapperTypes(surface, out MSlider armorThickness, out MMenu armorType);
            thickness = armorThickness?.Value ?? 0;
            type = armorType?.Value ?? 0;
        }

        public static float GetSurfaceThickness(BuildSurface surface, float angle)
        {
            GetSurfaceArmor(surface, out float armorThickness, out int armorType);
            return (armorThickness * GetArmorModifier(armorType)) / Mathf.Cos(angle);
        }

        public static float GetArmorModifier(int index)
        {
            if (index < ArmorHelper.ArmorTypesValues.Count)
                return ArmorHelper.ArmorTypesValues[index];
            return 1;
        }

        public static string GetModifierName(int index)
        {
            if (index < ArmorHelper.ArmorTypesKeys.Count)
                return ArmorHelper.ArmorTypesKeys[index];
            return "N/A";
        }

        public static void AddMapperTypes(BuildSurface surface)
        {
            MSlider thickness = surface.AddSlider(ARMOR_THICKNESS_NAME, ArmorHelper.ARMOR_THICKNESS_KEY, 20, 5, 500, "", "mm");
            MMenu type = surface.AddMenu(ArmorHelper.ARMOR_TYPE_KEY, 0, ArmorHelper.ArmorTypesKeys);

            thickness.ValueChanged += (value) =>
            {
                MSlider customMass = (MSlider)surface.GetMapperType("bmt-custom-mass");
                if (customMass != null)
                    if (Input.GetMouseButtonUp(0) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    {
                        customMass.Value = Mathf.Max(customMass.Value, Mod.GetSurfaceMass(surface, value, GetArmorModifier(type.Value)));
                        customMass.ApplyValue();
                    }
            };

            type.ValueChanged += (value) =>
            {
                thickness.DisplayName = value == REACTIVE_INDEX ? "Reactive Efficiency" : ARMOR_THICKNESS_NAME;
            };
        }

        public static float GetHeParticlePenetration(float filler, float distance)
        {
            const float Intersection = 6.598F;

            float value;
            if (filler < Intersection)
                value = 17 * Mathf.Pow(filler, 0.623F);
            else
                value = 44 * Mathf.Pow(filler, 0.116F) + filler * 0.047F;

            value /= Mathf.Pow(distance + 1, 0.38F);

            return value;
        }

        public static float CalculatePenetration(float angle, float velocity, float mass, float caliber, float angleReduce, float armorResistanceFactor = 2200)
        {
            float amplifiedAngle = Mathf.Max(angle - angleReduce * Mathf.Deg2Rad, 0);

            amplifiedAngle = Mathf.Clamp(amplifiedAngle, 0, 90 * Mathf.Deg2Rad);

            float penetration =
                Mathf.Pow(velocity / armorResistanceFactor, 1.43F)
                * (Mathf.Pow(mass, 0.71F) / Mathf.Pow(caliber / 100, 1.07F))
                * Mathf.Pow(Mathf.Cos(amplifiedAngle), 1.4F) * 100;

            return penetration;
        }

        public static float PreviewDefaultPenetration(float angle, float velocity, float mass, float explosiveFiller, float caliber, bool apCap)
        {
            return Mathf.RoundToInt(ArmorHelper.CalculatePenetration(angle * Mathf.Deg2Rad,
                velocity, mass + explosiveFiller, caliber,
                apCap ? Mod.Config.ArmorPiercingCap.AngleReduce : 0, Mod.Config.Shells.AP.ArmorResistanceFactor));
        }

        public static float PreviewAPFSDSPenetration(float angle, float velocity, float mass, float explosiveFiller, float caliber)
        {
            return Mathf.RoundToInt(ArmorHelper.CalculatePenetration(angle * Mathf.Deg2Rad,
                velocity, mass + explosiveFiller, caliber * Mod.Config.Shells.APFSDS.CaliberScale, Mod.Config.Shells.APFSDS.AngleReduce));
        }

        public static float PreviewHEATPenetration(float angle, float explosiveFiller)
        {
            return Mathf.RoundToInt(ArmorHelper.CalculatePenetration(angle * Mathf.Deg2Rad,
                Mod.Config.Shells.HEAT.VelocityPerKilo * explosiveFiller, Mod.Config.Shells.HEAT.FragmentMass, 10, 0));
        }

        public static void SetArmor(BuildSurface surface, int type, int thickness)
        {
            GetMapperTypes(surface, out MSlider thicknessSlider, out MMenu typeMenu);

            thicknessSlider.Value = thickness;
            typeMenu.Value = type;
        }
        
        private static void GetMapperTypes(BuildSurface surface, out MSlider thickness, out MMenu type)
        {
            thickness = (MSlider)surface.GetMapperType($"bmt-{ArmorHelper.ARMOR_THICKNESS_KEY}");
            type = (MMenu)surface.GetMapperType($"bmt-{ArmorHelper.ARMOR_TYPE_KEY}");
        }
    }
}