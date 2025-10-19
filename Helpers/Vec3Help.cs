
using UnityEngine;

namespace EasyVoxel
{
    public static class Vec3Help
    {
        public static bool IsEqual(Vector3Int v1, Vector3Int v2)
        {
            return v1.x == v2.x && v1.y == v2.y && v1.z == v2.z;
        }

        public static Vector3 Div(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x / v2.x, v1.y / v2.y, v1.z / v2.z);
        }

        public static Vector3 Mul(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
        }

        public static Vector3 Mul(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            return new Vector3(v1.x * v2.x * v3.x, v1.y * v2.y * v3.y, v1.z * v2.z * v3.z);
        }

        public static Vector3 Min(Vector3 v1, Vector3 v2)
        {
            return new Vector3(
                v1.x <= v2.x ? v1.x : v2.x,
                v1.y <= v2.y ? v1.y : v2.y,
                v1.z <= v2.z ? v1.z : v2.z);
        }

        public static Vector3 Max(Vector3 v1, Vector3 v2)
        {
            return new Vector3(
                v1.x >= v2.x ? v1.x : v2.x,
                v1.y >= v2.y ? v1.y : v2.y,
                v1.z >= v2.z ? v1.z : v2.z);
        }

        public static Vector3 Sign(Vector3 v)
        {
            return new(v.x < 0 ? -1 : 1, v.y < 0 ? -1 : 1, v.z < 0 ? -1 : 1);
        }

        public static Vector3 Abs(Vector3 v)
        {
            return new Vector3(
                Mathf.Abs(v.x),
                Mathf.Abs(v.y),
                Mathf.Abs(v.z));
        }

        public static Vector3 Floor(Vector3 v)
        {
            return new Vector3(Mathf.Floor(v.x), Mathf.Floor(v.y), Mathf.Floor(v.z));
        }

        public static Vector3 Ceil(Vector3 v)
        {
            return new Vector3(Mathf.Ceil(v.x), Mathf.Ceil(v.y), Mathf.Ceil(v.z));
        }

        public static Vector3 Step(Vector3 v0, Vector3 v1)
        {
            return new Vector3(
                v1.x < v0.x ? 0 : 1,
                v1.y < v0.y ? 0 : 1,
                v1.z < v0.z ? 0 : 1);
        }

        public static Vector3 Lerp(Vector3 v0, Vector3 v1, Vector3 t)
        {
            return new Vector3(
                v0.x + (v1.x - v0.x) * t.x,
                v0.y + (v1.y - v0.y) * t.y,
                v0.z + (v1.z - v0.z) * t.z);
        }

        public static Vector3 LerpInv(Vector3 a, Vector3 b, Vector3 x)
        {
            return Div(x - a, b - a);
        }

        public static Vector3Int AND(Vector3Int v, int a)
        {
            return new Vector3Int(
                v.x & a,
                v.y & a,
                v.z & a);
        }

        public static Vector3Int OR(Vector3Int v, int a)
        {
            return new Vector3Int(
                v.x | a,
                v.y | a,
                v.z | a);
        }

        public static Vector3Int LEFT(Vector3Int v, int a)
        {
            return new Vector3Int(
                v.x << a,
                v.y << a,
                v.z << a);
        }

        public static Vector3Int RIGHT(Vector3Int v, int a)
        {
            return new Vector3Int(
                v.x >> a,
                v.y >> a,
                v.z >> a);
        }
    }
}
