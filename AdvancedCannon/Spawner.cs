using Modding;
using Modding.Blocks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace AdvancedCannon
{
    public static class Spawner
    {
        private static int _uidCounter;
        private const int MaxTickCapacity = 50;
        private static int tickCapacity;

        struct SpawnData    
        {
            public ProjectileSpawnSettings settings;
            public object additionalData;
            public Action<ServerProjectile, object> callback;

            public SpawnData(ProjectileSpawnSettings settings, object additionalData, Action<ServerProjectile, object> callback)
            {
                this.settings = settings;
                this.additionalData = additionalData;
                this.callback = callback;
            }
        }

        private static Queue<SpawnData> _queue = new Queue<SpawnData>();

        public static void ResetTickCapacity()
        {
            tickCapacity = MaxTickCapacity;
        }

        public static void CheckSpawnQueue()
        {
            Helper.Instance.CheckProjectilePrefab();

            if (!StatMaster.levelSimulating)
            {
                ClearSpawnQueue();
                return;
            }

            int available = Mathf.Min(tickCapacity, _queue.Count);

            for (int i = 0; i < available; i++)
            {
                var data = _queue.Dequeue();
                var projectile = SpawnProjectile(data.settings);

                var obj = data.callback.Target;
                if (!(obj is Object && obj.Equals(null)))
                    data.callback(projectile, data.additionalData);
            }
            tickCapacity -= available;
        }

        public static int GetQueueCount() => _queue.Count;

        public static void ClearSpawnQueue()
        {
            _queue.Clear();
        }

        public static void SpawnSingleProjectile(ProjectileSpawnSettings settings, object additionalData, Action<ServerProjectile, object> callback)
        {
            _queue.Enqueue(new SpawnData(settings, additionalData, callback));
            CheckSpawnQueue();
        }

        private static ServerProjectile SpawnProjectile(ProjectileSpawnSettings settings)
        {
            int uid = _uidCounter++;

            Transform cannonball;
            if (ProjectileManager.Instance)
            {
                byte[] array = new byte[13 + 6 + 4 + 6 + 2 + 7 + 7];
                int num = 0;
                NetworkCompression.CompressPosition(settings.position, array, num);
                num += 6;
                NetworkCompression.CompressRotation(Quaternion.identity, array, num);
                num += 7;
                NetworkCompression.CompressVector(Vector3.one, 0, 1, array, num);
                num += 6;

                NetworkCompression.WriteUInt((uint)uid, false, array, num);
                num += 4;
                NetworkCompression.CompressVector((Vector4)settings.color, 0, 1, array, num);
                num += 6;

                num = Networking.WriteBlock(settings.cannon, array, num);
                num = Networking.WriteBlock(settings.surface, array, num);

                NetworkAddPiece instance = NetworkAddPiece.Instance;
                Transform transform = ProjectileManager.Instance
                    .Spawn((NetworkProjectileType)Helper.Instance.GetAvailableId(), instance.frame, Machine.Active().PlayerID, array);
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

            LineRenderer line = null;
            if (Mod.TraceVisible)
            {
                line = Utilities.CreateProjectileLine();
                line.material.color = settings.color;
            }
           

            Rigidbody rigidbody = obj.GetComponent<Rigidbody>();

            ModNetworkProjectile network = obj.GetComponent<ModNetworkProjectile>();

            ServerProjectile projectile = obj.AddComponent<ServerProjectile>();
            projectile.body = rigidbody;
            projectile.line = line;
            projectile.network = network;
            projectile.cannon = settings.cannon;
            projectile.surface = settings.surface;
            projectile.AddTracePoint(settings.position);

            projectile.uid = uid;

            projectile.transform.localScale = Vector3.one;

            if (settings.cannon != null)
            {
                projectile.transform.localScale = settings.cannon.transform.localScale;
                meshRenderer.material = settings.cannon.MeshRenderer.material;
                meshFilter.sharedMesh = settings.cannon.VisualController.MeshFilter.sharedMesh;
            }

            if (meshRenderer)
            {
                TracerController tracer = meshRenderer.gameObject.GetComponent<TracerController>();
                if (tracer == null)
                    tracer = meshRenderer.gameObject.AddComponent<TracerController>();
                var cannon = settings.cannon ? (Cannon)Block.From(settings.cannon).BlockScript : null;
                tracer.ResetCannon(cannon);

                if (tracer.smokeTrail)
                    Object.Destroy(tracer.smokeTrail.gameObject);
                if (meshRenderer.enabled && meshFilter.sharedMesh == Assets.Rocket.mesh)
                    EffectsSpawner.AttachRocketTrail(projectile.gameObject, out tracer.rocketFlame, out tracer.smokeTrail);
            }

            if (settings.surface != null)
            {
                projectile.transform.localScale = Vector3.one * Random.Range(0.85F, 1.15F);
                meshRenderer.sharedMaterial = settings.surface.MeshRenderer.sharedMaterial;
                meshFilter.sharedMesh = Assets.SpallingMesh;
            }
            return projectile;
        }

        public static void SpawnFragments(SpawnFragmentsSettings settings)
        {
            settings.cone = Mathf.Clamp(settings.cone, 0, 180);
            for (int i = 0; i < settings.count; i++)
            {
                Spawner.SpawnSingleProjectile(new ProjectileSpawnSettings()
                {
                    position = settings.position,
                    color = settings.color,
                    invisible = !settings.surface,
                    surface = settings.surface,
                }, settings, SpawnFragmentsCallback);
            }
        }

        private static void SpawnFragmentsCallback(ServerProjectile fragment, object data)
        {
            SpawnFragmentsSettings settings = (SpawnFragmentsSettings)data;
            Vector3 fragmentDirection = Utilities.RandomSpread(settings.velocity, settings.cone);
            float angleSpeedModifier = Mathf.Pow(Mathf.Clamp01(1F - Vector3.Angle(settings.velocity, fragmentDirection) / 60), 2);
            fragment.body.mass = settings.mass;
            fragment.body.velocity = fragmentDirection.normalized * Mathf.Max(fragmentDirection.magnitude * angleSpeedModifier, 150);
            fragment.dontRicochet = !settings.bounce;
            fragment.fragment = true;
            fragment.caliber = 10;
            fragment.timeToLive = settings.timeToLive;
            fragment.spallingPerFragment = settings.spallingPerFragment;
        }

        public static void SpawnHeshSpalling(Collider collider, Vector3 start, Vector3 normal, float power, BuildSurface surface = null)
        {
            if (collider.Raycast(new Ray(start - normal * Consts.EXIT_RAYCAST_DISTANCE, 
                normal), out RaycastHit hashHit, Consts.EXIT_RAYCAST_DISTANCE))
            {
                float flatThickness = surface ? ArmorHelper.GetSurfaceThickness(surface, 0) : Vector3.Distance(start, hashHit.point) - 0.1F;
                float heshPenetration = Mod.Config.Shells.HESH.PenetrationValue * Mathf.Pow(power, Mod.Config.Shells.HESH.PenetrationPower);
                float heshPenetrationPower = flatThickness / heshPenetration;

                if (flatThickness < heshPenetration)
                {
                    float powerPerArea = power * 15000;
                    float thicknessFactor = Mod.Config.Spalling.ThicknessFactor * flatThickness;
                    thicknessFactor *= thicknessFactor;

                    int fragmentsCount = Mathf.RoundToInt(Mathf.Pow(powerPerArea * 0.2F * Mod.Config.Spalling.ForceCountFactor * thicknessFactor, 0.5F));
                    float heshAngle = Mod.Config.Shells.HESH.BaseConeAngle * (1F - heshPenetrationPower);

                    SpawnFragments(new SpawnFragmentsSettings()
                    {
                        position = hashHit.point - normal * 0.05F,
                        velocity = -normal * 1000,
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
                Spawner.SpawnSingleProjectile(new ProjectileSpawnSettings()
                {
                    position = position,
                    color = Color.white,
                    invisible = true
                }, normal, SpawnReactiveExplosionCallback);
            }
        }

        private static void SpawnReactiveExplosionCallback(ServerProjectile fragment, object data)
        {
            Vector3 normal = (Vector3)data;
            Vector3 direction = Utilities.RandomSpread(normal, 60) * 400;

            fragment.body.mass = 0.001F;
            fragment.body.velocity = direction;
            fragment.fragment = true;
            fragment.explosiveParticle = true;
            fragment.caliber = 10;
            fragment.timeToLive = 0.02F;
        }

        public static void SpawnExplosion(Vector3 position, float power)
        {
            EffectsSpawner.SpawnExplosionEffect(position, Quaternion.identity, power + 1F);

            int explosiveFragmentsCount = Mod.Config.Shells.APHE.MinParticlesCount + Mathf.FloorToInt((float)Mod.Config.Shells.APHE.ParticlesCountPerKilo * power);

            for (int i = 0; i < explosiveFragmentsCount; i++)
            {
                ExplosionFragmentData data = new ExplosionFragmentData()
                {
                    spalling = 0,
                    power = power
                };
                Spawner.SpawnSingleProjectile(new ProjectileSpawnSettings()
                {
                    position = position,
                    color = Color.white,
                    invisible = true
                }, data, SpawnExplosionCallback);
            }
        }

        private static void SpawnExplosionCallback(ServerProjectile fragment, object rawData)
        {
            ExplosionFragmentData data = (ExplosionFragmentData)rawData;

            Vector3 direction = Random.onUnitSphere;

            fragment.body.mass = 0.1F;
            fragment.body.velocity = direction.normalized * 1000;
            fragment.fragment = true;
            fragment.explosiveParticle = true;
            fragment.caliber = 10;
            fragment.timeToLive = Mod.Config.Shells.APHE.ParticleTimeToLive;
        }

        public static void SpawnHeatExplosion(Vector3 position, Vector3 velocity, float explosiveFiller)
        {
            SpawnHighExplosion(position, explosiveFiller);
            SpawnFragments(new SpawnFragmentsSettings()
            {
                position = position,
                velocity = velocity.normalized * Mod.Config.Shells.HEAT.VelocityPerKilo * explosiveFiller,
                count = 10,
                cone = 1,
                spallingPerFragment = 1,
                mass = Mod.Config.Shells.HEAT.FragmentMass,
                color = Color.white,
                timeToLive = Mod.Config.Spalling.TimeToLive,
                accurate = true
            });
        }

        private static bool CheckCollider(Collider collider) => collider.attachedRigidbody
                    && collider.enabled
                    && collider.gameObject.activeInHierarchy
                    && collider.gameObject.activeSelf;

        public static void SpawnHighExplosion(Vector3 position, float power)
        {
            float radius = Mathf.Max(ArmorHelper.GetHeParticlePenetration(power, 25F), 10);          

            EffectsSpawner.SpawnExplosionEffect(position, Quaternion.identity, Mathf.Pow(power, 0.65F) + 2F);

            var colliders = Physics.OverlapSphere(position, radius, Game.BlockEntityLayerMask, QueryTriggerInteraction.Ignore);

            foreach (var collider in colliders)
            {
                float distance = Vector3.Distance(position, collider.transform.position);
                float roll = Random.Range(0F, 1F);
                float chance = 0.1F * Mathf.Clamp01(power);

                if (collider is CapsuleCollider)
                    chance = 1F; // Always hit FPS controller.

                if (CheckCollider(collider) && roll < chance)
                {
                    HighExplosionFragmentData data = new HighExplosionFragmentData()
                    {   
                        body = collider.attachedRigidbody,
                        power = power,
                        noSpread = collider is CapsuleCollider
                    };

                    Spawner.SpawnSingleProjectile(new ProjectileSpawnSettings()
                    {
                        position = position,
                        color = Color.yellow,
                        invisible = true,
                    }, data, SpawnHighExplosionCallback);
                }
            }
        }

        private static void SpawnHighExplosionCallback(ServerProjectile fragment, object rawData)
        {
            HighExplosionFragmentData data = (HighExplosionFragmentData)rawData;

            Vector3 direction = data.body.transform.TransformPoint(data.body.centerOfMass) - fragment.transform.position;

            if (!data.noSpread)
                direction = Utilities.RandomSpread(direction, 45F / direction.magnitude);

            fragment.body.mass = 2F;
            fragment.body.velocity = direction.normalized * Mod.Config.Shells.HE.Velocity;
            fragment.fragment = true;
            fragment.caliber = 10;
            fragment.timeToLive = Mod.Config.Shells.HE.FragmentTimeToLive;
            fragment.heParticle = true;
            fragment.heParticleFiller = data.power;
        }

        struct ExplosionFragmentData
        {
            public float power;
            public int spalling;
        }

        struct HighExplosionFragmentData
        {
            public Rigidbody body;
            public float power;
            public bool noSpread;
        }
    }
}