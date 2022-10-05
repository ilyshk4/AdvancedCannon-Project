using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AdvancedCannon
{
    public class TracerController : MonoBehaviour
    {
        const float MIN = 0.1F;

        public LineRenderer tracer;
        public MeshRenderer sphere;
        public ParticleSystem smokeTrail, rocketFlame;

        private float _scale;

        private void Awake()
        {
            CheckTracer();
        }

        private void CheckTracer()
        {
            tracer = gameObject.GetComponent<LineRenderer>();
            if (tracer == null)
            {
                tracer = gameObject.AddComponent<LineRenderer>();
                tracer.material = Assets.LineMaterial;
                tracer.material.color = Color.yellow;
                tracer.SetWidth(0, 0);
                tracer.SetVertexCount(2);

                var obj = new GameObject();
                obj.transform.parent = transform;
                obj.transform.localPosition = Vector3.zero;
                CheckTracer();

                MeshFilter filter = obj.AddComponent<MeshFilter>();
                filter.mesh = Assets.Sphere;
                sphere = obj.AddComponent<MeshRenderer>();
                sphere.material = Assets.LineMaterial;
                sphere.material.color = Color.yellow;
            }
        }

        public void ResetCannon(Cannon cannon)
        {
            CheckTracer();
            tracer.enabled = cannon;
            sphere.enabled = cannon;
            if (cannon)
            {
                sphere.material.color = tracer.material.color = cannon.tracerColor.Value;
                sphere.enabled = tracer.enabled = cannon.tracer.IsActive;
                _scale = 1F / cannon.transform.lossyScale.x;
            }
            tracer.SetWidth(0, 0);
            sphere.transform.localScale = Vector3.zero;

            if (rocketFlame)
            {
                rocketFlame.gameObject.SetActive(false);
                Destroy(rocketFlame.gameObject);
            }
        }

        public void DestroyRocketTrail()
        {
            if (smokeTrail)
                Destroy(smokeTrail.gameObject, 5);
        }

        private void OnDisable()
        {
            if (smokeTrail)
                smokeTrail.Stop();
        }

        private void Update()
        {
            float width = Mathf.Max(Vector3.Distance(transform.position, MouseOrbit.Instance.cam.transform.position) / 600F, MIN);
            tracer.SetWidth(width, 0);
            tracer.SetPosition(0, tracer.transform.position);
            tracer.SetPosition(1, tracer.transform.position + transform.transform.forward * -5F);
            sphere.transform.localScale = Vector3.one * width * _scale;

            if (smokeTrail)
                smokeTrail.transform.position = transform.position;
        }
    }
}
