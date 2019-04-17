using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
#if ENABLE_RAYTRACING

    public static class HDRaytracingLightProbeBakeManager
    {
        public static event System.Action<Camera, CommandBuffer> preRenderLightProbes;
        public static void PreRender(Camera camera, CommandBuffer cmdBuffer)
        {
            preRenderLightProbes?.Invoke(camera, cmdBuffer);
        }

        public static event System.Action<Camera, CommandBuffer, SkyManager> bakeLightProbes;
        public static void Bake(Camera camera, CommandBuffer cmdBuffer, SkyManager skyManager)
        {
            bakeLightProbes?.Invoke(camera, cmdBuffer, skyManager);
        }
    }
#endif
}
