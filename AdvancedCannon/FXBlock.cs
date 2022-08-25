using Modding;
using System.Collections;
using UnityEngine;

namespace AdvancedCannon
{
    public class FXBlock : BlockScript
    {
        public MKey activate;
        public MSlider distance;
        public MSlider visualPushback;

        private float _originalZ;
        private bool _pushback;

        public override void SafeAwake()
        {
            activate = AddKey("Activate", "activate", KeyCode.C);

            distance = BlockBehaviour.AddSlider("Distance", "distance", 3.5F, 0, 30, "", "m");
            visualPushback = BlockBehaviour.AddSlider("Visual Pushback", "vis-pushback", 1, 0, 30, "", "m");
        }

        private void Start()
        {
            _originalZ = BlockBehaviour.MeshRenderer.transform.localPosition.z;
        }
            
        private void Update()
        {
            Transform vis = BlockBehaviour.MeshRenderer.transform;
            Vector2 target;

            if (vis.localPosition.z - _originalZ + visualPushback.Value < 5F)
                _pushback = false;

            if (_pushback) 
                target = new Vector2(-visualPushback.Value, 0.5F * 100);
            else
                target = new Vector2(_originalZ, 0.1F * 100);
            vis.localPosition = Vector3.Lerp(vis.localPosition, new Vector3(vis.localPosition.x, vis.localPosition.y, target.x), target.y * Time.deltaTime);
        }

        public override void SimulateUpdateAlways()
        {
            if (activate.IsPressed)
            {
                Vector3 origin = transform.position + transform.forward * distance.Value;
                GameObject vfx = Instantiate(Assets.Fire0, origin, transform.rotation, ReferenceMaster.physicsGoalInstance) as GameObject;

                GameObject light = Instantiate(Assets.Flash0, origin, transform.rotation, ReferenceMaster.physicsGoalInstance) as GameObject;
                var pointLight = light.GetComponent<Light>();

                Helper.Instance.StartCoroutine(SpawnAudioWithDelay(origin, Vector3.Distance(MouseOrbit.Instance.cam.transform.position, transform.position) / 150));
                Helper.Instance.StartCoroutine(LightFlash(pointLight));

                _pushback = true;

                Destroy(light, 0.2F);
                Destroy(vfx, 10F);
            }
        }

        private IEnumerator SpawnAudioWithDelay(Vector3 origin, float delay)
        {
            yield return new WaitForSeconds(delay);

            AudioSource nearSfx = new GameObject().AddComponent<AudioSource>();
            nearSfx.transform.position = origin;
            nearSfx.transform.parent = ReferenceMaster.physicsGoalInstance;
            nearSfx.clip = Assets.FireSound0;
            nearSfx.maxDistance = 5000;
            nearSfx.spatialBlend = 1F;
            nearSfx.Play();

            GameObject distSfxObj = Instantiate(Assets.DistantSound, origin, Quaternion.identity, ReferenceMaster.physicsGoalInstance) as GameObject;
            AudioSource distSfx = distSfxObj.GetComponent<AudioSource>();
            distSfx.clip = Assets.FireDistantSound0;
            distSfx.spatialBlend = 1F;
            distSfx.Play();

            Helper.Instance.StartCoroutine(Utilities.TimescalePitch(nearSfx));
            Helper.Instance.StartCoroutine(Utilities.TimescalePitch(distSfx));

            Destroy(nearSfx.gameObject, 10F);
            Destroy(distSfx.gameObject, 10F);
        }

        private IEnumerator LightFlash(Light light)
        {
            float baseIntensity = light.intensity;
            float time = 0;
            float maxTime = 0.2F;

            while (true)
            {
                if (light)
                    light.intensity = baseIntensity * Mathf.Cos((time / maxTime) * Mathf.PI * 0.5F);

                time += Time.deltaTime;
                if (time >= maxTime)
                    break;
                yield return new WaitForEndOfFrame();
            }
        }
    }
}