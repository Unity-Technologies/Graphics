using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(DecalSettings), true)]
    internal class DecalRendererFeatureEditor : PropertyDrawer
    {
        private readonly static float s_DefaultLineSpace = EditorGUIUtility.singleLineHeight;
        private SerializedProperty m_Technique;
        private SerializedProperty m_MaxDrawDistance;
        private SerializedProperty m_DBufferSettings;
        private SerializedProperty m_ScreenSpaceSettings;

        private void Init(SerializedProperty property)
        {
            m_Technique = property.FindPropertyRelative("technique");
            m_MaxDrawDistance = property.FindPropertyRelative("maxDrawDistance");
            m_DBufferSettings = property.FindPropertyRelative("dBufferSettings");
            m_ScreenSpaceSettings = property.FindPropertyRelative("screenSpaceSettings");
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            Init(property);

            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginProperty(rect, label, property);

            EditorGUI.PropertyField(rect, m_Technique);
            rect.y += s_DefaultLineSpace;

            DecalTechniqueOption technique = (DecalTechniqueOption)m_Technique.intValue;

            if (technique == DecalTechniqueOption.DBuffer)
            {
                EditorGUI.indentLevel++;
                foreach (SerializedProperty prop in m_DBufferSettings)
                {
                    EditorGUI.PropertyField(rect, prop);
                    rect.y += s_DefaultLineSpace;
                }
                EditorGUI.indentLevel--;
            }

            if (technique == DecalTechniqueOption.ScreenSpace)
            {
                EditorGUI.indentLevel++;
                foreach (SerializedProperty prop in m_ScreenSpaceSettings)
                {
                    EditorGUI.PropertyField(rect, prop);
                    rect.y += s_DefaultLineSpace;
                }
                EditorGUI.indentLevel--;
            }

            EditorGUI.PropertyField(rect, m_MaxDrawDistance);
            rect.y += s_DefaultLineSpace;

            EditorGUI.EndProperty();
            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Init(property);

            float height = 0;
            height += s_DefaultLineSpace;
            height += s_DefaultLineSpace;

            DecalTechniqueOption technique = (DecalTechniqueOption)m_Technique.intValue;

            if (technique == DecalTechniqueOption.DBuffer)
            {
                foreach (SerializedProperty prop in m_DBufferSettings)
                {
                    height += s_DefaultLineSpace;
                }
            }
            if (technique == DecalTechniqueOption.ScreenSpace)
            {
                foreach (SerializedProperty prop in m_ScreenSpaceSettings)
                {
                    height += s_DefaultLineSpace;
                }
            }

            return height;
        }
    }
}
