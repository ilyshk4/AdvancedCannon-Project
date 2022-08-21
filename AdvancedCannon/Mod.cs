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

            Assets.OnLoad();
            Networking.OnLoad();
            ArmorHelper.OnLoad();

            LoadConfig();

            Object.DontDestroyOnLoad(Helper.Instance);

            ModConsole.RegisterCommand("rsc", (x) => LoadConfigFile(), "Reload shells config.");
        }

        private void LoadConfig()
        {
            if (!ConfigFileExists())
                CreateConfigFile();
            LoadConfigFile();
            if (Config == null)
            {
                if (ConfigFileExists())
                {
                    string previousConfig = Modding.ModIO.ReadAllText(CONFIG_PATH, true);
                    Modding.ModIO.WriteAllText(CONFIG_PATH + ".broken", previousConfig, true);
                    Modding.ModIO.DeleteFile(CONFIG_PATH, true);
                }
                CreateConfigFile();
                LoadConfigFile();
            }
        }

        private void Events_OnBlockInit(Block block)
        {
            if (block.CompareType(BlockType.BuildSurface))
                OnBuildSurfaceInit(block);
        }

        private void OnBuildSurfaceInit(Block block)
        {
            BuildSurface surface = (BuildSurface)block.InternalObject;

            ArmorHelper.AddMapperTypes(surface);

            if (block.SimBlock != null)
                block.SimBlock.InternalObject.StartCoroutine(InitBuildSurfaceBody(block));
        }

        IEnumerator InitBuildSurfaceBody(Block block)
        {
            yield return new WaitForFixedUpdate();

            BuildSurface surface = (BuildSurface)block.InternalObject;
            ArmorHelper.GetSurfaceArmor(surface, out float thickness, out int armorType);

            if (armorType == ArmorHelper.REACTIVE_INDEX)
            {
                armorType = 0;
                thickness = 20;
            }

            float density = ArmorHelper.GetArmorModifier(armorType);

            foreach (var i in block.SimBlock.InternalObject.transform.GetComponentsInChildren<ConfigurableJoint>())
                i.breakForce = i.breakTorque = float.PositiveInfinity;

            BlockHealthBar blockHealth = block.SimBlock.InternalObject.BlockHealth;
            if (blockHealth)
                blockHealth.health = float.PositiveInfinity;

            float size = 0;
            foreach (var collider in block.SimBlock.InternalObject.GetComponentsInChildren<BoxCollider>())
                size += collider.size.x * collider.size.y * collider.size.z;

            block.SimBlock.InternalObject.Rigidbody.mass = size * thickness * Mod.Config.Surface.BaseDensity * density;
        }
    }
}
