using System.Collections;
using UnityEngine;

namespace AdvancedCannon
{
    public static class Utilities
    {
        public static LineRenderer CreateProjectileLine()
        {
            GameObject lineObj = new GameObject("Line");
            lineObj.transform.parent = ReferenceMaster.physicsGoalInstance;

            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.sharedMaterial = Assets.LineMaterial;
            line.receiveShadows = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.SetWidth(0.02F, 0.02F);

            return line;
        }

        public static Vector3 FibSphere(int index, int total)
        {
            var k = index + .5f;

            var phi = Mathf.Acos(1f - 2f * k / total);
            var theta = Mathf.PI * (1 + Mathf.Sqrt(5)) * k;

            var x = Mathf.Cos(theta) * Mathf.Sin(phi);
            var y = Mathf.Sin(theta) * Mathf.Sin(phi);
            var z = Mathf.Cos(phi);

            return new Vector3(x, y, z).normalized;
        }

        public static Vector3 RandomSpread(Vector3 direction, float spread)
        {
            Vector2 offset = Random.insideUnitCircle;

            return RotateXY(direction, offset.x * spread, offset.y * spread);
        }

        public static Vector3 RotateXY(Vector3 direction, float x, float y)
        {
            return Quaternion.LookRotation(direction + Vector3.one * 0.01F) * (Quaternion.AngleAxis(x, Vector3.right)
                * Quaternion.AngleAxis(y, Vector3.up)) * Vector3.forward * direction.magnitude;
        }

        public static T GetRandom<T>(this T[] objs)
        {
            return objs[Random.Range(0, objs.Length)];
        }
    }
}