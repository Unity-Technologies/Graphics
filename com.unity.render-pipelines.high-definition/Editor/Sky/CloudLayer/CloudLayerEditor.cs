using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(CloudLayer))]
    class CloudLayerEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Enabled;

        SerializedDataParameter m_CloudMap;
        SerializedDataParameter m_UpperHemisphereOnly;
        SerializedDataParameter m_Tint;
        SerializedDataParameter m_IntensityMultiplier;
        SerializedDataParameter m_Rotation;

        SerializedDataParameter m_EnableDistortion;
        SerializedDataParameter m_Procedural;
        SerializedDataParameter m_Flowmap;
        SerializedDataParameter m_ScrollDirection;
        SerializedDataParameter m_ScrollSpeed;

        GUIContent[]    m_DistortionModes = { new GUIContent("Procedural"), new GUIContent("Flowmap") };
        int[]           m_DistortionModeValues = { 1, 0 };

        public override void OnEnable()
        {
            var o = new PropertyFetcher<CloudLayer>(serializedObject);

            m_Enabled                   = Unpack(o.Find(x => x.enabled));

            m_CloudMap                  = Unpack(o.Find(x => x.cloudMap));
            m_UpperHemisphereOnly       = Unpack(o.Find(x => x.upperHemisphereOnly));
            m_Tint                      = Unpack(o.Find(x => x.tint));
            m_IntensityMultiplier       = Unpack(o.Find(x => x.intensityMultiplier));
            m_Rotation                  = Unpack(o.Find(x => x.rotation));

            m_EnableDistortion          = Unpack(o.Find(x => x.enableDistortion));
            m_Procedural                = Unpack(o.Find(x => x.procedural));
            m_Flowmap                   = Unpack(o.Find(x => x.flowmap));
            m_ScrollDirection           = Unpack(o.Find(x => x.scrollDirection));
            m_ScrollSpeed               = Unpack(o.Find(x => x.scrollSpeed));
        }

        bool IsMapFormatInvalid(SerializedDataParameter map)
        {
            if (!map.overrideState.boolValue || map.value.objectReferenceValue == null)
                return false;
            var tex = map.value.objectReferenceValue;
            if (!tex.GetType().IsSubclassOf(typeof(Texture)))
                return true;
            return (tex as Texture).dimension != TextureDimension.Tex2D;
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Enabled, new GUIContent("Enable"));

            PropertyField(m_CloudMap);
            if (IsMapFormatInvalid(m_CloudMap))
                EditorGUILayout.HelpBox("The cloud map needs to be a 2D Texture in LatLong layout.", MessageType.Info);

            PropertyField(m_UpperHemisphereOnly);
            PropertyField(m_Tint);
            PropertyField(m_IntensityMultiplier);
            PropertyField(m_Rotation);

            PropertyField(m_EnableDistortion);
            if (m_EnableDistortion.value.boolValue)
            {
                EditorGUI.indentLevel++;

                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawOverrideCheckbox(m_Procedural);
                    using (new EditorGUI.DisabledScope(!m_Procedural.overrideState.boolValue))
                        m_Procedural.value.boolValue = EditorGUILayout.IntPopup(new GUIContent("Distortion Mode"), (int)m_Procedural.value.intValue, m_DistortionModes, m_DistortionModeValues) == 1;
                }

                if (!m_Procedural.value.boolValue)
                {
                    EditorGUI.indentLevel++;
                    PropertyField(m_Flowmap);
                    if (IsMapFormatInvalid(m_Flowmap))
                        EditorGUILayout.HelpBox("The flowmap needs to be a 2D Texture in LatLong layout.", MessageType.Info);
                    EditorGUI.indentLevel--;
                }

                PropertyField(m_ScrollDirection);
                PropertyField(m_ScrollSpeed);
                EditorGUI.indentLevel--;
            }
        }
    }
}
