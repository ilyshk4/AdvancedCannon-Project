using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AdvancedCannon
{
    public class AdvancedCannonHelper : SingleInstance<AdvancedCannonHelper>
    {
        public override string Name => "AC Helper";

        private Camera _camera;

        private float _thickness;
        private float _effThickness;
        private int _type;
        private float _angle;
        private bool _hit;

        private ProjectileManager manager;

        private void FixedUpdate()
        {
            if (manager != ProjectileManager.Instance)
            {
                manager = ProjectileManager.Instance;
                if (manager)
                {
                    manager.projectilePrefabs[0].GetComponent<CannonBallDamage>().explosionPrefab = Mod.Empty;
                    foreach (var inst in manager.GetPool(0).Pool)
                        inst.GetComponent<CannonBallDamage>().explosionPrefab = Mod.Empty;
                    Debug.Log("Removed cannonball explosion prefab.");
                }
            }
        }

        private void Update()
        {
            _hit = false;
            _type = 0;

            if (Machine.Active() && Machine.Active().SimulationMachine == null)
            {
                if (_camera == null)
                    _camera = Camera.main;

                if (_camera)
                {
                    Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out RaycastHit enterHit, 100, Projectile.HitMask, QueryTriggerInteraction.Ignore))
                    {
                        if (enterHit.collider &&
                            enterHit.collider.Raycast(new Ray(enterHit.point + ray.direction * 10, -ray.direction), out RaycastHit exitHit, 10))
                        {
                            float depth = (exitHit.point - enterHit.point).magnitude;
                            _angle = Vector3.Angle(enterHit.normal, -ray.direction) * Mathf.Deg2Rad;

                            _effThickness = _thickness = depth * 100;
                            BuildSurface surface = enterHit.collider.attachedRigidbody?.GetComponent<BuildSurface>();
                            if (surface)
                            {
                                Mod.GetSurfaceArmor(surface, out float thickness, out _type);
                                _thickness = thickness / Mathf.Cos(_angle);
                                _effThickness = _thickness * Mod.GetArmorModifier(_type);
                            }

                            _hit = true;
                        }
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (_hit)
            {
                GUI.Label(new Rect(Input.mousePosition.x + 16, Screen.height - Input.mousePosition.y, 500, 500), 
                    $"{Mathf.RoundToInt(_thickness)}mm {Mod.ArmorTypesKeys[_type]} ({Mathf.RoundToInt(_effThickness)}mm), {Mathf.RoundToInt(_angle * Mathf.Rad2Deg)}°");
            }
        }
    }
}
