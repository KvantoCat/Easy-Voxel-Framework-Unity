
using UnityEngine;

namespace EasyVoxel
{
    public class VoxelAbstraction : VoxelObject
    {
        public override void Build()
        {
            VoxelOctree.Build(Depth, (UnitCube unitCube) => Random.value < 0.8f, (Vector3 pos) => GetVoxelColor(pos));

            if (VoxelOctree.Nodes.Count == 0)
            {
                VoxelOctree.Nodes = new() { new OctreeNode(0, -1, -1, 0, 0, 0, 0) };
            }
        }

        private Color GetVoxelColor(Vector3 pos)
        {
            float length = Mathf.Pow(Vector3.Magnitude(pos), 1.5f);

            return new Color(length, length, length);
        }
    }
}