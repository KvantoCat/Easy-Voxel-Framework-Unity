
using UnityEngine;

namespace EasyVoxel
{
    public class VoxelChank : VoxelObject
    {
        public override void Build()
        {
            int[] mask = BitMask3DHelp.GetMask(Depth);
            Debug.Log(mask.Length);
            
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    float height = Mathf.PerlinNoise((float)i / Size, (float)j / Size) * Size;

                    for (int k = 0; k < height; k++)
                    {
                        BitMask3DHelp.SetBit(mask, Depth, i, k, j, true);
                    }
                }
            }

            VoxelOctree.Build(Depth, (UnitCube unitCube) =>
                BitMask3DHelp.IsUnitCubeIntersectMask(mask, Depth, unitCube),
                (Vector3 pos) => GetVoxelColor(pos));
        }

        private Color GetVoxelColor(Vector3 pos)
        {
            float length = Mathf.Pow(Vector3.Magnitude(pos), 1.5f);

            return new Color(length, length, length);
        }
    }
}