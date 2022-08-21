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

        public static ShellAssets AP, APHE, HE, APFSDS, HESH, HEAT;

        public static GameObject Empty;

        public static void OnLoad()
        {
            TraceMaterial = new Material(Shader.Find("Unlit/Color"));

            Empty = new GameObject("Empty");
            SpallingMesh = ModResource.GetMesh("SpallingMesh");
            SuperBigExplosionSound = ModResource.GetAudioClip("SuperBigExplosion");

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