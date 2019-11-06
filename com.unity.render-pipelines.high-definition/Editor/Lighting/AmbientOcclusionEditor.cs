using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(AmbientOcclusion))]
    class AmbientOcclusionEditor : VolumeComponentEditor
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
            var o = new PropertyFetcher<AmbientOcclusion>(serializedObject);

            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_StepCount = Unpack(o.Find(x => x.stepCount));
            m_Radius = Unpack(o.Find(x => x.radius));
            m_FullResolution = Unpack(o.Find(x => x.fullResolution));
            m_MaximumRadiusInPixels = Unpack(o.Find(x => x.maximumRadiusInPixels));

            m_TemporalAccumulation = Unpack(o.Find(x => x.temporalAccumulation));
            m_DirectionCount = Unpack(o.Find(x => x.directionCount));
            m_BlurSharpness = Unpack(o.Find(x => x.blurSharpness));
            m_DirectLightingStrength = Unpack(o.Find(x => x.directLightingStrength));
            m_GhostingAdjustement = Unpack(o.Find(x => x.ghostingReduction));
            m_BilateralUpsample = Unpack(o.Find(x => x.bilateralUpsample));

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

            // If the current pipeline supports ray tracing, display first the ray tracing checkbox
            bool raytracingSupported = (RenderPipelineManager.currentPipeline as HDRenderPipeline).rayTracingSupported;
            if (raytracingSupported)
                PropertyField(m_RayTracing, EditorGUIUtility.TrTextContent("Ray Tracing", "Enable ray traced ambient occlusion."));

            // Shared attributes
            PropertyField(m_Intensity, EditorGUIUtility.TrTextContent("Intensity", "Controls the strength of the ambient occlusion effect. Increase this value to produce darker areas."));
            PropertyField(m_DirectLightingStrength, EditorGUIUtility.TrTextContent("Direct Lighting Strength", "Controls how much the ambient light affects occlusion."));

            // If ray tracing is supported and it is enabled on this volume, display the ray tracing options.
            if (raytracingSupported && m_RayTracing.overrideState.boolValue && m_RayTracing.value.boolValue)
            {
                PropertyField(m_LayerMask, EditorGUIUtility.TrTextContent("Layer Mask", "Layer mask used to include the objects for ambient occlusion."));
                PropertyField(m_RayLength, EditorGUIUtility.TrTextContent("Ray Length", "Controls the length of ambient occlusion rays."));
                PropertyField(m_SampleCount, EditorGUIUtility.TrTextContent("Sample Count", "Number of samples for ray traced ambient occlusion."));
                PropertyField(m_Denoise, EditorGUIUtility.TrTextContent("Denoise", "Enable denoising on the ray traced ambient occlusion."));
                {
                    EditorGUI.indentLevel++;
                    PropertyField(m_DenoiserRadius, EditorGUIUtility.TrTextContent("Denoiser Radius", "Radius parameter for the denoising."));
                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                PropertyField(m_Radius, EditorGUIUtility.TrTextContent("Radius", "Sampling radius. Bigger the radius, wider AO will be achieved, risking to lose fine details and increasing cost of the effect due to increasing cache misses."));
                PropertyField(m_MaximumRadiusInPixels, EditorGUIUtility.TrTextContent("Maximum Radius In Pixels", "This poses a maximum radius in pixels that we consider. It is very important to keep this as tight as possible to preserve good performance. Note that this is the value used for 1080p when *not* running the effect at full resolution, it will be scaled accordingly for other resolutions."));
                PropertyField(m_FullResolution, EditorGUIUtility.TrTextContent("Full Resolution", "The effect runs at full resolution. This increases quality, but also decreases performance significantly."));
                PropertyField(m_StepCount, EditorGUIUtility.TrTextContent("Step Count", "Number of steps to take along one signed direction during horizon search (this is the number of steps in positive and negative direction)."));


                
                PropertyField(m_TemporalAccumulation, EditorGUIUtility.TrTextContent("Temporal Accumulation", "Whether the results are accumulated over time or not. This can get better results cheaper, but it can lead to temporal artifacts."));
                if(!m_TemporalAccumulation.value.boolValue)
                {
                    PropertyField(m_DirectionCount, EditorGUIUtility.TrTextContent("Direction Count", "Number of directions searched for occlusion at each each pixel."));
                    if(m_DirectionCount.value.intValue > 3)
                    {
                        EditorGUILayout.HelpBox("Performance will be seriously impacted by high direction count.", MessageType.Warning, wide: true);
                    }
                    PropertyField(m_BlurSharpness, EditorGUIUtility.TrTextContent("Blur sharpness", "Modify the non-temporal blur to change how sharp features are preserved. Lower values blurrier/softer, higher values sharper but with risk of noise."));
                }
                else
                {
                    PropertyField(m_GhostingAdjustement, EditorGUIUtility.TrTextContent("Ghosting reduction", "Moving this factor closer to 0 will increase the amount of accepted samples during temporal accumulation, increasing the ghosting, but reducing the temporal noise."));
                    if(isInAdvancedMode && !m_FullResolution.value.boolValue)
                        PropertyField(m_BilateralUpsample, EditorGUIUtility.TrTextContent("Bilateral Upsample", "This upsample method preserves sharp edges better, however can result in visible aliasing and it is slightly more expensive."));
                }

            }
        }
    }
}
