
using System.Collections.Generic;
using UnityEngine;

namespace EasyVoxel
{
    public readonly struct Box3D
    {
        private readonly Vector3 _min;
        private readonly Vector3 _max;

        public Vector3 Min
        {
            get { return _min; }
        }

        public Vector3 Max
        {
            get { return _max; }
        }

        public Vector3 Size
        {
            get { return Max - Min; }
        }

        public Box3D(Vector3 min, Vector3 max)
        {
            _min = min;
            _max = max;
        }

        public static Box3D GetFromTriangles(List<Triangle3D> triangles)
        {
            if (triangles.Count == 0)
            {
                return new Box3D(Vector3.zero, Vector3.zero);
            }

            Vector3 min = Vector3.one * float.MaxValue;
            Vector3 max = Vector3.one * float.MinValue;

            foreach (var t in triangles)
            {
                min = Vector3.Min(min, Vector3.Min(Vector3.Min(t.A, t.B), t.C));
                max = Vector3.Max(max, Vector3.Max(Vector3.Max(t.A, t.B), t.C));
            }

            return new Box3D(min, max);
        }

        public static bool IsIntersects(Box3D box0, Box3D box1)
        {
            return (box0.Min.x <= box1.Max.x && box0.Max.x >= box1.Min.x) &&
                   (box0.Min.y <= box1.Max.y && box0.Max.y >= box1.Min.y) &&
                   (box0.Min.z <= box1.Max.z && box0.Max.z >= box1.Min.z);
        }
    }
}