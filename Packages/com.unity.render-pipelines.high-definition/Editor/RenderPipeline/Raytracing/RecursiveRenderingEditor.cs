using UnityEngine;
using UnityEditor.Rendering;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(RecursiveRendering))]
    class RecursiveRenderingEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Enable;
        SerializedDataParameter m_LayerMask;
        SerializedDataParameter m_MaxDepth;
        SerializedDataParameter m_RayLength;
        SerializedDataParameter m_MinSmoothness;
        SerializedDataParameter m_RayMiss;
        SerializedDataParameter m_LastBounce;
        SerializedDataParameter m_AmbientProbeDimmer;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<RecursiveRendering>(serializedObject);

            m_Enable = Unpack(o.Find(x => x.enable));
            m_LayerMask = Unpack(o.Find(x => x.layerMask));
            m_MaxDepth = Unpack(o.Find(x => x.maxDepth));
            m_RayLength = Unpack(o.Find(x => x.rayLength));
            m_MinSmoothness = Unpack(o.Find(x => x.minSmoothness));
            m_RayMiss = Unpack(o.Find(x => x.rayMiss));
            m_LastBounce = Unpack(o.Find(x => x.lastBounce));
            m_AmbientProbeDimmer = Unpack(o.Find(x => x.ambientProbeDimmer));
        }

        static public readonly GUIContent k_RayLengthText = EditorGUIUtility.TrTextContent("Max Ray Length", "This defines the maximal travel distance of rays in meters.");

        public override void OnInspectorGUI()
        {
            HDEditorUtils.EnsureFrameSetting(FrameSettingsField.RayTracing);

            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            bool notSupported = currentAsset != null && !currentAsset.currentPlatformRenderPipelineSettings.supportRayTracing;
            if (notSupported)
            {
                EditorGUILayout.Space();
                HDEditorUtils.QualitySettingsHelpBox(HDRenderPipelineUI.Styles.rayTracingUnsupportedMessage,
                    MessageType.Warning, HDRenderPipelineUI.ExpandableGroup.Rendering,
                    "m_RenderPipelineSettings.supportRayTracing");
            }

            if (!notSupported && RenderPipelineManager.currentPipeline is not HDRenderPipeline { rayTracingSupported: true })
                HDRenderPipelineUI.DisplayRayTracingSupportBox();

            using var disableScope = new EditorGUI.DisabledScope(notSupported);

            PropertyField(m_Enable, EditorGUIUtility.TrTextContent("State"));

            if (m_Enable.overrideState.boolValue && m_Enable.value.boolValue)
            {
                using (new IndentLevelScope())
                {
                    PropertyField(m_LayerMask);
                    PropertyField(m_MaxDepth);
                    PropertyField(m_RayLength, k_RayLengthText);
                    PropertyField(m_MinSmoothness);
                    using (new IndentLevelScope())
                    {
                        if (showAdditionalProperties)
                        {
                            EditorGUILayout.LabelField("Fallback", EditorStyles.miniLabel);
                            PropertyField(m_RayMiss);
                            PropertyField(m_LastBounce);
                            PropertyField(m_AmbientProbeDimmer);
                        }
                    }
                }
            }
        }
    }
}
