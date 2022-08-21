using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AdvancedCannon
{
    public class Helper : SingleInstance<Helper>
    {
        public override string Name => "AC Mod Helper";

        private Camera _camera;

        private float _thickness;
        private float _effThickness;
        private int _type;
        private float _angle;
        private bool _hit;
        private float _message;

        private ProjectileManager manager;

        private void FixedUpdate()
        {
            if (manager != ProjectileManager.Instance)
            {
                manager = ProjectileManager.Instance;
                if (manager)
                {
                    manager.projectilePrefabs[0].GetComponent<CannonBallDamage>().explosionPrefab = Assets.Empty;
                    foreach (var inst in manager.GetPool(0).Pool)
                        inst.GetComponent<CannonBallDamage>().explosionPrefab = Assets.Empty;
                    Debug.Log("Removed cannonball explosion prefab.");
                }
            }
        }

        private void Update()
        {
            _hit = false;
            _type = 0;

            _message -= Time.deltaTime;

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.T))
            {
                Mod.TraceVisible = !Mod.TraceVisible;
                _message = 1;
            }

            if (Machine.Active() && Machine.Active().SimulationMachine == null)
            {
                if (_camera == null)
                    _camera = Camera.main;

                if (_camera)
                {
                    Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out RaycastHit enterHit, 100, ServerProjectile.HitMask, QueryTriggerInteraction.Ignore))
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
                                ArmorHelper.GetSurfaceArmor(surface, out float thickness, out _type);
                                _thickness = thickness / Mathf.Cos(_angle);
                                _effThickness = _thickness * ArmorHelper.GetArmorModifier(_type);
                            }

                            _hit = true;
                        }
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (_message > 0)
            {
                GUI.Label(new Rect(0, 0, 100, 100), Mod.TraceVisible ? "Traces visible." : "Traces invisible.");
            }

            if (_hit)
            {
                GUI.Label(new Rect(Input.mousePosition.x + 16, Screen.height - Input.mousePosition.y, 500, 500), 
                    $"{Mathf.RoundToInt(_thickness)}mm {ArmorHelper.GetModifierName(_type)} ({Mathf.RoundToInt(_effThickness)}mm), {Mathf.RoundToInt(_angle * Mathf.Rad2Deg)}°");
            }
        }
    }
}
