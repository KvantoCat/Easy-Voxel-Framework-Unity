
using System.Collections.Generic;

namespace EasyVoxel
{
    public class NodeBvh
    {
        public NodeBvh Left;
        public NodeBvh Right;
        public Box Box;

        public List<Triangle3D> Triangles;

        public bool IsLeaf
        {
            get { return Left == null && Right == null; }
        }
    }
}