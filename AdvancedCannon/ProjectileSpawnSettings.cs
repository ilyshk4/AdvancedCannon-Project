using System.Collections;
using UnityEngine;

namespace AdvancedCannon
{
    public struct ProjectileSpawnSettings
    {
        public Vector3 position;
        public Color color;
        public bool invisible;
        public BlockBehaviour cannon;
        public BlockBehaviour surface;
    }
}