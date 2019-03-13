using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
#if ENABLE_RAYTRACING

    public static class HDRaytracingLightProbeBakeManager
    {
        public static event System.Action<Camera, CommandBuffer> bakeLightProbes;
        public static void Bake(Camera camera, CommandBuffer cmdBuffer)
        {
            bakeLightProbes?.Invoke(camera, cmdBuffer);
        }
    }
#endif
}
