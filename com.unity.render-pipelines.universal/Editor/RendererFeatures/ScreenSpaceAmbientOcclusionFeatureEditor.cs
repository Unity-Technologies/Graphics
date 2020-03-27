using System.Linq;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Experimental.Rendering.Universal;
using System.Collections.Generic;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(ScreenSpaceAmbientOcclusionFeature.Settings), true)]
    internal class ScreenSpaceAmbientOcclusionFeatureEditor : PropertyDrawer
    {
        internal class Styles
        {
            public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            //public static GUIContent Shader = new GUIContent("Shader", "");
            public static GUIContent UseVolumes = new GUIContent("Use Volumes", "");
            //public static GUIContent DepthSource = new GUIContent("Depth Source", "");
            public static GUIContent NormalQuality = new GUIContent("Normal Quality", "");
            public static GUIContent DownScale = new GUIContent("Down Scale", "");
            public static GUIContent Blur = new GUIContent("Blur", "");
            public static GUIContent Intensity = new GUIContent("Intensity", "");
            public static GUIContent Radius = new GUIContent("Radius", "");
            public static GUIContent SampleCount = new GUIContent("Sample Count", "");
        }

        // Serialized Properties
        private SerializedProperty m_Shader;
        private SerializedProperty m_UseVolumes;
        //private SerializedProperty m_DepthSource;
        private SerializedProperty m_NormalQuality;
        private SerializedProperty m_DownScale;
        private SerializedProperty m_Blur;
        private SerializedProperty m_Intensity;
        private SerializedProperty m_Radius;
        private SerializedProperty m_SampleCount;

        private List<SerializedObject> m_properties = new List<SerializedObject>();

        private void Init(SerializedProperty property)
        {
            m_Shader = property.FindPropertyRelative("Shader");
            m_UseVolumes = property.FindPropertyRelative("UseVolumes");
            //m_DepthSource = property.FindPropertyRelative("DepthSource");
            m_NormalQuality = property.FindPropertyRelative("NormalQuality");
            m_DownScale = property.FindPropertyRelative("DownScale");
            m_Blur = property.FindPropertyRelative("Blur");
            m_Intensity = property.FindPropertyRelative("Intensity");
            m_Radius = property.FindPropertyRelative("Radius");
            m_SampleCount = property.FindPropertyRelative("SampleCount");
            m_properties.Add(property.serializedObject);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginProperty(rect, label, property);

            if (!m_properties.Contains(property.serializedObject))
            {
                Init(property);
            }

            //EditorGUI.PropertyField(rect, m_Shader, Styles.Shader);
            //rect.y += Styles.defaultLineSpace;

            rect.y += Styles.defaultLineSpace;
            EditorGUI.PropertyField(rect, m_UseVolumes, Styles.UseVolumes);
            rect.y += Styles.defaultLineSpace;

            if (m_UseVolumes.boolValue)
            {
                rect.height += Styles.defaultLineSpace * 5;
                EditorGUI.HelpBox(rect, "Settings will be taken from SSAO volumes. Make sure you have a Volume in your scene with a Screen Space Ambient Occlusion override.", MessageType.Info);
                rect.y += Styles.defaultLineSpace * 5;
            }
            else
            {
                //EditorGUI.PropertyField(rect, m_DepthSource, Styles.depthSource);
                //rect.y += Styles.defaultLineSpace;

                //if (m_DepthSource == DepthSource.Depth)
                {
                    EditorGUI.PropertyField(rect, m_NormalQuality, Styles.NormalQuality);
                    rect.y += Styles.defaultLineSpace;
                }

                EditorGUI.PropertyField(rect, m_DownScale, Styles.DownScale);
                rect.y += Styles.defaultLineSpace;

                EditorGUI.PropertyField(rect, m_Blur, Styles.Blur);
                rect.y += Styles.defaultLineSpace;

                EditorGUI.Slider(rect, m_Intensity, 0f, 10f, Styles.Intensity);
                rect.y += Styles.defaultLineSpace;

                EditorGUI.Slider(rect, m_Radius, 0f, 10f, Styles.Radius);
                rect.y += Styles.defaultLineSpace;

                EditorGUI.IntSlider(rect, m_SampleCount, 0, 32, Styles.SampleCount);
                rect.y += Styles.defaultLineSpace;
            }

            EditorGUI.EndProperty();
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = Styles.defaultLineSpace * 8f;

            if (m_properties.Contains(property.serializedObject))
            {
                height += m_UseVolumes.boolValue ? 0 : Styles.defaultLineSpace * 2f;
            }

            return height;
        }
    }
}
