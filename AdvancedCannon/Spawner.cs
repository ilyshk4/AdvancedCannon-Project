using Modding;
using System.Collections;
using UnityEngine;

namespace AdvancedCannon
{
    public static class Spawner
    {
        private static int _uidCounter;

        public static ServerProjectile SpawnSingleProjectile(ProjectileSpawnSettings settings)
        {
            Transform cannonball;
            if (ProjectileManager.Instance)
            {
                byte[] array = new byte[13];
                int num = 0;
                NetworkCompression.CompressPosition(settings.position, array, num);
                num += 6;
                NetworkCompression.CompressRotation(Quaternion.identity, array, num);
                NetworkAddPiece instance = NetworkAddPiece.Instance;
                Transform transform = ProjectileManager.Instance
                    .Spawn(NetworkProjectileType.Cannon, instance.frame, Machine.Active().PlayerID, array);
                cannonball = transform;
            }
            else
            {
                cannonball = GamePrefabs.InstantiateProjectile(
                    GamePrefabs.ProjectileType.CannonBall, settings.position, Quaternion.identity, ReferenceMaster.physicsGoalInstance
                    ).transform;
            }

            ServerProjectile oldProjectile = cannonball.GetComponent<ServerProjectile>();
            if (oldProjectile)
                Object.Destroy(oldProjectile);

            SphereCollider collider = cannonball.GetComponent<SphereCollider>();
            if (collider != null)
                collider.isTrigger = true;

            MeshFilter meshFilter = cannonball.GetComponentInChildren<MeshFilter>();
            MeshRenderer meshRenderer = cannonball.GetComponentInChildren<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.enabled = !settings.invisible;

            GameObject obj = cannonball.gameObject;
            obj.transform.parent = ReferenceMaster.physicsGoalInstance;
            obj.transform.position = settings.position;

            LineRenderer line = Utilities.CreateProjectileLine();
            line.material.color = settings.color;

            Rigidbody rigidbody = obj.GetComponent<Rigidbody>();

            NetworkCannonball network = obj.GetComponent<NetworkCannonball>();

            ServerProjectile projectile = obj.AddComponent<ServerProjectile>();
            projectile.body = rigidbody;
            projectile.line = line;
            projectile.network = network;
            projectile.cannon = settings.cannon;
            projectile.surface = settings.surface;
            projectile.AddTracePoint(settings.position);

            projectile.uid = _uidCounter++;

            projectile.transform.localScale = Vector3.one;
            if (settings.cannon != null)
            {
                projectile.transform.localScale = settings.cannon.transform.localScale;
                meshRenderer.material = settings.cannon.MeshRenderer.material;
                meshFilter.sharedMesh = settings.cannon.VisualController.MeshFilter.sharedMesh;
            }

            if (settings.surface != null)
            {
                projectile.transform.localScale = Vector3.one * Random.Range(0.85F, 1.15F);
                meshRenderer.sharedMaterial = settings.surface.MeshRenderer.sharedMaterial;
                meshFilter.sharedMesh = Assets.SpallingMesh;
            }

            if (ProjectileManager.Instance)
            {
                Networking.CreateServerProjectile(projectile);
            }

            return projectile;
        }

        public static void SpawnFragments(SpawnFragmentsSettings settings)
        {
            settings.cone = Mathf.Clamp(settings.cone, 0, 180);
            for (int i = 0; i < settings.count; i++)
            {
                ServerProjectile fragment = Spawner.SpawnSingleProjectile(new ProjectileSpawnSettings()
                {
                    position = settings.position,
                    color = settings.color,
                    invisible = !settings.surface,
                    surface = settings.surface,
                });

                Vector3 fragmentDirection = Utilities.RandomSpread(settings.velocity, settings.cone);
                float angleSpeedModifier = Mathf.Pow(Mathf.Clamp01(1F - Vector3.Angle(settings.velocity, fragmentDirection) / settings.cone), 2);
                fragment.body.mass = settings.mass;
                fragment.body.velocity = fragmentDirection.normalized * Mathf.Max(fragmentDirection.magnitude * angleSpeedModifier, 150);
                fragment.dontRicochet = !settings.bounce;
                fragment.fragment = true;
                fragment.caliber = 10;
                fragment.timeToLive = settings.timeToLive;
            }
        }

        public static void SpawnHeshSpalling(Collider collider, Vector3 start, Vector3 normal, float power, BuildSurface surface = null)
        {
            if (collider.Raycast(new Ray(start - normal * Consts.EXIT_RAYCAST_DISTANCE, 
                normal), out RaycastHit hashHit, Consts.EXIT_RAYCAST_DISTANCE))
            {
                float flatThickness = surface ? ArmorHelper.GetSurfaceThickness(surface, 0) : Vector3.Distance(start, hashHit.point) - 0.1F;
                float heshPenetration = Mod.Config.Shells.HESH.PenetrationPerKilo * power;
                float heshPenetrationPower = flatThickness / heshPenetration;

                if (flatThickness < heshPenetration)
                {
                    int fragmentsCount = Mathf.CeilToInt(Mod.Config.Shells.HESH.BaseSpallingCount * (1F - heshPenetrationPower));
                    float heshAngle = Mod.Config.Shells.HESH.BaseConeAngle * (1F - heshPenetrationPower);

                    SpawnFragments(new SpawnFragmentsSettings()
                    {
                        position = hashHit.point - normal * Consts.HIT_OFFSET,
                        velocity = -normal * 500,
                        count = fragmentsCount,
                        cone = heshAngle,
                        mass = 0.1F, 
                        bounce = true, 
                        surface = surface, 
                        color = Color.yellow, 
                        timeToLive = Mod.Config.Spalling.TimeToLive
                    });
                }
            }
        }

        public static void SpawnReactiveExplosion(Vector3 position, Vector3 normal, int count)
        {
            for (int i = 0; i < count; i++)
            {
                ServerProjectile fragment = Spawner.SpawnSingleProjectile(new ProjectileSpawnSettings()
                {
                    position = position,
                    color = Color.white,
                    invisible = true
                });

                Vector3 direction = Utilities.RandomSpread(normal, 60) * 400;

                fragment.body.mass = 0.001F;
                fragment.body.velocity = direction;
                fragment.fragment = true;
                fragment.particle = true;
                fragment.caliber = 10;
                fragment.timeToLive = 0.02F;
            }
        }

        public static void SpawnExplosion(Vector3 position, float power)
        {
            int explosiveFragmentsCount = 5 + Mathf.FloorToInt((float)Mod.Config.Shells.APHE.ParticlesCountPerKilo * power);

            for (int i = 0; i < explosiveFragmentsCount; i++)
            {
                ServerProjectile fragment = Spawner.SpawnSingleProjectile(new ProjectileSpawnSettings()
                {
                    position = position,
                    color = Color.white,
                    invisible = true
                });

                Vector3 direction = Random.insideUnitSphere.normalized * 1000;

                fragment.body.mass = 0.1F;
                fragment.body.velocity = direction;
                fragment.fragment = true;
                fragment.particle = true;
                fragment.caliber = 10;
                fragment.timeToLive = Mod.Config.Shells.APHE.ParticleTimeToLive;
            }
        }
        public static void SpawnHeatExplosion(Vector3 position, Vector3 velocity, float explosiveFiller)
        {
            SpawnFragments(new SpawnFragmentsSettings()
            {
                position = position,
                velocity = velocity.normalized * Mod.Config.Shells.HEAT.VelocityPerKilo * explosiveFiller,
                count = 10,
                cone = 1,
                mass = Mod.Config.Shells.HEAT.FragmentMass,
                color = Color.white,
                timeToLive = Mod.Config.Spalling.TimeToLive
            });
        }

        public static void SpawnHighExplosion(Vector3 position, float power)
        {
            int count = Mod.Config.Shells.HE.MinFragmentsCount + Mathf.CeilToInt(Mod.Config.Shells.HE.FragmentsCountPerKilo * power);
            for (int i = 0; i < count; i++)
            {
                ServerProjectile fragment = Spawner.SpawnSingleProjectile(new ProjectileSpawnSettings()
                {
                    position = position,
                    color = Color.yellow,
                    invisible = true
                });
                Vector3 fragmentDirection = Random.insideUnitSphere.normalized;
                fragment.body.mass = Mod.Config.Shells.HE.FragmentMass;
                fragment.body.velocity = fragmentDirection * (Mod.Config.Shells.HE.BaseVelocity + power * Mod.Config.Shells.HE.VelocityPerKilo);
                fragment.fragment = true;
                fragment.caliber = Mod.Config.Shells.HE.FragmentCaliber;
                fragment.timeToLive = Mod.Config.Shells.HE.FragmentTimeToLive;
            }
        }
    }
}