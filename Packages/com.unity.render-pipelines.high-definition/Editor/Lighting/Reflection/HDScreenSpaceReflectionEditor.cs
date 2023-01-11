using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(ScreenSpaceReflection))]
    class HDScreenSpaceReflectionEditor : VolumeComponentWithQualityEditor
    {
        // Shared data
        SerializedDataParameter m_Enable;
        SerializedDataParameter m_EnableTransparent;
        SerializedDataParameter m_Tracing;
        SerializedDataParameter m_MinSmoothness;
        SerializedDataParameter m_SmoothnessFadeStart;

        // SSR Only
        SerializedDataParameter m_ReflectSky;
        SerializedDataParameter m_UsedAlgorithm;
        SerializedDataParameter m_ScreenFadeDistance;
        SerializedDataParameter m_RayMaxIterations;
        SerializedDataParameter m_DepthBufferThickness;
        SerializedDataParameter m_AccumulationFactor;

        // Ray Tracing
        SerializedDataParameter m_RayMiss;
        SerializedDataParameter m_LastBounce;
        SerializedDataParameter m_AmbientProbeDimmer;
        SerializedDataParameter m_LayerMask;
        SerializedDataParameter m_TextureLodBias;
        SerializedDataParameter m_RayLength;
        SerializedDataParameter m_ClampValue;
        SerializedDataParameter m_Denoise;
        SerializedDataParameter m_DenoiserRadius;
        SerializedDataParameter m_AffectsSmoothSurfaces;
        SerializedDataParameter m_Mode;

        // Mixed
        SerializedDataParameter m_RayMaxIterationsRT;

        // Performance
        SerializedDataParameter m_FullResolution;

        // Quality
        SerializedDataParameter m_SampleCount;
        SerializedDataParameter m_BounceCount;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScreenSpaceReflection>(serializedObject);

            // Shared data
            m_Enable = Unpack(o.Find(x => x.enabled));
            m_EnableTransparent = Unpack(o.Find(x => x.enabledTransparent));
            m_Tracing = Unpack(o.Find(x => x.tracing));
            m_MinSmoothness = Unpack(o.Find(x => x.minSmoothness));
            m_SmoothnessFadeStart = Unpack(o.Find(x => x.smoothnessFadeStart));

            // SSR Data
            m_ReflectSky = Unpack(o.Find(x => x.reflectSky));
            m_UsedAlgorithm = Unpack(o.Find(x => x.usedAlgorithm));
            m_DepthBufferThickness = Unpack(o.Find(x => x.depthBufferThickness));
            m_RayMaxIterations = Unpack(o.Find(x => x.rayMaxIterations));
            m_ScreenFadeDistance = Unpack(o.Find(x => x.screenFadeDistance));
            m_AccumulationFactor = Unpack(o.Find(x => x.accumulationFactor));

            // Generic ray tracing
            m_RayMiss = Unpack(o.Find(x => x.rayMiss));
            m_LastBounce = Unpack(o.Find(x => x.lastBounceFallbackHierarchy));
            m_AmbientProbeDimmer = Unpack(o.Find(x => x.ambientProbeDimmer));
            m_LayerMask = Unpack(o.Find(x => x.layerMask));
            m_TextureLodBias = Unpack(o.Find(x => x.textureLodBias));
            m_RayLength = Unpack(o.Find(x => x.rayLength));
            m_ClampValue = Unpack(o.Find(x => x.clampValue));
            m_Denoise = Unpack(o.Find(x => x.denoise));
            m_DenoiserRadius = Unpack(o.Find(x => x.denoiserRadius));
            m_AffectsSmoothSurfaces = Unpack(o.Find(x => x.affectSmoothSurfaces));
            m_Mode = Unpack(o.Find(x => x.mode));

            // Mixed
            m_RayMaxIterationsRT = Unpack(o.Find(x => x.rayMaxIterationsRT));

            // Performance
            m_FullResolution = Unpack(o.Find(x => x.fullResolution));

            // Quality
            m_SampleCount = Unpack(o.Find(x => x.sampleCount));
            m_BounceCount = Unpack(o.Find(x => x.bounceCount));

            base.OnEnable();
        }

        static public readonly GUIContent k_EnabledOpaque = EditorGUIUtility.TrTextContent("Enabled (Opaque)", "Enable Screen Space Reflections.");
        static public readonly GUIContent k_EnabledTransparent = EditorGUIUtility.TrTextContent("Enabled (Transparent)", "Enable Transparent Screen Space Reflections");
        static public readonly GUIContent k_Algo = EditorGUIUtility.TrTextContent("Algorithm", "The screen space reflection algorithm used.");
        static public readonly GUIContent k_TracingText = EditorGUIUtility.TrTextContent("Tracing", "Controls the technique used to compute the reflection.Controls the technique used to compute the reflections. Ray marching uses a ray-marched screen-space solution, Ray tracing uses a hardware accelerated world-space solution. Mixed uses first Ray marching, then Ray tracing if it fails to intersect on-screen geometry.");
        static public readonly GUIContent k_ReflectSkyText = EditorGUIUtility.TrTextContent("Reflect Sky", "When enabled, SSR handles sky reflection.");
        static public readonly GUIContent k_LayerMaskText = EditorGUIUtility.TrTextContent("Layer Mask", "Layer mask used to include the objects for ray traced reflections.");
        static public readonly GUIContent k_RayMissFallbackHierarchyText = EditorGUIUtility.TrTextContent("Ray Miss", "Controls the order in which fall backs are used when a ray misses.");
        static public readonly GUIContent k_LastBounceFallbackHierarchyText = EditorGUIUtility.TrTextContent("Last Bounce", "Controls the fallback hierarchy for lighting the last bounce.");
        static public readonly GUIContent k_TextureLodBiasText = EditorGUIUtility.TrTextContent("Texture Lod Bias", "The LOD Bias HDRP applies to textures in the reflection. A higher value increases performance and makes denoising easier, but it might reduce visual fidelity.");
        static public readonly GUIContent k_MinimumSmoothnessText = EditorGUIUtility.TrTextContent("Minimum Smoothness", "Controls the smoothness value at which HDRP activates SSR and the smoothness-controlled fade out stops.");
        static public readonly GUIContent k_SmoothnessFadeStartText = EditorGUIUtility.TrTextContent("Smoothness Fade Start", "Controls the smoothness value at which the smoothness-controlled fade out starts. The fade is in the range [Min Smoothness, Smoothness Fade Start].");
        static public readonly GUIContent k_ScreenFaceDistanceText = EditorGUIUtility.TrTextContent("Screen Edge Fade Distance", "Controls the distance at which HDRP fades out SSR near the edge of the screen.");
        static public readonly GUIContent k_AccumulationFactorText = EditorGUIUtility.TrTextContent("Accumulation Factor", "Controls Controls the amount of accumulation (0 no accumulation, 1 just accumulate).");
        static public readonly GUIContent k_DepthBufferThicknessText = EditorGUIUtility.TrTextContent("Object Thickness", "Controls the typical thickness of objects the reflection rays may pass behind.");
        static public readonly GUIContent k_RayMaxIterationsText = EditorGUIUtility.TrTextContent("Max Ray Steps", "Sets the maximum number of steps HDRP uses for ray marching. Affects both correctness and performance.");
        static public readonly GUIContent k_RayLengthText = EditorGUIUtility.TrTextContent("Max Ray Length", "Controls the maximal length of reflection rays in meters. The higher this value is, the more expensive ray traced reflections are.");
        static public readonly GUIContent k_ClampValueText = EditorGUIUtility.TrTextContent("Clamp Value", "Clamps the exposed intensity, this only affects reflections on opaque objects.");
        static public readonly GUIContent k_SampleCountText = EditorGUIUtility.TrTextContent("Sample Count", "Number of samples for reflections.");
        static public readonly GUIContent k_BounceCountText = EditorGUIUtility.TrTextContent("Bounce Count", "Number of bounces for reflection rays.");
        static public readonly GUIContent k_ModeText = EditorGUIUtility.TrTextContent("Mode", "Controls which version of the effect should be used.");
        static public readonly GUIContent k_DenoiseText = EditorGUIUtility.TrTextContent("Denoise", "Enable denoising on the ray traced reflections.");
        static public readonly GUIContent k_FullResolutionText = EditorGUIUtility.TrTextContent("Full Resolution", "Enables full resolution mode.");
        static public readonly GUIContent k_DenoiseRadiusText = EditorGUIUtility.TrTextContent("Denoiser Radius", "Controls the radius of reflection denoiser.");
        static public readonly GUIContent k_AffectsSmoothSurfacesText = EditorGUIUtility.TrTextContent("Affects Smooth Surfaces", "When enabled, the denoiser also affects perfectly smooth surfaces. When you use Quality mode with multiple bounces, the denoiser always affects smooth surfaces by default.");
        static public readonly GUIContent k_MaxMixedRaySteps = EditorGUIUtility.TrTextContent("Max Ray Steps", "Sets the maximum number of steps HDRP uses for mixed tracing.");

        void RayTracingQualityModeGUI()
        {
            using (new QualityScope(this))
            {
                PropertyField(m_MinSmoothness, k_MinimumSmoothnessText);
                PropertyField(m_SmoothnessFadeStart, k_SmoothnessFadeStartText);
                m_SmoothnessFadeStart.value.floatValue = Mathf.Max(m_MinSmoothness.value.floatValue, m_SmoothnessFadeStart.value.floatValue);
                PropertyField(m_RayLength, k_RayLengthText);
                PropertyField(m_ClampValue, k_ClampValueText);
                PropertyField(m_SampleCount, k_SampleCountText);
                PropertyField(m_BounceCount, k_BounceCountText);
                PropertyField(m_Denoise, k_DenoiseText);
                using (new IndentLevelScope())
                {
                    PropertyField(m_DenoiserRadius, k_DenoiseRadiusText);
                    PropertyField(m_AffectsSmoothSurfaces, k_AffectsSmoothSurfacesText);
                }
            }
        }

        void RayTracingPerformanceModeGUI(bool mixed)
        {
            base.OnInspectorGUI();

            using (new IndentLevelScope())
            using (new QualityScope(this))
            {
                PropertyField(m_MinSmoothness, k_MinimumSmoothnessText);
                PropertyField(m_SmoothnessFadeStart, k_SmoothnessFadeStartText);
                m_SmoothnessFadeStart.value.floatValue = Mathf.Max(m_MinSmoothness.value.floatValue, m_SmoothnessFadeStart.value.floatValue);
                PropertyField(m_RayLength, k_RayLengthText);
                PropertyField(m_ClampValue, k_ClampValueText);
                PropertyField(m_FullResolution, k_FullResolutionText);
                if (mixed)
                    PropertyField(m_RayMaxIterationsRT, k_MaxMixedRaySteps);
                PropertyField(m_Denoise, k_DenoiseText);
                using (new IndentLevelScope())
                {
                    PropertyField(m_DenoiserRadius, k_DenoiseRadiusText);
                    PropertyField(m_AffectsSmoothSurfaces, k_AffectsSmoothSurfacesText);
                }
            }
        }

        void RayTracedReflectionGUI(RayCastingMode tracingMode)
        {
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;

            if (RenderPipelineManager.currentPipeline is not HDRenderPipeline { rayTracingSupported: true })
                HDRenderPipelineUI.DisplayRayTracingSupportBox();

            if (showAdditionalProperties)
            {
                using (new IndentLevelScope())
                {
                    EditorGUILayout.LabelField("Fallback", EditorStyles.miniLabel);
                    PropertyField(m_RayMiss, k_RayMissFallbackHierarchyText);
                    PropertyField(m_LastBounce, k_LastBounceFallbackHierarchyText);
                    PropertyField(m_AmbientProbeDimmer);
                }
            }

            PropertyField(m_LayerMask, k_LayerMaskText);
            PropertyField(m_TextureLodBias, k_TextureLodBiasText);

            if (currentAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Both)
            {
                if (tracingMode == RayCastingMode.RayTracing)
                {
                    PropertyField(m_Mode, k_ModeText);

                    using (new IndentLevelScope())
                    {
                        switch (m_Mode.value.GetEnumValue<RayTracingMode>())
                        {
                            case RayTracingMode.Performance:
                                RayTracingPerformanceModeGUI(false);
                                break;
                            case RayTracingMode.Quality:
                                RayTracingQualityModeGUI();
                                break;
                        }
                    }
                }
                else
                    RayTracingPerformanceModeGUI(true);
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

        public override void OnInspectorGUI()
        {
            // This whole editor has nothing to display if the SSR feature is not supported
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportSSR ?? false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Screen Space Reflection.", MessageType.Error, wide: true);
                return;
            }

            PropertyField(m_Enable, k_EnabledOpaque);

            bool transparentSSRSupported = currentAsset.currentPlatformRenderPipelineSettings.supportSSR
                                            && currentAsset.currentPlatformRenderPipelineSettings.supportSSRTransparent
                                            && currentAsset.currentPlatformRenderPipelineSettings.supportTransparentDepthPrepass;
            if (transparentSSRSupported)
            {
                PropertyField(m_EnableTransparent, k_EnabledTransparent);
            }
            else
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Transparent Screen Space Reflection.", MessageType.Info, wide: true);
            }

            // If ray tracing is supported display the tracing choice
            if (HDRenderPipeline.assetSupportsRayTracing)
                PropertyField(m_Tracing, k_TracingText);

            // Flag to track if the ray tracing parameters were displayed
            RayCastingMode tracingMode = m_Tracing.value.GetEnumValue<RayCastingMode>();
            bool rayTracingSettingsDisplayed = HDRenderPipeline.assetSupportsRayTracing
                && m_Tracing.overrideState.boolValue
                && tracingMode != RayCastingMode.RayMarching;

            // The rest of the ray tracing UI is only displayed if the asset supports ray tracing and the checkbox is checked.
            if (rayTracingSettingsDisplayed)
            {
                RayTracedReflectionGUI(tracingMode);
            }
            else
            {
                PropertyField(m_UsedAlgorithm, k_Algo);

                // Shared Data
                PropertyField(m_MinSmoothness, k_MinimumSmoothnessText);
                PropertyField(m_SmoothnessFadeStart, k_SmoothnessFadeStartText);
                PropertyField(m_ReflectSky, k_ReflectSkyText);
                m_SmoothnessFadeStart.value.floatValue = Mathf.Max(m_MinSmoothness.value.floatValue, m_SmoothnessFadeStart.value.floatValue);

                PropertyField(m_ScreenFadeDistance, k_ScreenFaceDistanceText);
                PropertyField(m_DepthBufferThickness, k_DepthBufferThicknessText);

                m_DepthBufferThickness.value.floatValue = Mathf.Clamp(m_DepthBufferThickness.value.floatValue, 0.001f, 1.0f);

                base.OnInspectorGUI();

                using (new IndentLevelScope())
                using (new QualityScope(this))
                {
                    PropertyField(m_RayMaxIterations, k_RayMaxIterationsText);
                    m_RayMaxIterations.value.intValue = Mathf.Max(0, m_RayMaxIterations.value.intValue);
                }
                if (m_UsedAlgorithm.value.intValue == (int)ScreenSpaceReflectionAlgorithm.PBRAccumulation)
                    PropertyField(m_AccumulationFactor, k_AccumulationFactorText);
            }
        }

        public override QualitySettingsBlob SaveCustomQualitySettingsAsObject(QualitySettingsBlob settings = null)
        {
            if (settings == null)
                settings = new QualitySettingsBlob();

            // RTR
            settings.Save<float>(m_MinSmoothness);
            settings.Save<float>(m_SmoothnessFadeStart);
            settings.Save<float>(m_RayLength);
            settings.Save<float>(m_ClampValue);
            settings.Save<bool>(m_FullResolution);
            settings.Save<bool>(m_RayMaxIterationsRT);
            settings.Save<bool>(m_Denoise);
            settings.Save<int>(m_DenoiserRadius);
            settings.Save<bool>(m_AffectsSmoothSurfaces);
            // SSR
            settings.Save<int>(m_RayMaxIterations);

            return settings;
        }

        public override void LoadSettingsFromObject(QualitySettingsBlob settings)
        {
            // RTR
            if (HDRenderPipeline.assetSupportsRayTracing && m_Tracing.overrideState.boolValue &&
                m_Tracing.value.GetEnumValue<RayCastingMode>() != RayCastingMode.RayMarching)
            {
                settings.TryLoad<float>(ref m_MinSmoothness);
                settings.TryLoad<float>(ref m_SmoothnessFadeStart);
                settings.TryLoad<float>(ref m_RayLength);
                settings.TryLoad<float>(ref m_ClampValue);
                settings.TryLoad<bool>(ref m_FullResolution);
                settings.TryLoad<bool>(ref m_RayMaxIterationsRT);
                settings.TryLoad<bool>(ref m_Denoise);
                settings.TryLoad<int>(ref m_DenoiserRadius);
                settings.TryLoad<bool>(ref m_AffectsSmoothSurfaces);
            }
            // SSR
            else
                settings.TryLoad<int>(ref m_RayMaxIterations);
        }

        public override void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level)
        {
            // RTR
            if (HDRenderPipeline.assetSupportsRayTracing && m_Tracing.overrideState.boolValue &&
                m_Tracing.value.GetEnumValue<RayCastingMode>() != RayCastingMode.RayMarching)
            {
                CopySetting(ref m_MinSmoothness, settings.lightingQualitySettings.RTRMinSmoothness[level]);
                CopySetting(ref m_SmoothnessFadeStart, settings.lightingQualitySettings.RTRSmoothnessFadeStart[level]);
                CopySetting(ref m_RayLength, settings.lightingQualitySettings.RTRRayLength[level]);
                CopySetting(ref m_ClampValue, settings.lightingQualitySettings.RTRClampValue[level]);
                CopySetting(ref m_FullResolution, settings.lightingQualitySettings.RTRFullResolution[level]);
                CopySetting(ref m_RayMaxIterationsRT, settings.lightingQualitySettings.RTRRayMaxIterations[level]);
                CopySetting(ref m_Denoise, settings.lightingQualitySettings.RTRDenoise[level]);
                CopySetting(ref m_DenoiserRadius, settings.lightingQualitySettings.RTRDenoiserRadius[level]);
                CopySetting(ref m_AffectsSmoothSurfaces, settings.lightingQualitySettings.RTRSmoothDenoising[level]);
            }
            // SSR
            else
                CopySetting(ref m_RayMaxIterations, settings.lightingQualitySettings.SSRMaxRaySteps[level]);
        }

        public override bool QualityEnabled()
        {
            // Quality always used for SSR
            if (!HDRenderPipeline.assetSupportsRayTracing || m_Tracing.value.GetEnumValue<RayCastingMode>() == RayCastingMode.RayMarching)
                return true;

            // Handle the quality usage for RTR
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;

            // Define if the asset supports Performance or Both Mode (Quality && Performance)
            bool assetSupportsPerf = currentAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Performance;
            bool assetSupportsBoth = currentAsset.currentPlatformRenderPipelineSettings.supportedRayTracingMode == RenderPipelineSettings.SupportedRayTracingMode.Both;

            // Define if the volume is in Performance or Mixed Mode
            bool volumeIsInPerfOrMixed = (m_Tracing.value.GetEnumValue<RayCastingMode>() == RayCastingMode.RayTracing && m_Mode.value.GetEnumValue<RayTracingMode>() == RayTracingMode.Performance)
                || (m_Tracing.value.GetEnumValue<RayCastingMode>() == RayCastingMode.Mixed);

            return (assetSupportsBoth && volumeIsInPerfOrMixed) || (assetSupportsPerf && m_Tracing.value.GetEnumValue<RayCastingMode>() != RayCastingMode.RayMarching);
        }
    }
}
