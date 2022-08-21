using System.Collections;
using UnityEngine;

namespace AdvancedCannon
{
    public class NuclearExplosionEffect : MonoBehaviour
    {
        public Texture2D overlay;
        public float alive;

        private void Start()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.clip = Assets.SuperBigExplosionSound;
            source.Play();

            overlay = new Texture2D(1, 1);
            overlay.SetPixel(0, 0, Color.white);
            overlay.Apply();
        }

        private void Update()
        {
            alive += Time.deltaTime;

            overlay.SetPixel(0, 0, Color.white * Mathf.Clamp01(Mathf.Pow(1F - alive / 10F, 0.2F)));
            overlay.Apply();

            if (alive >= 20F)
                Destroy(gameObject);
        }

        private void OnGUI()
        {
            if (!StatMaster.hudHidden)
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlay);
        }
    }
}