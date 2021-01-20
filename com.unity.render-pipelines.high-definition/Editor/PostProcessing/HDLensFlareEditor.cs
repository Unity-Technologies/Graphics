using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(HDLensFlare))]
    sealed class HDLensFlareEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Enable;
        SerializedDataParameter m_Type;
        SerializedDataParameter m_Threshold;
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_ElementsCount;
        SerializedDataParameter m_ElementIntensities;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<HDLensFlare>(serializedObject);

            m_Enable = Unpack(o.Find(x => x.enable));
            m_Type = Unpack(o.Find(x => x.type));
            m_Threshold = Unpack(o.Find(x => x.threshold));
            m_Intensity = Unpack(o.Find(x => x.intensity));
            //m_ElementsCount = Unpack(o.Find(x => x.elementsCount));
            //m_ElementIntensities = Unpack(o.Find(x => x.elementsIntensity));
        }

        public override void OnInspectorGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(EditorGUIUtility.TrTextContent("Lens Flare Editor", "")))
                {
                    HDLensFlareEditorWindow.OpenWindow();
                }
            }
            PropertyField(m_Enable, new GUIContent("Enable"));
            PropertyField(m_Type, new GUIContent("Type"));
            PropertyField(m_Threshold, new GUIContent("Threshold"));
            PropertyField(m_Intensity, new GUIContent("Intensity"));
            //PropertyField(m_ElementsCount, new GUIContent("Elements Count"));
        }
    }
}
