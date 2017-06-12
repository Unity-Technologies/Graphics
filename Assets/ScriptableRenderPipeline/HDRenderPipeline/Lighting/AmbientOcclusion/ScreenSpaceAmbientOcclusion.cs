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
        }

        Material m_Material;
        CommandBuffer m_Command;

        // For the AO buffer, use R8 or RHalf if available.
        static RenderTextureFormat GetAOBufferFormat()
        {
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8))
                return RenderTextureFormat.R8;
            if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf))
                return RenderTextureFormat.RHalf;
            return RenderTextureFormat.Default;
        }

        public ScreenSpaceAmbientOcclusionEffect()
        {}

        public void Build(RenderPipelineResources renderPipelineResources)
        {
            m_Material = Utilities.CreateEngineMaterial(renderPipelineResources.screenSpaceAmbientOcclusionShader);
            m_Material.hideFlags = HideFlags.DontSave;
        }

        public void Render(ScreenSpaceAmbientOcclusionSettings.Settings settings, HDRenderPipeline hdRP, HDCamera hdCamera, ScriptableRenderContext renderContext, bool isForward)
        {
            const RenderTextureFormat kTempFormat = RenderTextureFormat.ARGB32;
            const RenderTextureReadWrite kRWMode = RenderTextureReadWrite.Linear;
            const FilterMode kFilter = FilterMode.Bilinear;

            if (m_Command == null)
            {
                m_Command = new CommandBuffer { name = "Ambient Occlusion" };
            }
            else
            {
                m_Command.Clear();
            }

            // Note: Currently there is no SSAO in forward as we don't have normal buffer
            // If SSAO is disable, simply put a white 1x1 texture
            if (settings.enable == false || isForward)
            {
                m_Command.SetGlobalTexture(Uniforms._AOBuffer, PostProcessing.RuntimeUtilities.blackTexture); // Neutral is black, see the comment in the shaders
                renderContext.ExecuteCommandBuffer(m_Command);
                return;
            }

            var width = hdCamera.camera.pixelWidth;
            var height = hdCamera.camera.pixelHeight;
            var downsize = settings.downsampling ? 2 : 1;

            // Provide the settings via uniforms.
            m_Material.SetFloat(Uniforms._Intensity, settings.intensity);
            m_Material.SetFloat(Uniforms._Radius, settings.radius);
            m_Material.SetFloat(Uniforms._Downsample, 1.0f / downsize);
            m_Material.SetFloat(Uniforms._SampleCount, settings.sampleCount);

            // AO estimation.
            m_Command.GetTemporaryRT(Uniforms._TempTex1, width / downsize, height / downsize, 0, kFilter, kTempFormat, kRWMode);
            Utilities.DrawFullScreen(m_Command, m_Material, hdCamera, Uniforms._TempTex1, null, 0);
            hdRP.PushFullScreenDebugTexture(m_Command, Uniforms._TempTex1, hdCamera.camera, renderContext, FullScreenDebugMode.SSAOBeforeFiltering);

            // Denoising (horizontal pass).
            m_Command.GetTemporaryRT(Uniforms._TempTex2, width, height, 0, kFilter, kTempFormat, kRWMode);
            m_Command.SetGlobalTexture(Uniforms._MainTex, Uniforms._TempTex1);
            Utilities.DrawFullScreen(m_Command, m_Material, hdCamera, Uniforms._TempTex2, null, 1);
            m_Command.ReleaseTemporaryRT(Uniforms._TempTex1);

            // Denoising (vertical pass).
            m_Command.GetTemporaryRT(Uniforms._TempTex1, width, height, 0, kFilter, kTempFormat, kRWMode);
            m_Command.SetGlobalTexture(Uniforms._MainTex, Uniforms._TempTex2);
            Utilities.DrawFullScreen(m_Command, m_Material, hdCamera, Uniforms._TempTex1, null, 2);
            m_Command.ReleaseTemporaryRT(Uniforms._TempTex2);

            // Final filtering
            m_Command.GetTemporaryRT(Uniforms._AOBuffer, width, height, 0, kFilter, GetAOBufferFormat(), kRWMode);
            m_Command.SetGlobalTexture(Uniforms._MainTex, Uniforms._TempTex1);
            Utilities.DrawFullScreen(m_Command, m_Material, hdCamera, Uniforms._AOBuffer, null, 3);
            m_Command.ReleaseTemporaryRT(Uniforms._TempTex1);

            // Setup texture for lighting pass (automagic of unity)
            m_Command.SetGlobalTexture("_AmbientOcclusionTexture", Uniforms._AOBuffer);
            hdRP.PushFullScreenDebugTexture(m_Command, Uniforms._AOBuffer, hdCamera.camera, renderContext, FullScreenDebugMode.SSAO);

            // Register the command buffer and release it.
            renderContext.ExecuteCommandBuffer(m_Command);
        }

        public void Cleanup()
        {
            Utilities.Destroy(m_Material);
            if (m_Command != null) m_Command.Dispose();
        }
    }
}
