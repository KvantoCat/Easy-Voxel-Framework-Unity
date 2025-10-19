
using System.Runtime.InteropServices;
using UnityEngine;

namespace EasyVoxel
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct VTransformStuct
    {
        private readonly float _scale;
        private readonly int _index;
        private readonly int _depth;
        private readonly Vector3 _position;

        public readonly float Scale
        {
            get { return _scale; }
        }

        public readonly Vector3 Position
        {
            get { return _position; }
        }

        public readonly int Index
        {
            get { return _index; }
        }

        public VTransformStuct(float scale, Vector3 position, int index, int depth)
        {
            _scale = scale;
            _position = position;
            _index = index;
            _depth = depth;
        }

        public readonly override string ToString()
        {
            return $"{_index}, {_scale}, {_position}, {_depth}";
        }
    }
}