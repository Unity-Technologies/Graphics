using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(RecursiveRendering))]
    public class RecursiveRenderingEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Enable;
        SerializedDataParameter m_MaxDepth;
        SerializedDataParameter m_RayLength;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<RecursiveRendering>(serializedObject);

            m_Enable = Unpack(o.Find(x => x.enable));
            m_MaxDepth = Unpack(o.Find(x => x.maxDepth));
            m_RayLength = Unpack(o.Find(x => x.rayLength));
        }

        public override void OnInspectorGUI()
        {
            if (!(GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset)
                ?.currentPlatformRenderPipelineSettings.supportRayTracing ?? false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Ray Tracing.", MessageType.Error, wide: true);
                return;
            }
#if ENABLE_RAYTRACING

            PropertyField(m_Enable);

            if (m_Enable.overrideState.boolValue && m_Enable.value.boolValue)
            {
                EditorGUI.indentLevel++;
                PropertyField(m_MaxDepth);
                PropertyField(m_RayLength);
                EditorGUI.indentLevel--;
            }
#endif
        }
    }
}
