using UnityEditor;
using UnityEditor.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(IndirectLightingController))]
    public class IndirectLightingControllerEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_IndirectDiffuseIntensity;
        SerializedDataParameter m_IndirectSpecularIntensity;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<IndirectLightingController>(serializedObject);

            m_IndirectSpecularIntensity = Unpack(o.Find(x => x.indirectSpecularIntensity));
            m_IndirectDiffuseIntensity = Unpack(o.Find(x => x.indirectDiffuseIntensity));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_IndirectDiffuseIntensity, EditorGUIUtility.TrTextContent("Indirect Diffuse Intensity", "Multiplier for the baked diffuse lighting."));
            PropertyField(m_IndirectSpecularIntensity, EditorGUIUtility.TrTextContent("Indirect Specular Intensity", "Multiplier for the reflected specular light."));
        }
    }
}
