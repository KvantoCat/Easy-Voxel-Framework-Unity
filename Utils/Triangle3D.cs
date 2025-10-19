
using UnityEngine;

namespace EasyVoxel
{
    public readonly struct Triangle3D
    {
        private readonly Vector3 _a;
        private readonly Vector3 _b;
        private readonly Vector3 _c;

        public readonly Vector3 A
        {
            get { return _a; }
        }

        public readonly Vector3 B
        {
            get { return _b; }
        }

        public readonly Vector3 C
        {
            get { return _c; }
        }

        public readonly Vector3 Centroid
        {
            get { return (_a + _b + _c) / 3.0f; }
        }

        public Triangle3D(Vector3 a, Vector3 b, Vector3 c)
        {
            _a = a;
            _b = b;
            _c = c;
        }
    }
}