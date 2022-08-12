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

        private void FixedUpdate()
        {
            Vector3 direction = _lastPosition - transform.position;
            transform.rotation = Quaternion.LookRotation(direction);
            _lastPosition = transform.position;
        }
    }
}
