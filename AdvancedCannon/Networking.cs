using Modding;
using Modding.Blocks;
using Modding.Common;
using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace AdvancedCannon
{
    public static class Networking
    {
        public static MessageType AddRemotePoint;
        public static MessageType NuclearExplosionEffect;
        public static MessageType FXBlockFire;
        public static MessageType SpawnSFX;
        public static MessageType SpawnVFX;
        public static bool HasAuthority => StatMaster.isHosting || !StatMaster.isMP || StatMaster.isLocalSim;

        public static void OnLoad()
        {
            AddRemotePoint = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Vector3, DataType.Integer);
            NuclearExplosionEffect = ModNetworking.CreateMessageType();
            FXBlockFire = ModNetworking.CreateMessageType(DataType.Block);
            SpawnSFX = ModNetworking.CreateMessageType(DataType.Integer, DataType.Vector3);
            SpawnVFX = ModNetworking.CreateMessageType(DataType.Integer, DataType.Vector3, DataType.Vector3, DataType.Single);

            ModNetworking.MessageReceived += ModNetworking_MessageReceived;
        }

        private static void ModNetworking_MessageReceived(Message msg)
        {
            if (msg.Type == AddRemotePoint)
                OnAddRemotePoint(msg);

            if (msg.Type == NuclearExplosionEffect)
                OnSpawnNuclearExplosion(msg);

            if (msg.Type == FXBlockFire)
                OnFXBlockFire(msg);

            if (msg.Type == SpawnSFX)
                OnSpawnSFX(msg);

            if (msg.Type == SpawnVFX)
                OnSpawnVFX(msg);
        }

        private static void OnSpawnVFX(Message msg)
        {
            VFXType type = (VFXType)(int)msg.GetData(0);
            Vector3 position = (Vector3)msg.GetData(1);
            Quaternion rotation = Quaternion.Euler((Vector3)msg.GetData(2));
            float power = (float)msg.GetData(3);

            EffectsSpawner.ClientSpawnVFX(type, position, rotation, power);
        }

        private static void OnSpawnSFX(Message msg)
        {
            SFXType type = (SFXType)(int)msg.GetData(0);
            Vector3 position = (Vector3)msg.GetData(1);

            EffectsSpawner.ClientSpawnSFX(type, position);
        }

        private static void OnFXBlockFire(Message msg)
        {
            Block block = (Block)msg.GetData(0);
            FXBlock fx = (FXBlock)block.BlockScript;
            fx.Fire();

        }

        public static int WriteBlock(BlockBehaviour blockBehaviour, byte[] array2, int num2)
        {
            byte b2 = (byte)((!(blockBehaviour == null)) ? ((!blockBehaviour.isBuildBlock) ? 0 : 1) : 0);
            ushort val2 = (!(blockBehaviour == null)) ? blockBehaviour.ParentMachine.PlayerID : ushort.MaxValue;
            int val3 = (!(blockBehaviour == null)) ? blockBehaviour.BuildIndex : -1;
            array2[num2] = b2;
            num2++;
            NetworkCompression.WriteUInt16(val2, array2, num2);
            num2 += 2;
            NetworkCompression.WriteUInt((uint)val3, false, array2, num2);
            num2 += 4;
            return num2;
        }

        public static BlockBehaviour ReadBlock(byte[] buffer, int num)
        {
            bool flag2 = buffer[num] == 1;
            num++;
            ushort networkId2 = NetworkCompression.ReadUInt16(buffer, num);
            num += 2;
            int num4 = (int)NetworkCompression.ReadUInt(false, buffer, num);
            num += 4;
            if (num4 == -1)
            {
                return null;
            }
            else
            {
                Player player2 = Player.From(networkId2);
                if (player2 == null || player2.IsSpectator)
                {
                    return null;
                }
                else
                {
                    PlayerMachine machine = player2.Machine;
                    BlockBehaviour blockBehaviour;

                    if (machine == null || machine.InternalObject == null)
                        return null;

                    if (!machine.InternalObject.GetBlockFromIndex(num4, out blockBehaviour) || blockBehaviour == null)
                    {
                        return null;
                    }
                    else
                    {
                        Block block = Block.From(blockBehaviour);
                        if (block == null)
                            return null;
                        return ((!flag2) ? block.SimBlock : block.BuildingBlock)?.InternalObject;
                    }
                }
            }
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
                .StartCoroutine(TryFor(() => RemoteAddPoint(id, uid, point, count), 10));
        }

        public static void SpawnFXBlockFire(FXBlock block)
        {
            ModNetworking.SendToAll(FXBlockFire.CreateMessage(Block.From(block.BlockBehaviour)));
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
        public static void SpawnNuclearExplosionEffect()
        {
            ModNetworking.SendToAll(NuclearExplosionEffect.CreateMessage());
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

            for (int i = 0; i < Helper.POOLS_COUNT; i++)
                foreach (var item in ProjectileManager.Instance.GetPool(Helper.Instance.BaseProjectileId + i).Active)
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