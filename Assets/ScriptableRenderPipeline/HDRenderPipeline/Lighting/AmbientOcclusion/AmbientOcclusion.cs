using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public sealed class ScreenSpaceAmbientOcclusionEffect
    {
        static class Uniforms
        {
            internal static readonly int _Intensity = Shader.PropertyToID("_Intensity");
            internal static readonly int _Radius = Shader.PropertyToID("_Radius");
            internal static readonly int _Downsample = Shader.PropertyToID("_Downsample");
            internal static readonly int _SampleCount = Shader.PropertyToID("_SampleCount");
            internal static readonly int _MainTex = Shader.PropertyToID("_MainTex");
            internal static readonly int _AOBuffer = Shader.PropertyToID("_AmbientOcclusionTexture");
            internal static readonly int _TempTex1 = Shader.PropertyToID("_TempTex1");
            internal static readonly int _TempTex2 = Shader.PropertyToID("_TempTex2");
            internal static readonly int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
        }

        Material m_Material;

        readonly RenderTargetIdentifier m_AmbientOcclusionRT;

        public ScreenSpaceAmbientOcclusionEffect()
        {}

        public void Build(RenderPipelineResources renderPipelinesResources)
        {
            m_Material = Utilities.CreateEngineMaterial("Hidden/HDPipeline/ScreenSpace/AmbientOcclusion");
            m_Material.hideFlags = HideFlags.DontSave;
        }

        public void Render(ScreenSpaceAmbientOcclusionSettings.Settings settings, HDCamera hdCamera, ScriptableRenderContext renderContext, RenderTargetIdentifier depthID, bool isForward)
        {
            const RenderTextureFormat kFormat = RenderTextureFormat.ARGB32;
            const RenderTextureReadWrite kRWMode = RenderTextureReadWrite.Linear;
            const FilterMode kFilter = FilterMode.Bilinear;

            // Note: Currently there is no SSAO in forward as we don't have normal buffer
            // If SSAO is disable, simply put a white 1x1 texture
            if (settings.enable == false || isForward)
            {
                var cmd2 = new CommandBuffer { name = "Setup neutral Ambient Occlusion (1x1)" };
                cmd2.SetGlobalTexture("_AmbientOcclusionTexture", PostProcessing.RuntimeUtilities.blackTexture); // Neutral is black, see the comment in the shaders
                renderContext.ExecuteCommandBuffer(cmd2);
                cmd2.Dispose();

                return ;
            }

            var width = hdCamera.camera.pixelWidth;
            var height = hdCamera.camera.pixelHeight;
            var downsize = settings.downsampling ? 2 : 1;

            // Provide the settings via uniforms.
            m_Material.SetFloat(Uniforms._Intensity, settings.intensity);
            m_Material.SetFloat(Uniforms._Radius, settings.radius);
            m_Material.SetFloat(Uniforms._Downsample, 1.0f / downsize);
            m_Material.SetFloat(Uniforms._SampleCount, settings.sampleCount);

            // Start building a command buffer.
            var cmd = new CommandBuffer { name = "Ambient Occlusion" };
            cmd.SetGlobalTexture(Uniforms._CameraDepthTexture, depthID);
            // Note: GBuffer is automatically bind

            // AO estimation.
            cmd.GetTemporaryRT(Uniforms._TempTex1, width / downsize, height / downsize, 0, kFilter, kFormat, kRWMode);
            cmd.SetGlobalTexture(Uniforms._MainTex, depthID);
            Utilities.DrawFullScreen(cmd, m_Material, hdCamera, Uniforms._TempTex1, null, 0);

            // Denoising (horizontal pass).
            cmd.GetTemporaryRT(Uniforms._TempTex2, width, height, 0, kFilter, kFormat, kRWMode);
            cmd.SetGlobalTexture(Uniforms._MainTex, Uniforms._TempTex1);
            Utilities.DrawFullScreen(cmd, m_Material, hdCamera, Uniforms._TempTex2, null, 1);
            cmd.ReleaseTemporaryRT(Uniforms._TempTex1);

            // Denoising (vertical pass).
            cmd.GetTemporaryRT(Uniforms._TempTex1, width, height, 0, kFilter, kFormat, kRWMode);
            cmd.SetGlobalTexture(Uniforms._MainTex, Uniforms._TempTex2);
            Utilities.DrawFullScreen(cmd, m_Material, hdCamera, Uniforms._TempTex1, null, 2);
            cmd.ReleaseTemporaryRT(Uniforms._TempTex2);

            // Final filtering
            cmd.GetTemporaryRT(Uniforms._AOBuffer, width, height, 0, kFilter, kFormat, kRWMode);
            cmd.SetGlobalTexture(Uniforms._MainTex, Uniforms._TempTex1);
            Utilities.DrawFullScreen(cmd, m_Material, hdCamera, Uniforms._AOBuffer, null, 3);
            cmd.ReleaseTemporaryRT(Uniforms._TempTex1);

            // Setup texture for lighting pass (automagic of unity)
            cmd.SetGlobalTexture("_AmbientOcclusionTexture", Uniforms._AOBuffer);

            // Register the command buffer and release it.
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        public void Cleanup()
        {
            Utilities.Destroy(m_Material);
        }
    }
}
