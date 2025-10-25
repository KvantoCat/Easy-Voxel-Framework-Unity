
using System;
using UnityEngine;

namespace EasyVoxel
{
    public static class MathHelp
    {
        public static int ConvertColorRGB15(Color color)
        {
            int rgb = 0;
            int r = (int)(color.r * 31.0f);
            int g = (int)(color.g * 31.0f);
            int b = (int)(color.b * 31.0f);
            rgb |= r << 10;
            rgb |= g << 5;
            rgb |= b;  
            rgb &= 0x3fffffff;

            return rgb;
        }

        public static int PopCount(int x)
        {
            x -= ((x >> 1) & 0x55555555);
            x = (x & 0x33333333) + ((x >> 2) & 0x33333333);
            x = (x + (x >> 4)) & 0x0F0F0F0F;
            x *=  0x01010101;
            return x >> 24;
        }

        public static bool IsTriangleIntersectBox(Box3D box, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 boxCenter = (box.Min + box.Max) * 0.5f;
            Vector3 boxHalfSize = (box.Max - box.Min) * 0.5f;

            Vector3 v0 = a - boxCenter;
            Vector3 v1 = b - boxCenter;
            Vector3 v2 = c - boxCenter;

            Vector3[] verts = { v0, v1, v2 };
            Vector3[] boxAxes = { Vector3.right, Vector3.up, Vector3.forward };

            Vector3 triNormal = Vector3.Cross(v1 - v0, v2 - v0);

            if (!OverlapOnAxis(verts, boxHalfSize, triNormal))
            {
                return false;
            }

            Vector3[] edges = { v1 - v0, v2 - v1, v0 - v2 };

            foreach (var edge in edges)
            {
                foreach (var axis in boxAxes)
                {
                    Vector3 testAxis = Vector3.Cross(edge, axis);

                    if (testAxis.sqrMagnitude > 1e-6f) 
                    {
                        if (!OverlapOnAxis(verts, boxHalfSize, testAxis))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool OverlapOnAxis(Vector3[] verts, Vector3 boxHalfSize, Vector3 axis)
        {
            float triMin = float.MaxValue;
            float triMax = float.MinValue;

            foreach (var v in verts)
            {
                float proj = Vector3.Dot(v, axis);

                triMin = Mathf.Min(triMin, proj);
                triMax = Mathf.Max(triMax, proj);
            }

            float r = boxHalfSize.x * Mathf.Abs(axis.x) +
                      boxHalfSize.y * Mathf.Abs(axis.y) +
                      boxHalfSize.z * Mathf.Abs(axis.z);

            if (triMin > r || triMax < -r)
            {
                return false;
            }

            return true;
        }

        [Obsolete("", true)]
        public static bool IsPointOnTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c) 
        {  
            return true;
        }
    }
}
