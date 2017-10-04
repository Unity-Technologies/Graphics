using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class HDUtils
    {
        public const RendererConfiguration k_RendererConfigurationBakedLighting = RendererConfiguration.PerObjectLightProbe | RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbeProxyVolume;

        public static Matrix4x4 GetViewProjectionMatrix(Matrix4x4 worldToViewMatrix, Matrix4x4 projectionMatrix)
        {
            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)
            var gpuProj = GL.GetGPUProjectionMatrix(projectionMatrix, false);
            var gpuVP = gpuProj *  worldToViewMatrix * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API.

            return gpuVP;
        }

        // Helper to help to display debug info on screen
        static float s_OverlayLineHeight = -1.0f;
        public static void NextOverlayCoord(ref float x, ref float y, float overlayWidth, float overlayHeight, float width)
        {
            x += overlayWidth;
            s_OverlayLineHeight = Mathf.Max(overlayHeight, s_OverlayLineHeight);
            // Go to next line if it goes outside the screen.
            if (x + overlayWidth > width)
            {
                x = 0;
                y -= s_OverlayLineHeight;
                s_OverlayLineHeight = -1.0f;
            }
        }

        public static void SampleCopyChannel_xyzw2x(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier target, Vector2 size, RenderPipelineResources resources)
        {
            var s = new Vector4(size.x, size.y, 1f / size.x, 1f / size.y);
            cmd.SetComputeVectorParam(resources.copyChannelCS, HDShaderIDs._Size, s);
            cmd.SetComputeTextureParam(resources.copyChannelCS, resources.copyChannelKernel_xyzw2x, HDShaderIDs._Source4, source);
            cmd.SetComputeTextureParam(resources.copyChannelCS, resources.copyChannelKernel_xyzw2x, HDShaderIDs._Result1, target);
            cmd.DispatchCompute(resources.copyChannelCS, resources.copyChannelKernel_xyzw2x, (int)(size.x) / 8, (int)(size.y) / 8, 1);
        }
    }
}
