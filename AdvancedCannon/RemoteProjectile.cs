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
        public MeshRenderer meshRenderer;

        private Vector3 _direction;
        private Vector3 _lastPosition;
        private float _moved;

        private void FixedUpdate()
        {
            if (transform.position != _lastPosition)
            {
                _direction = transform.position - _lastPosition;
                _moved = 0.1F;
                _lastPosition = transform.position;
            }
        }

        private void Update()
        {
            _moved -= Time.deltaTime;
            meshRenderer.gameObject.SetActive(_moved > 0);
            transform.rotation = Quaternion.LookRotation(_direction + Vector3.one * 0.01F);
        }
    }
}
