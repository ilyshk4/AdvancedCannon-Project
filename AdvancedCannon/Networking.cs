using Modding;
using Modding.Blocks;
using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace AdvancedCannon
{
    public static class Networking
    {
        public static MessageType SetupRemoteProjectile;
        public static MessageType AddRemotePoint;
        public static MessageType NuclearExplosionEffect;

        public static void OnLoad()
        {
            SetupRemoteProjectile = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Vector3, DataType.Vector3, DataType.Block, DataType.Block);
            AddRemotePoint = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Vector3, DataType.Integer);
            NuclearExplosionEffect = ModNetworking.CreateMessageType();

            ModNetworking.MessageReceived += ModNetworking_MessageReceived;
        }

        private static void ModNetworking_MessageReceived(Message msg)
        {
            if (msg.Type == SetupRemoteProjectile)
                OnSetupRemoteProjectile(msg);

            if (msg.Type == AddRemotePoint)
                OnAddRemotePoint(msg);

            if (msg.Type == NuclearExplosionEffect)
                OnSpawnNuclearExplosion(msg);
        }
        private static void OnSpawnNuclearExplosion(Message msg)
        {
            Warhead.SpawnNuclearExplosionEffect();
        }

        private static void OnAddRemotePoint(Message msg)
        {
            int id = (int)msg.GetData(0);
            int uid = (int)msg.GetData(1);
            Vector3 point = (Vector3)msg.GetData(2);
            int count = (int)msg.GetData(3);

            Helper.Instance
                .StartCoroutine(TryFor(() => RemoteAddPoint(id, uid, point, count), 3));
        }

        private static void OnSetupRemoteProjectile(Message msg)
        {
            int id = (int)msg.GetData(0);
            int uid = (int)msg.GetData(1);
            Vector3 origin = (Vector3)msg.GetData(2);
            Color color = (Vector4)(Vector3)msg.GetData(3);
            Block cannonRef = (Block)msg.GetData(4);
            Block surfaceRef = (Block)msg.GetData(5);

            color.a = 1F;

            Helper.Instance
                .StartCoroutine(TryFor(() => RemoteSetupProjectile(id, uid, origin, color, cannonRef, surfaceRef), 3));
        }
        private static bool RemoteAddPoint(int id, int uid, Vector3 point, int count)
        {
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
                    return true;
                }
            }

            return false;
        }

        private static bool RemoteSetupProjectile(int id, int uid, Vector3 origin, Color lineColor, Block cannonRef, Block surfaceRef)
        {
            var projectile = GetProjectile(id);

            if (projectile)
            {
                RemoteProjectile remote = projectile.gameObject.GetComponent<RemoteProjectile>();
                if (remote == null)
                    remote = projectile.gameObject.AddComponent<RemoteProjectile>();
                remote.uid = uid;
                remote.vertexCount = 1;
                remote.line = Utilities.CreateProjectileLine();
                remote.line.material.color = lineColor;
                remote.line.SetVertexCount(remote.vertexCount);
                remote.line.SetPosition(0, origin);

                var meshRenderer = projectile.GetComponentInChildren<MeshRenderer>();
                var meshFilter = projectile.GetComponentInChildren<MeshFilter>();

                projectile.transform.localScale = Vector3.one;
                if (cannonRef != null)
                {
                    projectile.transform.localScale = cannonRef.InternalObject.transform.localScale;
                    meshRenderer.enabled = true;
                    meshRenderer.material = cannonRef.InternalObject.MeshRenderer.material;
                    meshFilter.sharedMesh = cannonRef.InternalObject.VisualController.MeshFilter.sharedMesh;
                }

                if (surfaceRef != null)
                {
                    projectile.transform.localScale = Vector3.one * Random.Range(0.85F, 1.15F);
                    meshRenderer.enabled = true;
                    meshRenderer.sharedMaterial = surfaceRef.InternalObject.MeshRenderer.sharedMaterial;
                    meshFilter.sharedMesh = Assets.SpallingMesh;
                }

                if (cannonRef == null && surfaceRef == null)
                    meshRenderer.enabled = false;

                Object.Destroy(remote.line.gameObject, Mod.Config.Trace.TimeToLive);
                return true;
            }

            return false;
        }

        public static void SpawnNuclearExplosionEffect()
        {
            ModNetworking.SendToAll(NuclearExplosionEffect.CreateMessage());
        }

        public static void CreateServerProjectile(ServerProjectile projectile)
        {
            ModNetworking.SendToAll(Networking.SetupRemoteProjectile.CreateMessage(
                (int)projectile.network.id, projectile.uid, projectile.transform.position, 
                (Vector3)(Vector4)projectile.line.material.color, projectile.cannon, projectile.surface));
        }

        private static IEnumerator TryFor(Func<bool> func, float maxTime)
        {
            const float step = 0.1F;
            float time = 0;

            while (true)
            {
                time += step;
                if (time >= maxTime)
                    break;

                if (func())
                    break;

                yield return new WaitForSecondsRealtime(step);
            }
        }
        private static NetworkProjectile GetProjectile(int id)
        {
            if (!ProjectileManager.Instance)
                return null;

            foreach (var item in ProjectileManager.Instance.GetPool(Helper.Instance.ProjectileId).Active)
                if (item.id == id)
                    return item;
            return null;
        }

        public static void AddTracePoint(ServerProjectile projectile, Vector3 point)
        {
            ModNetworking.SendToAll(AddRemotePoint.CreateMessage((int)projectile.network.id, projectile.uid, point, projectile.TracePointsCount));
        }
    }
}