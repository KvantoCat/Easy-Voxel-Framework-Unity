
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasyVoxel
{
    public class VoxelScene
    {
        private List<VoxelObject> _objects;

        public List<VoxelObject> Objects
        {
            get { return _objects; }
            set { _objects = value; }
        }

        public VoxelScene()
        {
            _objects = new List<VoxelObject>();
        }

        public void AddObject(VoxelObject voxelObject)
        {
            _objects.Add(voxelObject);
        }

        public void TrowInShader(Material shaderMaterial,
            ref ComputeBuffer objectsNodesBuffer, ref ComputeBuffer objectsVTransformBufer,
            string objectsNodesBufferName, string objectsVTransformBuferName, string objectsCountName)
        {
            if (shaderMaterial == null)
            {
                throw new ArgumentException("Null shader");
            }

            List<VoxelOctree> octreeLinks = GetVoxelOctreeLinks();
            OctreeNode[] objectNodes = MergeObjectsNode(octreeLinks);
            VTransformStuct[] objectsVTransform = MergeObjectsVTransform(octreeLinks);

            if (objectNodes.Length == 0 || objectsVTransform.Length == 0 || _objects.Count == 0)
            {
                throw new ArgumentException($"Empty arrays {objectNodes.Length}, {objectsVTransform.Length}");
            }

            RenderHelp.InitComputeBuffer(ref objectsNodesBuffer, objectNodes, 0.03f);
            RenderHelp.InitComputeBuffer(ref objectsVTransformBufer, objectsVTransform, 0.0f);

            shaderMaterial.SetBuffer(objectsNodesBufferName, objectsNodesBuffer);
            shaderMaterial.SetBuffer(objectsVTransformBuferName, objectsVTransformBufer);

            shaderMaterial.SetInt(objectsCountName, _objects.Count);
        }

        public OctreeNode[] MergeObjectsNode(List<VoxelOctree> voxelOctreeLinks)
        {
            int nodesCount = 0;

            foreach (VoxelOctree octreeLink in voxelOctreeLinks)
            {
                nodesCount += octreeLink.Nodes.Count;
            }

            OctreeNode[] result = new OctreeNode[nodesCount];

            int i = 0;

            foreach (VoxelOctree octreeLink in voxelOctreeLinks)
            {
                octreeLink.Nodes.CopyTo(result, i);
                i += octreeLink.Nodes.Count;
            }

            return result;
        }

        public VTransformStuct[] MergeObjectsVTransform(List<VoxelOctree> voxelOctreeLinks)
        {
            VTransformStuct[] result = new VTransformStuct[_objects.Count];

            int i = 0;
            foreach (VoxelObject voxelObject in _objects)
            {
                VTransform vTransform = voxelObject.Transform;

                int j = 0;
                foreach (VoxelOctree voxelOctreeLink in voxelOctreeLinks)
                {
                    if (voxelObject.VoxelOctree == voxelOctreeLink)
                    {
                        break;
                    }

                    j = voxelOctreeLink.Nodes.Count;
                }

                result[i] = new VTransformStuct(vTransform.Scale, vTransform.Position, j, voxelObject.VoxelOctree.Depth);

                i++;
            }

            return result;
        }

        public List<VoxelOctree> GetVoxelOctreeLinks()
        {
            List<VoxelOctree> voxelOctreeLinks = new();

            foreach (VoxelObject voxelObject in _objects)
            {
                if (voxelObject.VoxelOctree == null)
                {
                    continue;
                }

                if (voxelOctreeLinks.Count == 0)
                {
                    voxelOctreeLinks.Add(voxelObject.VoxelOctree);

                    continue;
                }

                bool a = false;

                foreach (VoxelOctree octreeLink in voxelOctreeLinks)
                {
                    if (voxelObject.VoxelOctree == octreeLink)
                    {
                        a = true;
                        break;
                    }
                }

                if (!a)
                {
                    voxelOctreeLinks.Add(voxelObject.VoxelOctree);
                }
            }

            return voxelOctreeLinks;
        }
    }
}