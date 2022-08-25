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
    // Break surface. + ShootingModule
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

            surface.wood.breakable = false;

            ArmorHelper.AddMapperTypes(surface);

            if (block.SimBlock != null && 
                (StatMaster.isHosting || !StatMaster.isMP || StatMaster.isLocalSim))
                block.SimBlock.InternalObject.StartCoroutine(InitBuildSurfaceBody(block));
        }

        IEnumerator InitBuildSurfaceBody(Block block)
        {
            for (int i = 0; i < 3; i++)
                yield return new WaitForFixedUpdate();
            while (block.SimBlock.InternalObject.Rigidbody.isKinematic)
                yield return new WaitForFixedUpdate();

            BuildSurface surface = (BuildSurface)block.InternalObject;

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
