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
        public Vector3[] positions;
        public LineRenderer line;

        private void OnEnable()
        {
            StartCoroutine(Stop());
        }

        private IEnumerator Stop()
        {
            yield return new WaitForSeconds(2F);
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (line)
                Destroy(line.gameObject, 1F);
        }
    }
}
