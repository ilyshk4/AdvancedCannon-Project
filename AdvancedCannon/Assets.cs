using Modding;
using System.Collections;
using UnityEngine;

namespace AdvancedCannon
{
    public class Assets
    {
        public static Material TraceMaterial;
        public static Mesh SpallingMesh;

        public static AudioClip SuperBigExplosionSound;
        public static AudioClip FireSound0;
        public static AudioClip FireDistantSound0;
        public static AudioClip Loop0;

        public static GameObject DistantSound;
        public static GameObject Fire0;
        public static GameObject Flash0;

        public static ShellAssets AP, APHE, HE, APFSDS, HESH, HEAT;

        public static GameObject Empty;

        public static void OnLoad()
        {
            TraceMaterial = new Material(Shader.Find("Unlit/Color"));

            Empty = new GameObject("Empty");

            SpallingMesh = ModResource.GetMesh("SpallingMesh");

            SuperBigExplosionSound = ModResource.GetAudioClip("SuperBigExplosion");
            FireDistantSound0 = ModResource.GetAudioClip("FireDistantSound0");
            FireSound0 = ModResource.GetAudioClip("FireSound0");
            Loop0 = ModResource.GetAudioClip("Loop0");

            var bundle = ModResource.GetAssetBundle("vfx");

            DistantSound = bundle.LoadAsset<GameObject>("DistantSound");
            Fire0 = bundle.LoadAsset<GameObject>("Fire0");
            Flash0 = bundle.LoadAsset<GameObject>("Flash0");

            AP = new ShellAssets("AP");
            HE = new ShellAssets("HE");
            APHE = new ShellAssets("APHE");
            APFSDS = new ShellAssets("APFSDS");
            HESH = new ShellAssets("HESH");
            HEAT = new ShellAssets("HEAT");

            Object.DontDestroyOnLoad(Empty);
        }
    }
}