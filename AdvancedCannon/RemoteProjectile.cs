using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AdvancedCannon
{
    public class RemoteProjectile : MonoBehaviour
    {
        public int uid;
        public int vertexCount;
        public LineRenderer line;

        private Vector3 _lastPosition;

        private void Update()
        {
            if (line) line.enabled = Mod.TraceVisible;
        }

        private void FixedUpdate()
        {
            Vector3 direction = transform.position - _lastPosition;
            transform.rotation = Quaternion.LookRotation(direction + Vector3.one * 0.01F);
            _lastPosition = transform.position;
        }
    }
}
