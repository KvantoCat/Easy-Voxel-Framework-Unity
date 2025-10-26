
using UnityEngine;

namespace EasyVoxel
{
    [RequireComponent(typeof(MeshFilter))]
    public class VoxelMesh : VoxelObject
    {
        private PolygonalTree _polygonalTree = new();

        public override void Build()
        {
            Mesh mesh = GetComponent<MeshFilter>().sharedMesh;

            if (mesh == null)
            {
                return;
            }

            _polygonalTree.Build(mesh);
            Bounds = _polygonalTree.Bounds;
            VoxelOctree.Build(Depth, (UnitCube unitCube) => _polygonalTree.IsIntersectUnitCube(unitCube), GetColor);
        }

        private Color GetColor(Vector3 vec)
        {
            float length = Mathf.Pow(Vector3.Magnitude(vec), 1.5f);

            return new Color(length, length, length);
        }
    }
}