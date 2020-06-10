using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(ScreenSpaceAmbientOcclusion))]
    internal class ScreenSpaceAmbientOcclusionEditor : Editor
    {
        // Serialized Properties
        private SerializedProperty m_Downsample;
        private SerializedProperty m_Source;
        private SerializedProperty m_NormalSamples;
        private SerializedProperty m_Intensity;
        private SerializedProperty m_DirectLightingStrength;
        private SerializedProperty m_Radius;
        private SerializedProperty m_SampleCount;
        private SerializedProperty m_BlurPasses;

        private bool m_IsInitialized = false;

        // Structs
        private struct Styles
        {
            public static GUIContent Downsample = EditorGUIUtility.TrTextContent("Downsample", "With this option enabled, Unity downsamples the SSAO effect texture to improve performance. Each dimension of the texture is reduced by a factor of 2.");
            public static GUIContent Source = EditorGUIUtility.TrTextContent("Source", "This option determines whether the ambient occlusion reconstructs the normal from depth or taken from _CameraNormalsTexture.");
            public static GUIContent NormalSamples = EditorGUIUtility.TrTextContent("Normal Samples", "The options in this field define the number of depth texture samples that Unity takes when computing the normals from the Depth texture.");
            public static GUIContent Intensity = EditorGUIUtility.TrTextContent("Intensity", "The degree of darkness that Ambient Occlusion adds.");
            public static GUIContent DirectLightingStrength = EditorGUIUtility.TrTextContent("Direct Lighting Strength", "Controls how much the ambient occlusion affects direct lighting.");
            public static GUIContent Radius = EditorGUIUtility.TrTextContent("Radius", "The radius around a given point, where Unity calculates and applies the effect.");
            public static GUIContent SampleCount = EditorGUIUtility.TrTextContent("Sample Count", "The number of samples that Unity takes when calculating the obscurance value. Higher values have high performance impact.");
            public static GUIContent BlurPasses = EditorGUIUtility.TrTextContent("Blur Passes", "The number of render passes for blurring the SSAO effect texture.");
        }

        private void Init()
        {
            SerializedProperty settings = serializedObject.FindProperty("settings");
            m_Source = settings.FindPropertyRelative("Source");
            m_Downsample = settings.FindPropertyRelative("Downsample");
            m_NormalSamples = settings.FindPropertyRelative("NormalSamples");
            m_Intensity = settings.FindPropertyRelative("Intensity");
            m_DirectLightingStrength = settings.FindPropertyRelative("DirectLightingStrength");
            m_Radius = settings.FindPropertyRelative("Radius");
            m_SampleCount = settings.FindPropertyRelative("SampleCount");
            m_BlurPasses = settings.FindPropertyRelative("BlurPasses");
            m_IsInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            if (!m_IsInitialized)
            {
                Init();
            }

            EditorGUILayout.PropertyField(m_Downsample, Styles.Downsample);
            EditorGUILayout.PropertyField(m_Source, Styles.Source);

            // We only enable this field when depth source is selected
            GUI.enabled = m_Source.enumValueIndex == (int) ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth;
            EditorGUILayout.PropertyField(m_NormalSamples, Styles.NormalSamples);
            GUI.enabled = true;

            m_Intensity.floatValue = EditorGUILayout.Slider(Styles.Intensity,m_Intensity.floatValue, 0f, 10f);
            m_DirectLightingStrength.floatValue = EditorGUILayout.Slider(Styles.DirectLightingStrength,m_DirectLightingStrength.floatValue, 0f, 1f);
            EditorGUILayout.PropertyField(m_Radius, Styles.Radius);
            m_SampleCount.intValue = EditorGUILayout.IntSlider(Styles.SampleCount,m_SampleCount.intValue, 4, 20);
            m_BlurPasses.intValue = EditorGUILayout.IntSlider(Styles.BlurPasses,m_BlurPasses.intValue, 0, 12);
        }
    }
}
