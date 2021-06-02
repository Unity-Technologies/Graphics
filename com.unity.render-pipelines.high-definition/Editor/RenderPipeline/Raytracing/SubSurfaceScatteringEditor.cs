using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(SubSurfaceScattering))]
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
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportRayTracing ?? false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Ray Tracing.", MessageType.Error, wide: true);
                return;
            }

            // If ray tracing is supported display the content of the volume component
            if (RenderPipelineManager.currentPipeline != null && (RenderPipelineManager.currentPipeline as HDRenderPipeline).rayTracingSupported)
            {
                PropertyField(m_RayTracing);
                if (m_RayTracing.overrideState.boolValue && m_RayTracing.value.boolValue)
                {
                    using (new HDEditorUtils.IndentScope())
                    {
                        PropertyField(m_SampleCount);
                    }
                }
            }
        }
    }
}
