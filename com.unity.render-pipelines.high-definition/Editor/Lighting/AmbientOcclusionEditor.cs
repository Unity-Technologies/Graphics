using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(AmbientOcclusion))]
    public class AmbientOcclusionEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_ThicknessModifier;
        SerializedDataParameter m_DirectLightingStrength;

        SerializedDataParameter m_EnableRaytracing;
        SerializedDataParameter m_RayLength;
        SerializedDataParameter m_EnableFilter;
        SerializedDataParameter m_NumSamples;
        SerializedDataParameter m_FilterRadius;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<AmbientOcclusion>(serializedObject);

            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_ThicknessModifier = Unpack(o.Find(x => x.thicknessModifier));
            m_DirectLightingStrength = Unpack(o.Find(x => x.directLightingStrength));

            m_EnableRaytracing = Unpack(o.Find(x => x.enableRaytracing));
            m_RayLength = Unpack(o.Find(x => x.rayLength));
            m_EnableFilter = Unpack(o.Find(x => x.enableFilter));
            m_NumSamples = Unpack(o.Find(x => x.numSamples));
            m_FilterRadius = Unpack(o.Find(x => x.filterRadius));
        }

        public override void OnInspectorGUI()
        {
            if (!(GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset)
                ?.currentPlatformRenderPipelineSettings.supportSSAO ?? false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Ambient Occlusion.", MessageType.Error, wide: true);
                return;
            }

            PropertyField(m_Intensity);
            PropertyField(m_ThicknessModifier);
            PropertyField(m_DirectLightingStrength);

#if ENABLE_RAYTRACING
            PropertyField(m_EnableRaytracing);
            if (m_EnableRaytracing.overrideState.boolValue && m_EnableRaytracing.value.boolValue)
            {
                EditorGUI.indentLevel++;
                PropertyField(m_RayLength);
                PropertyField(m_EnableFilter);
                PropertyField(m_NumSamples);
                PropertyField(m_FilterRadius);
                EditorGUI.indentLevel--;
            }
#endif
        }
    }
}
