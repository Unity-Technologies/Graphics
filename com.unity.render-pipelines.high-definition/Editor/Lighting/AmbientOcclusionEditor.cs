using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(AmbientOcclusion))]
    class AmbientOcclusionEditor : VolumeComponentWithQualityEditor
    {
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_StepCount;
        SerializedDataParameter m_Radius;
        SerializedDataParameter m_FullResolution;
        SerializedDataParameter m_MaximumRadiusInPixels;
        SerializedDataParameter m_DirectLightingStrength;

        // Temporal only parameters
        SerializedDataParameter m_TemporalAccumulation;
        SerializedDataParameter m_GhostingAdjustement;
        SerializedDataParameter m_BilateralUpsample;

        // Non-temporal only parameters
        SerializedDataParameter m_DirectionCount;
        SerializedDataParameter m_BlurSharpness;

        // Ray Tracing parameters
        SerializedDataParameter m_RayTracing;
        SerializedDataParameter m_LayerMask;
        SerializedDataParameter m_RayLength;
        SerializedDataParameter m_SampleCount;
        SerializedDataParameter m_Denoise;
        SerializedDataParameter m_DenoiserRadius;

        public override bool hasAdvancedMode => true;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<AmbientOcclusion>(serializedObject);

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

            m_RayTracing = Unpack(o.Find(x => x.rayTracing));
            m_LayerMask = Unpack(o.Find(x => x.layerMask));
            m_RayLength = Unpack(o.Find(x => x.rayLength));
            m_Denoise = Unpack(o.Find(x => x.denoise));
            m_SampleCount = Unpack(o.Find(x => x.sampleCount));
            m_DenoiserRadius = Unpack(o.Find(x => x.denoiserRadius));
        }

        public override void OnInspectorGUI()
        {
            if (!HDRenderPipeline.currentAsset?.currentPlatformRenderPipelineSettings.supportSSAO ?? false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Ambient Occlusion.", MessageType.Error, wide: true);
                return;
            }

            if (HDRenderPipeline.pipelineSupportsRayTracing)
                PropertyField(m_RayTracing, EditorGUIUtility.TrTextContent("Ray Tracing (Preview)", "Enable ray traced ambient occlusion."));

            // Shared attributes
            PropertyField(m_Intensity, EditorGUIUtility.TrTextContent("Intensity", "Controls the strength of the ambient occlusion effect. Increase this value to produce darker areas."));
            PropertyField(m_DirectLightingStrength, EditorGUIUtility.TrTextContent("Direct Lighting Strength", "Controls how much the ambient light affects occlusion."));

            // If ray tracing is supported and it is enabled on this volume, display the ray tracing options.
            if (HDRenderPipeline.pipelineSupportsRayTracing && m_RayTracing.overrideState.boolValue && m_RayTracing.value.boolValue)
            {
                PropertyField(m_LayerMask, EditorGUIUtility.TrTextContent("Layer Mask", "Layer mask used to include the objects for ambient occlusion."));
                base.OnInspectorGUI(); // Quality Setting

                using (new QualityScope(this))
                {
                    EditorGUI.indentLevel++;
                    PropertyField(m_RayLength, EditorGUIUtility.TrTextContent("Ray Length", "Controls the length of ambient occlusion rays."));
                    PropertyField(m_SampleCount, EditorGUIUtility.TrTextContent("Sample Count", "Number of samples for ray traced ambient occlusion."));
                    PropertyField(m_Denoise, EditorGUIUtility.TrTextContent("Denoise", "Enable denoising on the ray traced ambient occlusion."));
                    {
                        EditorGUI.indentLevel++;
                        PropertyField(m_DenoiserRadius, EditorGUIUtility.TrTextContent("Denoiser Radius", "Radius parameter for the denoising."));
                        EditorGUI.indentLevel--;
                    }
                    EditorGUI.indentLevel--;
                }
            }
            else
            {

                PropertyField(m_Radius, EditorGUIUtility.TrTextContent("Radius", "Sampling radius. Bigger the radius, wider AO will be achieved, risking to lose fine details and increasing cost of the effect due to increasing cache misses."));

                base.OnInspectorGUI(); // Quality Setting

                using (new QualityScope(this))
                {
                    PropertyField(m_MaximumRadiusInPixels, EditorGUIUtility.TrTextContent("Maximum Radius In Pixels", "This poses a maximum radius in pixels that we consider. It is very important to keep this as tight as possible to preserve good performance. Note that this is the value used for 1080p when *not* running the effect at full resolution, it will be scaled accordingly for other resolutions."));
                    PropertyField(m_FullResolution, EditorGUIUtility.TrTextContent("Full Resolution", "The effect runs at full resolution. This increases quality, but also decreases performance significantly."));
                    PropertyField(m_StepCount, EditorGUIUtility.TrTextContent("Step Count", "Number of steps to take along one signed direction during horizon search (this is the number of steps in positive and negative direction)."));

                    PropertyField(m_TemporalAccumulation, EditorGUIUtility.TrTextContent("Temporal Accumulation", "Whether the results are accumulated over time or not. This can get better results cheaper, but it can lead to temporal artifacts."));
                    EditorGUI.indentLevel++;
                    if (!m_TemporalAccumulation.value.boolValue)
                    {
                        PropertyField(m_DirectionCount, EditorGUIUtility.TrTextContent("Direction Count", "Number of directions searched for occlusion at each each pixel."));
                        if (m_DirectionCount.value.intValue > 3)
                        {
                            EditorGUILayout.HelpBox("Performance will be seriously impacted by high direction count.", MessageType.Warning, wide: true);
                        }
                        PropertyField(m_BlurSharpness, EditorGUIUtility.TrTextContent("Blur sharpness", "Modify the non-temporal blur to change how sharp features are preserved. Lower values blurrier/softer, higher values sharper but with risk of noise."));
                    }
                    else
                    {
                        PropertyField(m_GhostingAdjustement, EditorGUIUtility.TrTextContent("Ghosting reduction", "Moving this factor closer to 0 will increase the amount of accepted samples during temporal accumulation, increasing the ghosting, but reducing the temporal noise."));
                        if (isInAdvancedMode && !m_FullResolution.value.boolValue)
                        {
                            PropertyField(m_BilateralUpsample, EditorGUIUtility.TrTextContent("Bilateral Upsample", "This upsample method preserves sharp edges better, however can result in visible aliasing and it is slightly more expensive."));
                        }
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }

        public override QualitySettingsBlob SaveCustomQualitySettingsAsObject(QualitySettingsBlob settings = null)
        {
            if (settings == null)
                settings = new QualitySettingsBlob();

            // Ray tracing
            settings.Save<float>(m_RayLength);
            settings.Save<int>(m_SampleCount);
            settings.Save<bool>(m_Denoise);
            settings.Save<float>(m_DenoiserRadius);

            // Raster
            settings.Save<int>(m_MaximumRadiusInPixels);
            settings.Save<bool>(m_FullResolution);
            settings.Save<int>(m_StepCount);
            settings.Save<int>(m_DirectionCount);
            settings.Save<bool>(m_BilateralUpsample);

            return settings;
        }

        public override void LoadSettingsFromObject(QualitySettingsBlob settings)
        {
            // Ray tracing
            settings.TryLoad<float>(ref m_RayLength);
            settings.TryLoad<int>(ref m_SampleCount);
            settings.TryLoad<bool>(ref m_Denoise);
            settings.TryLoad<float>(ref m_DenoiserRadius);

            // Raster
            settings.TryLoad<int>(ref m_MaximumRadiusInPixels);
            settings.TryLoad<bool>(ref m_FullResolution);
            settings.TryLoad<int>(ref m_StepCount);
            settings.TryLoad<int>(ref m_DirectionCount);
            settings.TryLoad<bool>(ref m_BilateralUpsample);
        }

        public override void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level)
        {
            m_RayLength.value.floatValue = settings.lightingQualitySettings.RTAORayLength[level];
            m_SampleCount.value.intValue = settings.lightingQualitySettings.RTAOSampleCount[level];
            m_Denoise.value.boolValue = settings.lightingQualitySettings.RTAODenoise[level];
            m_DenoiserRadius.value.floatValue = settings.lightingQualitySettings.RTAODenoiserRadius[level];
            m_MaximumRadiusInPixels.value.intValue = settings.lightingQualitySettings.AOMaximumRadiusPixels[level];
            m_FullResolution.value.boolValue = settings.lightingQualitySettings.AOFullRes[level];
            m_StepCount.value.intValue = settings.lightingQualitySettings.AOStepCount[level];
            m_DirectionCount.value.intValue = settings.lightingQualitySettings.AODirectionCount[level];
            m_BilateralUpsample.value.boolValue = settings.lightingQualitySettings.AOBilateralUpsample[level];
            
            m_RayLength.overrideState.boolValue = true;
            m_SampleCount.overrideState.boolValue = true;
            m_Denoise.overrideState.boolValue = true;
            m_DenoiserRadius.overrideState.boolValue = true;
            m_MaximumRadiusInPixels.overrideState.boolValue = true;
            m_FullResolution.overrideState.boolValue = true;
            m_StepCount.overrideState.boolValue = true;
            m_DirectionCount.overrideState.boolValue = true;
            m_BilateralUpsample.overrideState.boolValue = true; 
        }
    }
}
