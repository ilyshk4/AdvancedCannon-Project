using Modding;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AdvancedCannon
{
    public class ServerProjectile : MonoBehaviour
    {
        public static int HitMask = Game.BlockEntityLayerMask | 1 << 29;

        public LineRenderer line;
        public Rigidbody body;
        public NetworkProjectile network;
        public BlockBehaviour cannon;
        public BlockBehaviour surface;

        public int uid;
        public float caliber;
        public bool arCap;
        public bool accurateRaycasting;
        public bool fragment;
        public bool shell;
        public bool particle;
        public bool he;
        public bool hesh;
        public bool heat;
        public bool fsds;
        public bool dontRicochet;
        public bool explosive;
        public bool proxFuse;

        public float timeToLive;
        public float explosiveFiller;
        public float explosiveDistance;
        public float explosiveDelay;
        public float armorResistanceFactor = 2200;
        public float proxFuseRadius;
        public float proxFuseDistance;
        public float distanceTravelled;

        private Vector3 _lastPosition;
        private int _vertexCount;
        private bool _exploded;
        private float _explodeDistance;
        private bool _shouldExplode;

        public int TracePointsCount => _vertexCount;

        private void Awake()
        {
            _lastPosition = transform.position;
        }

        private void FixedUpdate()
        {
            transform.rotation = Quaternion.LookRotation(body.velocity + Vector3.one * 0.01F);

            timeToLive -= Time.fixedDeltaTime;

            if (body.velocity.magnitude < Mod.Config.Shell.MinVelocity 
                || timeToLive <= 0)
            {
                Stop();
                return;
            }

            float oldExplodeDistance = _explodeDistance;
            _explodeDistance -= Vector3.Distance(_lastPosition, transform.position);

            if (!_exploded && _shouldExplode && _explodeDistance <= 0)
            {
                _exploded = true;
                Vector3 position = _lastPosition + (transform.position - _lastPosition).normalized * oldExplodeDistance;
                Spawner.SpawnExplosion(position, explosiveFiller);
                Stop();
                AddTracePoint(position);
                return;
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

                    Vector3 shellEnter = start - direction.normalized * Consts.SHELL_HIT_OFFSET;

                    if (fragment && hitSide)
                    {
                        Stop();
                        return;
                    }

                    if (collider.attachedRigidbody == null)
                    {
                        Ricochet(direction, start, normal, angle);

                        if (he)
                        {
                            Spawner.SpawnHighExplosion(shellEnter, explosiveFiller);
                            AddTracePoint(shellEnter);
                            Stop();
                            return;
                        }
                    }
                    else
                    {
                        if (collider.Raycast(new Ray(hit.point + direction.normalized * Consts.EXIT_RAYCAST_DISTANCE, -direction.normalized), out RaycastHit reverseHit, Consts.EXIT_RAYCAST_DISTANCE))
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
                                thickness = ArmorHelper.GetSurfaceThickness(surface, angle);

                            float angleReduce = 0;

                            if (arCap) angleReduce = Mod.Config.ArmorPiercingCap.AngleReduce;
                            if (fsds) angleReduce = Mod.Config.Shells.APFSDS.AngleReduce;

                            float penetration = ArmorHelper.CalculatePenetration(angle, relativeVelocity, body.mass, caliber, angleReduce, armorResistanceFactor);

                            Vector3 enter = start - direction.normalized * Consts.HIT_OFFSET;
                            Vector3 exit = end + direction.normalized * Consts.HIT_OFFSET;

                            if (surface)
                            {
                                ArmorHelper.GetSurfaceArmor(surface, out float efficiency, out int type);
                                if (type == ArmorHelper.REACTIVE_INDEX)
                                {
                                    thickness = fragment ? efficiency : depth * 20;
                                    Spawner.SpawnReactiveExplosion(enter, normal, fragment ? 1 : 10);
                                    surface.StartCoroutine(BreakReactiveArmor(surface));
                                }
                            }

                            float penetrationPower = thickness / penetration;
                            float fragmentsCone = Mod.Config.Spalling.BaseConeAngle * penetrationPower;

                            if (particle || he || hesh || heat)
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
                                    float thicknessFactor = Mod.Config.Spalling.ThicknessFactor * thickness;
                                    int fragmentsCount = Mathf.CeilToInt(powerPerArea * 0.0025F * Mod.Config.Spalling.ForceCountFactor * thicknessFactor);
                                    fragmentsCount = Mathf.Clamp(fragmentsCount, 5, 25);
                                    fragmentsCone *= Mathf.Min(powerPerArea * 0.0015F * Mod.Config.Spalling.ForceConeFactor, 70);

                                    float fragmentsTotalMass = 0.033F * (surface ? surface.currentType.density : 1);
                                    float fragmentMass = fragmentsTotalMass / fragmentsCount;

                                    Spawner.SpawnFragments(new SpawnFragmentsSettings()
                                    {
                                        position = body.position,
                                        velocity = body.velocity * 0.35F,
                                        count = fragmentsCount,
                                        cone = fragmentsCone,
                                        mass = fragmentMass,
                                        bounce = true,
                                        surface = surface,
                                        color = Color.yellow,
                                        timeToLive = Mod.Config.Spalling.TimeToLive
                                    });
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

                                if (heat)
                                {
                                    Spawner.SpawnHeatExplosion(shellEnter, body.velocity, explosiveFiller);
                                    AddTracePoint(enter);
                                    Stop();
                                    return;
                                }

                                if (he)
                                {
                                    Spawner.SpawnHighExplosion(shellEnter, explosiveFiller);
                                    AddTracePoint(enter);
                                    Stop();
                                    return;
                                }                              

                                if (hesh)
                                {
                                    Spawner.SpawnHeshSpalling(collider, start, normal, explosiveFiller, surface);
                                    AddTracePoint(enter);
                                    Stop();
                                    return;
                                }

                                Ricochet(direction, enter, normal, angle);

                                if (body.velocity.magnitude < Mod.Config.Shell.MinVelocity)
                                {
                                    AddTracePoint(transform.position);
                                    Stop();
                                    return;
                                }
                            }
                        }

                        accurateRaycasting = false;
                    }
                }

                direction = transform.position - _lastPosition;

                if (proxFuse &&
                    Physics.SphereCast(_lastPosition, proxFuseRadius, direction.normalized, out RaycastHit proxHit, direction.magnitude, Game.BlockEntityLayerMask, QueryTriggerInteraction.Ignore))
                {
                    Vector3 point = _lastPosition + direction.normalized * proxHit.distance;
                    if (distanceTravelled + proxHit.distance > proxFuseDistance)
                    {
                        Spawner.SpawnHighExplosion(point, explosiveFiller);
                        Stop();
                        AddTracePoint(point);
                        return;
                    }
                }

                distanceTravelled += direction.magnitude;
            }

            _lastPosition = transform.position;
            AddTracePoint(transform.position);
        }

        private IEnumerator BreakReactiveArmor(BuildSurface surface)
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            ArmorHelper.SetArmor(surface, 0, 5);
        }

        private void Update()
        {
            if (line) line.enabled = Mod.TraceVisible;
        }

        private static readonly Quaternion[] _offsets = new Quaternion[]
        {
            Quaternion.identity,
            Quaternion.AngleAxis(90, Vector3.up),
            Quaternion.AngleAxis(-90, Vector3.up),
            Quaternion.AngleAxis(90, Vector3.right),
            Quaternion.AngleAxis(-90, Vector3.right),
        };

        private static readonly float[] _scales = new float[]
        {
            1F
        };

        private bool Raycast(Vector3 position, Vector3 direction, float magnitude, int hitLayerMask, out RaycastHit hit, out BuildSurface surface, out bool hitSide)
        {
            Vector3 length = direction.normalized * Mathf.Max(caliber * Mod.Config.Shell.Scale / 200, 0.15F);
            hit = default;
            surface = null;
            hitSide = false;

            foreach (var scale in _scales)
                for (int i = 0; i < _offsets.Length; i++)
                {
                    var offset = _offsets[i] * length * scale;
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
        private void Ricochet(Vector3 direction, Vector3 enter, Vector3 normal, float angle)
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
                    ProjectileManager.Instance.Despawn(GetComponent<NetworkProjectile>(), array);
                }
            }
            else
            {
                Destroy(gameObject);
            }

            if (line) Destroy(line.gameObject, Mod.Config.Trace.TimeToLive);
        }

        public void AddTracePoint(Vector3 point)
        {
            if (line == null || point == Vector3.zero)
                return;

            line.SetVertexCount(_vertexCount + 1);
            line.SetPosition(_vertexCount++, point);

            if (network)
                Networking.AddTracePoint(this, point);
        }
    }
}
