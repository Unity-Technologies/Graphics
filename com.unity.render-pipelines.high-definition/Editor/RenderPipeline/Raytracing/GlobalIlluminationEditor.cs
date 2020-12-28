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

        public override void OnEnable()
        {
            var o = new PropertyFetcher<GlobalIllumination>(serializedObject);

            m_Enable = Unpack(o.Find(x => x.enable));

            // SSGI Parameters
            m_FullResolutionSS = Unpack(o.Find(x => x.fullResolutionSS));
            m_DepthBufferThickness = Unpack(o.Find(x => x.depthBufferThickness));
            m_RaySteps = Unpack(o.Find(x => x.raySteps));
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

            base.OnEnable();
        }

        static public readonly GUIContent k_RayLengthText = EditorGUIUtility.TrTextContent("Max Ray Length", "Controls the maximal length of global illumination rays. The higher this value is, the more expensive ray traced global illumination is.");
        static public readonly GUIContent k_DepthBufferThicknessText = EditorGUIUtility.TrTextContent("Object Thickness", "Controls the typical thickness of objects the global illumination rays may pass behind.");

        public void DenoiserGUI()
        {
            PropertyField(m_Denoise);

            using (new HDEditorUtils.IndentScope())
            {
                PropertyField(m_HalfResolutionDenoiser);
                PropertyField(m_DenoiserRadius);
                PropertyField(m_SecondDenoiserPass);
            }
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
                PropertyField(m_RayTracing, EditorGUIUtility.TrTextContent("Ray Tracing (Preview)", "Enable ray traced global illumination."));
            }

            // Flag to track if the ray tracing parameters were displayed
            bool rayTracingSettingsDisplayed = false;

            using (new HDEditorUtils.IndentScope())
            {
                if (HDRenderPipeline.pipelineSupportsRayTracing)
                {
                    if (m_RayTracing.overrideState.boolValue && m_RayTracing.value.boolValue)
                    {
                        rayTracingSettingsDisplayed = true;
                        PropertyField(m_LayerMask);
                        if (currentAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode ==
                            RenderPipelineSettings.SupportedRayTracingMode.Both)
                        {
                            PropertyField(m_Mode);

                            using (new HDEditorUtils.IndentScope())
                            {
                                switch (m_Mode.value.GetEnumValue<RayTracingMode>())
                                {
                                    case RayTracingMode.Performance:
                                    {
                                        base.OnInspectorGUI(); // Quality Setting

                                        using (new HDEditorUtils.IndentScope())
                                        using (new QualityScope(this))
                                        {
                                            PropertyField(m_RayLength, k_RayLengthText);
                                            PropertyField(m_ClampValue);
                                            PropertyField(m_FullResolution);
                                            PropertyField(m_UpscaleRadius);
                                            DenoiserGUI();
                                        }
                                    }
                                        break;
                                    case RayTracingMode.Quality:
                                    {
                                        using (new QualityScope(this))
                                        {
                                            PropertyField(m_RayLength, k_RayLengthText);
                                            PropertyField(m_ClampValue);
                                            PropertyField(m_SampleCount);
                                            PropertyField(m_BounceCount);
                                            DenoiserGUI();
                                        }
                                    }
                                        break;
                                }
                            }
                        }
                        else if (currentAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode ==
                                 RenderPipelineSettings.SupportedRayTracingMode.Quality)
                        {
                            using (new QualityScope(this))
                            {
                                PropertyField(m_RayLength, k_RayLengthText);
                                PropertyField(m_ClampValue);
                                PropertyField(m_SampleCount);
                                PropertyField(m_BounceCount);
                                DenoiserGUI();
                            }
                        }
                        else
                        {
                            base.OnInspectorGUI(); // Quality Setting
                            EditorGUI.indentLevel++;
                            using (new QualityScope(this))
                            {
                                PropertyField(m_RayLength, k_RayLengthText);
                                PropertyField(m_ClampValue);
                                PropertyField(m_FullResolution);
                                PropertyField(m_UpscaleRadius);
                                DenoiserGUI();
                            }

                            EditorGUI.indentLevel--;
                        }

                    }
                }

                // If we dit not display the ray tracing parameter, we display the ssgi ones
                if (!rayTracingSettingsDisplayed)
                {
                    base.OnInspectorGUI(); // Quality Setting

                    using (new HDEditorUtils.IndentScope())
                    using (new QualityScope(this))
                    {
                        PropertyField(m_FullResolutionSS,EditorGUIUtility.TrTextContent("Full Resolution", "Enables full resolution mode."));
                        PropertyField(m_RaySteps);
                        PropertyField(m_FilterRadius);
                    }

                    PropertyField(m_DepthBufferThickness, k_DepthBufferThicknessText);
                }
            }
        }
        public override QualitySettingsBlob SaveCustomQualitySettingsAsObject(QualitySettingsBlob settings = null)
        {
            if (settings == null)
                settings = new QualitySettingsBlob();

            // RTGI
            settings.Save<float>(m_RayLength);
            settings.Save<float>(m_ClampValue);
            settings.Save<bool>(m_FullResolution);
            settings.Save<int>(m_UpscaleRadius);
            settings.Save<bool>(m_Denoise);
            settings.Save<bool>(m_HalfResolutionDenoiser);
            settings.Save<float>(m_DenoiserRadius);
            settings.Save<bool>(m_SecondDenoiserPass);

            // SSGI
            settings.Save<bool>(m_FullResolutionSS);
            settings.Save<int>(m_RaySteps);
            settings.Save<int>(m_FilterRadius);

            return settings;
        }

        public override void LoadSettingsFromObject(QualitySettingsBlob settings)
        {
            // RTGI
            settings.TryLoad<float>(ref m_RayLength);
            settings.TryLoad<float>(ref m_ClampValue);
            settings.TryLoad<bool>(ref m_FullResolution);
            settings.TryLoad<int>(ref m_UpscaleRadius);
            settings.TryLoad<bool>(ref m_Denoise);
            settings.TryLoad<bool>(ref m_HalfResolutionDenoiser);
            settings.TryLoad<float>(ref m_DenoiserRadius);
            settings.TryLoad<bool>(ref m_SecondDenoiserPass);

            // SSGI
            settings.TryLoad<bool>(ref m_FullResolutionSS);
            settings.TryLoad<int>(ref m_RaySteps);
            settings.TryLoad<int>(ref m_FilterRadius);
        }

        public override void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level)
        {
            // RTGI
            CopySetting(ref m_RayLength, settings.lightingQualitySettings.RTGIRayLength[level]);
            CopySetting(ref m_ClampValue, settings.lightingQualitySettings.RTGIClampValue[level]);
            CopySetting(ref m_FullResolution, settings.lightingQualitySettings.RTGIFullResolution[level]);
            CopySetting(ref m_UpscaleRadius, settings.lightingQualitySettings.RTGIUpScaleRadius[level]);
            CopySetting(ref m_Denoise, settings.lightingQualitySettings.RTGIDenoise[level]);
            CopySetting(ref m_HalfResolutionDenoiser, settings.lightingQualitySettings.RTGIHalfResDenoise[level]);
            CopySetting(ref m_DenoiserRadius, settings.lightingQualitySettings.RTGIDenoiserRadius[level]);
            CopySetting(ref m_SecondDenoiserPass, settings.lightingQualitySettings.RTGISecondDenoise[level]);

            // SSGI
            CopySetting(ref m_FullResolutionSS, settings.lightingQualitySettings.SSGIFullResolution[level]);
            CopySetting(ref m_RaySteps, settings.lightingQualitySettings.SSGIRaySteps[level]);
            CopySetting(ref m_FilterRadius, settings.lightingQualitySettings.SSGIFilterRadius[level]);
        }

        public override bool QualityEnabled()
        {
            // Quality always used for SSGI
            if (!HDRenderPipeline.rayTracingSupportedBySystem || !m_RayTracing.value.boolValue)
                return true;

            // Handle the quality usage for RTGI
            var currentAsset = HDRenderPipeline.currentAsset;

            var bothSupportedAndPerformanceMode = (currentAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode ==
                RenderPipelineSettings.SupportedRayTracingMode.Both) && (m_Mode.value.GetEnumValue<RayTracingMode>() == RayTracingMode.Performance);

            var performanceMode = currentAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode ==
                RenderPipelineSettings.SupportedRayTracingMode.Performance;

            return bothSupportedAndPerformanceMode || performanceMode;
        }
    }
}
