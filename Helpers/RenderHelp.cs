
using UnityEngine;

namespace EasyVoxel
{
    public static class RenderHelp
    {
        public static void InitComputeBuffer<T>(ref ComputeBuffer buffer, T[] data, float additionaMemoryBufferPercent) where T : struct
        {
            int count = Mathf.Max(1, data.Length);
            int stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));

            if (buffer == null || !buffer.IsValid() || buffer.count < count || buffer.stride != stride)
            {
                ReleaseComputeBuffer(ref buffer);
                buffer = new ComputeBuffer(Mathf.FloorToInt(count * (1.0f + additionaMemoryBufferPercent)), stride, ComputeBufferType.Structured);
            }

            buffer.SetData(data);
        }

        public static void ReleaseComputeBuffer(ref ComputeBuffer buffer)
        {
            if (buffer != null)
            {
                buffer.Release();
                buffer = null;
            }
        }
    }
}