using System;
using UnityEngine.Rendering;

// TEMPORARY, minimalist post-processing stack until the fully-featured framework is ready

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    using GradingType = PostProcessing.ColorGradingSettings.GradingType;
    using EyeAdaptationType = PostProcessing.EyeAdaptationSettings.EyeAdaptationType;

    [ExecuteInEditMode]
    public sealed class PostProcessing : MonoBehaviour
    {
        [Serializable]
        public sealed class ColorGradingSettings
        {
            public enum GradingType
            {
                None,
                Neutral,
                Custom
            }

            public GradingType type = GradingType.None;
            public float exposure = 0f;

            public Texture logLut = null;

            [Range(-0.10f,  0.1f)] public float neutralBlackIn    = 0.02f;
            [Range( 1.00f, 20.0f)] public float neutralWhiteIn    = 10f;
            [Range(-0.09f,  0.1f)] public float neutralBlackOut   = 0f;
            [Range( 1.00f, 19.0f)] public float neutralWhiteOut   = 10f;
            [Range( 0.10f, 20.0f)] public float neutralWhiteLevel = 5.3f;
            [Range( 1.00f, 10.0f)] public float neutralWhiteClip  = 10f;
        }

        [Serializable]
        public sealed class EyeAdaptationSettings
        {
            public enum EyeAdaptationType
            {
                Progressive,
                Fixed
            }

            public bool enabled = false;
            public bool showDebugHistogramInGameView = true;

            [Range(1f, 99f)] public float lowPercent = 65f;
            [Range(1f, 99f)] public float highPercent = 95f;

            public float minLuminance = 0.03f;
            public float maxLuminance = 2f;
            public float exposureCompensation = 0.5f;

            public EyeAdaptationType adaptationType = EyeAdaptationType.Progressive;

            public float speedUp = 2f;
            public float speedDown = 1f;

            [Range(-16, -1)] public int logMin = -8;
            [Range(  1, 16)] public int logMax = 4;
        }

        public EyeAdaptationSettings eyeAdaptation = new EyeAdaptationSettings();
        public ColorGradingSettings colorGrading = new ColorGradingSettings();

        Material m_EyeAdaptationMaterial;
        Material m_FinalPassMaterial;

        ComputeShader m_EyeCompute;
        ComputeBuffer m_HistogramBuffer;

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

        void OnEnable()
        {
            m_FinalPassMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/FinalPass");
            m_EyeAdaptationMaterial = Utilities.CreateEngineMaterial("Hidden/HDRenderPipeline/EyeAdaptation");

            m_EyeCompute = Resources.Load<ComputeShader>("EyeHistogram");
            m_HistogramBuffer = new ComputeBuffer(k_HistogramBins, sizeof(uint));

            m_AutoExposurePool[0] = new RenderTexture(1, 1, 0, RenderTextureFormat.RFloat);
            m_AutoExposurePool[1] = new RenderTexture(1, 1, 0, RenderTextureFormat.RFloat);

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
        }

        public void Render(Camera camera, ScriptableRenderContext context, RenderTargetIdentifier source, RenderTargetIdentifier destination)
        {
            m_FinalPassMaterial.shaderKeywords = null;

            var cmd = new CommandBuffer { name = "Final Pass" };

            if (eyeAdaptation.enabled)
            {
                int tempRt = Shader.PropertyToID("_Source");

                // Downscale the framebuffer, we don't need an absolute precision for auto exposure
                // and it helps making it more stable - should be using a previously downscaled pass
                var scaleOffsetRes = GetHistogramScaleOffsetRes(camera);

                cmd.GetTemporaryRT(tempRt, (int)scaleOffsetRes.z, (int)scaleOffsetRes.w, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf);
                cmd.Blit(source, tempRt);

                // Clears the buffer on every frame as we use it to accumulate luminance values on each frame
                m_HistogramBuffer.SetData(s_EmptyHistogramBuffer);

                // Gets a log histogram
                int kernel = m_EyeCompute.FindKernel("KEyeHistogram");

                cmd.SetComputeBufferParam(m_EyeCompute, kernel, "_Histogram", m_HistogramBuffer);
                cmd.SetComputeTextureParam(m_EyeCompute, kernel, "_Source", tempRt);
                cmd.SetComputeVectorParam(m_EyeCompute, "_ScaleOffsetRes", scaleOffsetRes);
                cmd.DispatchCompute(m_EyeCompute, kernel, Mathf.CeilToInt(scaleOffsetRes.z / (float)k_HistogramThreadX), Mathf.CeilToInt(scaleOffsetRes.w / (float)k_HistogramThreadY), 1);

                // Cleanup
                cmd.ReleaseTemporaryRT(tempRt);

                // Make sure filtering values are correct to avoid apocalyptic consequences
                const float kMinDelta = 1e-2f;
                eyeAdaptation.highPercent = Mathf.Clamp(eyeAdaptation.highPercent, 1f + kMinDelta, 99f);
                eyeAdaptation.lowPercent = Mathf.Clamp(eyeAdaptation.lowPercent, 1f, eyeAdaptation.highPercent - kMinDelta);

                // Compute auto exposure
                m_EyeAdaptationMaterial.SetBuffer("_Histogram", m_HistogramBuffer);
                m_EyeAdaptationMaterial.SetVector("_Params", new Vector4(eyeAdaptation.lowPercent * 0.01f, eyeAdaptation.highPercent * 0.01f, eyeAdaptation.minLuminance, eyeAdaptation.maxLuminance));
                m_EyeAdaptationMaterial.SetVector("_Speed", new Vector2(eyeAdaptation.speedDown, eyeAdaptation.speedUp));
                m_EyeAdaptationMaterial.SetVector("_ScaleOffsetRes", scaleOffsetRes);
                m_EyeAdaptationMaterial.SetFloat("_ExposureCompensation", eyeAdaptation.exposureCompensation);

                if (m_FirstFrame || !Application.isPlaying)
                {
                    // We don't want eye adaptation when not in play mode because the GameView isn't
                    // animated, thus making it harder to tweak. Just use the final audo exposure value.
                    m_CurrentAutoExposure = m_AutoExposurePool[0];
                    cmd.Blit(null, m_CurrentAutoExposure, m_EyeAdaptationMaterial, (int)EyeAdaptationType.Fixed);

                    // Copy current exposure to the other pingpong target on first frame to avoid adapting from black
                    cmd.Blit(m_AutoExposurePool[0], m_AutoExposurePool[1]);
                } else
                {
                    int pp = m_AutoExposurePingPing;
                    var src = m_AutoExposurePool[++pp % 2];
                    var dst = m_AutoExposurePool[++pp % 2];
                    cmd.Blit(src, dst, m_EyeAdaptationMaterial, (int)eyeAdaptation.adaptationType);
                    m_AutoExposurePingPing = ++pp % 2;
                    m_CurrentAutoExposure = dst;
                }

                m_FinalPassMaterial.EnableKeyword("EYE_ADAPTATION");
                m_FinalPassMaterial.SetTexture("_AutoExposure", m_CurrentAutoExposure);

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

                    m_EyeAdaptationMaterial.SetFloat("_DebugWidth", m_DebugHistogram.width);
                    cmd.Blit(null, m_DebugHistogram, m_EyeAdaptationMaterial, 2);
                }

                m_FirstFrame = false;
            }

            float ev = Mathf.Exp(colorGrading.exposure * 0.6931471805599453f);
            m_FinalPassMaterial.SetFloat("_Exposure", ev);

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

                m_FinalPassMaterial.SetVector("_NeutralTonemapperParams1", new Vector4(a, b, c, d));
                m_FinalPassMaterial.SetVector("_NeutralTonemapperParams2", new Vector4(e, f, colorGrading.neutralWhiteLevel, colorGrading.neutralWhiteClip / kScaleFactorHalf));
                m_FinalPassMaterial.EnableKeyword("NEUTRAL_GRADING");
            }
            else if (colorGrading.type == GradingType.Custom)
            {
                if (colorGrading.logLut != null)
                {
                    var lut = colorGrading.logLut;
                    m_FinalPassMaterial.SetTexture("_LogLut", lut);
                    m_FinalPassMaterial.SetVector("_LogLut_Params", new Vector3(1f / lut.width, 1f / lut.height, lut.height - 1f));
                    m_FinalPassMaterial.EnableKeyword("CUSTOM_GRADING");
                }
            }

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

        void OnGUI()
        {
            if (!eyeAdaptation.enabled || !eyeAdaptation.showDebugHistogramInGameView || m_DebugHistogram == null || !m_DebugHistogram.IsCreated())
                return;

            var rect = new Rect(8f, 8f, m_DebugHistogram.width, m_DebugHistogram.height);
            GUI.DrawTexture(rect, m_DebugHistogram);
        }
    }
}
