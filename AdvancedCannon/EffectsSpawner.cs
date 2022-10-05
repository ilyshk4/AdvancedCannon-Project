using Modding;
using System.Collections;
using UnityEngine;

namespace AdvancedCannon
{
    public enum VFXType
    {
        Penetration,
        Explosion
    }
   
    public enum SFXType
    {
        Ricochet,
        RicochetBullet,
        UnpierceSmall,
        UnpierceMedium,
        UnpierceBullet,
        HitAPSmall,
        HitAPMedium,
        HitAPHESmall,
        HitAPHEMedium,
        HitAPHEBig,
        HitHESmall,
        HitHEMedium,
        HitHEBig,
        HitHEATSmall,
        HitHEATMedium,
        HitHEATBig,
        RocketFire
    }        

    public static class EffectsSpawner
    {
        public static void ClientSpawnVFX(VFXType type, Vector3 position, Quaternion rotation, float power)
        {
            if (type == VFXType.Penetration)
                SpawnPenetrationEffect(position, rotation, power);

            if (type == VFXType.Explosion)
                SpawnExplosionEffect(position, rotation, power);
        }

        public static void ClientSpawnSFX(SFXType type, Vector3 position)
        {
            if (type == SFXType.Ricochet)
                SpawnSFX(type, Assets.Ricochet, position);

            if (type == SFXType.RicochetBullet)
                SpawnSFX(type, Assets.RicochetBullet, position);

            if (type == SFXType.UnpierceSmall)
                SpawnSFX(type, Assets.UnpierceSmall, position);

            if (type == SFXType.UnpierceMedium)
                SpawnSFX(type, Assets.UnpierceMedium, position);

            if (type == SFXType.UnpierceBullet)
                SpawnSFX(type, Assets.UnpierceBullet, position);

            if (type == SFXType.HitAPSmall)
                SpawnSFX(type, Assets.HitAPSmall, position);

            if (type == SFXType.HitAPMedium)
                SpawnSFX(type, Assets.HitAPMedium, position);

            if (type == SFXType.HitAPHESmall)
                SpawnSFX(type, Assets.HitAPHESmall, position);

            if (type == SFXType.HitAPHEMedium)
                SpawnSFX(type, Assets.HitAPHEMedium, position);

            if (type == SFXType.HitAPHEBig)
                SpawnSFX(type, Assets.HitAPHEBig, position);

            if (type == SFXType.HitHESmall)
                SpawnSFX(type, Assets.HitHESmall, position);

            if (type == SFXType.HitHEMedium)
                SpawnSFX(type, Assets.HitHEMedium, position);

            if (type == SFXType.HitHEBig)
                SpawnSFX(type, Assets.HitHEBig, position);

            if (type == SFXType.HitHEATSmall)
                SpawnSFX(type, Assets.HitHEATSmall, position);

            if (type == SFXType.HitHEATMedium)
                SpawnSFX(type, Assets.HitHEATMedium, position);

            if (type == SFXType.HitHEATBig)
                SpawnSFX(type, Assets.HitHEATBig, position);

            if (type == SFXType.RocketFire)
                SpawnSFX(type, Assets.RocketFire, position);
        }

        private static IEnumerator TimescalePitch(AudioSource source)
        {
            while (source)
            {
                source.pitch = Time.timeScale < 1 ? Mathf.Clamp01(Time.timeScale * 2) : Time.timeScale;
                yield return new WaitForEndOfFrame();
            }
        }

        public static void SpawnLightFlash(Vector3 position, float range)
        {
            GameObject light = Object.Instantiate(Assets.Flash0, position, Quaternion.identity, ReferenceMaster.physicsGoalInstance) as GameObject;
            var pointLight = light.GetComponent<Light>();
            pointLight.range *= range;
            Helper.Instance.StartCoroutine(LightFlash(pointLight));
            Object.Destroy(light, 0.2F);
        }

        private static IEnumerator LightFlash(Light light)
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
        public static void HandleAudioSource(AudioSource source)
        {
            Helper.Instance.StartCoroutine(TimescalePitch(source));
        }

        private static void SpawnAudioClip(AudioClip clip, Vector3 position)
        {
            Helper.Instance.StartCoroutine(SpawnAudioClipWithDelay(clip, position,
                Vector3.Distance(position, MouseOrbit.Instance.camPos) / 300
                ));
        }

        private static IEnumerator SpawnAudioClipWithDelay(AudioClip clip, Vector3 position, float delay)
        {
            yield return new WaitForSeconds(delay);
            AudioSource sfx = new GameObject().AddComponent<AudioSource>();
            sfx.transform.position = position;
            sfx.transform.parent = ReferenceMaster.physicsGoalInstance;
            sfx.spatialBlend = 1F;
            sfx.PlayOneShot(clip);
            HandleAudioSource(sfx);
            Object.Destroy(sfx.gameObject, 1 + clip.length);
        }

        private static void SendSpawnSFXMessage(SFXType type, Vector3 position)
        {
            ModNetworking.SendToAll(Networking.SpawnSFX.CreateMessage((int)type, position));
        }

        private static void SendSpawnVFXMessage(VFXType type, Vector3 position, Quaternion rotation, float power)
        {
            ModNetworking.SendToAll(Networking.SpawnVFX.CreateMessage((int)type, position, rotation.eulerAngles, power));
        }

        public static void SpawnPenetrationEffect(Vector3 position, Quaternion rotation, float power)
        {
            GameObject obj = Object.Instantiate(Assets.Sparkles0, position, rotation, ReferenceMaster.physicsGoalInstance) as GameObject;
            obj.transform.localScale *= Mathf.Clamp01(0.8F + power / 100);
            foreach (ParticleSystem system in obj.GetComponentsInChildren<ParticleSystem>())
                system.maxParticles = 2 + (int)(system.maxParticles * Mathf.Clamp01(power / 110F));
            Object.Destroy(obj, 1F);
            SpawnLightFlash(position, 0.1F + 0.4F * Mathf.Clamp01(power / 100));
            if (Networking.HasAuthority)
                SendSpawnVFXMessage(VFXType.Penetration, position, obj.transform.rotation, power);
        }

        public static void SpawnExplosionEffect(Vector3 position, Quaternion rotation, float power)
        {
            GameObject obj = Object.Instantiate(Assets.Explosion0, position, rotation, ReferenceMaster.physicsGoalInstance) as GameObject;
            obj.transform.localScale = Vector3.one * power;
            var ps = obj.GetComponent<ParticleSystem>();
            ps.playbackSpeed = 5F / power;
            Object.Destroy(obj, ps.duration / ps.playbackSpeed + 1F);
            SpawnLightFlash(position, 0.25F * power);
            if (Networking.HasAuthority)
                SendSpawnVFXMessage(VFXType.Explosion, position, rotation, power);
        }

        public static void SpawnSFX(SFXType type, AudioClip[] clips, Vector3 position)
        {
            SpawnAudioClip(clips.GetRandom(), position);
            if (Networking.HasAuthority)
                SendSpawnSFXMessage(type, position);
        }

        public static void AttachRocketTrail(GameObject rocket, out ParticleSystem flame, out ParticleSystem smoke)
        {
            GameObject trail = Object.Instantiate(Assets.Trail0, rocket.transform.position - rocket.transform.forward * (rocket.transform.localScale.x * 3F), Quaternion.Euler(-rocket.transform.eulerAngles), rocket.transform) as GameObject;
            trail.transform.localScale *= rocket.transform.localScale.x;

            Transform smokeTransform = trail.transform.GetChild(0);
            smokeTransform.parent = null;
            smokeTransform.localScale = Vector3.one * rocket.transform.localScale.x;

            flame = trail.transform.GetChild(0).GetComponent<ParticleSystem>();
            smoke = smokeTransform.GetComponent<ParticleSystem>();
        }
    }
}
