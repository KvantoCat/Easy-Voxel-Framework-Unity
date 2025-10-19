
using System;
using UnityEngine;

namespace EasyVoxel
{
    public class VoxelObject
    {
        private VTransform _transform;
        private VoxelOctree _voxelOctree;

        public VTransform Transform
        {
            get { return _transform; }
            set { _transform = value; }
        }

        public VoxelOctree VoxelOctree
        {
            get { return _voxelOctree; }
            set { _voxelOctree = value; }
        }

        public int MinVoxelSize
        {
            get { return 1 << (_voxelOctree.Depth); }
        }

        public float MinVoxelScale
        {
            get { return _transform.Scale / MinVoxelSize; }
        }

        public VoxelObject()
        {
            _transform = new VTransform();
            _voxelOctree = null;
        }

        public VoxelObject(VTransform transform, VoxelOctree voxelOctree)
        {
            _transform = transform;
            _voxelOctree = voxelOctree;
        }

        public void SetVoxel(Vector3 pointPos, Vector3 normal, Color color)
        {
            pointPos += normal / (1 << _voxelOctree.Depth) / 2.0f * _transform.Scale;
            Vector3 pointPosNew = (pointPos - _transform.Position) / _transform.Scale;

            VoxelOctree voxelOctree = new();
            voxelOctree.Build(_voxelOctree.Depth, pointPosNew, (Vector3 voxPos) => SetVoxelColorFunction(voxPos, pointPos, color));

            _voxelOctree.MergeWith(voxelOctree);
        }

        private Color SetVoxelColorFunction(Vector3 voxPos, Vector3 pointPos, Color color)
        {
            Vector3Int pointCoord = Vector3Int.FloorToInt((pointPos - _transform.Position) / MinVoxelScale);
            Vector3Int voxCoord = Vector3Int.FloorToInt(voxPos * MinVoxelSize);

            return Vec3Help.IsEqual(pointCoord, voxCoord) ? color : Color.black;
        }

        [Obsolete("Old method", false)]
        public bool RayTrace(Vector3 ro, Vector3 rd, out float distance, out Vector3 normal)
        {
            float epsilon = 0.000707f * _transform.Scale / _voxelOctree.Depth;

            Vector3 pos = _transform.Position - 0.5f * _transform.Scale * Vector3.one;
            rd += new Vector3(rd.x == 0.0f ? 1.0f : 0.0f, rd.y == 0.0f ? 1.0f : 0.0f, rd.z == 0.0f ? 1.0f : 0.0f) * 1e-6f;

            distance = 0.0f;
            normal = Vector3.zero;
            bool isHit = false;

            if (!(ro.x >= pos.x && ro.x < pos.x + _transform.Scale &&
                 ro.y >= pos.y && ro.y < pos.y + _transform.Scale &&
                 ro.z >= pos.z && ro.z < pos.z + _transform.Scale))
            {
                if (!RayTracingHelpOLD.GetBoxIntersection(ro, rd, pos, _transform.Scale, out distance, out normal))
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

                Vector3 nodePo = (po - pos) * (1 << depth) / _transform.Scale;
                int nodeICoord = RayTracingHelpOLD.GetICoord(nodePo);

                int nodeICoordFromStack = (stack >> (depth * 3)) & 7;
                int childDepth = depth + 1;

                if (nodeICoord == nodeICoordFromStack)
                {
                    float childScale = _transform.Scale / (1u << childDepth);
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