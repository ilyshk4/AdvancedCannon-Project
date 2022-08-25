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

        private int projectileId = -1;
        public int ProjectileId => projectileId;
        
        private void Update()
        {
            CheckProjectilePrefab();

            if (BlockMapper.CurrentInstance && BlockMapper.CurrentInstance.Block.Prefab.ID == (int)BlockType.BuildSurface)
            {
                BlockBehaviour block = BlockMapper.CurrentInstance.Block;
                if (block)
                {
                    ArmorHelper.GetSurfaceArmor((BuildSurface)block, out float thickness, out int armorType);
                    if (armorType == ArmorHelper.REACTIVE_INDEX)
                        thickness = 20;

                    float surfaceMass = Mod.GetSurfaceMass(block, thickness, ArmorHelper.GetArmorModifier(armorType));
                    float mass = surfaceMass;

                    if (block.Rigidbody && block.Rigidbody.mass > mass)
                        mass = block.Rigidbody.mass;

                    MSlider customMass = (MSlider)block.GetMapperType("bmt-custom-mass");
                    if (customMass != null && customMass.Value > mass)
                        mass = customMass.Value;

                    BlockMapper.CurrentInstance.SetBlockName(mass.ToString("0.00kg"));
                }
            }

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

        public void CheckProjectilePrefab()
        {
            if (ProjectileManager.Instance)
            {
                if (projectileId == -1)
                    AddProjectilePrefab();
                try { if (projectileId != -1) ProjectileManager.Instance.GetPool(projectileId); } catch { AddProjectilePrefab(); }
            }
        }

        public void AddProjectilePrefab()
        {
            ProjectileManager manager = ProjectileManager.Instance;

            projectileId = manager.projectilePrefabs.Length;

            GameObject copy = Instantiate(manager.projectilePrefabs[0]);
            copy.SetActive(false);

            NetworkProjectile original = copy.GetComponent<NetworkProjectile>();
            NetworkProjectile network = copy.AddComponent<ModNetworkProjectile>();

            DestroyImmediate(copy.GetComponent<CannonBallDamage>());
            DestroyImmediate(original);

            network.fireController = original.fireController;
            network.fireTag = original.fireTag;
            network.hasBase = original.hasBase;
            network.hasCogMotorDamage = original.hasCogMotorDamage;
            network.hasFireController = original.hasFireController;
            network.hasProjectileScript = original.hasProjectileScript;
            network.hasWheelSmoke = original.hasWheelSmoke;
            network.iceTag = original.iceTag;
            network.id = original.id;
            network.isBaseBlock = original.isBaseBlock;
            network.isBlock = original.isBlock;
            network.isDestroyed = original.isDestroyed;
            network.isEssential = original.isEssential;
            network.myTransform = original.myTransform;
            network.playerId = original.playerId;
            network.pollTransform = original.pollTransform;
            network.projectileInfo = original.projectileInfo;
            network.projectileScript = original.projectileScript;
            network.sendEntity = original.sendEntity;
            network.staticIndex = original.staticIndex;
            network.transformRotation = original.transformRotation;
            network.turningOff = original.turningOff;
            network.wheelSmoke = original.wheelSmoke;

            network.projectileInfo.projectileType = (NetworkProjectileType)projectileId;

            manager.AddAdditionalProjectile(projectileId, copy);
            Debug.Log($"Added additional projectile {manager.GetPool(projectileId)}");
        }
    }
}
