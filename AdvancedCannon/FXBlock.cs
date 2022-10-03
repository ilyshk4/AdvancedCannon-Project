using Modding;
using System;
using System.Collections;
using UnityEngine;

namespace AdvancedCannon
{
    public class FXBlock : BlockScript
    {
        public MKey activate;

        public MSlider reload;
        public MSlider distance;
        public MSlider effectSize;
        public MSlider effectSpeed;
        public MSlider visualPushback;
        public MSlider returnSpeed;
        public MSlider pushbackSpeed;

        public MColourSlider color;

        public MMenu sound;

        private float _nextFireTime;
        private float _originalZ;
        private bool _pushback;

        public override void SafeAwake()
        {
            activate = AddKey("Activate", "activate", KeyCode.C);

            reload = BlockBehaviour.AddSlider("Reload", "reload", 6, 0, 100, "", "s");
            distance = BlockBehaviour.AddSlider("Distance", "distance", 8, 0, 30, "", "m");
            effectSize = BlockBehaviour.AddSlider("Effect Size", "effect-size", 1, 0, 2, "", "x");
            effectSpeed = BlockBehaviour.AddSlider("Effect Speed", "effect-speed", 1, 0, 2, "", "x");
            visualPushback = BlockBehaviour.AddSlider("Visual Pushback", "vis-pushback", 2, 0, 30, "", "m");
            returnSpeed = BlockBehaviour.AddSlider("Return Speed", "return-speed", 0.05F, 0, 1, "", "");
            pushbackSpeed = BlockBehaviour.AddSlider("Pushback Speed", "pushback-speed", 0.4F, 0, 1, "", "");

            color = AddColourSlider("Color", "color", Color.white, false);

            sound = AddMenu("sound", 0, Assets.FireSoundNames);

            color.ValueChanged += Color_ValueChanged;
        }

        private void Color_ValueChanged(Color value)
        {
            BlockBehaviour.MeshRenderer.material.color = value;
            BlockBehaviour.MeshRenderer.material.SetFloat("_Glossiness", 0F);
            BlockBehaviour.MeshRenderer.material.SetFloat("_Metallic", 0F);
        }

        private void Start()
        {
            _originalZ = BlockBehaviour.MeshRenderer.transform.localPosition.z;
            Color_ValueChanged(color.Value);
        }

        private void Update()
        {
            Transform vis = BlockBehaviour.MeshRenderer.transform;
            Vector2 target;

            if (vis.localPosition.z - _originalZ + visualPushback.Value < 0.15F)
                _pushback = false;

            if (_pushback) 
                target = new Vector2(-visualPushback.Value, pushbackSpeed.Value);
            else
                target = new Vector2(_originalZ, returnSpeed.Value);
            vis.localPosition = Vector3.Lerp(vis.localPosition, new Vector3(vis.localPosition.x, vis.localPosition.y, target.x), target.y * Time.deltaTime * Time.timeScale * 50);
        }

        public override void SimulateUpdateAlways()
        {
            if (activate.IsHeld)
            {
                TryFire();
            }
        }
        public override void KeyEmulationUpdate()
        {
            if (activate.EmulationHeld())
            {
                TryFire();
            }
        }

        private void TryFire()
        {
            if (Networking.HasAuthority && Time.time >= _nextFireTime)
            {
                _nextFireTime = Time.time + reload.Value;
                Fire();
                Networking.SpawnFXBlockFire(this);
            }
        }

        public void Fire()
        {
            Vector3 origin = transform.position + transform.forward * distance.Value;

            GameObject vfx = Instantiate(Assets.Fire0, origin, transform.rotation, ReferenceMaster.physicsGoalInstance) as GameObject;
            vfx.transform.localScale *= effectSize.Value;
            vfx.GetComponent<ParticleSystem>().playbackSpeed = effectSpeed.Value;

            EffectsSpawner.SpawnLightFlash(origin, effectSize.Value);

            Helper.Instance.StartCoroutine(SpawnAudioWithDelay(origin, Vector3.Distance(MouseOrbit.Instance.cam.transform.position, transform.position) / 600));

            _pushback = true;

            Destroy(vfx, 10F * effectSpeed.Value);
        }

        private IEnumerator SpawnAudioWithDelay(Vector3 origin, float delay)
        {
            yield return new WaitForSeconds(delay);

            var sfx = Assets.FireSounds[sound.Value];

            AudioSource nearSfx = new GameObject().AddComponent<AudioSource>();
            nearSfx.transform.position = origin;
            nearSfx.transform.parent = ReferenceMaster.physicsGoalInstance;
            nearSfx.clip = sfx.close.GetRandom();
            nearSfx.maxDistance = 5000;
            nearSfx.spatialBlend = 1F;
            nearSfx.Play();

            GameObject distSfxObj = Instantiate(Assets.DistantSound, origin, Quaternion.identity, ReferenceMaster.physicsGoalInstance) as GameObject;
            AudioSource distSfx = distSfxObj.GetComponent<AudioSource>();
            distSfx.clip = sfx.distant.GetRandom();
            distSfx.spatialBlend = 1F;
            distSfx.Play();

            EffectsSpawner.HandleAudioSource(nearSfx);
            EffectsSpawner.HandleAudioSource(distSfx);

            Destroy(nearSfx.gameObject, 10F);
            Destroy(distSfx.gameObject, 10F);
        }
    }
}