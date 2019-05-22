using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(GlobalIllumination))]
    public class GlobalIlluminatorEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_EnableRayTracing;
        SerializedDataParameter m_RayLength;
        SerializedDataParameter m_ClampValue;
        SerializedDataParameter m_NumSamples;
        SerializedDataParameter m_EnableFilter;
        SerializedDataParameter m_FilterRadius;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<GlobalIllumination>(serializedObject);

            m_EnableRayTracing = Unpack(o.Find(x => x.enableRayTracing));
            m_RayLength = Unpack(o.Find(x => x.rayLength));
            m_ClampValue = Unpack(o.Find(x => x.clampValue));
            m_NumSamples = Unpack(o.Find(x => x.numSamples));
            m_EnableFilter = Unpack(o.Find(x => x.enableFilter));
            m_FilterRadius = Unpack(o.Find(x => x.filterRadius));
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

            PropertyField(m_EnableRayTracing);

            if (m_EnableRayTracing.overrideState.boolValue && m_EnableRayTracing.value.boolValue)
            {
                EditorGUI.indentLevel++;
                PropertyField(m_RayLength);
                PropertyField(m_ClampValue);
                PropertyField(m_NumSamples);
                PropertyField(m_EnableFilter);
                PropertyField(m_FilterRadius);
                EditorGUI.indentLevel--;
            }
#endif
        }
    }
}
