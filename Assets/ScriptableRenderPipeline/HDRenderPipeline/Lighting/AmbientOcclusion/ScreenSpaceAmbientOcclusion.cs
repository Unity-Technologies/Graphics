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

        public void Render(ScreenSpaceAmbientOcclusionSettings.Settings settings, HDRenderPipeline hdRP, HDCamera hdCamera, ScriptableRenderContext renderContext, CommandBuffer cmd, bool isForward)
        {
            const RenderTextureFormat kTempFormat = RenderTextureFormat.ARGB32;
            const RenderTextureReadWrite kRWMode = RenderTextureReadWrite.Linear;
            const FilterMode kFilter = FilterMode.Bilinear;

            // Note: Currently there is no SSAO in forward as we don't have normal buffer
            // If SSAO is disable, simply put a white 1x1 texture
            if (settings.enable == false || isForward)
            {
                cmd.SetGlobalTexture(Uniforms._AOBuffer, UnityEngine.Rendering.PostProcessing.RuntimeUtilities.blackTexture); // Neutral is black, see the comment in the shaders
                cmd.SetGlobalFloat("_AmbientOcclusionDirectLightStrenght", 0.0f);
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

            using (new Utilities.ProfilingSample("Screenspace ambient occlusion", cmd))
            {
                // AO estimation.
                cmd.GetTemporaryRT(Uniforms._TempTex1, width / downsize, height / downsize, 0, kFilter, kTempFormat, kRWMode);
                Utilities.DrawFullScreen(cmd, m_Material, Uniforms._TempTex1, null, 0);
                hdRP.PushFullScreenDebugTexture(cmd, Uniforms._TempTex1, hdCamera.camera, renderContext, FullScreenDebugMode.SSAOBeforeFiltering);

                // Denoising (horizontal pass).
                cmd.GetTemporaryRT(Uniforms._TempTex2, width, height, 0, kFilter, kTempFormat, kRWMode);
                cmd.SetGlobalTexture(Uniforms._MainTex, Uniforms._TempTex1);
                Utilities.DrawFullScreen(cmd, m_Material, Uniforms._TempTex2, null, 1);
                cmd.ReleaseTemporaryRT(Uniforms._TempTex1);

                // Denoising (vertical pass).
                cmd.GetTemporaryRT(Uniforms._TempTex1, width, height, 0, kFilter, kTempFormat, kRWMode);
                cmd.SetGlobalTexture(Uniforms._MainTex, Uniforms._TempTex2);
                Utilities.DrawFullScreen(cmd, m_Material, Uniforms._TempTex1, null, 2);
                cmd.ReleaseTemporaryRT(Uniforms._TempTex2);

                // Final filtering
                cmd.GetTemporaryRT(Uniforms._AOBuffer, width, height, 0, kFilter, GetAOBufferFormat(), kRWMode);
                cmd.SetGlobalTexture(Uniforms._MainTex, Uniforms._TempTex1);
                Utilities.DrawFullScreen(cmd, m_Material, Uniforms._AOBuffer, null, 3);
                cmd.ReleaseTemporaryRT(Uniforms._TempTex1);

                // Setup texture for lighting pass (automatic of unity)
                cmd.SetGlobalTexture("_AmbientOcclusionTexture", Uniforms._AOBuffer);
                cmd.SetGlobalFloat("_AmbientOcclusionDirectLightStrenght", settings.affectDirectLigthingStrenght);
                hdRP.PushFullScreenDebugTexture(cmd, Uniforms._AOBuffer, hdCamera.camera, renderContext, FullScreenDebugMode.SSAO);
            }
        }

        public void Cleanup()
        {
            Utilities.Destroy(m_Material);
        }
    }
}
