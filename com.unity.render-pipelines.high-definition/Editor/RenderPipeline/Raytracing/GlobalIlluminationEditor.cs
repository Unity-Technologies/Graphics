using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(GlobalIllumination))]
    class GlobalIlluminatorEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_DepthBufferThickness;
        SerializedDataParameter m_RaySteps;
        SerializedDataParameter m_MaximalRadius;
        SerializedDataParameter m_UseProbeAsFallback;
        SerializedDataParameter m_ProbeFallbackBias;
        SerializedDataParameter m_PyramidBias;

        SerializedDataParameter m_LayerMask;
        SerializedDataParameter m_RayTracing;
        SerializedDataParameter m_RayLength;
        SerializedDataParameter m_ClampValue;
        SerializedDataParameter m_Mode;

        // Performance
        SerializedDataParameter m_FullResolution;
        SerializedDataParameter m_UpscaleRadius;

        // Quality
        SerializedDataParameter m_SampleCount;
        SerializedDataParameter m_BounceCount;

        // Filtering
        SerializedDataParameter m_Denoise;
        SerializedDataParameter m_HalfResolutionDenoiser;
        SerializedDataParameter m_DenoiserRadius;
        SerializedDataParameter m_SecondDenoiserPass;
        SerializedDataParameter m_SecondDenoiserRadius;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<GlobalIllumination>(serializedObject);

            m_DepthBufferThickness = Unpack(o.Find(x => x.depthBufferThickness));
            m_RaySteps = Unpack(o.Find(x => x.raySteps));
            m_MaximalRadius = Unpack(o.Find(x => x.maximalRadius));
            m_UseProbeAsFallback = Unpack(o.Find(x => x.useProbeAsFallback));
            m_ProbeFallbackBias = Unpack(o.Find(x => x.probeFallbackBias));
            m_PyramidBias = Unpack(o.Find(x => x.pyramidBias));

            m_LayerMask = Unpack(o.Find(x => x.layerMask));
            m_RayTracing = Unpack(o.Find(x => x.rayTracing));
            m_RayLength = Unpack(o.Find(x => x.rayLength));
            m_ClampValue = Unpack(o.Find(x => x.clampValue));
            m_Mode = Unpack(o.Find(x => x.mode));

            // Performance
            m_FullResolution = Unpack(o.Find(x => x.fullResolution));
            m_UpscaleRadius = Unpack(o.Find(x => x.upscaleRadius));

            // Quality
            m_SampleCount = Unpack(o.Find(x => x.sampleCount));
            m_BounceCount = Unpack(o.Find(x => x.bounceCount));

            // Filtering
            m_Denoise = Unpack(o.Find(x => x.denoise));
            m_HalfResolutionDenoiser = Unpack(o.Find(x => x.halfResolutionDenoiser));
            m_DenoiserRadius = Unpack(o.Find(x => x.denoiserRadius));
            m_SecondDenoiserPass = Unpack(o.Find(x => x.secondDenoiserPass));
            m_SecondDenoiserRadius = Unpack(o.Find(x => x.secondDenoiserRadius));
        }

        public override void OnInspectorGUI()
        {
            // If ray tracing is supported display the content of the volume component
            if (HDRenderPipeline.pipelineSupportsRayTracing)
            {
                PropertyField(m_RayTracing);
            }

            bool rayTracingSettingsDisplayed = false;

            EditorGUI.indentLevel++;
            if (HDRenderPipeline.pipelineSupportsRayTracing)
            {
                if (m_RayTracing.overrideState.boolValue && m_RayTracing.value.boolValue)
                {
                    rayTracingSettingsDisplayed = true;
                    PropertyField(m_LayerMask);
                    PropertyField(m_RayLength);
                    PropertyField(m_ClampValue);
                    PropertyField(m_Mode);
                    EditorGUI.indentLevel++;
                    switch (m_Mode.value.GetEnumValue<RayTracingMode>())
                    {
                        case RayTracingMode.Performance:
                            {
                                PropertyField(m_FullResolution);
                                PropertyField(m_UpscaleRadius);
                            }
                            break;
                        case RayTracingMode.Quality:
                            {
                                PropertyField(m_SampleCount);
                                PropertyField(m_BounceCount);
                            }
                            break;
                    }
                    EditorGUI.indentLevel--;
                    PropertyField(m_Denoise);
                    {
                        EditorGUI.indentLevel++;
                        PropertyField(m_HalfResolutionDenoiser);
                        PropertyField(m_DenoiserRadius);
                        PropertyField(m_SecondDenoiserPass);
                        PropertyField(m_SecondDenoiserRadius);
                        EditorGUI.indentLevel--;
                    }
                }
            }

            if (!rayTracingSettingsDisplayed)
            {
                PropertyField(m_FullResolution);
                PropertyField(m_ClampValue);
                PropertyField(m_DepthBufferThickness);
                PropertyField(m_RaySteps);
                PropertyField(m_MaximalRadius);
                PropertyField(m_PyramidBias);
                PropertyField(m_Denoise);
                {
                    EditorGUI.indentLevel++;
                    PropertyField(m_HalfResolutionDenoiser);
                    PropertyField(m_SecondDenoiserPass);
                    PropertyField(m_DenoiserRadius);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUI.indentLevel--;
        }
    }
}
