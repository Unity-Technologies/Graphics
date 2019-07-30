using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(ScreenSpaceReflection))]
    class HDScreenSpaceReflectionEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_ScreenFadeDistance;
        SerializedDataParameter m_RayMaxIterations;
        SerializedDataParameter m_DepthBufferThickness;
        SerializedDataParameter m_MinSmoothness;
        SerializedDataParameter m_SmoothnessFadeStart;
        SerializedDataParameter m_ReflectSky;

        SerializedDataParameter m_EnableRaytracing;
        SerializedDataParameter m_RayLength;
        SerializedDataParameter m_ClampValue;
        SerializedDataParameter m_SpatialFilterRadius;
        SerializedDataParameter m_FullResolution;
        SerializedDataParameter m_EnableFilter;
        SerializedDataParameter m_FilterRadius;
        SerializedDataParameter m_DeferredMode;
        SerializedDataParameter m_RayBinning;

        // Tier 2
        SerializedDataParameter m_NumSamples;
        SerializedDataParameter m_NumBounces;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScreenSpaceReflection>(serializedObject);
            m_DepthBufferThickness = Unpack(o.Find(x => x.depthBufferThickness));
            m_RayMaxIterations = Unpack(o.Find(x => x.rayMaxIterations));
            m_ScreenFadeDistance = Unpack(o.Find(x => x.screenFadeDistance));
            m_MinSmoothness = Unpack(o.Find(x => x.minSmoothness));
            m_SmoothnessFadeStart = Unpack(o.Find(x => x.smoothnessFadeStart));
            m_ReflectSky          = Unpack(o.Find(x => x.reflectSky));

            m_EnableRaytracing              = Unpack(o.Find(x => x.enableRaytracing));
            m_RayLength                     = Unpack(o.Find(x => x.rayLength));
            m_ClampValue                    = Unpack(o.Find(x => x.clampValue));
            m_SpatialFilterRadius           = Unpack(o.Find(x => x.spatialFilterRadius));
            m_FullResolution                = Unpack(o.Find(x => x.fullResolution));
            m_EnableFilter                  = Unpack(o.Find(x => x.enableFilter));
            m_FilterRadius                  = Unpack(o.Find(x => x.filterRadius));
            m_DeferredMode                  = Unpack(o.Find(x => x.deferredMode));
            m_RayBinning                    = Unpack(o.Find(x => x.rayBinning));

            // Tier 2
            m_NumBounces                    = Unpack(o.Find(x => x.numBounces));
            m_NumSamples                    = Unpack(o.Find(x => x.numSamples));
        }

        public override void OnInspectorGUI()
        {
            HDRenderPipelineAsset currentAsset = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset);
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportSSR ?? false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Screen Space Reflection.", MessageType.Error, wide: true);
                return;
            }

            PropertyField(m_ScreenFadeDistance,   EditorGUIUtility.TrTextContent("Screen Edge Fade Distance", "Controls the distance at which HDRP fades out SSR near the edge of the screen."));
            PropertyField(m_RayMaxIterations,     EditorGUIUtility.TrTextContent("Max Number of Ray Steps", "Sets the maximum number of steps HDRP uses for raytracing. Affects both correctness and performance."));
            PropertyField(m_DepthBufferThickness, EditorGUIUtility.TrTextContent("Object Thickness", "Controls the typical thickness of objects the reflection rays may pass behind."));
            PropertyField(m_MinSmoothness,        EditorGUIUtility.TrTextContent("Min Smoothness", "Controls the smoothness value at which HDRP activates SSR and the smoothness-controlled fade out stops."));
            PropertyField(m_SmoothnessFadeStart,  EditorGUIUtility.TrTextContent("Smoothness Fade Start", "Controls the smoothness value at which the smoothness-controlled fade out starts. The fade is in the range [Min Smoothness, Smoothness Fade Start]."));
            PropertyField(m_ReflectSky,           EditorGUIUtility.TrTextContent("Reflect sky", "When enabled, SSR handles sky reflection."));


            m_RayMaxIterations.value.intValue       = Mathf.Max(0, m_RayMaxIterations.value.intValue);
            m_DepthBufferThickness.value.floatValue = Mathf.Clamp(m_DepthBufferThickness.value.floatValue, 0.001f, 1.0f);
            m_SmoothnessFadeStart.value.floatValue  = Mathf.Max(m_MinSmoothness.value.floatValue, m_SmoothnessFadeStart.value.floatValue);

#if ENABLE_RAYTRACING
            PropertyField(m_EnableRaytracing);
            if ( m_EnableRaytracing.overrideState.boolValue && m_EnableRaytracing.value.boolValue)
            {
                EditorGUI.indentLevel++;
                PropertyField(m_RayLength);
                PropertyField(m_ClampValue);
                RenderPipelineSettings.RaytracingTier currentTier = currentAsset.currentPlatformRenderPipelineSettings.supportedRaytracingTier;
                switch (currentTier)
                {
                    case RenderPipelineSettings.RaytracingTier.Tier1:
                    {
                        PropertyField(m_SpatialFilterRadius);
                        PropertyField(m_FullResolution);
                        PropertyField(m_EnableFilter);
                        PropertyField(m_FilterRadius);
                        PropertyField(m_DeferredMode);
                        PropertyField(m_RayBinning);
                    }
                    break;
                    case RenderPipelineSettings.RaytracingTier.Tier2:
                    {
                        PropertyField(m_NumSamples);
                        PropertyField(m_NumBounces);
                        PropertyField(m_EnableFilter);
                        PropertyField(m_FilterRadius);
                    }
                    break;
                }
                EditorGUI.indentLevel--;
            }
#endif
        }
    }
}
