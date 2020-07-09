using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal struct TileAllocations
    {
        public int4 tileRect;

        public NativeArray<int> searchBuffer0Size;
        public NativeArray<int> searchBuffer1Size;

        public NativeArray<OutlineSearchNode> searchBuffer0;
        public NativeArray<OutlineSearchNode> searchBuffer1;

        public TileAllocations(int4 rect, int alphaCutMin, int alphaCutMax)
        {
            int arraySize = (rect.z + 2 * OutlineConstants.k_BorderSize) * (rect.w + 2 * OutlineConstants.k_BorderSize);
            tileRect = rect;

            searchBuffer0Size = new NativeArray<int>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            searchBuffer1Size = new NativeArray<int>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            searchBuffer0 = new NativeArray<OutlineSearchNode>(arraySize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            searchBuffer1 = new NativeArray<OutlineSearchNode>(arraySize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        }

        public void Dispose()
        {
            searchBuffer0Size.Dispose();
            searchBuffer1Size.Dispose();
            searchBuffer0.Dispose();
            searchBuffer1.Dispose();
        }
    }
}
