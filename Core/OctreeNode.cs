
using System.Runtime.InteropServices;
using System;

namespace EasyVoxel
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct OctreeNode
    {
        private readonly int _mask;
        private readonly int _child;
        private readonly int _parent;
        private readonly int _col0;
        private readonly int _col1;
        private readonly int _col2;
        private readonly int _col3;

        public OctreeNode(int mask, int child, int parent, int col0, int col1, int col2, int col3)
        {
            _mask = mask;
            _child = child;
            _parent = parent;
            _col0 = col0;
            _col1 = col1;
            _col2 = col2;
            _col3 = col3;
        }

        public readonly int Mask
        {
            get { return _mask; }
        }

        public readonly int Child
        {
            get { return _child; }
        }

        public readonly int Parent
        {
            get { return _parent; }
        }

        public readonly int Col0 { get { return _col0; } }
        public readonly int Col1 { get { return _col1; } }
        public readonly int Col2 { get { return _col2; } }
        public readonly int Col3 { get { return _col3; } }

        public readonly override string ToString()
        {
            return $"{_child}, {Convert.ToString(_mask, 2).PadLeft(8, '0')}, {_parent}, {Col0}, {Col1}, {Col2}, {Col3}";
        }
    }
}