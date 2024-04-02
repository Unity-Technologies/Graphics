using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(ScreenSpaceAmbientOcclusion))]
    internal class ScreenSpaceAmbientOcclusionEditor : Editor
    {
        #region Serialized Properties
        private SerializedProperty m_AOMethod;
        private SerializedProperty m_Downsample;
        private SerializedProperty m_AfterOpaque;
        private SerializedProperty m_Source;
        private SerializedProperty m_NormalQuality;
        private SerializedProperty m_Intensity;
        private SerializedProperty m_DirectLightingStrength;
        private SerializedProperty m_Radius;
        private SerializedProperty m_Falloff;
        private SerializedProperty m_Samples;
        private SerializedProperty m_BlurQuality;
        #endregion

        private bool m_IsInitialized = false;
        private HeaderBool m_ShowQualitySettings;

        class HeaderBool
        {
            private string key;
            public bool value;

            internal HeaderBool(string _key, bool _default = false)
            {
                key = _key;
                if (EditorPrefs.HasKey(key))
                    value = EditorPrefs.GetBool(key);
                else
                    value = _default;
                EditorPrefs.SetBool(key, value);
            }

            internal void SetValue(bool newValue)
            {
                value = newValue;
                EditorPrefs.SetBool(key, value);
            }
        }


        // Structs
        private struct Styles
        {
            public static GUIContent AOMethod = EditorGUIUtility.TrTextContent("Method", "The noise method to use when calculating the Ambient Occlusion value.");
            public static GUIContent Intensity = EditorGUIUtility.TrTextContent("Intensity", "The degree of darkness that Ambient Occlusion adds.");
            public static GUIContent Radius = EditorGUIUtility.TrTextContent("Radius", "The radius around a given point, where Unity calculates and applies the effect.");
            public static GUIContent Falloff = EditorGUIUtility.TrTextContent("Falloff Distance", "The distance from the camera where Ambient Occlusion should be visible.");
            public static GUIContent DirectLightingStrength = EditorGUIUtility.TrTextContent("Direct Lighting Strength", "Controls how much the ambient occlusion affects direct lighting.");

            public static GUIContent Quality = EditorGUIUtility.TrTextContent("Quality", "");
            public static GUIContent Source = EditorGUIUtility.TrTextContent("Source", "The source of the normal vector values.\nDepth Normals: the feature uses the values generated in the Depth Normal prepass.\nDepth: the feature reconstructs the normal values using the depth buffer.\nIn the Deferred rendering path, the feature uses the G-buffer normals texture.");
            public static GUIContent NormalQuality = new GUIContent("Normal Quality", "The number of depth texture samples that Unity takes when computing the normals. Low:1 sample, Medium: 5 samples, High: 9 samples.");
            public static GUIContent Downsample = EditorGUIUtility.TrTextContent("Downsample", "With this option enabled, Unity downsamples the SSAO effect texture to improve performance. Each dimension of the texture is reduced by a factor of 2.");
            public static GUIContent AfterOpaque = EditorGUIUtility.TrTextContent("After Opaque", "With this option enabled, Unity calculates and apply SSAO after the opaque pass to improve performance on mobile platforms with tiled-based GPU architectures. This is not physically correct.");
            public static GUIContent BlurQuality = EditorGUIUtility.TrTextContent("Blur Quality", "High: Bilateral, Medium: Gaussian. Low: Kawase (Single Pass).");
            public static GUIContent Samples = EditorGUIUtility.TrTextContent("Samples", "The number of samples that Unity takes when calculating the obscurance value. Low:4 samples, Medium: 8 samples, High: 12 samples.");
        }

        private void Init()
        {
            m_ShowQualitySettings = new HeaderBool($"SSAO.QualityFoldout", false);

            SerializedProperty settings = serializedObject.FindProperty("m_Settings");

            m_AOMethod = settings.FindPropertyRelative("AOMethod");
            m_Intensity = settings.FindPropertyRelative("Intensity");
            m_Radius = settings.FindPropertyRelative("Radius");
            m_Falloff = settings.FindPropertyRelative("Falloff");
            m_DirectLightingStrength = settings.FindPropertyRelative("DirectLightingStrength");

            m_Source = settings.FindPropertyRelative("Source");
            m_NormalQuality = settings.FindPropertyRelative("NormalSamples");
            m_Downsample = settings.FindPropertyRelative("Downsample");
            m_AfterOpaque = settings.FindPropertyRelative("AfterOpaque");
            m_BlurQuality = settings.FindPropertyRelative("BlurQuality");
            m_Samples = settings.FindPropertyRelative("Samples");

            m_IsInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            if (!m_IsInitialized)
                Init();

            EditorGUILayout.PropertyField(m_AOMethod, Styles.AOMethod);
            EditorGUILayout.PropertyField(m_Intensity, Styles.Intensity);
            EditorGUILayout.PropertyField(m_Radius, Styles.Radius);
            EditorGUILayout.PropertyField(m_Falloff, Styles.Falloff);
            m_DirectLightingStrength.floatValue = EditorGUILayout.Slider(Styles.DirectLightingStrength, m_DirectLightingStrength.floatValue, 0f, 1f);

            // Make sure these fields are never below 0.0...
            m_Intensity.floatValue = Mathf.Max(m_Intensity.floatValue, 0f);
            m_Radius.floatValue = Mathf.Max(m_Radius.floatValue, 0f);
            m_Falloff.floatValue = Mathf.Max(m_Falloff.floatValue, 0f);

            m_ShowQualitySettings.SetValue(EditorGUILayout.Foldout(m_ShowQualitySettings.value, Styles.Quality));
            if (m_ShowQualitySettings.value)
            {
                bool isDeferredRenderingMode = RendererIsDeferred();

                EditorGUI.indentLevel++;

                // Selecting source is not available for Deferred Rendering...
                GUI.enabled = !isDeferredRenderingMode;
                EditorGUILayout.PropertyField(m_Source, Styles.Source);

                // We only enable this field when depth source is selected...
                GUI.enabled = !isDeferredRenderingMode && m_Source.enumValueIndex == (int)ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth;
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_NormalQuality, Styles.NormalQuality);
                EditorGUI.indentLevel--;
                GUI.enabled = true;

                EditorGUILayout.PropertyField(m_Downsample, Styles.Downsample);
                EditorGUILayout.PropertyField(m_AfterOpaque, Styles.AfterOpaque);
                EditorGUILayout.PropertyField(m_BlurQuality, Styles.BlurQuality);
                EditorGUILayout.PropertyField(m_Samples, Styles.Samples);

                EditorGUI.indentLevel--;
            }
        }

        private bool RendererIsDeferred()
        {
            ScreenSpaceAmbientOcclusion ssaoFeature = (ScreenSpaceAmbientOcclusion) target;
            UniversalRenderPipelineAsset pipelineAsset = (UniversalRenderPipelineAsset) GraphicsSettings.currentRenderPipeline;

            if (ssaoFeature == null || pipelineAsset == null)
                return false;

            // We have to find the renderer related to the SSAO feature, then test if it is in deferred mode.
            var rendererDataList = pipelineAsset.m_RendererDataList;
            for (int rendererIndex = 0; rendererIndex < rendererDataList.Length; ++rendererIndex)
            {
                var rendererData = rendererDataList[rendererIndex] as UniversalRendererData;
                if (rendererData == null || rendererData.renderingMode != RenderingMode.Deferred)
                    continue;

                var rendererFeatures = rendererData.rendererFeatures;
                foreach (var feature in rendererFeatures)
                    if (feature is ScreenSpaceAmbientOcclusion occlusion && occlusion == ssaoFeature)
                        return true;
            }

            return false;
        }
    }
}
