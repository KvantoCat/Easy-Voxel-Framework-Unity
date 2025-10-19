
using UnityEngine;

namespace EasyVoxel
{
    public static class RayTracingHelpOLD 
    {
        public static bool GetBoxIntersection(Vector3 ro, Vector3 rd, Vector3 pos, float sizeF, out float dist, out Vector3 normal)
        {
            Vector3 rdInv = Vec3Help.Div(Vector3.one, rd);
            Vector3 t0 = Vec3Help.Mul(pos - ro, rdInv);
            Vector3 t1 = Vec3Help.Mul(pos + Vector3.one * sizeF - ro, rdInv);

            Vector3 tMin = Vec3Help.Min(t0, t1);
            Vector3 tMax = Vec3Help.Max(t0, t1);

            float tNear = Mathf.Max(tMin.x, Mathf.Max(tMin.y, tMin.z));
            float tFar = Mathf.Min(tMax.x, Mathf.Min(tMax.y, tMax.z));

            normal = Vector3.zero;

            if (tFar < 0.0 || tNear > tFar)
            {
                dist = 0.0f;
                return false;
            }

            dist = (tNear < 0.0f) ? tFar : tNear;

            if (dist > 1000.0f)
            {
                return false;
            }

            Vector3 sig = Vec3Help.Sign(rd);

            normal = Vec3Help.Mul(new Vector3(tNear == tMin.x ? 1.0f : 0.0f, tNear == tMin.y ? 1.0f : 0.0f, tNear == tMin.z ? 1.0f : 0.0f), -sig);

            return true;
        }

        public static float GetInternalDistance(Vector3 po, Vector3 rd, Vector3 pos, float s, out Vector3 n)
        {
            Vector3 b = pos + Vec3Help.Step(Vector3.zero, rd) * s;
            Vector3 t = Vec3Help.Div(b - po, rd);

            float dist = Mathf.Min(t.x, Mathf.Min(t.y, t.z));
            Vector3 sig = Vec3Help.Sign(rd);

            n = Vec3Help.Mul(new Vector3(dist == t.x ? 1.0f : 0.0f, dist == t.y ? 1.0f : 0.0f, dist == t.z ? 1.0f : 0.0f), -sig);

            return dist;
        }

        public static int GetICoord(Vector3 nodePo)
        {
            Vector3Int res = Vec3Help.AND(Vector3Int.FloorToInt(Vec3Help.Abs(Vec3Help.Floor(nodePo))), 1);
            return res.x | (res.y << 1) | (res.z << 2);
        }
    }
}