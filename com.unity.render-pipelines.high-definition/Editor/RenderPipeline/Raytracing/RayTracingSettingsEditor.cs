using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(RayTracingSettings))]
    class RayTracingSettingsEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_RayBias;
        SerializedDataParameter m_ExtendCulling;
        SerializedDataParameter m_ShadowCullingRange;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<RayTracingSettings>(serializedObject);

            m_RayBias = Unpack(o.Find(x => x.rayBias));
            m_ExtendCulling = Unpack(o.Find(x => x.extendCulling));
            m_ShadowCullingRange = Unpack(o.Find(x => x.shadowCullingRange));
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

            PropertyField(m_RayBias);
            PropertyField(m_ExtendCulling);
            if (m_ExtendCulling.overrideState.boolValue && m_ExtendCulling.value.boolValue)
            {
                EditorGUI.indentLevel++;
                PropertyField(m_ShadowCullingRange);
                EditorGUI.indentLevel--;
            }
        }
    }
}
