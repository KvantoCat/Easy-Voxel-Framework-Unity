
using System.Collections.Generic;
using UnityEngine;

namespace EasyVoxel
{
    public readonly struct BoundsBox
    {
        private readonly Vector3 _min;
        private readonly Vector3 _max;

        public readonly Vector3 Min 
        {
            get { return _min; } 
        }

        public readonly Vector3 Max 
        { 
            get { return _max; } 
        }

        public readonly Vector3 Size
        {
            get { return Max - Min; }
        }

        public BoundsBox(Vector3 min, Vector3 max)
        {
            _min = min;
            _max = max;
        }

        public readonly bool IsContain(Vector3 point)
        {
            return (point.x >= _min.x && point.x <= _max.x &&
                    point.y >= _min.y && point.y <= _max.y &&
                    point.z >= _min.z && point.z <= _max.z);
        }

        public static bool IsIntersects(BoundsBox a, BoundsBox b)
        {
            return (a.Min.x <= b.Max.x && a.Max.x >= b.Min.x) &&
                   (a.Min.y <= b.Max.y && a.Max.y >= b.Min.y) &&
                   (a.Min.z <= b.Max.z && a.Max.z >= b.Min.z);
        }

        public static BoundsBox GetFromTriangle(Triangle3D t)
        {
            var min = Vector3.Min(Vector3.Min(t.A, t.B), t.C);
            var max = Vector3.Max(Vector3.Max(t.A, t.B), t.C);
            return new BoundsBox(min, max);
        }

        public static BoundsBox GetFromTriangles(List<Triangle3D> triangles)
        {
            if (triangles.Count == 0)
            {
                return new BoundsBox(Vector3.zero, Vector3.zero);
            }

            Vector3 min = Vector3.one * float.MaxValue;
            Vector3 max = Vector3.one * float.MinValue;

            foreach (var t in triangles)
            {
                min = Vector3.Min(min, Vector3.Min(Vector3.Min(t.A, t.B), t.C));
                max = Vector3.Max(max, Vector3.Max(Vector3.Max(t.A, t.B), t.C));
            }

            return new BoundsBox(min, max);
        }
    }
}

