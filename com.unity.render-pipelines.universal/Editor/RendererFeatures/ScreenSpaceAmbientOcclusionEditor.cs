using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(ScreenSpaceAmbientOcclusion), false)]
    internal class ScreenSpaceAmbientOcclusionEditor : ScriptableRendererFeaturePropertyDrawer
    {
        #region Serialized Properties
        private SerializedProperty m_Downsample;
        private SerializedProperty m_AfterOpaque;
        private SerializedProperty m_Source;
        private SerializedProperty m_NormalQuality;
        private SerializedProperty m_Intensity;
        private SerializedProperty m_DirectLightingStrength;
        private SerializedProperty m_Radius;
        private SerializedProperty m_SampleCount;
        #endregion

        private bool isDeferredRenderingMode;

        // Structs
        private struct Styles
        {
            public static GUIContent Downsample = EditorGUIUtility.TrTextContent("Downsample", "With this option enabled, Unity downsamples the SSAO effect texture to improve performance. Each dimension of the texture is reduced by a factor of 2.");
            public static GUIContent AfterOpaque = EditorGUIUtility.TrTextContent("After Opaque", "With this option enabled, Unity calculates and apply SSAO after the opaque pass to improve performance on mobile platforms with tiled-based GPU architectures. This is not physically correct.");
            public static GUIContent Source = EditorGUIUtility.TrTextContent("Source", "The source of the normal vector values.\nDepth Normals: the feature uses the values generated in the Depth Normal prepass.\nDepth: the feature reconstructs the normal values using the depth buffer.\nIn the Deferred rendering path, the feature uses the G-buffer normals texture.");
            public static GUIContent NormalQuality = new GUIContent("Normal Quality", "The number of depth texture samples that Unity takes when computing the normals. Low:1 sample, Medium: 5 samples, High: 9 samples.");
            public static GUIContent Intensity = EditorGUIUtility.TrTextContent("Intensity", "The degree of darkness that Ambient Occlusion adds.");
            public static GUIContent DirectLightingStrength = EditorGUIUtility.TrTextContent("Direct Lighting Strength", "Controls how much the ambient occlusion affects direct lighting.");
            public static GUIContent Radius = EditorGUIUtility.TrTextContent("Radius", "The radius around a given point, where Unity calculates and applies the effect.");
            public static GUIContent SampleCount = EditorGUIUtility.TrTextContent("Sample Count", "The number of samples that Unity takes when calculating the obscurance value. Higher values have high performance impact.");
        }

        protected override void Init(SerializedProperty property)
        {
            SerializedProperty settings = property.FindPropertyRelative("m_Settings");
            m_Source = settings.FindPropertyRelative("Source");
            m_Downsample = settings.FindPropertyRelative("Downsample");
            m_AfterOpaque = settings.FindPropertyRelative("AfterOpaque");
            m_NormalQuality = settings.FindPropertyRelative("NormalSamples");
            m_Intensity = settings.FindPropertyRelative("Intensity");
            m_DirectLightingStrength = settings.FindPropertyRelative("DirectLightingStrength");
            m_Radius = settings.FindPropertyRelative("Radius");
            m_SampleCount = settings.FindPropertyRelative("SampleCount");
        }

        protected override void OnGUIRendererFeature(SerializedProperty property)
        {
            SerializedProperty rendererProp =
                property.serializedObject
                    .FindProperty(nameof(UniversalRenderPipelineAsset.m_RendererDataReferenceList))
                    .GetArrayElementAtIndex(ScriptableRendererDataEditor.CurrentIndex);
            var deferredProp = rendererProp.FindPropertyRelative("m_RenderingMode");
            isDeferredRenderingMode = deferredProp != null && deferredProp.intValue == (int)RenderingMode.Deferred;

            EditorGUILayout.PropertyField(m_Downsample, Styles.Downsample);
            EditorGUILayout.PropertyField(m_AfterOpaque, Styles.AfterOpaque);

            if (!isDeferredRenderingMode)
            {
                EditorGUILayout.PropertyField(m_Source, Styles.Source);

                // We only enable this field when depth source is selected
                if (m_Source.enumValueIndex == (int)ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_NormalQuality, Styles.NormalQuality);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.PropertyField(m_Intensity, Styles.Intensity);
            EditorGUILayout.PropertyField(m_Radius, Styles.Radius);
            EditorGUILayout.PropertyField(m_DirectLightingStrength, Styles.DirectLightingStrength);
            EditorGUILayout.PropertyField(m_SampleCount, Styles.SampleCount);

            m_Intensity.floatValue = Mathf.Clamp(m_Intensity.floatValue, 0f, m_Intensity.floatValue);
            m_Radius.floatValue = Mathf.Clamp(m_Radius.floatValue, 0f, m_Radius.floatValue);
        }
    }
}
