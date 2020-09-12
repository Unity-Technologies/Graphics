using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;


namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(GlobalIllumination))]
    class GlobalIlluminatorEditor : VolumeComponentWithQualityEditor
    {
        // Shared rasterization / ray tracing parameter
        SerializedDataParameter m_Enable;

        // Screen space global illumination parameters
        SerializedDataParameter m_FullResolutionSS;
        SerializedDataParameter m_DepthBufferThickness;
        SerializedDataParameter m_RaySteps;
        SerializedDataParameter m_MaximalRadius;
        SerializedDataParameter m_ClampValueSS;
        SerializedDataParameter m_FilterRadius;

        // Ray tracing generic attributes
        SerializedDataParameter m_RayTracing;
        SerializedDataParameter m_LayerMask;
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

        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<GlobalIllumination>(serializedObject);

            m_Enable = Unpack(o.Find(x => x.enable));

            // SSGI Parameters
            m_FullResolutionSS = Unpack(o.Find(x => x.fullResolutionSS));
            m_DepthBufferThickness = Unpack(o.Find(x => x.depthBufferThickness));
            m_RaySteps = Unpack(o.Find(x => x.raySteps));
            m_MaximalRadius = Unpack(o.Find(x => x.maximalRadius));
            m_ClampValueSS = Unpack(o.Find(x => x.clampValueSS));
            m_FilterRadius = Unpack(o.Find(x => x.filterRadius));

            // Ray Tracing shared parameters
            m_RayTracing = Unpack(o.Find(x => x.rayTracing));
            m_LayerMask = Unpack(o.Find(x => x.layerMask));
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

            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportSSGI ?? false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Screen Space Global illumination.", MessageType.Error, wide: true);
                return;
            }

            PropertyField(m_Enable);
            
            // If ray tracing is supported display the content of the volume component
            if (HDRenderPipeline.pipelineSupportsRayTracing)
            {
                PropertyField(m_RayTracing);
            }

            // Flag to track if the ray tracing parameters were displayed
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
                    if (currentAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Both)
                    {
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
                    }
                    else if (currentAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Quality)
                    {
                        PropertyField(m_SampleCount);
                        PropertyField(m_BounceCount);
                    }
                    else
                    {
                        PropertyField(m_FullResolution);
                        PropertyField(m_UpscaleRadius);
                    }

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

            // If we dit not display the ray tracing parameter, we display the ssgi ones
            if (!rayTracingSettingsDisplayed)
            {
                base.OnInspectorGUI(); // Quality Setting
                EditorGUI.indentLevel++;
                GUI.enabled = useCustomValue;
                PropertyField(m_FullResolutionSS, EditorGUIUtility.TrTextContent("Full Resolution", "Enables full resolution mode."));
                PropertyField(m_RaySteps);
                PropertyField(m_MaximalRadius);
                PropertyField(m_ClampValueSS, EditorGUIUtility.TrTextContent("Clamp Value", "Clamps the exposed intensity."));
                PropertyField(m_FilterRadius);
                GUI.enabled = true;
                EditorGUI.indentLevel--;
                PropertyField(m_DepthBufferThickness);
            }

            EditorGUI.indentLevel--;
        }
    }
}
