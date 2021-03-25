using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile]
    struct ReorderJob<T> : IJobFor
        where T : struct
    {
        [ReadOnly]
        public NativeArray<int> indices;

        [ReadOnly]
        public NativeArray<T> input;

        [NativeDisableParallelForRestriction]
        public NativeArray<T> output;

        public void Execute(int index)
        {
            var newIndex = indices[index];
            output[newIndex] = input[index];
        }
    }
}
