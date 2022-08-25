using System.Collections;
using UnityEngine;

namespace AdvancedCannon
{
    public class ModNetworkProjectile : NetworkProjectile
    {
        public override void Spawn(uint frame, ushort playerId, byte[] spawnInfo)
        {
            base.Spawn(frame, playerId, spawnInfo);

            Reset();

        }

        public override void Despawn(byte[] despawnInfo)
        {
            base.Despawn(despawnInfo);

            Reset();
        }

        private void Reset()
        {
            GetComponentInChildren<MeshRenderer>().enabled = false;
        }
    }
}