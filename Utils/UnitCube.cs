
using UnityEngine;

namespace EasyVoxel
{
    public readonly struct UnitCube
    {
        private readonly Vector3 _min;
        private readonly float _unitSize;

        public readonly Vector3 Min
        {
            get { return _min; }
        }

        public readonly Vector3 Max
        {
            get { return _min + Vector3.one * _unitSize; }
        }

        public readonly float UnitSize
        {
            get { return _unitSize; }
        }

        public readonly Vector3 Center
        {
            get { return Min + Vector3.one * _unitSize / 2.0f; }
        }

        public UnitCube(Vector3 min, float unitSize)
        {
            _min = min;
            _unitSize = Mathf.Clamp(unitSize, 0.0f, 1.0f);
        }

        public bool IsContain(Vector3 point)
        {
            return IsContain(this, point);
        }

        public static bool IsContain(UnitCube cube, Vector3 point)
        {
            Vector3 min = cube.Min;
            Vector3 max = cube.Max;

            return
            point.x > min.x && point.x <= max.x &&
            point.y > min.y && point.y <= max.y &&
            point.z > min.z && point.z <= max.z;
        }
    }
}