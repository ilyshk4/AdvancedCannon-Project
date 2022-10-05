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
        public int spallingPerFragment;

        public float caliber;
        public bool arCap;
        public bool fragment;
        public bool shell;
        public bool explosiveParticle;
        public bool he;
        public bool hesh;
        public bool heat;
        public bool fsds;
        public bool dontRicochet;
        public bool explosive;
        public bool proxFuse;
        public bool noForce;
        public bool heParticle;
        
        public float timeToLive;
        public float explosiveFiller;
        public float explosiveDistance;
        public float explosiveDelay;
        public float armorResistanceFactor = 2200;
        public float proxFuseRadius;
        public float proxFuseDistance;
        public float distanceTravelled;
        public float appliedForceScale = 1;
        public float heParticleFiller;
        public float constantVelocity;

        private Vector3 _lastPosition;
        private int _vertexCount;
        private bool _exploded;
        private float _explodeDistance;
        private bool _shouldExplode;
        private bool _soundSpawned;
        private float _soundSpawnedTime;

        public int TracePointsCount => _vertexCount;
        public float PhysicalRadius => Mathf.Max(caliber * Mod.Config.Shell.Scale / 200, shell ? 0.2F : 0.15F);

        private void Awake()
        {
            _lastPosition = transform.position;
        }

        private void Start()
        {
            transform.position += body.velocity * Time.fixedDeltaTime;
            UpdateLookRotation();
        }

        private void FixedUpdate()
        {
            body.velocity += constantVelocity * body.velocity.normalized;

            bool solid = !(explosiveParticle || he || hesh || heat);

            UpdateLookRotation();

            timeToLive -= Time.fixedDeltaTime;

            if (_soundSpawned)
            {
                _soundSpawnedTime += Time.fixedDeltaTime;

                if (_soundSpawnedTime > 0.1F)
                {
                    _soundSpawned = false;
                    _soundSpawnedTime = 0;
                }
            }

            if (CheckVelocity() || timeToLive <= 0)
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

                if (Raycast(_lastPosition, direction.normalized, direction.magnitude, HitMask, fragment, out RaycastHit hit, out BuildSurface surface))
                {
                    Vector3 start = _lastPosition + direction.normalized * hit.distance;
                    Vector3 normal = hit.normal;
                    Collider collider = hit.collider;
                    float angle = Vector3.Angle(normal, -direction) * Mathf.Deg2Rad;

                    if (_shouldExplode)
                    {
                        _exploded = true;
                        Spawner.SpawnExplosion(start, explosiveFiller);
                        AddTracePoint(start);
                        Stop();
                        return;
                    }

                    if (collider.attachedRigidbody == null)
                    {
                        if (he || hesh || heat)
                        {
                            SpawnHitSound(start);
                            Spawner.SpawnHighExplosion(start, explosiveFiller);
                            AddTracePoint(start);
                            Stop();
                            return;
                        }

                        Ricochet(direction, start, normal, angle);
                    }
                    else
                    {
                        if (ColliderRaycast(collider, new Ray(hit.point + direction.normalized * Consts.EXIT_RAYCAST_DISTANCE, -direction.normalized), out RaycastHit reverseHit, Consts.EXIT_RAYCAST_DISTANCE))
                        {
                            float distTravelledByHit = distanceTravelled + hit.distance;
                            float hePenetration = 0;
                            if (heParticle)
                                hePenetration = ArmorHelper.GetHeParticlePenetration(heParticleFiller, distTravelledByHit);

                            BlockBehaviour hitBlock = hit.collider.attachedRigidbody.GetComponent<BlockBehaviour>();
                            if (hitBlock && !surface)
                            {
                                float damage = body.mass * body.velocity.magnitude * body.velocity.magnitude * Mod.Config.Collision.DamageScale;
                                damage = Mathf.Sqrt(damage);

                                if (heParticle)
                                    damage = hePenetration * 0.2F;

                                hitBlock.BlockHealth?.DamageBlock(damage);
                            }

                            if (!(he || hesh || heat))
                            {
                                float force;
                                if (heParticle)
                                    force = hePenetration * 30F;
                                else
                                    force = Mod.Config.Collision.ForceScale * body.mass * appliedForceScale * body.velocity.magnitude;
                                collider.attachedRigidbody.AddForceAtPosition(force * body.velocity.normalized, transform.position);
                            }

                            Vector3 end = reverseHit.point + direction.normalized * 0.05F;

                            float depth = (reverseHit.point - hit.point).magnitude;
                            float relativeVelocity = (body.velocity - collider.attachedRigidbody.velocity).magnitude;

                            float thickness = depth * 100;

                            if (surface)
                                thickness = ArmorHelper.GetSurfaceThickness(surface, angle);

                            float angleReduce = 0;

                            if (arCap) angleReduce = Mod.Config.ArmorPiercingCap.AngleReduce;
                            if (fsds) angleReduce = Mod.Config.Shells.APFSDS.AngleReduce;

                            float penetration = ArmorHelper.CalculatePenetration(angle, relativeVelocity, body.mass, caliber, angleReduce, armorResistanceFactor);

                            if (heParticle)
                                penetration = hePenetration;

                            if (surface)
                            {
                                ArmorHelper.GetSurfaceArmor(surface, out float efficiency, out int type);
                                if (type == ArmorHelper.REACTIVE_INDEX)
                                {
                                    thickness = fragment ? efficiency : depth * 20;
                                    surface.StartCoroutine(BreakReactiveArmor(surface));
                                }
                            }

                            float penetrationPower = thickness / penetration;
                            float fragmentsCone = Mod.Config.Spalling.BaseConeAngle * penetrationPower;

                            if (solid)
                            {
                                if (shell)
                                    EffectsSpawner.SpawnPenetrationEffect(start, Quaternion.LookRotation(-body.velocity), caliber);
                            } else
                                penetration = 0;

                            if (penetration > thickness)
                            {
                                transform.position = end;
                                    
                                float exitAngle = Random.Range(0, Mod.Config.Penetration.BaseExitAngle) * penetrationPower;

                                exitAngle = Mathf.Clamp(exitAngle, 0, 80 - angle);

                                direction = Quaternion.AngleAxis(exitAngle, Vector3.Cross(direction, normal)) * direction;
                                
                                if (surface)
                                    if (shell || spallingPerFragment > 0)
                                    {
                                        float powerPerArea = (body.velocity.magnitude * body.velocity.magnitude * body.mass) / (caliber * caliber);
                                        float thicknessFactor = Mod.Config.Spalling.ThicknessFactor * thickness;
                                        thicknessFactor *= thicknessFactor;

                                        int fragmentsCount = Mathf.RoundToInt(Mathf.Pow(powerPerArea * 0.2F * Mod.Config.Spalling.ForceCountFactor * thicknessFactor, 0.5F));

                                        if (spallingPerFragment > 0)
                                        {
                                            fragmentsCount = spallingPerFragment;
                                            spallingPerFragment = 0;
                                        }

                                        fragmentsCone *= powerPerArea * 0.0015F * Mod.Config.Spalling.ForceConeFactor;

                                        fragmentsCone = Mathf.Min(fragmentsCone, 70);

                                        float fragmentsTotalMass = 0.033F * (surface ? surface.currentType.density : 1);
                                        float fragmentMass = fragmentsTotalMass / fragmentsCount;

                                        Spawner.SpawnFragments(new SpawnFragmentsSettings()
                                        {
                                            position = end,
                                            velocity = body.velocity * 0.6F,
                                            count = fragmentsCount,
                                            cone = fragmentsCone,
                                            mass = fragmentMass,
                                            bounce = true,
                                            surface = surface,
                                            color = Color.yellow,
                                            timeToLive = Mod.Config.Spalling.TimeToLive
                                        });
                                    }

                                if (!heParticle)
                                {
                                    body.mass *= 1F - Mod.Config.Penetration.MassLoose;
                                    body.velocity = direction.normalized * body.velocity.magnitude * (1F - penetrationPower);
                                }

                                if (shell)
                                    EffectsSpawner.SpawnPenetrationEffect(end, Quaternion.LookRotation(body.velocity, -Vector3.forward), caliber);

                                if (explosive && thickness >= explosiveDelay)
                                {
                                    _shouldExplode = true;
                                    _explodeDistance = explosiveDistance;
                                }

                                SpawnHitSound(start);
                            }
                            else
                            {
                                if (heat)
                                {
                                    SpawnHitSound(start);
                                    Spawner.SpawnHeatExplosion(start, body.velocity, explosiveFiller);
                                    AddTracePoint(start);
                                    Stop();
                                    return;
                                }

                                if (he)
                                {
                                    SpawnHitSound(start);
                                    Spawner.SpawnHighExplosion(start, explosiveFiller);
                                    AddTracePoint(start);
                                    Stop();
                                    return;
                                }                              

                                if (hesh)
                                {
                                    SpawnHitSound(start);
                                    Spawner.SpawnHighExplosion(start, explosiveFiller);
                                    Spawner.SpawnHeshSpalling(collider, start, normal, explosiveFiller, surface);
                                    AddTracePoint(start);
                                    Stop();
                                    return;
                                }

                                Ricochet(direction, start, normal, angle);

                                if (CheckVelocity())
                                {
                                    SpawnUnpierceSound();

                                    AddTracePoint(transform.position);
                                    Stop();
                                    return;
                                }
                                else
                                {
                                    if (shell)
                                    {
                                        EffectsSpawner.SpawnPenetrationEffect(start, Quaternion.LookRotation(-body.velocity), caliber);
                                        if (caliber < 10)
                                            EffectsSpawner.SpawnSFX(SFXType.RicochetBullet, Assets.RicochetBullet, start);
                                        else
                                            EffectsSpawner.SpawnSFX(SFXType.Ricochet, Assets.Ricochet, start);
                                    }
                                }
                            }
                        } else
                        {
                            if (he || hesh || heat)
                            {
                                SpawnHitSound(start);
                                Spawner.SpawnHighExplosion(start, explosiveFiller);
                                AddTracePoint(start);
                                Stop();
                                return;
                            }

                            Ricochet(direction, start, normal, angle);

                            if (CheckVelocity())
                            {
                                AddTracePoint(start);
                                Stop();
                                return;
                            }
                        }
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

        private void UpdateLookRotation()
        {
            transform.rotation = Quaternion.LookRotation(body.velocity + Vector3.one * 0.01F);
        }

        private void SpawnUnpierceSound()
        {
            bool solid = !(explosiveParticle || he || hesh || heat);

            if (shell && solid && !_soundSpawned)
                _soundSpawned = true;
            else
                return;

            if (caliber < 10)
                EffectsSpawner.SpawnSFX(SFXType.UnpierceBullet, Assets.UnpierceBullet, transform.position);
            else if (caliber < 50)
                EffectsSpawner.SpawnSFX(SFXType.UnpierceSmall, Assets.UnpierceSmall, transform.position);
            else
                EffectsSpawner.SpawnSFX(SFXType.UnpierceMedium, Assets.UnpierceMedium, transform.position);
        }

        private void SpawnHitSound(Vector3 position)
        {
            if (shell && !_soundSpawned)
                _soundSpawned = true;
            else
                return;

            if (explosiveFiller == 0)
            {
                if (caliber < 10)
                    EffectsSpawner.SpawnSFX(SFXType.UnpierceBullet, Assets.UnpierceBullet, position);
                else if (caliber < 50)
                    EffectsSpawner.SpawnSFX(SFXType.HitAPSmall, Assets.HitAPSmall, position);
                else
                    EffectsSpawner.SpawnSFX(SFXType.HitAPMedium, Assets.HitAPMedium, position);
            } else 
            {
                if (explosiveFiller < 2)
                {
                    if (he || hesh)
                        EffectsSpawner.SpawnSFX(SFXType.HitHESmall, Assets.HitHESmall, position);
                    else if (heat)
                        EffectsSpawner.SpawnSFX(SFXType.HitHEATSmall, Assets.HitHEATSmall, position);
                    else
                        EffectsSpawner.SpawnSFX(SFXType.HitAPHESmall, Assets.HitAPHESmall, position);
                }
                else if (explosiveFiller < 8)
                {
                    if (he || hesh)
                        EffectsSpawner.SpawnSFX(SFXType.HitHEMedium, Assets.HitHEMedium, position);
                    else if (heat)
                        EffectsSpawner.SpawnSFX(SFXType.HitHEATMedium, Assets.HitHEATMedium, position);
                    else
                        EffectsSpawner.SpawnSFX(SFXType.HitAPHEMedium, Assets.HitAPHEMedium, position);
                }
                else
                {
                    if (he || hesh)
                        EffectsSpawner.SpawnSFX(SFXType.HitHEBig, Assets.HitHEBig, position);
                    else if (heat)
                        EffectsSpawner.SpawnSFX(SFXType.HitHEATBig, Assets.HitHEATBig, position);
                    else
                        EffectsSpawner.SpawnSFX(SFXType.HitAPHEBig, Assets.HitAPHEBig, position);
                }
            }
        }

        private bool CheckVelocity()
        {
            return body.velocity.magnitude < (shell ? Mod.Config.Shell.MinShellVelocity : Mod.Config.Shell.MinFragmentVelocity);
        }

        private bool ColliderRaycast(Collider collider, Ray ray, out RaycastHit reverseHit, float distance)
        {
            reverseHit = default;
            for (float i = 0; i < 5; i += 0.1F)
                for (int j = 0; j < 3; j++)
                    if (collider.Raycast(new Ray(ray.origin, Utilities.RandomSpread(ray.direction, i)), out reverseHit, distance))
                    return true;
            return false;
        }

        private IEnumerator BreakReactiveArmor(BuildSurface surface)
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            ArmorHelper.SetArmor(surface, 0, 5);
        }

        private BuildSurface GetHitSurface(RaycastHit hit) => hit.collider?.attachedRigidbody?.GetComponent<BuildSurface>();

        private bool Raycast(Vector3 position, Vector3 direction, float magnitude, int hitLayerMask, bool isFragment, out RaycastHit hit, out BuildSurface surface)
        {
            surface = null;

            if (Physics.SphereCast(position, PhysicalRadius, direction, out hit, magnitude, hitLayerMask, QueryTriggerInteraction.Ignore))
            {
                surface = GetHitSurface(hit);
                return true;
            }

            if (Physics.Raycast(position, direction, out hit, magnitude, hitLayerMask, QueryTriggerInteraction.Ignore))
            {
                surface = GetHitSurface(hit);
                return true;
            }

            return false;
        }

        private void Ricochet(Vector3 direction, Vector3 enter, Vector3 normal, float angle)
        {
            transform.position = enter;
            direction = Vector3.Reflect(direction, normal); 
            body.velocity = direction.normalized * body.velocity.magnitude 
                * (explosiveParticle || heParticle ? 1 : Mathf.Pow(Mathf.Sin(angle), Mod.Config.Ricochet.VelocityDecreasePower));

            if (dontRicochet || 
                (fsds && angle < 75 * Mathf.Deg2Rad))
                body.velocity = Vector3.zero;
        }

        private void OnDestroy()
        {
            if (line) Destroy(line.gameObject, Mod.Config.Trace.TimeToLive);
        }
         
        public void Stop()
        {
            var tracer = GetComponent<TracerController>();
            if (tracer)
                tracer.DestroyRocketTrail();

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
