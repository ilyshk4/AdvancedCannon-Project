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
            line.sharedMaterial = Assets.TraceMaterial;
            line.receiveShadows = false;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.SetWidth(0.02F, 0.02F);

            return line;
        }

        public static Vector3 RandomSpread(Vector3 direction, float spread)
        {
            Vector2 offset = Random.insideUnitCircle;

            return Quaternion.LookRotation(direction + Vector3.one * 0.01F) * (Quaternion.AngleAxis(offset.x * spread, Vector3.right)
                * Quaternion.AngleAxis(offset.y * spread, Vector3.up)) * Vector3.forward * direction.magnitude;
        }
    }
}