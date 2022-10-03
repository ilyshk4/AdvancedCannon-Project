using Modding.Blocks;
using System.Collections;
using UnityEngine;

namespace AdvancedCannon
{
    public class ModNetworkProjectile : NetworkProjectile
    {
        public override void Spawn(uint frame, ushort playerId, byte[] spawnInfo)
        {
            base.Spawn(frame, playerId, spawnInfo);

            int num = 13 + 6;
            int uid = (int)NetworkCompression.ReadUInt(false, spawnInfo, num);
            num += 4;
            NetworkCompression.DecompressVector(spawnInfo, num, 0, 1, out Vector3 rawColor);
            Color color = new Color(rawColor.x, rawColor.y, rawColor.z, 1);
            num += 6;

            BlockBehaviour cannonRef = Networking.ReadBlock(spawnInfo, num);
            num += 7;
            BlockBehaviour surfaceRef = Networking.ReadBlock(spawnInfo, num);

            var meshRenderer = GetComponentInChildren<MeshRenderer>(true);
            var meshFilter = GetComponentInChildren<MeshFilter>(true);

            if (!Networking.HasAuthority)
            {
                RemoteProjectile remote = gameObject.GetComponent<RemoteProjectile>();
                if (remote == null)
                    remote = gameObject.AddComponent<RemoteProjectile>();
                remote.uid = uid;
                remote.vertexCount = 1;
                remote.meshRenderer = meshRenderer;

                if (Mod.TraceVisible)
                {
                    remote.line = Utilities.CreateProjectileLine();
                    remote.line.material.color = color;
                    remote.line.SetVertexCount(remote.vertexCount);
                    remote.line.SetPosition(0, transform.position);
                }

                if (remote && remote.line)
                    Object.Destroy(remote.line.gameObject, Mod.Config.Trace.TimeToLive);

                if (meshRenderer)
                {
                    TracerController tracer = meshRenderer.gameObject.GetComponent<TracerController>();
                    if (tracer == null)
                        tracer = meshRenderer.gameObject.AddComponent<TracerController>();
                    var cannon = cannonRef ? (Cannon)Block.From(cannonRef).BlockScript : null;
                    tracer.ResetCannon(cannon);
                }
            }

            transform.localScale = Vector3.one;
            if (cannonRef != null)
            {
                transform.localScale = cannonRef.transform.localScale;
                meshRenderer.enabled = true;
                meshRenderer.material = cannonRef.MeshRenderer.material;
                meshFilter.sharedMesh = cannonRef.VisualController.MeshFilter.sharedMesh;
            }

            if (surfaceRef != null)
            {
                transform.localScale = Vector3.one * Random.Range(0.85F, 1.15F);
                meshRenderer.enabled = true;
                meshRenderer.sharedMaterial = surfaceRef.MeshRenderer.sharedMaterial;
                meshFilter.sharedMesh = Assets.SpallingMesh;
            }

            if (cannonRef == null && surfaceRef == null)
                meshRenderer.enabled = false;
        }

        public override void Despawn(byte[] despawnInfo)
        {
            base.Despawn(despawnInfo);

            Reset();
        }

        private void Reset()
        {
            var meshRenderer = GetComponentInChildren<MeshRenderer>();
            meshRenderer.enabled = false;
            var whistle = meshRenderer.gameObject.GetComponent<AudioSource>();
            if (whistle)
                whistle.Stop();
        }
    }
}