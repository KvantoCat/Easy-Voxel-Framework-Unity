
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyVoxel
{
    public class PolygonalTree
    {
        private NodeBvh _rootNodeBvh;
        private Bounds _bounds;

        public NodeBvh RootNode 
        { 
            get { return _rootNodeBvh; } 
        }

        public Bounds Bounds
        {
            get { return _bounds; }
        }

        public void Build(Mesh mesh)
        {
            List<Triangle3D> triangles = GetTriangles(mesh);
            _bounds = mesh.bounds;
            _rootNodeBvh = BuildNode(triangles);
        }

        private NodeBvh BuildNode(List<Triangle3D> triangles)
        {
            NodeBvh node = new()
            {
                Box = Box.GetFromTriangles(triangles)
            };

            if (triangles.Count <= 2)
            {
                node.Triangles = triangles;

                return node;
            }

            Vector3 boundsSize = node.Box.Size;
            int axis = 0;

            if (boundsSize.y > boundsSize.x && boundsSize.y > boundsSize.z)
            { 
                axis = 1; 
            }
            else if (boundsSize.z > boundsSize.x) 
            { 
                axis = 2; 
            }

            triangles.Sort((a, b) => a.Centroid[axis].CompareTo(b.Centroid[axis]));

            int mid = triangles.Count / 2;
            List<Triangle3D> leftTriangles = triangles.Take(mid).ToList();
            List<Triangle3D> rightTriangles = triangles.Skip(mid).ToList();

            node.Left = BuildNode(leftTriangles);
            node.Right = BuildNode(rightTriangles);

            return node;
        }

        public bool IsIntersectUnitCube(UnitCube cube)
        {
            if (_rootNodeBvh != null)
            {
                Vector3 boundBoxSize = _bounds.size;
                float maxBoundBoxSize = Mathf.Max(Mathf.Max(boundBoxSize.x, boundBoxSize.y), boundBoxSize.z);

                Box box = new(
                    (cube.Min + _bounds.center / 2.0f) * maxBoundBoxSize,
                    (cube.Max + _bounds.center / 2.0f) * maxBoundBoxSize);

                return IsNodeIntersectBox(_rootNodeBvh, box);
            }

            return false;
        }

        private static bool IsNodeIntersectBox(NodeBvh node, Box box)
        {
            if (!Box.IsIntersects(node.Box, box))
            {
                return false;
            }

            if (node.IsLeaf)
            {
                foreach (var tri in node.Triangles)
                {
                    if (MathHelp.IsTriangleIntersectBox(box, tri.A, tri.B, tri.C))
                    {
                        return true;
                    }
                }

                return false;
            }

            bool hitLeft = node.Left != null && IsNodeIntersectBox(node.Left, box);
            if (hitLeft) return true;

            bool hitRight = node.Right != null && IsNodeIntersectBox(node.Right, box);
            return hitRight;
        }

        public static List<Triangle3D> GetTriangles(Mesh mesh)
        {
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;

            return GetTriangles(triangles, vertices);
        }

        public static List<Triangle3D> GetTriangles(int[] triangles, Vector3[] vertices)
        {
            List<Triangle3D> trianglesList = new();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Triangle3D triangle = new(
                    vertices[triangles[i]],
                    vertices[triangles[i + 1]],
                    vertices[triangles[i + 2]]
                );

                trianglesList.Add(triangle);
            }

            return trianglesList;
        }

        public void Clear()
        {
            if (_rootNodeBvh != null)
            {
                ClearNode(_rootNodeBvh);
                _rootNodeBvh = null;
            }
        }

        private static void ClearNode(NodeBvh node)
        {
            if (node == null)
            {
                return;
            }

            ClearNode(node.Left);
            ClearNode(node.Right);

            node.Left = null;
            node.Right = null;
        }
    }
}
