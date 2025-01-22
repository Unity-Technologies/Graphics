using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ScreenSpaceAmbientOcclusion))]
    class ScreenSpaceAmbientOcclusionEditor : VolumeComponentWithQualityEditor
    {
        private const int k_PerformanceImpactLimit = 3;

        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_StepCount;
        SerializedDataParameter m_Radius;
        SerializedDataParameter m_FullResolution;
        SerializedDataParameter m_MaximumRadiusInPixels;
        SerializedDataParameter m_DirectLightingStrength;
        SerializedDataParameter m_SpatialBilateralAggressiveness;

        // Temporal only parameters
        SerializedDataParameter m_TemporalAccumulation;
        SerializedDataParameter m_GhostingAdjustement;
        SerializedDataParameter m_BilateralUpsample;

        // Non-temporal only parameters
        SerializedDataParameter m_DirectionCount;
        SerializedDataParameter m_BlurSharpness;

        // Ray Tracing parameters
        SerializedDataParameter m_RayTracing;
        SerializedDataParameter m_SpecularOcclusion;
        SerializedDataParameter m_LayerMask;

        SerializedDataParameter m_OccluderMotionRejection;
        SerializedDataParameter m_ReceiverMotionRejection;
        SerializedDataParameter m_RayLength;
        SerializedDataParameter m_SampleCount;
        SerializedDataParameter m_Denoise;
        SerializedDataParameter m_DenoiserRadius;

        public override bool hasAdditionalProperties => true;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScreenSpaceAmbientOcclusion>(serializedObject);

            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_StepCount = Unpack(o.Find("m_StepCount"));
            m_Radius = Unpack(o.Find(x => x.radius));
            m_FullResolution = Unpack(o.Find("m_FullResolution"));
            m_MaximumRadiusInPixels = Unpack(o.Find("m_MaximumRadiusInPixels"));

            m_TemporalAccumulation = Unpack(o.Find(x => x.temporalAccumulation));
            m_DirectionCount = Unpack(o.Find("m_DirectionCount"));
            m_BlurSharpness = Unpack(o.Find(x => x.blurSharpness));
            m_DirectLightingStrength = Unpack(o.Find(x => x.directLightingStrength));
            m_GhostingAdjustement = Unpack(o.Find(x => x.ghostingReduction));
            m_BilateralUpsample = Unpack(o.Find("m_BilateralUpsample"));
            m_SpatialBilateralAggressiveness = Unpack(o.Find(x => x.spatialBilateralAggressiveness));

            m_RayTracing = Unpack(o.Find(x => x.rayTracing));
            m_SpecularOcclusion = Unpack(o.Find(x => x.specularOcclusion));
            m_LayerMask = Unpack(o.Find(x => x.layerMask));
            m_OccluderMotionRejection = Unpack(o.Find(x => x.occluderMotionRejection));
            m_ReceiverMotionRejection = Unpack(o.Find(x => x.receiverMotionRejection));
            m_RayLength = Unpack(o.Find(x => x.rayLength));
            m_Denoise = Unpack(o.Find(x => x.denoise));
            m_SampleCount = Unpack(o.Find(x => x.sampleCount));
            m_DenoiserRadius = Unpack(o.Find(x => x.denoiserRadius));

            base.OnEnable();
        }

        private static class Styles
        {
            public static readonly string currentAssetDoesNotSupportSSAO = "The current HDRP Asset does not support Screen Space Ambient Occlusion.";
            public static readonly string performanceImpacted = "Performance will be seriously impacted by high direction count.";
            public static readonly GUIContent rayTracing = EditorGUIUtility.TrTextContent("Ray Tracing", "Enable ray traced ambient occlusion.");
            public static readonly GUIContent intesity = EditorGUIUtility.TrTextContent("Intensity", "Controls the strength of the ambient occlusion effect. Increase this value to produce darker areas.");
            public static readonly GUIContent directLightingStrenght = EditorGUIUtility.TrTextContent("Direct Lighting Strength", "Controls how much the ambient light affects occlusion.");
            public static readonly GUIContent layerMask = EditorGUIUtility.TrTextContent("Layer Mask", "Layer mask used to include the objects for ambient occlusion.");
            static public readonly GUIContent specularOcclusion = EditorGUIUtility.TrTextContent("Specular Occlusion", "Controls the influence of the ambient occlusion on the specular occlusion. Increase this value to reduce the amount of specular leaking in occluded areas.");
            public static readonly GUIContent maxRayLength = EditorGUIUtility.TrTextContent("Max Ray Length", "Controls the maximal length of ambient occlusion rays. The higher this value is, the more expensive ray traced ambient occlusion is.");
            public static readonly GUIContent sampleCount = EditorGUIUtility.TrTextContent("Sample Count", "Number of samples for ray traced ambient occlusion.");
            public static readonly GUIContent denoise = EditorGUIUtility.TrTextContent("Denoise", "Enable denoising on the ray traced ambient occlusion.");
            public static readonly GUIContent denoiseRadius = EditorGUIUtility.TrTextContent("Denoiser Radius", "Radius parameter for the denoising.");
            public static readonly GUIContent occulderMotionRejection = EditorGUIUtility.TrTextContent("Occluder Motion Rejection", "When enabled, ambient occlusion generated by moving objects will not be accumulated, generating less ghosting but introducing additional noise.");
            public static readonly GUIContent receiverMotionRejection = EditorGUIUtility.TrTextContent("Receiver Motion Rejection", "When enabled, ambient occlusion generated on moving objects will not be accumulated, generating less ghosting but introducing additional noise.");
            public static readonly GUIContent radius = EditorGUIUtility.TrTextContent("Radius", "Sampling radius. Bigger the radius, wider AO will be achieved, risking to lose fine details and increasing cost of the effect due to increasing cache misses.");
            public static readonly GUIContent maxRadiusInPixels = EditorGUIUtility.TrTextContent("Maximum Radius In Pixels", "This poses a maximum radius in pixels that we consider. It is very important to keep this as tight as possible to preserve good performance. Note that this is the value used for 1080p when *not* running the effect at full resolution, it will be scaled accordingly for other resolutions.");
            public static readonly GUIContent fullResolution = EditorGUIUtility.TrTextContent("Full Resolution", "The effect runs at full resolution. This increases quality, but also decreases performance significantly.");
            public static readonly GUIContent stepCount = EditorGUIUtility.TrTextContent("Step Count", "Number of steps to take along one signed direction during horizon search (this is the number of steps in positive and negative direction).");
            public static readonly GUIContent tempAccum = EditorGUIUtility.TrTextContent("Temporal Accumulation", "Whether the results are accumulated over time or not. This can get better results cheaper, but it can lead to temporal artifacts. Requires Motion Vectors to be enabled.");
            public static readonly GUIContent directionCount = EditorGUIUtility.TrTextContent("Direction Count", "Number of directions searched for occlusion at each each pixel.");
            public static readonly GUIContent blurSharpness = EditorGUIUtility.TrTextContent("Blur Sharpness", "Modify the non-temporal blur to change how sharp features are preserved. Lower values blurrier/softer, higher values sharper but with risk of noise.");
            public static readonly GUIContent bilateralAggressiveness = EditorGUIUtility.TrTextContent("Bilateral Aggressiveness", "Higher this value, the less lenient with depth differences the spatial filter is. Increase if for example noticing white halos where AO should be.");
            public static readonly GUIContent ghostingReduction = EditorGUIUtility.TrTextContent("Ghosting Reduction", "Moving this factor closer to 0 will increase the amount of accepted samples during temporal accumulation, increasing the ghosting, but reducing the temporal noise.");
            public static readonly GUIContent bilateralUpsample = EditorGUIUtility.TrTextContent("Bilateral Upsample", "This upsample method preserves sharp edges better, however can result in visible aliasing and it is slightly more expensive.");
        }

        public override void OnInspectorGUI()
        {
            HDEditorUtils.EnsureFrameSetting(FrameSettingsField.SSAO);
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            bool notSupported = currentAsset != null && !currentAsset.currentPlatformRenderPipelineSettings.supportSSAO;
            if (notSupported)
            {
                EditorGUILayout.Space();
                HDEditorUtils.QualitySettingsHelpBox(Styles.currentAssetDoesNotSupportSSAO, MessageType.Warning,
                    HDRenderPipelineUI.ExpandableGroup.Lighting, "m_RenderPipelineSettings.supportSSAO");
            }
            using var disableScope = new EditorGUI.DisabledScope(notSupported);

            if (HDRenderPipeline.assetSupportsRayTracing)
            {
                PropertyField(m_RayTracing, Styles.rayTracing);

                if (m_RayTracing.overrideState.boolValue && m_RayTracing.value.boolValue)
                {
                    HDEditorUtils.EnsureFrameSetting(FrameSettingsField.RayTracing);
                    // If ray tracing is supported display the content of the volume component
                    if (RenderPipelineManager.currentPipeline is not HDRenderPipeline { rayTracingSupported: true })
                        HDRenderPipelineUI.DisplayRayTracingSupportBox();
                }
            }

            // Shared attributes
            PropertyField(m_Intensity, Styles.intesity);
            PropertyField(m_DirectLightingStrength, Styles.directLightingStrenght);

            // If ray tracing is supported and it is enabled on this volume, display the ray tracing options.
            if (HDRenderPipeline.assetSupportsRayTracing && m_RayTracing.overrideState.boolValue && m_RayTracing.value.boolValue)
            {
                PropertyField(m_SpecularOcclusion, Styles.specularOcclusion);
                PropertyField(m_LayerMask, Styles.layerMask);

                base.OnInspectorGUI(); // Quality Setting
                using (new QualityScope(this))
                {
                    using (new IndentLevelScope())
                    {
                        PropertyField(m_RayLength, Styles.maxRayLength);
                        PropertyField(m_SampleCount, Styles.sampleCount);
                        PropertyField(m_Denoise, Styles.denoise);
                        using (new IndentLevelScope())
                        {
                            PropertyField(m_DenoiserRadius, Styles.denoiseRadius);
                        }
                    }
                }

                PropertyField(m_OccluderMotionRejection, Styles.occulderMotionRejection);
                PropertyField(m_ReceiverMotionRejection, Styles.receiverMotionRejection);
            }
            else
            {
                PropertyField(m_Radius, Styles.radius);

                base.OnInspectorGUI(); // Quality Setting

                using (new QualityScope(this))
                {
                    using (new IndentLevelScope())
                    {
                        PropertyField(m_MaximumRadiusInPixels, Styles.maxRadiusInPixels);
                        PropertyField(m_FullResolution, Styles.fullResolution);
                        PropertyField(m_StepCount, Styles.stepCount);
                    }

                    PropertyField(m_TemporalAccumulation, Styles.tempAccum);

                    using (new IndentLevelScope())
                    {
                        if (!m_TemporalAccumulation.value.boolValue)
                        {
                            PropertyField(m_DirectionCount, Styles.directionCount);

                            if (m_DirectionCount.overrideState.boolValue && m_DirectionCount.value.intValue > k_PerformanceImpactLimit)
                            {
                                CoreEditorUtils.DrawFixMeBox(Styles.performanceImpacted, () => m_DirectionCount.value.intValue = k_PerformanceImpactLimit);
                            }

                            PropertyField(m_BlurSharpness, Styles.blurSharpness);
                        }
                        else
                        {
                            PropertyField(m_SpatialBilateralAggressiveness, Styles.bilateralAggressiveness);
                            PropertyField(m_GhostingAdjustement, Styles.ghostingReduction);
                            if (!m_FullResolution.value.boolValue)
                            {
                                PropertyField(m_BilateralUpsample, Styles.bilateralUpsample);
                            }
                        }
                    }
                }
            }
        }

        public override QualitySettingsBlob SaveCustomQualitySettingsAsObject(QualitySettingsBlob settings = null)
        {
            if (settings == null)
                settings = new QualitySettingsBlob();

            // RTAO
            settings.Save<float>(m_RayLength);
            settings.Save<int>(m_SampleCount);
            settings.Save<bool>(m_Denoise);
            settings.Save<float>(m_DenoiserRadius);

            // SSAO
            settings.Save<int>(m_MaximumRadiusInPixels);
            settings.Save<bool>(m_FullResolution);
            settings.Save<int>(m_StepCount);
            settings.Save<int>(m_DirectionCount);
            settings.Save<bool>(m_BilateralUpsample);

            return settings;
        }

        public override void LoadSettingsFromObject(QualitySettingsBlob settings)
        {
            // RTAO
            settings.TryLoad<float>(ref m_RayLength);
            settings.TryLoad<int>(ref m_SampleCount);
            settings.TryLoad<bool>(ref m_Denoise);
            settings.TryLoad<float>(ref m_DenoiserRadius);

            // SSAO
            settings.TryLoad<int>(ref m_MaximumRadiusInPixels);
            settings.TryLoad<bool>(ref m_FullResolution);
            settings.TryLoad<int>(ref m_StepCount);
            settings.TryLoad<int>(ref m_DirectionCount);
            settings.TryLoad<bool>(ref m_BilateralUpsample);
        }

        public override void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level)
        {
            // RTAO
            CopySetting(ref m_RayLength, settings.lightingQualitySettings.RTAORayLength[level]);
            CopySetting(ref m_SampleCount, settings.lightingQualitySettings.RTAOSampleCount[level]);
            CopySetting(ref m_Denoise, settings.lightingQualitySettings.RTAODenoise[level]);
            CopySetting(ref m_DenoiserRadius, settings.lightingQualitySettings.RTAODenoiserRadius[level]);

            // SSAO
            CopySetting(ref m_MaximumRadiusInPixels, settings.lightingQualitySettings.AOMaximumRadiusPixels[level]);
            CopySetting(ref m_FullResolution, settings.lightingQualitySettings.AOFullRes[level]);
            CopySetting(ref m_StepCount, settings.lightingQualitySettings.AOStepCount[level]);
            CopySetting(ref m_DirectionCount, settings.lightingQualitySettings.AODirectionCount[level]);
            CopySetting(ref m_BilateralUpsample, settings.lightingQualitySettings.AOBilateralUpsample[level]);
        }
    }
}
