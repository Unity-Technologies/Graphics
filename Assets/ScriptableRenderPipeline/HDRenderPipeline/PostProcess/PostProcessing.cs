using System;
using UnityEngine.Rendering;

// TEMPORARY, minimalist post-processing stack until the fully-featured framework is ready

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    using GradingType = PostProcessing.ColorGradingSettings.GradingType;
    using EyeAdaptationType = PostProcessing.EyeAdaptationSettings.EyeAdaptationType;

    [ExecuteInEditMode, ImageEffectAllowedInSceneView]
    [RequireComponent(typeof(Camera))]
    public sealed partial class PostProcessing : MonoBehaviour
    {
        public EyeAdaptationSettings eyeAdaptation = new EyeAdaptationSettings();
        public ColorGradingSettings colorGrading = new ColorGradingSettings();
        public ChromaticAberrationSettings chromaSettings = new ChromaticAberrationSettings();
        public VignetteSettings vignetteSettings = new VignetteSettings();
        public bool globalDithering = false;

        Material m_EyeAdaptationMaterial;
        Material m_FinalPassMaterial;

        ComputeShader m_EyeCompute;
        ComputeBuffer m_HistogramBuffer;

        int m_TempRt;

        Texture m_DefaultSpectralLut;

        readonly RenderTexture[] m_AutoExposurePool = new RenderTexture[2];
        int m_AutoExposurePingPing;
        RenderTexture m_CurrentAutoExposure;
        RenderTexture m_DebugHistogram = null;

        static uint[] s_EmptyHistogramBuffer = new uint[k_HistogramBins];

        bool m_FirstFrame = true;

        // Don't forget to update 'EyeAdaptation.cginc' if you change these values !
        const int k_HistogramBins = 64;
        const int k_HistogramThreadX = 16;
        const int k_HistogramThreadY = 16;

        // Holds 64 64x64 Alpha8 textures (256kb total)
        const int k_BlueNoiseTextureCount = 64;
        Texture2D[] m_BlueNoiseTextures;
        int m_DitheringTexIndex = 0;

        void OnEnable()
        {
            m_EyeAdaptationMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/EyeAdaptation");
            m_FinalPassMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/FinalPass");

            m_EyeCompute = Resources.Load<ComputeShader>("EyeHistogram");
            m_HistogramBuffer = new ComputeBuffer(k_HistogramBins, sizeof(uint));

            m_AutoExposurePool[0] = new RenderTexture(1, 1, 0, RenderTextureFormat.RFloat);
            m_AutoExposurePool[1] = new RenderTexture(1, 1, 0, RenderTextureFormat.RFloat);

            m_BlueNoiseTextures = new Texture2D[k_BlueNoiseTextureCount];
            for (int i = 0; i < k_BlueNoiseTextureCount; i++)
                m_BlueNoiseTextures[i] = Resources.Load<Texture2D>("Textures/LDR_LLL1_" + i);

            m_TempRt = Shader.PropertyToID("_Source");

            m_DefaultSpectralLut = Resources.Load<Texture2D>("Textures/SpectralLut_GreenPurple");

            m_FirstFrame = true;
        }

        void OnDisable()
        {
            Utilities.Destroy(m_EyeAdaptationMaterial);
            Utilities.Destroy(m_FinalPassMaterial);

            foreach (var rt in m_AutoExposurePool)
                Utilities.Destroy(rt);

            Utilities.Destroy(m_DebugHistogram);
            Utilities.SafeRelease(m_HistogramBuffer);

            m_EyeAdaptationMaterial = null;
            m_FinalPassMaterial = null;
        }

        public void Render(Camera camera, ScriptableRenderContext context, RenderTargetIdentifier source, RenderTargetIdentifier destination)
        {
            m_FinalPassMaterial.shaderKeywords = null;

            var cmd = new CommandBuffer { name = "Final Pass" };

            if (eyeAdaptation.enabled)
                DoEyeAdaptation(camera, cmd, source);

            if (chromaSettings.enabled)
                DoChromaticAberration();

            if (vignetteSettings.enabled)
                DoVignette();

            if (globalDithering)
                DoDithering(camera);

            DoColorGrading();

            cmd.Blit(source, destination, m_FinalPassMaterial, 0);

            context.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
        }

        Vector4 GetHistogramScaleOffsetRes(Camera camera)
        {
            float diff = eyeAdaptation.logMax - eyeAdaptation.logMin;
            float scale = 1f / diff;
            float offset = -eyeAdaptation.logMin * scale;
            return new Vector4(scale, offset, Mathf.Floor(camera.pixelWidth / 2f), Mathf.Floor(camera.pixelHeight / 2f));
        }

        void DoEyeAdaptation(Camera camera, CommandBuffer cmd, RenderTargetIdentifier source)
        {
            // Downscale the framebuffer, we don't need an absolute precision for auto exposure
            // and it helps making it more stable - should be using a previously downscaled pass
            var scaleOffsetRes = GetHistogramScaleOffsetRes(camera);

            cmd.GetTemporaryRT(m_TempRt, (int)scaleOffsetRes.z, (int)scaleOffsetRes.w, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf);
            cmd.Blit(source, m_TempRt);

            // Clears the buffer on every frame as we use it to accumulate luminance values on each frame
            m_HistogramBuffer.SetData(s_EmptyHistogramBuffer);

            // Gets a log histogram
            int kernel = m_EyeCompute.FindKernel("KEyeHistogram");

            cmd.SetComputeBufferParam(m_EyeCompute, kernel, "_Histogram", m_HistogramBuffer);
            cmd.SetComputeTextureParam(m_EyeCompute, kernel, "_Source", m_TempRt);
            cmd.SetComputeVectorParam(m_EyeCompute, "_ScaleOffsetRes", scaleOffsetRes);
            cmd.DispatchCompute(m_EyeCompute, kernel, Mathf.CeilToInt(scaleOffsetRes.z / (float)k_HistogramThreadX), Mathf.CeilToInt(scaleOffsetRes.w / (float)k_HistogramThreadY), 1);

            // Cleanup
            cmd.ReleaseTemporaryRT(m_TempRt);

            // Make sure filtering values are correct to avoid apocalyptic consequences
            const float kMinDelta = 1e-2f;
            eyeAdaptation.highPercent = Mathf.Clamp(eyeAdaptation.highPercent, 1f + kMinDelta, 99f);
            eyeAdaptation.lowPercent = Mathf.Clamp(eyeAdaptation.lowPercent, 1f, eyeAdaptation.highPercent - kMinDelta);

            // Compute auto exposure
            m_EyeAdaptationMaterial.SetBuffer(Uniforms._Histogram, m_HistogramBuffer);
            m_EyeAdaptationMaterial.SetVector(Uniforms._Params, new Vector4(eyeAdaptation.lowPercent * 0.01f, eyeAdaptation.highPercent * 0.01f, eyeAdaptation.minLuminance, eyeAdaptation.maxLuminance));
            m_EyeAdaptationMaterial.SetVector(Uniforms._Speed, new Vector2(eyeAdaptation.speedDown, eyeAdaptation.speedUp));
            m_EyeAdaptationMaterial.SetVector(Uniforms._ScaleOffsetRes, scaleOffsetRes);
            m_EyeAdaptationMaterial.SetFloat(Uniforms._ExposureCompensation, eyeAdaptation.exposureCompensation);

            if (m_FirstFrame || !Application.isPlaying)
            {
                // We don't want eye adaptation when not in play mode because the GameView isn't
                // animated, thus making it harder to tweak. Just use the final audo exposure value.
                m_CurrentAutoExposure = m_AutoExposurePool[0];
                cmd.Blit(null, m_CurrentAutoExposure, m_EyeAdaptationMaterial, (int)EyeAdaptationType.Fixed);

                // Copy current exposure to the other pingpong target on first frame to avoid adapting from black
                cmd.Blit(m_AutoExposurePool[0], m_AutoExposurePool[1]);
            }
            else
            {
                int pp = m_AutoExposurePingPing;
                var src = m_AutoExposurePool[++pp % 2];
                var dst = m_AutoExposurePool[++pp % 2];
                cmd.Blit(src, dst, m_EyeAdaptationMaterial, (int)eyeAdaptation.adaptationType);
                m_AutoExposurePingPing = ++pp % 2;
                m_CurrentAutoExposure = dst;
            }

            m_FinalPassMaterial.EnableKeyword("EYE_ADAPTATION");
            m_FinalPassMaterial.SetTexture(Uniforms._AutoExposure, m_CurrentAutoExposure);

            // Debug histogram visualization
            if (eyeAdaptation.showDebugHistogramInGameView)
            {
                if (m_DebugHistogram == null || !m_DebugHistogram.IsCreated())
                {
                    m_DebugHistogram = new RenderTexture(256, 128, 0, RenderTextureFormat.ARGB32)
                    {
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Clamp
                    };
                }

                m_EyeAdaptationMaterial.SetFloat(Uniforms._DebugWidth, m_DebugHistogram.width);
                cmd.Blit(null, m_DebugHistogram, m_EyeAdaptationMaterial, 2);
            }

            m_FirstFrame = false;
        }

        void DoChromaticAberration()
        {
            var spectralLut = chromaSettings.spectralTexture == null
                ? m_DefaultSpectralLut
                : chromaSettings.spectralTexture;

            m_FinalPassMaterial.EnableKeyword("CHROMATIC_ABERRATION");
            m_FinalPassMaterial.SetFloat(Uniforms._ChromaticAberration_Amount, chromaSettings.intensity * 0.03f);
            m_FinalPassMaterial.SetTexture(Uniforms._ChromaticAberration_Lut, spectralLut);
        }

        void DoVignette()
        {
            m_FinalPassMaterial.EnableKeyword("VIGNETTE");
            m_FinalPassMaterial.SetColor(Uniforms._Vignette_Color, vignetteSettings.color);
            m_FinalPassMaterial.SetVector(Uniforms._Vignette_Settings, new Vector4(vignetteSettings.intensity * 3f, vignetteSettings.smoothness * 5f, vignetteSettings.center.x, vignetteSettings.center.y));
        }

        void DoDithering(Camera camera)
        {
            m_FinalPassMaterial.EnableKeyword("DITHERING");

            if (++m_DitheringTexIndex >= k_BlueNoiseTextureCount)
                m_DitheringTexIndex = 0;

            var noiseTex = m_BlueNoiseTextures[m_DitheringTexIndex];
            m_FinalPassMaterial.SetTexture(Uniforms._DitheringTex, noiseTex);
            m_FinalPassMaterial.SetVector(Uniforms._DitheringCoords, new Vector4(
                (float)camera.pixelWidth / (float)noiseTex.width,
                (float)camera.pixelHeight / (float)noiseTex.height,
                Random.value, Random.value
            ));
        }

        void DoColorGrading()
        {
            float ev = Mathf.Exp(colorGrading.exposure * 0.6931471805599453f);
            m_FinalPassMaterial.SetFloat(Uniforms._Exposure, ev);

            if (colorGrading.type == GradingType.Neutral)
            {
                const float kScaleFactor = 20f;
                const float kScaleFactorHalf = kScaleFactor * 0.5f;

                float inBlack = colorGrading.neutralBlackIn * kScaleFactor + 1f;
                float outBlack = colorGrading.neutralBlackOut * kScaleFactorHalf + 1f;
                float inWhite = colorGrading.neutralWhiteIn / kScaleFactor;
                float outWhite = 1f - colorGrading.neutralWhiteOut / kScaleFactor;
                float blackRatio = inBlack / outBlack;
                float whiteRatio = inWhite / outWhite;

                const float a = 0.2f;
                float b = Mathf.Max(0f, Mathf.LerpUnclamped(0.57f, 0.37f, blackRatio));
                float c = Mathf.LerpUnclamped(0.01f, 0.24f, whiteRatio);
                float d = Mathf.Max(0f, Mathf.LerpUnclamped(0.02f, 0.20f, blackRatio));
                const float e = 0.02f;
                const float f = 0.30f;

                m_FinalPassMaterial.SetVector(Uniforms._NeutralTonemapperParams1, new Vector4(a, b, c, d));
                m_FinalPassMaterial.SetVector(Uniforms._NeutralTonemapperParams2, new Vector4(e, f, colorGrading.neutralWhiteLevel, colorGrading.neutralWhiteClip / kScaleFactorHalf));
                m_FinalPassMaterial.EnableKeyword("NEUTRAL_GRADING");
            }
            else if (colorGrading.type == GradingType.Custom)
            {
                if (colorGrading.logLut != null)
                {
                    var lut = colorGrading.logLut;
                    m_FinalPassMaterial.SetTexture(Uniforms._LogLut, lut);
                    m_FinalPassMaterial.SetVector(Uniforms._LogLut_Params, new Vector3(1f / lut.width, 1f / lut.height, lut.height - 1f));
                    m_FinalPassMaterial.EnableKeyword("CUSTOM_GRADING");
                }
            }
        }

        void OnGUI()
        {
            if (!eyeAdaptation.enabled || !eyeAdaptation.showDebugHistogramInGameView || m_DebugHistogram == null || !m_DebugHistogram.IsCreated())
                return;

            var rect = new Rect(8f, 8f, m_DebugHistogram.width, m_DebugHistogram.height);
            GUI.DrawTexture(rect, m_DebugHistogram);
        }

        // Pre-hash uniforms, no need to stress the CPU more than needed on every frame
        static class Uniforms
        {
            internal static readonly int _Histogram                  = Shader.PropertyToID("_Histogram");
            internal static readonly int _Params                     = Shader.PropertyToID("_Params");
            internal static readonly int _Speed                      = Shader.PropertyToID("_Speed");
            internal static readonly int _ScaleOffsetRes             = Shader.PropertyToID("_ScaleOffsetRes");
            internal static readonly int _ExposureCompensation       = Shader.PropertyToID("_ExposureCompensation");

            internal static readonly int _AutoExposure               = Shader.PropertyToID("_AutoExposure");

            internal static readonly int _DebugWidth                 = Shader.PropertyToID("_DebugWidth");

            internal static readonly int _ChromaticAberration_Amount = Shader.PropertyToID("_ChromaticAberration_Amount");
            internal static readonly int _ChromaticAberration_Lut    = Shader.PropertyToID("_ChromaticAberration_Lut");

            internal static readonly int _Vignette_Color             = Shader.PropertyToID("_Vignette_Color");
            internal static readonly int _Vignette_Settings          = Shader.PropertyToID("_Vignette_Settings");

            internal static readonly int _DitheringTex               = Shader.PropertyToID("_DitheringTex");
            internal static readonly int _DitheringCoords            = Shader.PropertyToID("_DitheringCoords");

            internal static readonly int _Exposure                   = Shader.PropertyToID("_Exposure");

            internal static readonly int _NeutralTonemapperParams1   = Shader.PropertyToID("_NeutralTonemapperParams1");
            internal static readonly int _NeutralTonemapperParams2   = Shader.PropertyToID("_NeutralTonemapperParams2");

            internal static readonly int _LogLut                     = Shader.PropertyToID("_LogLut");
            internal static readonly int _LogLut_Params              = Shader.PropertyToID("_LogLut_Params");
        }
    }
}
