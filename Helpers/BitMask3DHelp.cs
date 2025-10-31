
using UnityEngine;

namespace EasyVoxel
{
    public static class BitMask3DHelp
    {
        public static int[] GetMask(int depth)
        {
            int size = 1 << depth;
            int zSize = size / 32 + 1;

            int[] mask = new int[size * size * zSize];

            return mask;
        }

        public static void SetBit(int[] mask, int depth, int x, int y, int z, bool bit)
        {
            int size = 1 << depth;
            int zSize = size / 32 + 1;

            int xIndex = x * size * zSize;
            int yIndex = y * zSize;
            int zIndex = z / 32;
            int cellIndex = xIndex + yIndex + zIndex;
            int cell = mask[cellIndex];

            int bitIndex = z % 32;
            cell &= ~(1 << bitIndex);
            cell = bit ? cell | (1 << bitIndex) : cell;

            mask[cellIndex] = cell;
        }

        public static bool IsUnitCubeIntersectMask(int[] mask, int depth, UnitCube unitCube)
        {
            int size = 1 << depth;
            int zSize = size / 32 + 1;
            int cubeSize = Mathf.FloorToInt(size * unitCube.UnitSize);
            Vector3Int cubeMin = Vector3Int.FloorToInt(size * (unitCube.Min + Vector3.one / 2.0f));

            for (int x = cubeMin.x; x < cubeMin.x + cubeSize; x++)
            {
                for (int y = cubeMin.y; y < cubeMin.y + cubeSize; y++)
                {
                    int zm = cubeMin.z / 32;
                    int zs = cubeSize / 32 + 1;

                    for (int z = zm; z < zm + zs; z++)
                    {
                        int xIndex = x * size * zSize;
                        int yIndex = y * zSize;
                        int cellIndex = xIndex + yIndex + z;
                        int cell = mask[cellIndex];

                        if (cubeSize < 32)
                        {
                            int startBit = cubeMin.z % 32;
                            int endBit = (cubeMin.z + cubeSize - 1) % 32;

                            int mask0 = ((1 << (endBit - startBit + 1)) - 1) << startBit;

                            if ( (cell & mask0) != 0)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            if (cell != 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}