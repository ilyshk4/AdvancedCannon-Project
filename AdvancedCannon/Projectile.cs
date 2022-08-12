using Modding;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AdvancedCannon
{
    public class Projectile : MonoBehaviour
    {
        public static int HitMask = Game.BlockEntityLayerMask | 1 << 29;

        const float MAX_DISTANCE = 4;
        const float OFFSET = 0.1F;

        public LineRenderer line;
        public Rigidbody body;
        public NetworkProjectile network;

        public int uid;
        public float caliber;
        public bool arCap;
        public bool ballisticCap;
        public bool accurateRaycasting;
        public bool fragment;
        public bool shell;
        public bool particle;
        public bool highExplosive;
        public float timeToLive;
        public float explosiveFiller;
        public float explosiveDistance;
        public bool fsds;

        private Vector3 _lastPosition;
        private int _vertexCount;
        private bool _exploded;

        private void Awake()
        {
            _lastPosition = transform.position;
        }

        private void FixedUpdate()
        {
            timeToLive -= Time.fixedDeltaTime;

            if (body.velocity.magnitude < Mod.Config.MinProjectileVelocity 
                || OutOfBounds(transform.position) 
                || timeToLive <= 0)
            {
                Stop();
                return;
            }

            if (_lastPosition != transform.position)
            {
                Vector3 direction = transform.position - _lastPosition;

                if (Raycast(_lastPosition, direction.normalized, direction.magnitude, HitMask, out RaycastHit hit, out BuildSurface surface, out bool hitSide))
                {
                    Vector3 start = hit.point;
                    Vector3 normal = hit.normal;
                    Collider collider = hit.collider;
                    float angle = Vector3.Angle(normal, -direction) * Mathf.Deg2Rad;

                    if (fragment && hitSide)
                    {
                        Stop();
                        return;
                    }

                    if (collider.attachedRigidbody == null)
                    {
                        Ricochet(ref direction, start, normal, angle);

                        if (highExplosive)
                        {
                            SpawnHighExplosion();
                        }
                    }
                    else
                    {
                        if (collider.Raycast(new Ray(hit.point + direction.normalized * MAX_DISTANCE, -direction.normalized), out RaycastHit reverseHit, MAX_DISTANCE))
                        {
                            Vector3 end = reverseHit.point;
                            float depth = (end - start).magnitude;
                            float relativeVelocity = (body.velocity - collider.attachedRigidbody.velocity).magnitude;

                            // DEBUG: Debug.Log($"New hit: from {start} to {end}, thickness = {depth}mm, angle = {angle * Mathf.Rad2Deg}deg at {relativeVelocity}ms");

                            float thickness = depth * 100;

                            if (surface)
                            {
                                thickness = Mod.GetSurfaceThickness(surface, angle);
                                // DEBUG:Debug.Log($"Detected build surface, thickness = {armorThickness.Value}mm, modifier = {armorModifier.Value}x");
                            }

                            // DEBUG:Debug.Log($"Armor thickness: {thickness}mm");

                            float amplifiedAngle = arCap ? Mathf.Max(angle - Random.Range(0, Mod.Config.ArmorPiercingCapAngleReduce), 0) : angle;

                            float penetration =
                                Mathf.Pow(relativeVelocity / 2200, 1.43F)
                                * (Mathf.Pow(body.mass, 0.71F) / Mathf.Pow(caliber / 100, 1.07F))
                                * Mathf.Pow(Mathf.Cos(amplifiedAngle), 1.4F) * 100;

                            // DEBUG:Debug.Log($"Final penetration: {penetration}mm");

                            Vector3 enter = start - direction.normalized * OFFSET;
                            Vector3 exit = end + direction.normalized * OFFSET;

                            float penetrationPower = thickness / penetration;
                            float fragmentsCone = Mod.Config.BaseFragmentsConeAngle * penetrationPower;
                            float explosionCone = Mod.Config.BaseExplosionConeAngle * penetrationPower;

                            if (particle || highExplosive)
                                penetration = 0;

                            if (penetration > thickness)
                            {
                                transform.position = exit;
                                float exitAngle = Random.Range(0, angle * 0.2F +
                                    (ballisticCap ? Mod.Config.BallisticCapExitAngle : Mod.Config.BaseExitAngle)) * penetrationPower;

                                exitAngle = Mathf.Clamp(exitAngle, 0, 80 - angle);

                                direction = Quaternion.AngleAxis(exitAngle, Vector3.Cross(direction, normal)) * direction;
                                
                                if (shell)
                                {
                                    float originalShellSpeed = body.velocity.magnitude;
                                    int fragmentsCount = Mod.Config.SpallingFragmentsCount;

                                    if (fsds)
                                    {
                                        fragmentsCount = Mathf.FloorToInt(Mod.Config.FSDS_SpallingCaliberScale * caliber * 4);
                                        fragmentsCone *= Mod.Config.FSDS_ConeScale;
                                    }

                                    float fragmentsTotalMass = 0.1F;
                                    float fragmentMass = fragmentsTotalMass / fragmentsCount;

                                    SpawnFragments(fragmentsCount, fragmentsCone, fragmentMass);

                                    body.mass *= 1F - Mod.Config.PenetrationMassLoose;
                                    body.velocity = direction.normalized * originalShellSpeed * (1F - penetrationPower);

                                    SpawnExplosion(explosionCone, explosiveDistance);
                                }
                            }
                            else
                            {
                                Ricochet(ref direction, enter, normal, angle);

                                if (highExplosive)
                                {
                                    SpawnHighExplosion();
                                }

                                if (!fragment && !highExplosive)
                                    SpawnExplosion(explosionCone, 0);

                                if (highExplosive || body.velocity.magnitude < Mod.Config.MinProjectileVelocity)
                                {
                                    AddPoint(transform.position);
                                    Stop();
                                    return;
                                }
                            }

                            BlockBehaviour hitBlock = hit.collider.attachedRigidbody.GetComponent<BlockBehaviour>();
                            if (hitBlock && !surface)
                            {
                                float damage = body.mass * body.velocity.magnitude * body.velocity.magnitude * Mod.Config.CollisionDamageScale;
                                hitBlock.BlockHealth?.DamageBlock(damage); 
                            }

                            collider.attachedRigidbody.AddForceAtPosition(Mod.Config.CollisionForceScale * body.velocity * body.mass, transform.position);
                        }

                        accurateRaycasting = false;
                    }
                }
            }
            _lastPosition = transform.position;
            AddPoint(transform.position);
        }

        private void SpawnHighExplosion()
        {
            int count = Mathf.CeilToInt(Mod.Config.HighExplosiveFragmentsCountPerKilo * explosiveFiller);
            for (int i = 0; i < count; i++)
            {
                Projectile fragment = Mod.SpawnProjectile(transform.position, Color.yellow);
                Vector3 fragmentDirection = Mod.RandomSpread(body.velocity.normalized, 180);
                fragment.body.mass = 0.05F;
                fragment.body.velocity = fragmentDirection * (Mod.Config.HighExplosiveFragmentBaseVelocity + explosiveFiller * Mod.Config.HighExplosiveFragmentVelocityPerKilo);
                fragment.fragment = true;
                fragment.caliber = 10;
                fragment.timeToLive = Mod.Config.HighExplosiveFragmentTimeToLive;
            }
        }

        private void SpawnFragments(int count, float cone, float mass)
        {
            cone = Mathf.Clamp(cone, 0, 180);
            for (int i = 0; i < count; i++)
            {
                Projectile fragment = Mod.SpawnProjectile(transform.position, Color.yellow);
                Vector3 fragmentDirection = Mod.RandomSpread(body.velocity, cone);
                float angleSpeedModifier = Mathf.Pow(Mathf.Clamp01(1F - Vector3.Angle(body.velocity, fragmentDirection) / cone) * 0.5F, 2);

                fragment.body.mass = mass;
                fragment.body.velocity = fragmentDirection.normalized * Mathf.Max(fragmentDirection.magnitude * angleSpeedModifier, 150F, 1000F);
                fragment.fragment = true;
                fragment.caliber = 10;
                fragment.timeToLive = Mod.Config.SpallingFragmentTimeToLive;
            }
        }

        private void SpawnExplosion(float cone, float distance)
        {
            if (_exploded)
                return;
            _exploded = true;

            cone = Mathf.Clamp(cone, 0, 180);

            int explosiveFragmentsCount = Mathf.FloorToInt((float)Mod.Config.ExplosiveParticlesCountPerKilo * explosiveFiller);

            Vector3 position = transform.position + body.velocity.normalized * distance;
            if (Physics.Raycast(transform.position, body.velocity.normalized, out RaycastHit explHit, distance, HitMask, QueryTriggerInteraction.Ignore))
                position = explHit.point - body.velocity.normalized * 0.3F;

            for (int i = 0; i < explosiveFragmentsCount; i++)
            {
                Projectile fragment = Mod.SpawnProjectile(position, Color.white, false);
                Vector3 direction = Mod.RandomSpread(body.velocity.normalized * 1000, cone);

                fragment.body.mass = 0.1F;
                fragment.body.velocity = direction;
                fragment.fragment = true;
                fragment.particle = true;
                fragment.caliber = 10;
                fragment.timeToLive = Mod.Config.ExplosiveParticleTimeToLive;
            }
        }   

        static Quaternion[] offsets = new Quaternion[]
        {
            Quaternion.identity,
            Quaternion.AngleAxis(90, Vector3.up),
            Quaternion.AngleAxis(-90, Vector3.up),
            Quaternion.AngleAxis(90, Vector3.right),
            Quaternion.AngleAxis(-90, Vector3.right),
        };

        private bool Raycast(Vector3 position, Vector3 direction, float magnitude, int hitLayerMask, out RaycastHit hit, out BuildSurface surface, out bool hitSide)
        {
            // Check for explosive;
            Vector3 length = direction.normalized * Mathf.Max(caliber / 200, 0.1F);
            hit = default;
            surface = null;
            hitSide = false;

            for (int i = 0; i < offsets.Length; i++)
            {
                var offset = offsets[i] * length;
                if (Physics.Raycast(position + offset, direction, out RaycastHit other, magnitude, hitLayerMask, QueryTriggerInteraction.Ignore))
                {
                    hit = other;

                    surface = hit.collider?.attachedRigidbody?.GetComponent<BuildSurface>();

                    if (surface)
                    {
                        hitSide = Mathf.Abs(Vector3.Dot(hit.collider.transform.up, hit.normal)) < 0.001F;

                        if (hitSide)
                            continue;
                        else
                            return true;
                    }

                    if (hit.collider)
                        return true;
                }

                if (!accurateRaycasting)
                    return false;
            }

            return false;
        }

        private bool OutOfBounds(Vector3 position)
        {
            const float bounds = 900;
            return 
                Mathf.Abs(position.x) > bounds
                || Mathf.Abs(position.y) > bounds
                || Mathf.Abs(position.z) > bounds;
        }

        private void Ricochet(ref Vector3 direction, Vector3 enter, Vector3 normal, float angle)
        {
            transform.position = enter;
            direction = Vector3.Reflect(direction, normal); 
            body.velocity = direction.normalized * body.velocity.magnitude 
                * (particle ? 1 : Mathf.Pow(Mathf.Sin(angle), Mod.Config.RicochetVelocityDecreasePower));
        }

        private void OnDestroy()
        {
            if (line) Destroy(line.gameObject, 1);
        }
         
        private void Stop()
        {
            if (StatMaster.isMP && !StatMaster.isLocalSim)
            {
                if (StatMaster.isHosting)
                {
                    byte[] array = new byte[13];
                    NetworkCompression.CompressPosition(transform.position, array, 0);
                    NetworkCompression.CompressRotation(transform.rotation, array, 6);
                    ProjectileManager.Instance.Despawn(GetComponent<NetworkCannonball>(), array);
                }
            }
            else
            {
                Destroy(gameObject);
            }

            if (line) Destroy(line.gameObject, 1);
        }

        public void AddPoint(Vector3 vector)
        {
            if (line == null)
                return;
            if (vector == Vector3.zero)
                return;
            if (OutOfBounds(vector))
                return;
            line.SetVertexCount(_vertexCount + 1);
            line.SetPosition(_vertexCount++, vector);

            if (network)
                ModNetworking.SendToAll(Mod.AddRemotePoint.CreateMessage((int)network.id, uid, vector, _vertexCount));
        }

    }
}
