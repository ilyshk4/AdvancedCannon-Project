using Modding;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedCannon
{
    public struct FireAsset
    {
        public string name;
        public AudioClip[] close, distant;
    }

    public class Assets
    {
        public static Material LineMaterial;
        public static Mesh SpallingMesh;

        public static AudioClip NuclearExplosion;

        public static FireAsset[] FireSounds;
        public static List<string> FireSoundNames;

        public static AudioClip[] HitAPBig;
        public static AudioClip[] HitAPMedium;
        public static AudioClip[] HitAPSmall;

        public static AudioClip[] HitAPHEBig;
        public static AudioClip[] HitAPHEMedium;
        public static AudioClip[] HitAPHESmall;

        public static AudioClip[] HitHEBig;
        public static AudioClip[] HitHEMedium;
        public static AudioClip[] HitHESmall;

        public static AudioClip[] HitHEATBig;
        public static AudioClip[] HitHEATMedium;
        public static AudioClip[] HitHEATSmall;


        public static AudioClip[] Ricochet;
        public static AudioClip[] RicochetBullet;
        public static AudioClip[] UnpierceMedium;
        public static AudioClip[] UnpierceSmall;
        public static AudioClip[] UnpierceBullet;

        public static AudioClip[] RocketFire;

        public static GameObject DistantSound;
        public static GameObject Fire0;
        public static GameObject Flash0;
        public static GameObject Whistle;
        public static GameObject Sparkles0;
        public static GameObject Explosion0;
        public static GameObject Trail0;

        public static Mesh Sphere;

        public static ShellAssets AP, APHE, HE, APFSDS, HESH, HEAT, Bomb, Rocket;

        public static GameObject Empty;

        public static AudioClip[] GetClipRange(ModAssetBundle bundle, string format, int count, string index_format = "00")
        {
            var clips = new AudioClip[count];
            for (int i = 0; i < count; i++)
            {
                var assetName = format.Replace("#", (i + 1).ToString(index_format));
                clips[i] = bundle.LoadAsset<AudioClip>(assetName);
            }
            return clips;
        }

        public static void OnLoad()
        {
            LineMaterial = new Material(Shader.Find("Unlit/Color"));

            Empty = new GameObject("Empty");

            SpallingMesh = ModResource.GetMesh("SpallingMesh");

            var sfx = ModResource.GetAssetBundle("sfx");

            FireSounds = new FireAsset[] {
                new FireAsset()
                {
                    name = "Default",
                    close = GetClipRange(sfx, "canonn_def_shot_01", 1),
                    distant = GetClipRange(sfx, "canonn_def_shot_far_01", 1)
                },
                new FireAsset()
                {
                    name = "Machinegun",
                    close = GetClipRange(sfx, "maxim_close-#", 7, "000"),
                    distant = GetClipRange(sfx, "maxim_far-#", 7, "000")
                },
                new FireAsset()
                {
                    name = "20mm KWK30",
                    close = GetClipRange(sfx, "cannon_20mm_kwk30_shot_#", 3),
                    distant = GetClipRange(sfx, "cannon_20mm_kwk30_shot_far_#", 3)
                },
                new FireAsset()
                {
                    name = "20mm Oerlikon",
                    close = GetClipRange(sfx, "cannon_20mm_oerlikon_shot_#", 3),
                    distant = GetClipRange(sfx, "cannon_20mm_oerlikon_shot_far_#", 3)
                },
                new FireAsset()
                {
                    name = "25mm 72k",
                    close = GetClipRange(sfx, "cannon_25mm_72k_shot_#", 3),
                    distant = GetClipRange(sfx, "cannon_25mm_72k_shot_far_#", 3)
                },
                new FireAsset()
                {
                    name = "30mm mk103 38",
                    close = GetClipRange(sfx, "cannon_30mm_mk103_38_shot_#", 3, "0"),
                    distant = GetClipRange(sfx, "cannon_30mm_mk103_38_shot_far_#", 3, "0")
                },
                new FireAsset()
                {
                    name = "37mm Flak 36",
                    close = GetClipRange(sfx, "cannon_37mm_flak36_shot_#", 3),
                    distant = GetClipRange(sfx, "cannon_37mm_flak36_shot_far_#", 3)
                },
                new FireAsset()
                {
                    name = "37mm Sh 37",
                    close = GetClipRange(sfx, "cannon_37mm_sh37_shot_#", 4, "0"),
                    distant = GetClipRange(sfx, "cannon_37mm_sh37_shot_far_#", 4, "0")
                },
                new FireAsset()
                {
                    name = "57mm Zis 4",
                    close = GetClipRange(sfx, "cannon_57mm_zis4_shot_#", 3),
                    distant = GetClipRange(sfx, "cannon_57mm_zis4_shot_far_#", 3)
                },
                new FireAsset()
                {
                    name = "75mm M3",
                    close = GetClipRange(sfx, "cannon_75mm_m3_shot_#", 3),
                    distant = GetClipRange(sfx, "cannon_75mm_m3_shot_far_#", 3)
                },
                new FireAsset()
                {
                    name = "76mm Qf17",
                    close = GetClipRange(sfx, "cannon_76mm_qf17_shot_#", 3),
                    distant = GetClipRange(sfx, "cannon_76mm_qf17_shot_far_#", 3)
                },
                new FireAsset()
                {
                    name = "88mm Pak43",
                    close = GetClipRange(sfx, "cannon_88mm_pak43_shot_#", 3),
                    distant = GetClipRange(sfx, "cannon_88mm_pak43_shot_far_#", 3)
                },
                new FireAsset()
                {
                    name = "94mm Qf32",
                    close = GetClipRange(sfx, "cannon_94mm_qf32_shot_#", 3),
                    distant = GetClipRange(sfx, "cannon_94mm_qf32_shot_far_#", 3)
                },
                new FireAsset()
                {
                    name = "105mm Howitzer",
                    close = GetClipRange(sfx, "105mm_howitzer_shot_#", 3),
                    distant = GetClipRange(sfx, "105mm_howitzer_shot_far_#", 3) 
                },
                new FireAsset()
                {
                    name = "105mm M4",
                    close = GetClipRange(sfx, "cannon_105mm_m4_shot_#", 3),
                    distant = GetClipRange(sfx, "cannon_105mm_m4_shot_far_#", 3)
                },
                new FireAsset()
                {
                    name = "105mm M4",
                    close = GetClipRange(sfx, "cannon_105mm_m4_shot_#", 3),
                    distant = GetClipRange(sfx, "cannon_105mm_m4_shot_far_#", 3)
                },
                new FireAsset()
                {
                    name = "120mm M58",
                    close = GetClipRange(sfx, "cannon_120mm_m58_shot_#", 3),
                    distant = GetClipRange(sfx, "cannon_120mm_m58_shot_far_#", 3)
                },
                new FireAsset()
                {
                    name = "128mm Pak44",
                    close = GetClipRange(sfx, "cannon_128mm_pak44_shot_#", 3),
                    distant = GetClipRange(sfx, "cannon_128mm_pak44_shot_far_#", 3)
                },
                new FireAsset()
                {
                    name = "150mm Type38",
                    close = GetClipRange(sfx, "cannon_150mm_type38_shot_#", 3),
                    distant = GetClipRange(sfx, "cannon_150mm_type38_shot_far_#", 3)
                },
                new FireAsset()
                {
                    name = "380mm STUM",
                    close = GetClipRange(sfx, "cannon_380mm_stum_shot_#", 3),
                    distant = GetClipRange(sfx, "cannon_380mm_stum_shot_far_#", 3)
                },
            };

            FireSoundNames = FireSounds.Select(x => x.name).ToList();

            NuclearExplosion = sfx.LoadAsset<AudioClip>("nuclear_01");


            HitAPBig = GetClipRange(sfx, "tank_hit_big_#", 3);
            HitAPMedium = GetClipRange(sfx, "tank_hit_med_#", 3);
            HitAPSmall = GetClipRange(sfx, "tank_hit_small2_#", 5);

            HitAPHEBig = GetClipRange(sfx, "tank_hit_aphe_big_#", 3);
            HitAPHEMedium = GetClipRange(sfx, "tank_hit_aphe_mid_#", 3);
            HitAPHESmall = GetClipRange(sfx, "tank_hit_aphe_small2_#", 5);

            HitHEBig = GetClipRange(sfx, "tank_hit_he_big_#", 3);
            HitHEMedium = GetClipRange(sfx, "tank_hit_he_mid_#", 3);
            HitHESmall = GetClipRange(sfx, "tank_hit_he_small2_#", 5);

            HitHEATBig = GetClipRange(sfx, "tank_hit_heat_big_#", 3);
            HitHEATMedium = GetClipRange(sfx, "tank_hit_heat_mid_#", 3);
            HitHEATSmall = GetClipRange(sfx, "tank_hit_heat_small2_#", 5);

            Ricochet = GetClipRange(sfx, "tank_rico_#", 4);
            RicochetBullet = GetClipRange(sfx, "bullet_whiz-#", 6, "000");

            UnpierceMedium = GetClipRange(sfx, "tank_hit_med_unpierce_#", 3);
            UnpierceSmall = GetClipRange(sfx, "tank_hit_small2_unpierce_#", 5);
            UnpierceBullet = GetClipRange(sfx, "tank_bullet_impact_ext-#", 6, "000");

            RocketFire = GetClipRange(sfx, "tow_shot-#", 4, "000");

            var vfx = ModResource.GetAssetBundle("vfx");

            DistantSound = vfx.LoadAsset<GameObject>("DistantSound");
            Fire0 = vfx.LoadAsset<GameObject>("Fire0");
            Flash0 = vfx.LoadAsset<GameObject>("Flash0");
            Whistle = vfx.LoadAsset<GameObject>("Whistle");
            Sparkles0 = vfx.LoadAsset<GameObject>("Sparkles0");
            Explosion0 = vfx.LoadAsset<GameObject>("Explosion0");
            Trail0 = vfx.LoadAsset<GameObject>("Trail0");

            AP = new ShellAssets("AP");
            HE = new ShellAssets("HE");
            APHE = new ShellAssets("APHE");
            APFSDS = new ShellAssets("APFSDS");
            HESH = new ShellAssets("HESH");
            HEAT = new ShellAssets("HEAT");
            Bomb = new ShellAssets("Bomb");
            Rocket = new ShellAssets("Rocket");

            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Sphere = Object.Instantiate(obj.GetComponent<MeshFilter>().mesh);
            Object.Destroy(obj);

            Object.DontDestroyOnLoad(Empty);
        }
    }
}