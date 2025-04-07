using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(SubSurfaceScattering))]
    class SubSurfaceScatteringEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_RayTracing;
        SerializedDataParameter m_SampleCount;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SubSurfaceScattering>(serializedObject);
            m_RayTracing = Unpack(o.Find(x => x.rayTracing));
            m_SampleCount = Unpack(o.Find(x => x.sampleCount));
        }

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

            using var disableScope = new EditorGUI.DisabledScope(notSupported);

            PropertyField(m_RayTracing);
            if (m_RayTracing.overrideState.boolValue && m_RayTracing.value.boolValue)
            {
                using (new IndentLevelScope())
                {
                    // If ray tracing is supported display the content of the volume component
                    if (RenderPipelineManager.currentPipeline is not HDRenderPipeline { rayTracingSupported: true })
                        HDRenderPipelineUI.DisplayRayTracingSupportBox();

                    PropertyField(m_SampleCount);
                }
            }
        }
    }
}
