using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class AtmosphericScatteringEditor
    {
        private class Styles
        {
            public readonly GUIContent fog = new GUIContent("Fog Parameters");
            public readonly GUIContent type = new GUIContent("Type", "Type of fog.");
            public readonly GUIContent colorMode = new GUIContent("Color Mode");
            public readonly GUIContent color = new GUIContent("Color", "Constant Fog Color");
            public readonly GUIContent mipFogNear = new GUIContent("Mip Fog Near", "Distance at which minimum mip of blurred sky texture is used as fog color.");
            public readonly GUIContent mipFogFar = new GUIContent("Mip Fog Far", "Distance at which maximum mip of blurred sky texture is used as fog color.");
            public readonly GUIContent mipFogMaxMip = new GUIContent("Mip Fog Max Mip", "Maximum mip map used for mip fog (0 being lowest and 1 heighest mip).");
            public readonly GUIContent linearFogDensity = new GUIContent("Fog Density");
            public readonly GUIContent linearFogStart = new GUIContent("Fog Start Distance");
            public readonly GUIContent linearFogEnd = new GUIContent("Fog End Distance");
            public readonly GUIContent expFogDensity = new GUIContent("Fog Density");
            public readonly GUIContent expFogDistance = new GUIContent("Fog Distance");
        }

        private static Styles s_Styles = null;
        private static Styles styles { get { if (s_Styles == null) s_Styles = new Styles(); return s_Styles; } }

        private SerializedProperty m_Type;

        private SerializedProperty m_ColorMode;
        private SerializedProperty m_Color;
        private SerializedProperty m_MipFogNear;
        private SerializedProperty m_MipFogFar;
        private SerializedProperty m_MipFogMaxMip;

        private SerializedProperty m_LinearFogDensity;
        private SerializedProperty m_LinearFogStart;
        private SerializedProperty m_LinearFogEnd;

        private SerializedProperty m_ExpFogDistance;
        private SerializedProperty m_ExpFogDensity;

        public void OnEnable(SerializedProperty atmScatterProperty)
        {
            m_Type = atmScatterProperty.FindPropertyRelative("type");
            // Fog Color
            m_ColorMode = atmScatterProperty.FindPropertyRelative("colorMode");
            m_Color = atmScatterProperty.FindPropertyRelative("fogColor");
            m_MipFogNear = atmScatterProperty.FindPropertyRelative("mipFogNear");
            m_MipFogFar = atmScatterProperty.FindPropertyRelative("mipFogFar");
            m_MipFogMaxMip = atmScatterProperty.FindPropertyRelative("mipFogMaxMip");
            // Linear Fog
            m_LinearFogDensity = atmScatterProperty.FindPropertyRelative("linearFogDensity");
            m_LinearFogStart = atmScatterProperty.FindPropertyRelative("linearFogStart");
            m_LinearFogEnd = atmScatterProperty.FindPropertyRelative("linearFogEnd");
            // Exp fog
            m_ExpFogDistance = atmScatterProperty.FindPropertyRelative("expFogDistance");
            m_ExpFogDensity = atmScatterProperty.FindPropertyRelative("expFogDensity");
        }

        public void OnGUI()
        {
            EditorGUILayout.LabelField(styles.fog, EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(m_Type, styles.type);
            if(!m_Type.hasMultipleDifferentValues)
            {
                if((AtmosphericScatteringSettings.FogType)m_Type.intValue != AtmosphericScatteringSettings.FogType.None)
                {
                    EditorGUILayout.PropertyField(m_ColorMode, styles.colorMode);
                    if(!m_ColorMode.hasMultipleDifferentValues && (AtmosphericScatteringSettings.FogColorMode)m_ColorMode.intValue == AtmosphericScatteringSettings.FogColorMode.ConstantColor)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(m_Color, styles.color);
                        EditorGUI.indentLevel--;
                    }
                    else
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(m_MipFogNear, styles.mipFogNear);
                        EditorGUILayout.PropertyField(m_MipFogFar, styles.mipFogFar);
                        EditorGUILayout.PropertyField(m_MipFogMaxMip, styles.mipFogMaxMip);
                        EditorGUI.indentLevel--;
                    }

                    if ((AtmosphericScatteringSettings.FogType)m_Type.intValue == AtmosphericScatteringSettings.FogType.Linear)
                    {
                        EditorGUILayout.PropertyField(m_LinearFogDensity, styles.linearFogDensity);
                        EditorGUILayout.PropertyField(m_LinearFogStart, styles.linearFogStart);
                        EditorGUILayout.PropertyField(m_LinearFogEnd, styles.linearFogEnd);
                    }
                    else if((AtmosphericScatteringSettings.FogType)m_Type.intValue == AtmosphericScatteringSettings.FogType.Exponential)
                    {
                        EditorGUILayout.PropertyField(m_ExpFogDensity, styles.expFogDensity);
                        EditorGUILayout.PropertyField(m_ExpFogDistance, styles.expFogDistance);
                    }
                }
            }
        }
    }
}