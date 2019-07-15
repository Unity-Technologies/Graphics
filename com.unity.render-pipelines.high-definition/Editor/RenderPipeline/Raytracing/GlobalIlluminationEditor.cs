using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(GlobalIllumination))]
    class GlobalIlluminatorEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_EnableRayTracing;
        SerializedDataParameter m_RayLength;
        SerializedDataParameter m_ClampValue;

        // Tier 1
        SerializedDataParameter m_DeferredMode;

        // Tier 2
        SerializedDataParameter m_NumSamples;
        SerializedDataParameter m_NumBounces;

        SerializedDataParameter m_EnableFilter;
        SerializedDataParameter m_FilterRadius;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<GlobalIllumination>(serializedObject);

            m_EnableRayTracing = Unpack(o.Find(x => x.enableRayTracing));
            m_RayLength = Unpack(o.Find(x => x.rayLength));
            m_ClampValue = Unpack(o.Find(x => x.clampValue));

            // Tier 1
            m_DeferredMode = Unpack(o.Find(x => x.deferredMode));

            // Tier 2
            m_NumSamples = Unpack(o.Find(x => x.numSamples));
            m_NumBounces = Unpack(o.Find(x => x.numBounces));

            m_EnableFilter = Unpack(o.Find(x => x.enableFilter));
            m_FilterRadius = Unpack(o.Find(x => x.filterRadius));
        }

        public override void OnInspectorGUI()
        {
            HDRenderPipelineAsset currentAsset = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset);
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportRayTracing ?? false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Ray Tracing.", MessageType.Error, wide: true);
                return;
            }
#if ENABLE_RAYTRACING

            PropertyField(m_EnableRayTracing);

            if (m_EnableRayTracing.overrideState.boolValue && m_EnableRayTracing.value.boolValue)
            {
                EditorGUI.indentLevel++;
                PropertyField(m_RayLength);
                PropertyField(m_ClampValue);

                RenderPipelineSettings.RaytracingTier currentTier = currentAsset.currentPlatformRenderPipelineSettings.supportedRaytracingTier;
                switch (currentTier)
                {
                    case RenderPipelineSettings.RaytracingTier.Tier1:
                    {
                        PropertyField(m_DeferredMode);
                    }
                    break;
                    case RenderPipelineSettings.RaytracingTier.Tier2:
                    {
                        PropertyField(m_NumSamples);
                        PropertyField(m_NumBounces);
                    }
                    break;
                }

                PropertyField(m_EnableFilter);
                PropertyField(m_FilterRadius);
                EditorGUI.indentLevel--;
            }
#endif
        }
    }
}
