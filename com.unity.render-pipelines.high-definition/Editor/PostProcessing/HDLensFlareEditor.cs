using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(HDLensFlare))]
    sealed class HDLensFlareEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Enable;
        SerializedDataParameter m_Threshold;
        SerializedDataParameter m_Intensity;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<HDLensFlare>(serializedObject);

            m_Enable = Unpack(o.Find(x => x.enable));
            m_Threshold = Unpack(o.Find(x => x.threshold));
            m_Intensity = Unpack(o.Find(x => x.intensity));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Enable, new GUIContent("Enable"));
            PropertyField(m_Threshold, new GUIContent("Threshold"));
            PropertyField(m_Intensity, new GUIContent("Intensity"));
            if (GUILayout.Button(EditorGUIUtility.TrTextContent("Lens Flare Editor", "")))
            {
                
            }
        }
    }
}

