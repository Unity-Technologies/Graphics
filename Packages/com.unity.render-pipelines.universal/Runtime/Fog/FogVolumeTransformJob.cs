#if VOLUMETRIC_FOG

using Unity.Burst;
using Unity.Collections;
using UnityEngine.Jobs;

namespace UnityEngine.Rendering.Universal
{
    [BurstCompile]
    struct FogVolumeTransformJob : IJobParallelForTransform
    {
        [WriteOnly] public NativeArray<FogVolumeBox> boxes;

        public void Execute(int index, TransformAccess transform)
        {
            boxes[index] = new FogVolumeBox(transform.localToWorldMatrix);
        }
    }
}

#endif
