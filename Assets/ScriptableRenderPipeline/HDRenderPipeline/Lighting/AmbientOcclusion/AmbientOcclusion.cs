using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.PostProcessing;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ScreenSpaceAmbientOcclusionSettings : ScriptableObject
    {
        [Serializable]
        public struct Settings
        {
            bool m_Enable;

            [SerializeField]
            float m_Intensity;
            [SerializeField]
            float m_Radius;

            [SerializeField]
            int m_SampleCount;
            [SerializeField]
            bool m_Downsampling;

            public bool enable { set { m_Enable = value; } get { return m_Enable; } }
            public float intensity { set { m_Intensity = value; OnValidate(); } get { return m_Intensity; } }
            public float radius { set { m_Radius = value; OnValidate(); } get { return m_Radius; } }
            public int sampleCount { set { m_SampleCount = value; OnValidate(); } get { return m_SampleCount; } }
            public bool downsampling { set { m_Downsampling = value; } get { return m_Downsampling; } }

            void OnValidate()
            {
                m_Intensity = Mathf.Min(2, Mathf.Max(0, m_Intensity));
                m_Radius = Mathf.Max(0, m_Radius);
                m_SampleCount = Mathf.Min(1, Mathf.Max(32, m_SampleCount));
            }

            public static readonly Settings s_Defaultsettings = new Settings
            {
                m_Enable = false,
                m_Intensity = 1.0f,
                m_Radius = 0.5f,
                m_SampleCount = 8,
                m_Downsampling = true
            };
        }

        [SerializeField]
        Settings m_Settings = Settings.s_Defaultsettings;

        public Settings settings
        {
            get { return m_Settings; }
            set { m_Settings = value; }
        }
    }

    public class ScreenSpaceAmbientOcclusionSettingsSingleton : Singleton<ScreenSpaceAmbientOcclusionSettingsSingleton>
    {
        private ScreenSpaceAmbientOcclusionSettings settings { get; set; }

        public static ScreenSpaceAmbientOcclusionSettings overrideSettings
        {
            get { return instance.settings; }
            set { instance.settings = value; }
        }
    }

    public sealed class ScreenSpaceAmbientOcclusionEffect
    {
        static class Uniforms
        {
            internal static readonly int _Intensity = Shader.PropertyToID("_Intensity");
            internal static readonly int _Radius = Shader.PropertyToID("_Radius");
            internal static readonly int _Downsample = Shader.PropertyToID("_Downsample");
            internal static readonly int _SampleCount = Shader.PropertyToID("_SampleCount");
            internal static readonly int _AOBuffer = Shader.PropertyToID("_AmbientOcclusionTexture");
            internal static readonly int _TempTex1 = Shader.PropertyToID("_TempTex1");
            internal static readonly int _TempTex2 = Shader.PropertyToID("_TempTex2");
            internal static readonly int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
        }

        PropertySheet m_Sheet;

        readonly RenderTargetIdentifier m_AmbientOcclusionRT;

        public ScreenSpaceAmbientOcclusionEffect()
        {}

        public void Build(RenderPipelineResources renderPipelinesResources)
        {
            var material = Utilities.CreateEngineMaterial("Hidden/HDPipeline/ScreenSpace/AmbientOcclusion");
            // TODO: Don't we need to also free the material ?
            m_Sheet = new PropertySheet(material);
        }

        public void Render(ScreenSpaceAmbientOcclusionSettings.Settings settings, Camera camera, ScriptableRenderContext renderContext, RenderTargetIdentifier depthID, bool isForward)
        {
            const RenderTextureFormat kFormat = RenderTextureFormat.ARGB32;
            const RenderTextureReadWrite kRWMode = RenderTextureReadWrite.Linear;
            const FilterMode kFilter = FilterMode.Bilinear;

            // Note: Currently there is no SSAO in forward as we don't have normal buffer
            if (settings.enable == false || isForward)
            {
                var cmd2 = new CommandBuffer { name = "Ambient Occlusion (1x1)" };
                // TODO: Create a white 1x1 texture to setup here when AO is disabled (we could also do a variant in shader, but this increase number of combination)
                cmd2.GetTemporaryRT(Uniforms._AOBuffer, 1, 1, 0, kFilter, kFormat, kRWMode);
                cmd2.SetRenderTarget(Uniforms._AOBuffer);
                cmd2.ClearRenderTarget(false, true, Color.white);
                // Setup texture for lighting pass (automatic of unity)
                cmd2.SetGlobalTexture("_AmbientOcclusionTexture", Uniforms._AOBuffer);

                // Register the command buffer and release it.
                renderContext.ExecuteCommandBuffer(cmd2);
                cmd2.Dispose();

                return ;
            }

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
            // Note: GBuffer is automatically bind

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

            // Final filtering
            cmd.GetTemporaryRT(Uniforms._AOBuffer, width, height, 0, kFilter, kFormat, kRWMode);
            cmd.BlitFullscreenTriangle(Uniforms._TempTex1, Uniforms._AOBuffer, depthID, m_Sheet, 3);
            cmd.ReleaseTemporaryRT(Uniforms._TempTex1);

            // Setup texture for lighting pass (automagic of unity)
            cmd.SetGlobalTexture("_AmbientOcclusionTexture", Uniforms._AOBuffer);

            // Register the command buffer and release it.
            renderContext.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        public void Cleanup()
        {
            if (m_Sheet != null)
                m_Sheet.Release();
        }
    }
}
