
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
                    float height = Mathf.PerlinNoise(
                        (float)i / Size + transform.position.x / transform.localScale.x, 
                        (float)j / Size + transform.position.z / transform.localScale.z)
                        * Size;
                    int y = Mathf.FloorToInt(height);

                    BitMask3DHelp.SetBit(mask, Depth, i, y, j, true);                         
                    BitMask3DHelp.SetBit(mask, Depth, i, y + 1, j, true);                         
                }
            }

            VoxelOctree.Build(Depth, (UnitCube unitCube) =>
                BitMask3DHelp.IsUnitCubeIntersectMask(mask, Depth, unitCube),
                (Vector3 pos) => GetVoxelColor(pos));
        }

        private Color GetVoxelColor(Vector3 pos)
        {
            return new Color(Random.value, 0.5f, 0.3f);
        }
    }
}