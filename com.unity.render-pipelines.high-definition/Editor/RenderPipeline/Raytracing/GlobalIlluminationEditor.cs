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
        SerializedDataParameter m_Tracing;

        // Screen space global illumination parameters
        SerializedDataParameter m_DepthBufferThickness;
        SerializedDataParameter m_RaySteps;
        SerializedDataParameter m_FilterRadius;

        // Ray tracing generic attributes
        SerializedDataParameter m_LayerMask;
        SerializedDataParameter m_TextureLodBias;
        SerializedDataParameter m_RayLength;
        SerializedDataParameter m_ClampValue;
        SerializedDataParameter m_Mode;

        // Mixed
        SerializedDataParameter m_MaxMixedRaySteps;

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
            m_Tracing = Unpack(o.Find(x => x.tracing));

            // SSGI Parameters
            m_DepthBufferThickness = Unpack(o.Find(x => x.depthBufferThickness));
            m_RaySteps = Unpack(o.Find(x => x.maxRaySteps));
            m_FilterRadius = Unpack(o.Find(x => x.filterRadius));

            // Ray Tracing shared parameters
            m_LayerMask = Unpack(o.Find(x => x.layerMask));
            m_TextureLodBias = Unpack(o.Find(x => x.textureLodBias));
            m_RayLength = Unpack(o.Find(x => x.rayLength));
            m_ClampValue = Unpack(o.Find(x => x.clampValue));
            m_Mode = Unpack(o.Find(x => x.mode));

            // Mixed
            m_MaxMixedRaySteps = Unpack(o.Find(x => x.maxMixedRaySteps));

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
        static public readonly GUIContent k_DepthBufferThicknessText = EditorGUIUtility.TrTextContent("Depth Tolerance", "Controls the tolerance when comparing the depth of two pixels.");
        static public readonly GUIContent k_MaxMixedRaySteps = EditorGUIUtility.TrTextContent("Max Ray Steps", "Sets the maximum number of steps HDRP uses for mixed tracing.");


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

        void RayTracingPerformanceModeGUI(bool mixed)
        {
            base.OnInspectorGUI(); // Quality Setting
            using (new HDEditorUtils.IndentScope())
            using (new QualityScope(this))
            {
                PropertyField(m_RayLength, k_RayLengthText);
                PropertyField(m_ClampValue);
                PropertyField(m_FullResolution);
                PropertyField(m_UpscaleRadius);
                if (mixed)
                    PropertyField(m_MaxMixedRaySteps, k_MaxMixedRaySteps);
                DenoiserGUI();
            }
        }

        void RayTracingQualityModeGUI()
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
                PropertyField(m_Tracing);

            // Flag to track if the ray tracing parameters were displayed
            RayCastingMode tracingMode = m_Tracing.value.GetEnumValue<RayCastingMode>();
            bool rayTracingSettingsDisplayed = HDRenderPipeline.pipelineSupportsRayTracing
                && m_Tracing.overrideState.boolValue
                && tracingMode != RayCastingMode.RayMarching;

            using (new HDEditorUtils.IndentScope())
            {
                if (rayTracingSettingsDisplayed)
                {
                    PropertyField(m_LayerMask);
                    PropertyField(m_TextureLodBias);

                    if (currentAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Both)
                    {
                        if (tracingMode == RayCastingMode.RayTracing)
                        {
                            PropertyField(m_Mode);
                            using (new HDEditorUtils.IndentScope())
                            {
                                switch (m_Mode.value.GetEnumValue<RayTracingMode>())
                                {
                                    case RayTracingMode.Performance:
                                    {
                                        RayTracingPerformanceModeGUI(false);
                                    }
                                    break;
                                    case RayTracingMode.Quality:
                                    {
                                        RayTracingQualityModeGUI();
                                    }
                                    break;
                                }
                            }
                        }
                        else
                        {
                            RayTracingPerformanceModeGUI(true);
                        }
                    }
                    else if (currentAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Quality)
                    {
                        if (tracingMode == RayCastingMode.RayTracing)
                            RayTracingQualityModeGUI();
                        else
                            EditorGUILayout.HelpBox("The current HDRP Asset does not support the mixed mode which is only available in performance mode.", MessageType.Error, wide: true);
                    }
                    else
                    {
                        RayTracingPerformanceModeGUI(tracingMode == RayCastingMode.Mixed);
                    }
                }
                else
                {
                    base.OnInspectorGUI(); // Quality Setting

                    using (new HDEditorUtils.IndentScope())
                    using (new QualityScope(this))
                    {
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
            settings.Save<int>(m_MaxMixedRaySteps);
            settings.Save<bool>(m_Denoise);
            settings.Save<bool>(m_HalfResolutionDenoiser);
            settings.Save<float>(m_DenoiserRadius);
            settings.Save<bool>(m_SecondDenoiserPass);

            // SSGI
            settings.Save<int>(m_RaySteps);
            settings.Save<int>(m_FilterRadius);

            return settings;
        }

        public override void LoadSettingsFromObject(QualitySettingsBlob settings)
        {
            if (HDRenderPipeline.pipelineSupportsRayTracing && m_Tracing.overrideState.boolValue &&
                m_Tracing.value.GetEnumValue<RayCastingMode>() != RayCastingMode.RayMarching)
            {
                // RTGI
                settings.TryLoad<float>(ref m_RayLength);
                settings.TryLoad<float>(ref m_ClampValue);
                settings.TryLoad<bool>(ref m_FullResolution);
                settings.TryLoad<int>(ref m_UpscaleRadius);
                settings.TryLoad<int>(ref m_MaxMixedRaySteps);
                settings.TryLoad<bool>(ref m_Denoise);
                settings.TryLoad<bool>(ref m_HalfResolutionDenoiser);
                settings.TryLoad<float>(ref m_DenoiserRadius);
                settings.TryLoad<bool>(ref m_SecondDenoiserPass);
            }
            else
            {
                // SSGI
                settings.TryLoad<int>(ref m_RaySteps);
                settings.TryLoad<int>(ref m_FilterRadius);
            }
        }

        public override void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level)
        {
            if (HDRenderPipeline.pipelineSupportsRayTracing && m_Tracing.overrideState.boolValue &&
                m_Tracing.value.GetEnumValue<RayCastingMode>() != RayCastingMode.RayMarching)
            {
                // RTGI
                CopySetting(ref m_RayLength, settings.lightingQualitySettings.RTGIRayLength[level]);
                CopySetting(ref m_ClampValue, settings.lightingQualitySettings.RTGIClampValue[level]);
                CopySetting(ref m_FullResolution, settings.lightingQualitySettings.RTGIFullResolution[level]);
                CopySetting(ref m_UpscaleRadius, settings.lightingQualitySettings.RTGIUpScaleRadius[level]);
                CopySetting(ref m_MaxMixedRaySteps, settings.lightingQualitySettings.RTGIRaySteps[level]);
                CopySetting(ref m_Denoise, settings.lightingQualitySettings.RTGIDenoise[level]);
                CopySetting(ref m_HalfResolutionDenoiser, settings.lightingQualitySettings.RTGIHalfResDenoise[level]);
                CopySetting(ref m_DenoiserRadius, settings.lightingQualitySettings.RTGIDenoiserRadius[level]);
                CopySetting(ref m_SecondDenoiserPass, settings.lightingQualitySettings.RTGISecondDenoise[level]);
            }
            else
            {
                // SSGI
                CopySetting(ref m_RaySteps, settings.lightingQualitySettings.SSGIRaySteps[level]);
                CopySetting(ref m_FilterRadius, settings.lightingQualitySettings.SSGIFilterRadius[level]);
            }
        }

        public override bool QualityEnabled()
        {
            // Quality always used for SSGI
            if (!HDRenderPipeline.rayTracingSupportedBySystem || m_Tracing.value.GetEnumValue<RayCastingMode>() == RayCastingMode.RayMarching)
                return true;

            // Handle the quality usage for RTGI
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;

            // Define if the asset supports Peformance or Both Mode (Quality && Performance)
            bool assetSupportsPerf = currentAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Performance;
            bool assetSupportsBoth = currentAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Both;

            // Define if the volume is in Peformance or Mixed Mode
            bool volumeIsInPerfOrMixed = (m_Tracing.value.GetEnumValue<RayCastingMode>() == RayCastingMode.RayTracing && m_Mode.value.GetEnumValue<RayTracingMode>() == RayTracingMode.Performance)
                || (m_Tracing.value.GetEnumValue<RayCastingMode>() == RayCastingMode.Mixed);

            return (assetSupportsBoth && volumeIsInPerfOrMixed) || (assetSupportsPerf && m_Tracing.value.GetEnumValue<RayCastingMode>() != RayCastingMode.RayMarching);
        }
    }
}
