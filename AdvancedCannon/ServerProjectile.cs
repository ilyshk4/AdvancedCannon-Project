using Modding;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AdvancedCannon
{
    public class ServerProjectile : MonoBehaviour
    {
        public static int HitMask = Game.BlockEntityLayerMask | 1 << 29;

        const float MAX_DISTANCE = 10;
        const float OFFSET = 0.15F;

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
        public bool hesh;
        public bool heat;
        public bool fsds;
        public bool dontRicochet;
        public bool explosive;
        public float timeToLive;
        public float explosiveFiller;
        public float explosiveDistance;
        public float explosiveDelay;
        public float velocityDivider = 2200;

        private Vector3 _lastPosition;
        private int _vertexCount;
        private bool _exploded;
        private float _explodeDistance;
        private bool _shouldExplode;

        private void Awake()
        {
            _lastPosition = transform.position;
        }

        private void FixedUpdate()
        {
            transform.rotation = Quaternion.LookRotation(body.velocity);

            timeToLive -= Time.fixedDeltaTime;

            if (body.velocity.magnitude < Mod.Config.Shell.MinVelocity 
                || OutOfBounds(transform.position) 
                || timeToLive <= 0)
            {
                Stop();
                return;
            }

            float oldExplodeDistance = _explodeDistance;
            _explodeDistance -= Vector3.Distance(_lastPosition, transform.position);

            if (_shouldExplode && _explodeDistance <= 0)
            {
                Vector3 position = _lastPosition + (transform.position - _lastPosition).normalized * oldExplodeDistance;
                SpawnExplosion(position, Mod.Config.Shells.APHE.ConeAngle, explosiveDistance);
            }

            if (_lastPosition != transform.position)
            {   
                Vector3 direction = transform.position - _lastPosition;

                if (Raycast(_lastPosition, direction.normalized, direction.magnitude, HitMask, out RaycastHit hit, out BuildSurface surface, out bool hitSide))
                {
                    timeToLive += Time.fixedDeltaTime;

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
                            SpawnHighExplosion(transform.position, body.velocity, explosiveFiller);
                            AddPoint(transform.position);
                            Stop();
                            return;
                        }
                    }
                    else
                    {
                        if (collider.Raycast(new Ray(hit.point + direction.normalized * MAX_DISTANCE, -direction.normalized), out RaycastHit reverseHit, MAX_DISTANCE))
                        {
                            BlockBehaviour hitBlock = hit.collider.attachedRigidbody.GetComponent<BlockBehaviour>();
                            if (hitBlock && !surface)
                            {
                                float damage = body.mass * body.velocity.magnitude * body.velocity.magnitude * Mod.Config.Collision.DamageScale;
                                hitBlock.BlockHealth?.DamageBlock(damage);
                            }

                            collider.attachedRigidbody.AddForceAtPosition(Mod.Config.Collision.ForceScale * body.velocity * body.mass, transform.position);

                            Vector3 end = reverseHit.point;
                            float depth = (end - start).magnitude;
                            float relativeVelocity = (body.velocity - collider.attachedRigidbody.velocity).magnitude;

                            float thickness = depth * 100;

                            if (surface)
                                thickness = Mod.GetSurfaceThickness(surface, angle);

                            float penetration = CalculatePenetration(angle, relativeVelocity, body.mass, caliber, arCap, fsds, velocityDivider);

                            Vector3 enter = start - direction.normalized * OFFSET;
                            Vector3 exit = end + direction.normalized * OFFSET;

                            float penetrationPower = thickness / penetration;
                            float fragmentsCone = Mod.Config.Spalling.BaseConeAngle * penetrationPower;

                            if (particle || highExplosive || hesh || heat)
                                penetration = 0;

                            if (penetration > thickness)
                            {
                                transform.position = exit;
                                float exitAngle = Random.Range(0, Mod.Config.Penetration.BaseExitAngle) * penetrationPower;

                                exitAngle = Mathf.Clamp(exitAngle, 0, 80 - angle);

                                direction = Quaternion.AngleAxis(exitAngle, Vector3.Cross(direction, normal)) * direction;

                                if (shell)
                                {
                                    float powerPerArea = (body.velocity.magnitude * body.velocity.magnitude * body.mass) / (caliber * caliber);

                                    int fragmentsCount = Mathf.CeilToInt(powerPerArea * 0.01F * Mod.Config.Spalling.CountFactor);
                                    fragmentsCount = Mathf.Clamp(fragmentsCount, 5, 25);
                                    fragmentsCone *= powerPerArea * 0.0015F * Mod.Config.Spalling.ConeFactor;

                                    float fragmentsTotalMass = 0.033F * (surface ? surface.currentType.density : 1);
                                    float fragmentMass = fragmentsTotalMass / fragmentsCount;

                                    SpawnFragments(body.position, body.velocity * 0.35F, fragmentsCount, fragmentsCone, fragmentMass, true, surface, Color.yellow, Mod.Config.Spalling.TimeToLive);
                                }

                                body.mass *= 1F - Mod.Config.Penetration.MassLoose;
                                body.velocity = direction.normalized * body.velocity.magnitude * (1F - penetrationPower);

                                if (explosive && thickness >= explosiveDelay)
                                {
                                    _shouldExplode = true;
                                    _explodeDistance = explosiveDistance;
                                }
                            }
                            else
                            {
                                int fragCount = Mathf.Min(Mathf.FloorToInt(body.mass * 2), 4);
                                SpawnFragments(enter, 
                                    body.velocity * 0.3F + body.velocity.normalized * explosiveFiller * 100, 
                                    fragCount, 90 + explosiveFiller * 45, 
                                    0.1F, 
                                    true, null, new Color(1, 0.5F, 0), 0.01F);
                                
                                if (heat)
                                {
                                    SpawnHeatExplosion(enter, body.velocity, explosiveFiller);
                                    AddPoint(transform.position);
                                    Stop();
                                    return;
                                }

                                Ricochet(ref direction, enter, normal, angle);

                                if (highExplosive)
                                {
                                    SpawnHighExplosion(transform.position, body.velocity, explosiveFiller);
                                    AddPoint(transform.position);
                                    Stop();
                                    return;
                                }
                                    
                                if (explosive)
                                {
                                    SpawnExplosion(enter, Mod.Config.Shells.APHE.ConeAngle, 0);
                                    AddPoint(transform.position);
                                    Stop();
                                    return;
                                }

                                if (hesh)
                                {
                                    HeshPenetration(collider, start, normal, surface, explosiveFiller);
                                    AddPoint(transform.position);
                                    Stop();
                                    return;
                                }

                                if (body.velocity.magnitude < Mod.Config.Shell.MinVelocity)
                                {
                                    AddPoint(transform.position);
                                    Stop();
                                    return;
                                }
                            }
                        }

                        accurateRaycasting = false;
                    }
                }
            }

            _lastPosition = transform.position;
            AddPoint(transform.position);
        }

        public static void HeshPenetration(Collider collider, Vector3 start, Vector3 normal, BuildSurface surface, float explosiveFiller)
        {
            if (collider.Raycast(new Ray(start - normal * MAX_DISTANCE, normal), out RaycastHit hashHit, MAX_DISTANCE))
            {
                float flatThickness = surface ? Mod.GetSurfaceThickness(surface, 0) : Vector3.Distance(start, hashHit.point) - 0.1F;
                float heshPenetration = Mod.Config.Shells.HESH.PenetrationPerKilo * explosiveFiller;
                float heshPenetrationPower = flatThickness / heshPenetration;

                if (flatThickness < heshPenetration)
                {
                    int fragmentsCount = Mathf.CeilToInt(Mod.Config.Shells.HESH.BaseSpallingCount * (1F - heshPenetrationPower));
                    float heshAngle = Mod.Config.Shells.HESH.BaseConeAngle * (1F - heshPenetrationPower);

                    SpawnFragments(hashHit.point - normal * OFFSET, -normal * 500, fragmentsCount, heshAngle, 0.1F, true, surface, Color.yellow, Mod.Config.Spalling.TimeToLive);
                }
            }
        }

        public static void SpawnHeatExplosion(Vector3 position, Vector3 velocity, float explosiveFiller)
        {
            SpawnFragments(position, velocity.normalized * Mod.Config.Shells.HEAT.VelocityPerKilo * explosiveFiller, 10, 1, Mod.Config.Shells.HEAT.FragmentMass, false, null, Color.white, Mod.Config.Spalling.TimeToLive);
        }

        public static float CalculatePenetration(float angle, float velocity, float mass, float caliber, bool arCap, bool fsds, float velocityDivider = 2200)
        {
            float amplifiedAngle = arCap ? Mathf.Max(angle - Mod.Config.ArmorPiercingCap.AngleReduce * Mathf.Deg2Rad, 0) : angle;

            if (fsds)
                amplifiedAngle = Mathf.Max(0, amplifiedAngle - Mod.Config.Shells.APFSDS.AngleReduce * Mathf.Deg2Rad);

            amplifiedAngle = Mathf.Clamp(amplifiedAngle, 0, 90 * Mathf.Deg2Rad);

            float penetration =
                Mathf.Pow(velocity / velocityDivider, 1.43F)
                * (Mathf.Pow(mass, 0.71F) / Mathf.Pow(caliber / 100, 1.07F))
                * Mathf.Pow(Mathf.Cos(amplifiedAngle), 1.4F) * 100;

            return penetration;
        }

        public static void SpawnHighExplosion(Vector3 position, Vector3 direction, float power)
        {
            int count = Mod.Config.Shells.HE.MinFragmentsCount + Mathf.CeilToInt(Mod.Config.Shells.HE.FragmentsCountPerKilo * power);
            for (int i = 0; i < count; i++)
            {
                ServerProjectile fragment = Mod.SpawnProjectile(position, Color.yellow, false);
                Vector3 fragmentDirection = Mod.RandomSpread(direction.normalized, 180);
                fragment.body.mass = Mod.Config.Shells.HE.FragmentMass;
                fragment.body.velocity = fragmentDirection * (Mod.Config.Shells.HE.BaseVelocity + power * Mod.Config.Shells.HE.VelocityPerKilo);
                fragment.fragment = true;
                fragment.caliber = Mod.Config.Shells.HE.FragmentCaliber;
                fragment.timeToLive = Mod.Config.Shells.HE.FragmentTimeToLive;
            }
        }

        public static void SpawnFragments(Vector3 position, Vector3 velocity, int count, float cone, float mass, bool bounce, BuildSurface surface, Color color, float timeToLive)
        {
            cone = Mathf.Clamp(cone, 0, 180);
            for (int i = 0; i < count; i++)
            {
                ServerProjectile fragment = Mod.SpawnProjectile(position, color, surface, null, surface);
                Vector3 fragmentDirection = Mod.RandomSpread(velocity, cone);
                float angleSpeedModifier = Mathf.Pow(Mathf.Clamp01(1F - Vector3.Angle(velocity, fragmentDirection) / cone), 2);
                fragment.body.mass = mass;
                fragment.body.velocity = fragmentDirection.normalized * Mathf.Max(fragmentDirection.magnitude * angleSpeedModifier, 150);
                fragment.dontRicochet = !bounce;
                fragment.fragment = true;
                fragment.caliber = 10;
                fragment.timeToLive = timeToLive;
            }
        }

        private void SpawnExplosion(Vector3 position, float cone, float distance)
        {
            if (_exploded)
                return;
            _exploded = true;

            cone = Mathf.Clamp(cone, 0, 180);

            int explosiveFragmentsCount = 5 + Mathf.FloorToInt((float)Mod.Config.Shells.APHE.ParticlesCountPerKilo * explosiveFiller);

            //Vector3 position = transform.position + body.velocity.normalized * distance;
            //if (Physics.Raycast(transform.position, body.velocity.normalized, out RaycastHit explHit, distance, HitMask, QueryTriggerInteraction.Ignore))
            //    position = explHit.point - body.velocity.normalized * 0.3F;

            for (int i = 0; i < explosiveFragmentsCount; i++)
            {
                ServerProjectile fragment = Mod.SpawnProjectile(position, Color.white, false);
                Vector3 direction = Mod.RandomSpread(body.velocity.normalized * 1000, cone);

                fragment.body.mass = 0.1F;
                fragment.body.velocity = direction;
                fragment.fragment = true;
                fragment.particle = true;
                fragment.caliber = 10;
                fragment.timeToLive = Mod.Config.Shells.APHE.ParticleTimeToLive;
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
            Vector3 length = direction.normalized * Mathf.Max(caliber * Mod.Config.Shell.Scale / 200, 0.1F);
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
                * (particle ? 1 : Mathf.Pow(Mathf.Sin(angle), Mod.Config.Ricochet.VelocityDecreasePower));

            if (dontRicochet || 
                (fsds && angle < 75 * Mathf.Deg2Rad))
                body.velocity = Vector3.zero;
        }

        private void OnDestroy()
        {
            if (line) Destroy(line.gameObject, Mod.Config.Trace.TimeToLive);
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

            if (line) Destroy(line.gameObject, Mod.Config.Trace.TimeToLive);
        }

        public void AddPoint(Vector3 vector)
        {
            if (line == null || vector == Vector3.zero || OutOfBounds(vector))
                return;

            line.SetVertexCount(_vertexCount + 1);
            line.SetPosition(_vertexCount++, vector);

            if (network)
                ModNetworking.SendToAll(Mod.AddRemotePoint.CreateMessage((int)network.id, uid, vector, _vertexCount));
        }

    }
}
