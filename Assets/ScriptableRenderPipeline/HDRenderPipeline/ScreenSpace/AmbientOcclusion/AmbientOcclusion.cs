using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.PostProcessing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [System.Serializable]
    public sealed class ScreenSpaceAmbientOcclusionSettings
    {
        [Range(0, 2)]
        public float intensity = 1.0f;

        public float radius = 0.5f;

        [Range(1, 32)]
        public int sampleCount = 8;

        public bool downsampling = true;
    }

    public sealed class ScreenSpaceAmbientOcclusionEffect
    {
        static class Uniforms
        {
            internal static readonly int _Intensity = Shader.PropertyToID("_Intensity");
            internal static readonly int _Radius = Shader.PropertyToID("_Radius");
            internal static readonly int _Downsample = Shader.PropertyToID("_Downsample");
            internal static readonly int _SampleCount = Shader.PropertyToID("_SampleCount");
            internal static readonly int _TempTex1 = Shader.PropertyToID("_TempTex1");
            internal static readonly int _TempTex2 = Shader.PropertyToID("_TempTex2");
            internal static readonly int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
            internal static readonly int _CameraGBufferTexture1 = Shader.PropertyToID("_CameraGBufferTexture1");
        }

        RenderTargetIdentifier[] m_GBufferIDs;
        RenderTargetIdentifier[] m_MRT = { 0, 0 };
        PropertySheet m_Sheet;

        public ScreenSpaceAmbientOcclusionEffect(RenderTargetIdentifier[] gbufferIDs)
        {
            m_GBufferIDs = gbufferIDs;
        }

        public void Render(ScreenSpaceAmbientOcclusionSettings settings, Camera camera, ScriptableRenderContext renderContext, RenderTargetIdentifier depthID)
        {
            if (settings == null) return;

            if (m_Sheet == null)
            {
                var shader = Shader.Find("Hidden/HDPipeline/ScreenSpace/AmbientOcclusion");
                var material = new Material(shader) { hideFlags = HideFlags.DontSave };
                m_Sheet = new PropertySheet(material);
            }

            const RenderTextureFormat kFormat = RenderTextureFormat.ARGB32;
            const RenderTextureReadWrite kRWMode = RenderTextureReadWrite.Linear;
            const FilterMode kFilter = FilterMode.Bilinear;

            var width = camera.pixelWidth;
            var height = camera.pixelHeight;
            var downsize = settings.downsampling ? 2 : 1;

            // Provide the settings via uniforms.
            m_Sheet.properties.SetFloat(Uniforms._Intensity, settings.intensity);
            m_Sheet.properties.SetFloat(Uniforms._Radius, settings.radius);
            m_Sheet.properties.SetFloat(Uniforms._Downsample, 1.0f / downsize);
            m_Sheet.properties.SetFloat(Uniforms._SampleCount, settings.sampleCount);

            // Start building a command buffer.
            var cmd = new CommandBuffer { name = "Ambient Occlusion" };
            cmd.SetGlobalTexture(Uniforms._CameraDepthTexture, depthID);
            cmd.SetGlobalTexture(Uniforms._CameraGBufferTexture1, m_GBufferIDs[1]);

            // AO estimation.
            cmd.GetTemporaryRT(Uniforms._TempTex1, width / downsize, height / downsize, 0, kFilter, kFormat, kRWMode);
            cmd.BlitFullscreenTriangle(depthID, Uniforms._TempTex1, m_Sheet, 0);

            // Denoising (horizontal pass).
            cmd.GetTemporaryRT(Uniforms._TempTex2, width, height, 0, kFilter, kFormat, kRWMode);
            cmd.BlitFullscreenTriangle(Uniforms._TempTex1, Uniforms._TempTex2, m_Sheet, 1);
            cmd.ReleaseTemporaryRT(Uniforms._TempTex1);

            // Denoising (vertical pass).
            cmd.GetTemporaryRT(Uniforms._TempTex1, width, height, 0, kFilter, kFormat, kRWMode);
            cmd.BlitFullscreenTriangle(Uniforms._TempTex2, Uniforms._TempTex1, m_Sheet, 2);
            cmd.ReleaseTemporaryRT(Uniforms._TempTex2);

            // Composite into the emission buffer.
            m_MRT[0] = m_GBufferIDs[0];
            m_MRT[1] = m_GBufferIDs[3];
            cmd.BlitFullscreenTriangle(Uniforms._TempTex1, m_MRT, depthID, m_Sheet, 3);
            cmd.ReleaseTemporaryRT(Uniforms._TempTex1);

            // Register the command buffer and release it.
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        public void Cleanup()
        {
            if (m_Sheet != null) m_Sheet.Release();
        }
    }
}
