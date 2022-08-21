using System.Collections;
using UnityEngine;

namespace AdvancedCannon
{
    public struct SpawnFragmentsSettings
    {
        public Vector3 position;
        public Vector3 velocity;
        public int count;
        public float cone;
        public float mass;
        public bool bounce;
        public BuildSurface surface;
        public Color color;
        public float timeToLive;
    }
}