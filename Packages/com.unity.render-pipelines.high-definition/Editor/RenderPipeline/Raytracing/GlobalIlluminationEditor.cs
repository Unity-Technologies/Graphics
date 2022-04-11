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
        SerializedDataParameter m_RayMiss;

        // Screen space global illumination parameters
        SerializedDataParameter m_FullResolutionSS;
        SerializedDataParameter m_DepthBufferThickness;
        SerializedDataParameter m_RaySteps;

        // Ray tracing generic attributes
        SerializedDataParameter m_LastBounce;
        SerializedDataParameter m_LayerMask;
        SerializedDataParameter m_ReceiverMotionRejection;
        SerializedDataParameter m_TextureLodBias;
        SerializedDataParameter m_RayLength;
        SerializedDataParameter m_ClampValue;
        SerializedDataParameter m_Mode;

        // Mixed
        SerializedDataParameter m_MaxMixedRaySteps;

        // Performance
        SerializedDataParameter m_FullResolution;

        // Quality
        SerializedDataParameter m_SampleCount;
        SerializedDataParameter m_BounceCount;

        // Filtering RT
        SerializedDataParameter m_Denoise;
        SerializedDataParameter m_HalfResolutionDenoiser;
        SerializedDataParameter m_DenoiserRadius;
        SerializedDataParameter m_SecondDenoiserPass;

        // Filtering SS
        SerializedDataParameter m_DenoiseSS;
        SerializedDataParameter m_HalfResolutionDenoiserSS;
        SerializedDataParameter m_DenoiserRadiusSS;
        SerializedDataParameter m_SecondDenoiserPassSS;

        public override bool hasAdditionalProperties => true;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<GlobalIllumination>(serializedObject);

            m_Enable = Unpack(o.Find(x => x.enable));
            m_Tracing = Unpack(o.Find(x => x.tracing));
            m_RayMiss = Unpack(o.Find(x => x.rayMiss));

            // SSGI Parameters
            m_FullResolutionSS = Unpack(o.Find(x => x.fullResolutionSS));
            m_DepthBufferThickness = Unpack(o.Find(x => x.depthBufferThickness));
            m_RaySteps = Unpack(o.Find(x => x.maxRaySteps));

            // Ray Tracing shared parameters
            m_LastBounce = Unpack(o.Find(x => x.lastBounceFallbackHierarchy));
            m_LayerMask = Unpack(o.Find(x => x.layerMask));
            m_ReceiverMotionRejection = Unpack(o.Find(x => x.receiverMotionRejection));
            m_TextureLodBias = Unpack(o.Find(x => x.textureLodBias));
            m_RayLength = Unpack(o.Find(x => x.rayLength));
            m_ClampValue = Unpack(o.Find(x => x.clampValue));
            m_Mode = Unpack(o.Find(x => x.mode));

            // Mixed
            m_MaxMixedRaySteps = Unpack(o.Find(x => x.maxMixedRaySteps));

            // Performance
            m_FullResolution = Unpack(o.Find(x => x.fullResolution));

            // Quality
            m_SampleCount = Unpack(o.Find(x => x.sampleCount));
            m_BounceCount = Unpack(o.Find(x => x.bounceCount));

            // Filtering
            m_Denoise = Unpack(o.Find(x => x.denoise));
            m_HalfResolutionDenoiser = Unpack(o.Find(x => x.halfResolutionDenoiser));
            m_DenoiserRadius = Unpack(o.Find(x => x.denoiserRadius));
            m_SecondDenoiserPass = Unpack(o.Find(x => x.secondDenoiserPass));

            // Filtering SS
            m_DenoiseSS = Unpack(o.Find(x => x.denoiseSS));
            m_HalfResolutionDenoiserSS = Unpack(o.Find(x => x.halfResolutionDenoiserSS));
            m_DenoiserRadiusSS = Unpack(o.Find(x => x.denoiserRadiusSS));
            m_SecondDenoiserPassSS = Unpack(o.Find(x => x.secondDenoiserPassSS));

            base.OnEnable();
        }

        static public readonly GUIContent k_RayLengthText = EditorGUIUtility.TrTextContent("Max Ray Length", "Controls the maximal length of global illumination rays in meters. The higher this value is, the more expensive ray traced global illumination is.");
        static public readonly GUIContent k_FullResolutionSSText = EditorGUIUtility.TrTextContent("Full Resolution", "Controls if the screen space global illumination should be evaluated at half resolution.");
        static public readonly GUIContent k_DepthBufferThicknessText = EditorGUIUtility.TrTextContent("Depth Tolerance", "Controls the tolerance when comparing the depth of two pixels.");
        static public readonly GUIContent k_RayMissFallbackHierarchyText = EditorGUIUtility.TrTextContent("Ray Miss", "Controls the fallback hierarchy for indirect diffuse in case the ray misses.");
        static public readonly GUIContent k_LastBounceFallbackHierarchyText = EditorGUIUtility.TrTextContent("Last Bounce", "Controls the fallback hierarchy for lighting the last bounce.");
        static public readonly GUIContent k_MaxMixedRaySteps = EditorGUIUtility.TrTextContent("Max Ray Steps", "Sets the maximum number of steps HDRP uses for mixed tracing.");

        static public readonly GUIContent k_DenoiseText = EditorGUIUtility.TrTextContent("Denoise", "Denoise the screen space GI.");
        static public readonly GUIContent k_HalfResolutionDenoiserText = EditorGUIUtility.TrTextContent("Half Resolution Denoiser", "Use a half resolution denoiser.");
        static public readonly GUIContent k_DenoiserRadiusText = EditorGUIUtility.TrTextContent("Denoiser Radius", "Controls the radius of the GI denoiser (First Pass).");
        static public readonly GUIContent k_SecondDenoiserPassText = EditorGUIUtility.TrTextContent("Second Denoiser Pass", "Enable second denoising pass.");

        public void DenoiserGUI()
        {
            PropertyField(m_Denoise);

            using (new IndentLevelScope())
            {
                PropertyField(m_HalfResolutionDenoiser);
                PropertyField(m_DenoiserRadius);
                PropertyField(m_SecondDenoiserPass);
            }
        }

        public void DenoiserSSGUI()
        {
            PropertyField(m_DenoiseSS, k_DenoiseText);

            using (new IndentLevelScope())
            {
                PropertyField(m_HalfResolutionDenoiserSS, k_HalfResolutionDenoiserText);
                PropertyField(m_DenoiserRadiusSS, k_DenoiserRadiusText);
                PropertyField(m_SecondDenoiserPassSS, k_SecondDenoiserPassText);
            }
        }

        void RayTracingPerformanceModeGUI(bool mixed)
        {
            base.OnInspectorGUI(); // Quality Setting
            using (new IndentLevelScope())
            using (new QualityScope(this))
            {
                PropertyField(m_RayLength, k_RayLengthText);
                PropertyField(m_ClampValue);
                PropertyField(m_FullResolution);
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
            EditorGUILayout.Space();

            // If ray tracing is supported display the content of the volume component
            if (HDRenderPipeline.assetSupportsRayTracing)
                PropertyField(m_Tracing);

            // Flag to track if the ray tracing parameters were displayed
            RayCastingMode tracingMode = m_Tracing.value.GetEnumValue<RayCastingMode>();
            bool rayTracingSettingsDisplayed = HDRenderPipeline.assetSupportsRayTracing
                && m_Tracing.overrideState.boolValue
                && tracingMode != RayCastingMode.RayMarching;

            using (new IndentLevelScope())
            {
                if (rayTracingSettingsDisplayed)
                {
                    PropertyField(m_LayerMask);
                    PropertyField(m_TextureLodBias);

                    using (new IndentLevelScope())
                    {
                        EditorGUILayout.LabelField("Fallback", EditorStyles.miniLabel);
                        PropertyField(m_RayMiss, k_RayMissFallbackHierarchyText);
                        PropertyField(m_LastBounce, k_LastBounceFallbackHierarchyText);
                    }

                    if (currentAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Both)
                    {
                        if (tracingMode == RayCastingMode.RayTracing)
                        {
                            PropertyField(m_Mode);
                            using (new IndentLevelScope())
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
                    PropertyField(m_ReceiverMotionRejection);
                }
                else
                {
                    base.OnInspectorGUI(); // Quality Setting

                    using (new IndentLevelScope())
                    using (new QualityScope(this))
                    {
                        PropertyField(m_RaySteps);
                        DenoiserSSGUI();
                    }
                    PropertyField(m_FullResolutionSS, k_FullResolutionSSText);
                    PropertyField(m_DepthBufferThickness, k_DepthBufferThicknessText);
                    PropertyField(m_RayMiss, k_RayMissFallbackHierarchyText);
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
            settings.Save<int>(m_MaxMixedRaySteps);
            settings.Save<bool>(m_Denoise);
            settings.Save<bool>(m_HalfResolutionDenoiser);
            settings.Save<float>(m_DenoiserRadius);
            settings.Save<bool>(m_SecondDenoiserPass);

            // SSGI
            settings.Save<int>(m_RaySteps);
            settings.Save<bool>(m_DenoiseSS);
            settings.Save<bool>(m_HalfResolutionDenoiserSS);
            settings.Save<float>(m_DenoiserRadiusSS);
            settings.Save<bool>(m_SecondDenoiserPassSS);

            return settings;
        }

        public override void LoadSettingsFromObject(QualitySettingsBlob settings)
        {
            if (HDRenderPipeline.assetSupportsRayTracing && m_Tracing.overrideState.boolValue &&
                m_Tracing.value.GetEnumValue<RayCastingMode>() != RayCastingMode.RayMarching)
            {
                // RTGI
                settings.TryLoad<float>(ref m_RayLength);
                settings.TryLoad<float>(ref m_ClampValue);
                settings.TryLoad<bool>(ref m_FullResolution);
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
                settings.TryLoad<bool>(ref m_DenoiseSS);
                settings.TryLoad<bool>(ref m_HalfResolutionDenoiserSS);
                settings.TryLoad<float>(ref m_DenoiserRadiusSS);
                settings.TryLoad<bool>(ref m_SecondDenoiserPassSS);
            }
        }

        public override void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level)
        {
            if (HDRenderPipeline.assetSupportsRayTracing && m_Tracing.overrideState.boolValue &&
                m_Tracing.value.GetEnumValue<RayCastingMode>() != RayCastingMode.RayMarching)
            {
                // RTGI
                CopySetting(ref m_RayLength, settings.lightingQualitySettings.RTGIRayLength[level]);
                CopySetting(ref m_ClampValue, settings.lightingQualitySettings.RTGIClampValue[level]);
                CopySetting(ref m_FullResolution, settings.lightingQualitySettings.RTGIFullResolution[level]);
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
                CopySetting(ref m_DenoiseSS, settings.lightingQualitySettings.SSGIDenoise[level]);
                CopySetting(ref m_HalfResolutionDenoiserSS, settings.lightingQualitySettings.SSGIHalfResDenoise[level]);
                CopySetting(ref m_DenoiserRadiusSS, settings.lightingQualitySettings.SSGIDenoiserRadius[level]);
                CopySetting(ref m_SecondDenoiserPassSS, settings.lightingQualitySettings.SSGISecondDenoise[level]);
            }
        }

        public override bool QualityEnabled()
        {
            // Quality always used for SSGI
            if (!HDRenderPipeline.assetSupportsRayTracing || m_Tracing.value.GetEnumValue<RayCastingMode>() == RayCastingMode.RayMarching)
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
