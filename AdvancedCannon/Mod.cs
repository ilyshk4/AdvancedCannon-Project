using Modding;
using Modding.Blocks;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

namespace AdvancedCannon
{
    public class Mod : ModEntryPoint
    {
        public static readonly string CONFIG_PATH = "Config.xml";
        public static Config Config = new Config();

        public static bool TraceVisible = true;

        private bool ConfigFileExists() => Modding.ModIO.ExistsFile(CONFIG_PATH, true);
        private void CreateConfigFile() => Modding.ModIO.SerializeXml(new Config(), CONFIG_PATH, true);
        private void LoadConfigFile() => Config = Modding.ModIO.DeserializeXml<Config>(CONFIG_PATH, true);

        public override void OnLoad()
		{
            Events.OnBlockInit += Events_OnBlockInit;
            Events.OnSimulationToggle += Events_OnSimulationToggle;

            Assets.OnLoad();
            Networking.OnLoad();
            ArmorHelper.OnLoad();

            LoadConfig();

            Object.DontDestroyOnLoad(Helper.Instance);

            ModConsole.RegisterCommand("rsc", (x) => LoadConfigFile(), "Reload shells config.");
        }

        private void Events_OnSimulationToggle(bool obj)
        {
            Spawner.ClearSpawnQueue();
        }

        private void LoadConfig()
        {
            if (!ConfigFileExists())
                CreateConfigFile();
            LoadConfigFile();
            if (Config == null || Config.Version != Config.ModVersion)
            {
                Debug.Log("Besoig++ config is nonexistent or outdated. Making new.");
                if (ConfigFileExists())
                {
                    string previousConfig = Modding.ModIO.ReadAllText(CONFIG_PATH, true);
                    Modding.ModIO.WriteAllText(CONFIG_PATH + ".broken", previousConfig, true);
                    Modding.ModIO.DeleteFile(CONFIG_PATH, true);
                    Debug.Log("Renamed old config to Config.xml.broken");
                }
                CreateConfigFile();
                LoadConfigFile();
            }
        }

        private void Events_OnBlockInit(Block block)
        {
            if (block.CompareType(BlockType.BuildSurface))
                OnBuildSurfaceInit(block);

            if (block.InternalObject is CogMotorControllerHinge hinge && hinge != null)
                OnHingeInit(hinge);
        }

        private void OnHingeInit(CogMotorControllerHinge hinge)
        {
            hinge.AddToggle("invincible", "Invincible", false).Toggled += value =>
            {
                if (hinge.isSimulating && value)
                {
                    if (hinge.BlockHealth)
                        hinge.BlockHealth.health = float.PositiveInfinity;
                    if (hinge.blockJoint)
                        hinge.blockJoint.breakForce = hinge.blockJoint.breakTorque = float.PositiveInfinity;
                    if (hinge.fireTag)
                        hinge.fireTag.hasBeenBurned = true;
                }
            };
        }

        private void OnBuildSurfaceInit(Block block)
        {
            BuildSurface surface = (BuildSurface)block.InternalObject;

            ArmorHelper.AddMapperTypes(surface);

            if (block.SimBlock != null && Networking.HasAuthority)
                block.SimBlock.InternalObject.StartCoroutine(InitBuildSurfaceBody(block));
        }

        IEnumerator InitBuildSurfaceBody(Block block)
        {
            for (int i = 0; i < 3; i++)
                yield return new WaitForFixedUpdate();
            while (block.SimBlock.InternalObject.Rigidbody.isKinematic)
                yield return new WaitForFixedUpdate();

            BuildSurface surface = (BuildSurface)block.InternalObject;

            var vis = surface.VisualController as SurfaceVisualController;

            if (vis)
                vis.breakIntoPieces = false;

            ArmorHelper.GetSurfaceArmor(surface, out float thickness, out int armorType);

            if (armorType == ArmorHelper.REACTIVE_INDEX)
            {
                armorType = 0;
                thickness = 20;
            }

            float density = ArmorHelper.GetArmorModifier(armorType);

            Rigidbody body = block.SimBlock.InternalObject.Rigidbody;

            body.mass = Mathf.Max(GetSurfaceMass(block.InternalObject, thickness, density), body.mass);
        }

        public static float GetSurfaceMass(BlockBehaviour block, float thickness, float density)
        {
            float size = 0;
            foreach (var collider in block.transform.Find("SimColliders").GetComponentsInChildren<BoxCollider>(true))
                size += collider.size.x * collider.size.y * collider.size.z;
            return size * thickness * Mod.Config.Surface.BaseDensity * density;
        }
    }
}
