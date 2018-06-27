using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public abstract class ScriptableRenderer
    {
        // Lights are culled per-object. In platforms that don't use StructuredBuffer
        // the engine will set 4 light indices in the following constant unity_4LightIndices0
        // Additionally the engine set unity_4LightIndices1 but LWRP doesn't use that.
        const int k_MaxConstantLocalLights = 4;

        // LWRP uses a fixed constant buffer to hold light data. This must match the value of
        // MAX_VISIBLE_LIGHTS 16 in Input.hlsl
        const int k_MaxVisibleLocalLights = 16;

        const int k_MaxVertexLights = 4;
        public int maxSupportedLocalLightsPerPass
        {
            get
            {
                return useComputeBufferForPerObjectLightIndices ? k_MaxVisibleLocalLights : k_MaxConstantLocalLights;
            }
        }

        // TODO: Profile performance of using ComputeBuffer on mobiles that support it
        public bool useComputeBufferForPerObjectLightIndices
        {
            get
            {
                return SystemInfo.supportsComputeShaders &&
                       SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLCore &&
                       !Application.isMobilePlatform &&
                       Application.platform != RuntimePlatform.WebGLPlayer;
            }
        }

        public int maxVisibleLocalLights { get { return k_MaxVisibleLocalLights; } }

        public int maxSupportedVertexLights { get { return k_MaxVertexLights; } }

        public abstract void Dispose();
        public abstract void Setup(ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData);
        public abstract void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData);
    }
}
