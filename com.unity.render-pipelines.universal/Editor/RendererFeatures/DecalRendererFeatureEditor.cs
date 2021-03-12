using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(DecalSettings), true)]
    internal class DecalRendererFeatureEditor : PropertyDrawer
    {
        private readonly static float defaultLineSpace = EditorGUIUtility.singleLineHeight;
        private SerializedProperty m_Technique;
        private SerializedProperty m_MaxDrawDistance;
        private SerializedProperty m_DBufferSettings;
        private SerializedProperty m_ScreenSpaceSettings;

        private void Init(SerializedProperty property)
        {
            //if (m_Technique != null)
            //    return;

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
            rect.y += defaultLineSpace;

            DecalTechnique technique = (DecalTechnique)m_Technique.intValue;

            //rect.y += defaultLineSpace;

            if (technique == DecalTechnique.DBuffer)
            {
                EditorGUI.indentLevel++;
                foreach (SerializedProperty prop in m_DBufferSettings)
                {
                    EditorGUI.PropertyField(rect, prop);
                    rect.y += defaultLineSpace;
                }
                EditorGUI.indentLevel--;

                //rect.y += m_DBufferSettings.CountInProperty() * defaultLineSpace;
            }

            if (technique == DecalTechnique.ScreenSpace)
            {
                EditorGUI.indentLevel++;
                foreach (SerializedProperty prop in m_ScreenSpaceSettings)
                {
                    EditorGUI.PropertyField(rect, prop);
                    rect.y += defaultLineSpace;
                }
                EditorGUI.indentLevel--;

                //EditorGUI.PropertyField(rect, m_ScreenSpaceSettings, true);
                //rect.y += m_DBufferSettings.CountInProperty() * defaultLineSpace;
            }

            EditorGUI.PropertyField(rect, m_MaxDrawDistance);
            rect.y += defaultLineSpace;

            EditorGUI.EndProperty();
            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Init(property);

            float height = 0;
            height += defaultLineSpace;
            height += defaultLineSpace;

            //height += defaultLineSpace;

            DecalTechnique technique = (DecalTechnique)m_Technique.intValue;

            if (technique == DecalTechnique.DBuffer)
                foreach (SerializedProperty prop in m_DBufferSettings)
                {
                    height += defaultLineSpace;
                }
            if (technique == DecalTechnique.ScreenSpace)
                foreach (SerializedProperty prop in m_ScreenSpaceSettings)
                {
                    height += defaultLineSpace;
                }

            return height;
        }
    }
}
