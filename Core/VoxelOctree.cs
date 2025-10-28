
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyVoxel
{
    public class VoxelOctree
    {
        private List<OctreeNode> _nodes;

        public List<OctreeNode> Nodes
        {
            get { return _nodes; }
            set { _nodes = value; }
        }

        public VoxelOctree()
        {
            _nodes = new();
        }

        public VoxelOctree(List<OctreeNode> nodes, bool isCopy)
        {
            _nodes = isCopy ? new(nodes) : nodes;
        }

        public void Build(int depth, Func<UnitCube, bool> intersectUnitCubeFunc, Func<Vector3, Color> colorFunc)
        {
            Clear();

            UnitCube cube = new(-0.5f * Vector3.one, 1.0f);

            if (!intersectUnitCubeFunc(cube))
            {
                return;
            }

            float voxelSize = 1.0f / (1 << depth);
            Queue<(Vector3 pos, float size, int parentInd)> queue = new();
            queue.Enqueue((-0.5f * Vector3.one, 1.0f, -1));

            while (queue.Count > 0)
            {
                var (pos, size, parentInd) = queue.Dequeue();
                float halfSize = size / 2.0f;
                int mask = 0;
                int child = 0;

                int col0 = 0;
                int col1 = 0;
                int col2 = 0;
                int col3 = 0;

                for (int i = 0; i < 8; i++)
                {
                    float dx = (i & 1) != 0 ? halfSize : 0;
                    float dy = (i & 2) != 0 ? halfSize : 0;
                    float dz = (i & 4) != 0 ? halfSize : 0;
                    Vector3 d = new(dx, dy, dz);
                    Vector3 dPos = pos + d;
                    cube = new(dPos, halfSize);

                    if (intersectUnitCubeFunc(cube))
                    {
                        mask |= (1 << i);

                        if (halfSize > voxelSize)
                        {
                            int newChildIndex = _nodes.Count + queue.Count + 1;

                            queue.Enqueue((dPos, halfSize, _nodes.Count));

                            if (child == 0)
                            {
                                child = newChildIndex;
                            }
                        }
                        else
                        {
                            child = -1;

                            int k = i / 2;
                            int m = i % 2;
                            int g = m == 0 ? 0 : 15;

                            if (k == 0)
                            {
                                col0 |= MathHelp.ConvertColorRGB15(colorFunc(dPos)) << g;
                            }
                            else if (k == 1)
                            {
                                col1 |= MathHelp.ConvertColorRGB15(colorFunc(dPos)) << g;
                            }
                            else if (k == 2)
                            {
                                col2 |= MathHelp.ConvertColorRGB15(colorFunc(dPos)) << g;
                            }
                            else if (k == 3)
                            {
                                col3 |= MathHelp.ConvertColorRGB15(colorFunc(dPos)) << g;
                            }
                        }
                    }
                }

                _nodes.Add(new(mask, child, parentInd, col0, col1, col2, col3));
            }
        }

        public void MergeWith(VoxelOctree tree)
        {
            VoxelOctree copyTree = new(_nodes, true);
            _nodes.Clear();

            VoxelOctree resultTree = Merge(copyTree, tree);

            _nodes = resultTree.Nodes;
        }

        public static VoxelOctree Merge(VoxelOctree tree0, VoxelOctree tree1)
        {
            List<OctreeNode> nodes = new();

            Queue<(int rNodeInd, int lNodeInd, int parentInd)> queue = new();
            queue.Enqueue((0, 0, -1));

            while (queue.Count > 0)
            {
                var (rNodeInd, lNodeInd, parentInd) = queue.Dequeue();

                OctreeNode? rNode = rNodeInd != -1 ? tree0.Nodes[rNodeInd] : null;
                OctreeNode? lNode = lNodeInd != -1 ? tree1.Nodes[lNodeInd] : null;

                int mask = 0;
                int child = 0;

                int col0 = 0;
                int col1 = 0;
                int col2 = 0;
                int col3 = 0;

                for (int i = 0; i < 8; i++)
                {
                    bool rIsNode = rNode != null && (rNode.Value.Mask & (1u << i)) != 0;
                    bool lIsNode = lNode != null && (lNode.Value.Mask & (1u << i)) != 0;

                    if (rIsNode || lIsNode)
                    {
                        mask |= (1 << i);

                        int rChildIndex = -1;
                        int lChildIndex = -1;

                        if (rIsNode && rNode.Value.Child != -1)
                        {
                            rChildIndex = rNode.Value.Child + MathHelp.PopCount(rNode.Value.Mask & ((1 << i) - 1));
                        }

                        if (lIsNode && lNode.Value.Child != -1)
                        {
                            lChildIndex = lNode.Value.Child + MathHelp.PopCount(lNode.Value.Mask & ((1 << i) - 1));
                        }

                        if (rChildIndex != -1 || lChildIndex != -1)
                        {
                            child = child == 0 ? nodes.Count + queue.Count + 1 : child;
                            queue.Enqueue((rChildIndex, lChildIndex, nodes.Count));
                        }
                        else
                        {
                            child = -1;
                        }
                    }
                }

                if (rNode != null && lNode != null)
                {
                    col0 = rNode.Value.Col0 | lNode.Value.Col0;
                    col1 = rNode.Value.Col1 | lNode.Value.Col1;
                    col2 = rNode.Value.Col2 | lNode.Value.Col2;
                    col3 = rNode.Value.Col3 | lNode.Value.Col3;
                }
                else
                {
                    if (rNode != null)
                    {
                        col0 = rNode.Value.Col0;
                        col1 = rNode.Value.Col1;
                        col2 = rNode.Value.Col2;
                        col3 = rNode.Value.Col3;
                    }
                    else if (lNode != null)
                    {
                        col0 = lNode.Value.Col0;
                        col1 = lNode.Value.Col1;
                        col2 = lNode.Value.Col2;
                        col3 = lNode.Value.Col3;
                    }
                }

                nodes.Add(new(mask, child, parentInd, col0, col1, col2, col3));
            }

            return new(nodes, false);
        }

        public void Clear()
        {
            _nodes.Clear();
        }

        public void Print(bool printAll)
        {
            Debug.Log($"Nodes count: {_nodes.Count}");

            if (_nodes.Count < 250 && printAll)
            {
                for (int i = 0; i < _nodes.Count; i++)
                {
                    Debug.Log($"{i}: {_nodes[i]}");
                }
            }
        }

        [Obsolete("Very slow", true)]
        public void BuildOld(uint[] grid, Vector3Int gridSize)
        {
            int maxGridSide = (int)Mathf.Max(Mathf.NextPowerOfTwo(gridSize.x),
                Mathf.Max(Mathf.NextPowerOfTwo(gridSize.y), Mathf.NextPowerOfTwo(gridSize.z)));

            _nodes.Clear();

            Queue<(Vector3Int coord, int size, int parentInd)> queue = new();
            queue.Enqueue((Vector3Int.zero, maxGridSide, -1));

            while (queue.Count > 0)
            {
                var (coord, size, parentInd) = queue.Dequeue();
                int halfSize = size / 2;
                int mask = 0;
                int child = 0;

                for (int i = 0; i < 8; i++)
                {
                    int dx = (i & 1) != 0 ? halfSize : 0;
                    int dy = (i & 2) != 0 ? halfSize : 0;
                    int dz = (i & 4) != 0 ? halfSize : 0;

                    Vector3Int dcoord = new(dx, dy, dz);

                    if (!IsGridBoxEmptyOld(grid, coord + dcoord, halfSize, gridSize))
                    {
                        mask |= (1 << i);

                        if (halfSize > 1)
                        {
                            int newChildIndex = _nodes.Count + queue.Count + 1;

                            queue.Enqueue((coord + dcoord, halfSize, _nodes.Count));

                            if (child == 0)
                            {
                                child = newChildIndex;
                            }
                        }
                        else
                        {
                            child = -1;
                        }
                    }
                }

                int r0 = (int)UnityEngine.Random.Range(0, int.MaxValue);
                int r1 = (int)UnityEngine.Random.Range(0, int.MaxValue);
                int r2 = (int)UnityEngine.Random.Range(0, int.MaxValue);
                int r3 = (int)UnityEngine.Random.Range(0, int.MaxValue);
                _nodes.Add(new(mask, child, parentInd, r0, r1, r2, r3));
            }
        }

        [Obsolete("Very slow", true)]
        public static uint[] BuildGridOld(Mesh mesh, float voxelSize, out Vector3Int gridSize)
        {
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;

            int gridSizeX = Mathf.CeilToInt(mesh.bounds.size.x / voxelSize);
            int gridSizeY = Mathf.CeilToInt(mesh.bounds.size.y / voxelSize);
            int gridSizeZ = Mathf.CeilToInt(mesh.bounds.size.z / voxelSize);
            int voxelsCount = gridSizeX * gridSizeY * gridSizeZ;
            gridSize = new(gridSizeX, gridSizeY, gridSizeZ);

            uint[] result = new uint[(voxelsCount + 31) / 32];

            for (int i = 0; i < mesh.triangles.Length / 3; i++)
            {
                Vector3 v0 = Vec3Help.Mul(Vec3Help.LerpInv(mesh.bounds.min, mesh.bounds.max, vertices[triangles[i * 3 + 0]]), ((Vector3)gridSize - Vector3.one));
                Vector3 v1 = Vec3Help.Mul(Vec3Help.LerpInv(mesh.bounds.min, mesh.bounds.max, vertices[triangles[i * 3 + 1]]), ((Vector3)gridSize - Vector3.one));
                Vector3 v2 = Vec3Help.Mul(Vec3Help.LerpInv(mesh.bounds.min, mesh.bounds.max, vertices[triangles[i * 3 + 2]]), ((Vector3)gridSize - Vector3.one));

                Vector3 triMin = Vec3Help.Min(v0, Vec3Help.Min(v1, v2));
                Vector3 triMax = Vec3Help.Max(v0, Vec3Help.Max(v1, v2));

                Vector3Int min = Vector3Int.RoundToInt(Vec3Help.Floor(triMin));
                Vector3Int max = Vector3Int.RoundToInt(Vec3Help.Ceil(triMax));

                Vector3 normal = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));

                for (int x = min.x; x <= max.x; x++)
                {
                    for (int y = min.y; y <= max.y; y++)
                    {
                        for (int z = min.z; z <= max.z; z++)
                        {
                            if (x < 0 || y < 0 || z < 0 || x >= gridSize.x || y >= gridSize.y || z >= gridSize.z)
                            {
                                continue;
                            }

                            Vector3 p = new(x, y, z);

                            float dist = Vector3.Dot(p - v0, normal);

                            if (Mathf.Abs(dist) < 0.7)
                            {
                                if (MathHelp.IsPointOnTriangle(p - normal * dist, v0, v1, v2))
                                {
                                    int coord = x + y * gridSize.x + z * gridSize.x * gridSize.y;

                                    int index = coord / 32;
                                    uint bits = 1u << (coord % 32);

                                    result[index] |= bits;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        [Obsolete("Very slow", true)]
        private bool IsGridBoxEmptyOld(uint[] grid, Vector3Int coord, int size, Vector3Int greedSize)
        {
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    for (int k = 0; k < size; k++)
                    {
                        Vector3Int dcoord = new Vector3Int(i, j, k) + coord;

                        if (dcoord.x >= greedSize.x || dcoord.y >= greedSize.y || dcoord.z >= greedSize.z)
                        {
                            continue;
                        }

                        if (GetBitFromGridOld(grid, dcoord.x + dcoord.y * greedSize.x + dcoord.z * greedSize.x * greedSize.y))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        [Obsolete("Very slow", true)]
        private static bool GetBitFromGridOld(uint[] grid, int index)
        {
            return (grid[index / 32] & (1u << (index % 32))) != 0;
        }
    }
}
