
namespace EasyVoxel
{
    public class VoxelEmpty : VoxelObject
    {
        public override void Build()
        {
            VoxelOctree.Nodes = new() { new OctreeNode(0, -1, -1, 0, 0, 0, 0) };
        }
    }
}