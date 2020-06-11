using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(ScreenSpaceAmbientOcclusionSettings), true)]
    internal class ScreenSpaceAmbientOcclusionEditor : PropertyDrawer
    {
        #region Serialized Properties

        private SerializedProperty m_Downsample;
        //private SerializedProperty m_Source;
        private SerializedProperty m_NormalSamples;
        private SerializedProperty m_Intensity;
        private SerializedProperty m_DirectLightingStrength;
        private SerializedProperty m_Radius;
        private SerializedProperty m_SampleCount;
        private SerializedProperty m_BlurPasses;
        private List<SerializedObject> m_Properties = new List<SerializedObject>();

        #endregion

        // Structs
        internal struct Styles
        {
            public static GUIContent Downsample = EditorGUIUtility.TrTextContent("Downsample", "With this option enabled, Unity downsamples the SSAO effect texture to improve performance. Each dimension of the texture is reduced by a factor of 2.");
            //public static GUIContent Source = EditorGUIUtility.TrTextContent("Source", "This option determines whether the ambient occlusion reconstructs the normal from depth or is given it from a DepthNormal/Deferred Gbuffer texture.");
            public static GUIContent NormalQuality = new GUIContent("Normal Quality", "The options in this field define the number of depth texture samples that Unity takes when computing the normals. Low: 1 sample, Medium: 5 samples, High: 9 samples.");
            public static GUIContent Intensity = EditorGUIUtility.TrTextContent("Intensity", "The degree of darkness that Ambient Occlusion adds.");
            public static GUIContent DirectLightingStrength = EditorGUIUtility.TrTextContent("Direct Lighting Strength", "Controls how much the ambient occlusion affects direct lighting.");
            public static GUIContent Radius = EditorGUIUtility.TrTextContent("Radius", "The radius around a given point, where Unity calculates and applies the effect.");
            public static GUIContent SampleCount = EditorGUIUtility.TrTextContent("Sample Count", "The number of samples that Unity takes when calculating the obscurance value. Higher values have high performance impact.");
            public static GUIContent BlurPasses = EditorGUIUtility.TrTextContent("Blur Passes", "The number of render passes for blurring the SSAO effect texture.");
        }

        private void Init(SerializedProperty property)
        {
            m_Properties.Clear();
            //m_Source = property.FindPropertyRelative("Source");
            m_Downsample = property.FindPropertyRelative("Downsample");
            m_NormalSamples = property.FindPropertyRelative("NormalSamples");
            m_Intensity = property.FindPropertyRelative("Intensity");
            m_DirectLightingStrength = property.FindPropertyRelative("DirectLightingStrength");
            m_Radius = property.FindPropertyRelative("Radius");
            m_SampleCount = property.FindPropertyRelative("SampleCount");
            m_BlurPasses = property.FindPropertyRelative("BlurPasses");
            m_Properties.Add(property.serializedObject);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            if (!m_Properties.Contains(property.serializedObject))
            {
                Init(property);
            }

            EditorGUILayout.PropertyField(m_Downsample, Styles.Downsample);
            //EditorGUILayout.PropertyField(m_Source, Styles.Source);

            // We only enable this field when depth source is selected
            //GUI.enabled = m_Source.enumValueIndex == (int) ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth;
            EditorGUILayout.PropertyField(m_NormalSamples, Styles.NormalQuality);
            //GUI.enabled = true;

            m_Intensity.floatValue = EditorGUILayout.Slider(Styles.Intensity,m_Intensity.floatValue, 0f, 10f);
            m_DirectLightingStrength.floatValue = EditorGUILayout.Slider(Styles.DirectLightingStrength,m_DirectLightingStrength.floatValue, 0f, 1f);
            EditorGUILayout.PropertyField(m_Radius, Styles.Radius);
            m_Radius.floatValue = Mathf.Clamp(m_Radius.floatValue, 0f, m_Radius.floatValue);
            m_SampleCount.intValue = EditorGUILayout.IntSlider(Styles.SampleCount,m_SampleCount.intValue, 4, 20);
            m_BlurPasses.intValue = EditorGUILayout.IntSlider(Styles.BlurPasses,m_BlurPasses.intValue, 1, 12);
        }
    }
}
