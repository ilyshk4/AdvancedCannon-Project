using Modding;
using Modding.Blocks;
using Modding.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

namespace AdvancedCannon
{
    public class Mod : ModEntryPoint
    {
        public static MessageType SetupRemoteProjectile;
        public static MessageType AddRemotePoint;

        public static readonly string CONFIG_PATH = "Config.xml";

        public static Config Config = new Config();
        public static Material Trace;

        public const string ARMOR_THICKNESS = "armor-thickness";
        public const string ARMOR_TYPE = "armor-type";

        private static Dictionary<string, float> ArmorTypes = new Dictionary<string, float>()
        {
            { "RHA", 1.00F },
            { "CHA", 0.94F },
            { "HHRA", 1.25F },
            { "Struct. Steel", 0.45F },
            { "Tracks", 0.75F },
            { "Aluminium", 0.25F },
        };

        public static List<string> ArmorTypesKeys;
        public static List<float> ArmorTypesValues;

        public static GameObject Empty;

        private static int _uidCounter;

        public override void OnLoad()
		{
            Events.OnBlockInit += Events_OnBlockInit;

            SetupRemoteProjectile = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Vector3, DataType.Vector3);
            AddRemotePoint = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Vector3, DataType.Integer);

            ModNetworking.MessageReceived += ModNetworking_MessageReceived;

            Trace = new Material(Shader.Find("Unlit/Color"));
            Trace.color = Color.red;
                
            if (!ConfigExists())
                CreateConfig();
            try
            {
                LoadConfig();
            } catch (Exception exception)
            {
                if (ConfigExists())
                    Modding.ModIO.DeleteFile(CONFIG_PATH);
                CreateConfig();
                LoadConfig();
            }

            ArmorTypesKeys = ArmorTypes.Keys.ToList();
            ArmorTypesValues = ArmorTypes.Values.ToList();

            Empty = new GameObject("Empty");

            Object.DontDestroyOnLoad(AdvancedCannonHelper.Instance);
            Object.DontDestroyOnLoad(Empty);
        }
        private bool ConfigExists() => Modding.ModIO.ExistsFile(CONFIG_PATH);
        private void CreateConfig() => Modding.ModIO.SerializeXml(new Config(), CONFIG_PATH);
        private void LoadConfig() => Config = Modding.ModIO.DeserializeXml<Config>(CONFIG_PATH);

        private void Events_OnBlockInit(Block block)
        {
            if ((BlockType)block.Prefab.InternalObject.ID == BlockType.BuildSurface)
            {
                BuildSurface surface = (BuildSurface)block.InternalObject;

                surface.AddSlider("Armor Thickness", ARMOR_THICKNESS, 20, 0, 300, "", "mm");
                surface.AddMenu(ARMOR_TYPE, 0, ArmorTypesKeys);
            }
        }
        private void ModNetworking_MessageReceived(Message msg)
        {
            if (msg.Type == SetupRemoteProjectile)
                OnSetupRemoteProjectile(msg);

            if (msg.Type == AddRemotePoint)
                OnAddRemotePoint(msg);
        }

        private void OnAddRemotePoint(Message msg)
        {
            int id = (int)msg.GetData(0);
            int uid = (int)msg.GetData(1);
            Vector3 point = (Vector3)msg.GetData(2);
            int count = (int)msg.GetData(3);

            AdvancedCannonHelper.Instance.StartCoroutine(TryAddPoint(id, uid, point, count));
        }

        private void OnSetupRemoteProjectile(Message msg)
        {
            int id = (int)msg.GetData(0);
            int uid = (int)msg.GetData(1);
            Vector3 origin = (Vector3)msg.GetData(2);
            Color color = (Vector4)(Vector3)msg.GetData(3);
            color.a = 1F;
            AdvancedCannonHelper.Instance.StartCoroutine(TrySetupProjectile(id, uid, origin, color));
        }

        IEnumerator TryAddPoint(int id, int uid, Vector3 point, int count)
        {
            int ticks = 0;

            while (true)
            {
                ticks += 1;
                if (ticks >= 500)
                    break;

                var projectile = GetProjectile(id);

                if (projectile && projectile.gameObject)
                {
                    RemoteProjectile remote = projectile.gameObject.GetComponent<RemoteProjectile>();
                    if (remote && remote.line && remote.uid == uid)
                    {
                        int previousCount = remote.vertexCount;
                        remote.vertexCount = Mathf.Max(remote.vertexCount, count);
                        remote.line.SetVertexCount(remote.vertexCount);
                        remote.line.SetPosition(count - 1, point);
                        for (int i = previousCount; i < count; i++)
                            remote.line.SetPosition(i, point);
                        break;
                    }
                }

                yield return new WaitForFixedUpdate();
            }
        }

        IEnumerator TrySetupProjectile(int id, int uid, Vector3 origin, Color lineColor)
        {
            int ticks = 0;

            while (true)
            {
                ticks += 1;
                if (ticks >= 500)
                    break;

                var projectile = GetProjectile(id);

                if (projectile)
                {
                    RemoteProjectile remote = projectile.gameObject.GetComponent<RemoteProjectile>();
                    if (remote == null)
                        remote = projectile.gameObject.AddComponent<RemoteProjectile>();
                    remote.uid = uid;
                    remote.vertexCount = 1;
                    remote.line = CreateProjectileLine();
                    remote.line.material.color = lineColor;
                    remote.line.SetVertexCount(remote.vertexCount);
                    remote.line.SetPosition(0, origin);
                    break;
                }

                yield return new WaitForFixedUpdate();
            }
        }

        public static NetworkProjectile GetProjectile(int id)
        {
            if (!ProjectileManager.Instance)
                return null;

            foreach (var item in ProjectileManager.Instance.GetPool(0).Active)
                if (item.id == id)
                    return item;
            return null;
        }

        public static Projectile SpawnProjectile(Vector3 position, Color lineColor, bool visible = true)
        {
            Transform cannonball;
            if (ProjectileManager.Instance)
            {
                byte[] array = new byte[13];
                int num = 0;
                NetworkCompression.CompressPosition(position, array, num);
                num += 6;
                NetworkCompression.CompressRotation(Quaternion.identity, array, num);
                NetworkAddPiece instance = NetworkAddPiece.Instance;
                Transform transform = ProjectileManager.Instance.Spawn(NetworkProjectileType.Cannon, instance.frame, Machine.Active().PlayerID, array);
                cannonball = transform;
            }
            else
            {
                cannonball = GamePrefabs.InstantiateProjectile(GamePrefabs.ProjectileType.CannonBall, position, Quaternion.identity, ReferenceMaster.physicsGoalInstance).transform;
            }

            Projectile oldProjectile = cannonball.GetComponent<Projectile>();
            if (oldProjectile)
                Object.Destroy(oldProjectile);

            SphereCollider collider = cannonball.GetComponent<SphereCollider>();
            if (collider != null)
                collider.isTrigger = true;

            MeshRenderer meshRenderer = cannonball.GetComponentInChildren<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.enabled = visible;

            GameObject obj = cannonball.gameObject;
            obj.transform.parent = ReferenceMaster.physicsGoalInstance;
            obj.transform.position = position;

            LineRenderer line = CreateProjectileLine();
            line.material.color = lineColor;

            Rigidbody rigidbody = obj.GetComponent<Rigidbody>();

            NetworkCannonball network = obj.GetComponent<NetworkCannonball>();

            Projectile projectile = obj.AddComponent<Projectile>();
            projectile.body = rigidbody;
            projectile.line = line;
            projectile.network = network;
            projectile.AddPoint(position);

            projectile.uid = _uidCounter++;

            if (ProjectileManager.Instance)
            {
                ModNetworking.SendToAll(SetupRemoteProjectile.CreateMessage((int)network.id, projectile.uid, position, (Vector3)(Vector4)lineColor));
            }

            return projectile;
        }

        public static LineRenderer CreateProjectileLine()
        {
            GameObject lineObj = new GameObject("Line");
            lineObj.transform.parent = ReferenceMaster.physicsGoalInstance;

            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = Trace;
            line.receiveShadows = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.SetWidth(0.02F, 0.02F);

            return line;
        }

        public static void GetSurfaceArmor(BuildSurface surface, out float thickness, out int type)
        {
            MSlider armorThickness = (MSlider)surface.GetMapperType($"bmt-{Mod.ARMOR_THICKNESS}");
            MMenu armorType = (MMenu)surface.GetMapperType($"bmt-{Mod.ARMOR_TYPE}");
            thickness = armorThickness.Value;
            type = armorType.Value;
        }

        public static float GetSurfaceThickness(BuildSurface surface, float angle)
        {
            GetSurfaceArmor(surface, out float armorThickness, out int armorType);
            return (armorThickness * GetArmorModifier(armorType)) / Mathf.Cos(angle);
        }

        public static float GetArmorModifier(int index)
        {
            if (index < ArmorTypesValues.Count)
                return ArmorTypesValues[index];
            return 1;
        }

        public static Vector3 RandomSpread(Vector3 direction, float spread)
        {
            Vector2 offset = Random.insideUnitCircle;

            return Quaternion.AngleAxis(offset.x * Random.Range(-spread, spread), Vector3.right)
                * Quaternion.AngleAxis(offset.y * Random.Range(-spread, spread), Vector3.up)
                * direction;
        }
    }
}
