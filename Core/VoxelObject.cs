
using System;
using UnityEngine;

namespace EasyVoxel
{
    [DisallowMultipleComponent]
    public class VoxelObject : MonoBehaviour
    {
        [SerializeField, Range(1, 9)] private int _depth = 5;

        private VoxelOctree _voxelOctree;
        private Bounds _bounds;

        public VoxelOctree VoxelOctree
        {
            get { return _voxelOctree; }
            set 
            { 
                _voxelOctree = value;
                _bounds = new(Vector3.zero, Vector3.one);
                _depth = CalculateDepth();
            }
        }

        public int Depth
        {
            get { return _depth; }
        }

        public Bounds Bounds
        {
            get { return _bounds; }
        }

        public int MinVoxelSize
        {
            get { return 1 << (_depth); }
        }

        public float MinVoxelScale
        {
            get { return transform.localScale.x / MinVoxelSize; }
        }

        public void Build(Vector3 point, Func<Vector3, Color> colorFunc)
        {
            _voxelOctree ??= new VoxelOctree();
            _bounds = new(Vector3.zero, Vector3.one);
            _voxelOctree.Build(_depth, (UnitCube unitCube) => unitCube.IsContain(point), colorFunc);
        }

        public void Build(PolygonalTree polygonTree, Func<Vector3, Color> colorFunc)
        {
            _voxelOctree ??= new VoxelOctree();
            _bounds = polygonTree.Bounds;
            _voxelOctree.Build(_depth, (UnitCube unitCube) => polygonTree.IsIntersectUnitCube(unitCube), colorFunc);
        }

        public void SetVoxel(Vector3 pointPos, Vector3 normal, Color color)
        {
            pointPos += normal / (1 << _depth) / 2.0f * transform.localScale.x;
            Vector3 pointPosNew = (pointPos - transform.position) / transform.localScale.x;

            VoxelOctree voxelOctree = new();
            Build(pointPosNew, (Vector3 voxPos) => SetVoxelColorFunction(voxPos, pointPos, color));

            _voxelOctree.MergeWith(voxelOctree);
        }

        private Color SetVoxelColorFunction(Vector3 voxPos, Vector3 pointPos, Color color)
        {
            Vector3Int pointCoord = Vector3Int.FloorToInt((pointPos - transform.position) / MinVoxelScale);
            Vector3Int voxCoord = Vector3Int.FloorToInt(voxPos * MinVoxelSize);

            return Vec3Help.IsEqual(pointCoord, voxCoord) ? color : Color.black;
        }

        public int CalculateDepth()
        {
            if (_voxelOctree.Nodes.Count == 0) 
            {
                return -1; 
            }

            int count = 1;
            OctreeNode node = _voxelOctree.Nodes[0];

            while (node.Child != -1)
            {
                for (int i = 0; i < MathHelp.PopCount(node.Mask); i++)
                {
                    if (node.Child != -1)
                    {
                        count++;
                        node = _voxelOctree.Nodes[node.Child + i];
                        break;
                    }
                }
            }

            return count;
        }

        [Obsolete("Old method", false)]
        public bool RayTrace(Vector3 ro, Vector3 rd, out float distance, out Vector3 normal)
        {
            float epsilon = 0.000707f * transform.localScale.x / _depth;

            Vector3 pos = transform.position - 0.5f * transform.localScale.x * Vector3.one;
            rd += new Vector3(rd.x == 0.0f ? 1.0f : 0.0f, rd.y == 0.0f ? 1.0f : 0.0f, rd.z == 0.0f ? 1.0f : 0.0f) * 1e-6f;

            distance = 0.0f;
            normal = Vector3.zero;
            bool isHit = false;

            if (!(ro.x >= pos.x && ro.x < pos.x + transform.localScale.x &&
                 ro.y >= pos.y && ro.y < pos.y + transform.localScale.x &&
                 ro.z >= pos.z && ro.z < pos.z + transform.localScale.x))
            {
                if (!RayTracingHelpOLD.GetBoxIntersection(ro, rd, pos, transform.localScale.x, out distance, out normal))
                {
                    distance = 1000.0f;
                    return isHit;
                }

                normal *= -1;
                distance += epsilon;
            }

            int depth = 0;
            int stack = 0;
            Vector3 po = ro + rd * distance;

            OctreeNode node = _voxelOctree.Nodes[0];

            int iter = 0;
            while (depth < 10u)
            {
                iter++;

                if (iter == 150)
                {
                    isHit = false;
                    break;
                }

                Vector3 nodePo = (po - pos) * (1 << depth) / transform.localScale.x;
                int nodeICoord = RayTracingHelpOLD.GetICoord(nodePo);

                int nodeICoordFromStack = (stack >> (depth * 3)) & 7;
                int childDepth = depth + 1;

                if (nodeICoord == nodeICoordFromStack)
                {
                    float childScale = transform.localScale.x / (1u << childDepth);
                    Vector3 childPo = (po - pos) / childScale;
                    int childICoord = RayTracingHelpOLD.GetICoord(childPo);

                    if ((node.Mask & (1u << childICoord)) == 0u)
                    {
                        Vector3 childPos = Vec3Help.Floor(childPo) * childScale + pos;

                        float dist = RayTracingHelpOLD.GetInternalDistance(po, rd, childPos, childScale, out normal);
                        float distE = dist + epsilon;

                        if (distE <= 0.0)
                        {
                            break;
                        }

                        distance += distE;
                        po = ro + rd * distance;
                    }
                    else
                    {
                        if (node.Child == -1)
                        {
                            isHit = true;
                            break;
                        }

                        stack &= ~(7 << (childDepth * 3));
                        stack |= (childICoord << (childDepth * 3));

                        int count = MathHelp.PopCount((int)node.Mask & ((1 << childICoord) - 1));
                        node = _voxelOctree.Nodes[node.Child + count];
                        depth += 1;
                    }
                }
                else
                {
                    if (depth == 0u)
                    {
                        break;
                    }

                    depth -= 1;
                    node = _voxelOctree.Nodes[node.Parent];
                }
            }

            if (!isHit)
            {
                distance = 1000.0f;
                normal = Vector3.zero;
            }

            return isHit;
        }
    }
}