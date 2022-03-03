using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(ScreenSpaceAmbientOcclusion))]
    internal class ScreenSpaceAmbientOcclusionEditor : Editor
    {
        #region Serialized Properties
        private SerializedProperty m_BlueNoiseTexture;
        private SerializedProperty m_AOAlgorithm;
        private SerializedProperty m_Downsample;
        private SerializedProperty m_AfterOpaque;
        private SerializedProperty m_Source;
        private SerializedProperty m_NormalQuality;
        private SerializedProperty m_Intensity;
        private SerializedProperty m_DirectLightingStrength;
        private SerializedProperty m_Radius;
        private SerializedProperty m_Falloff;
        private SerializedProperty m_Samples;
        private SerializedProperty m_SinglePassBlur;
        private SerializedProperty m_FinalUpsample;
        private SerializedProperty m_BlurType;
        private SerializedProperty m_OnlyAO;

        #endregion

        private bool m_IsInitialized = false;

        // Structs
        private struct Styles
        {
            public static GUIContent AOAlgorithm = EditorGUIUtility.TrTextContent("AO Algorithm", "");
            public static GUIContent BlueNoiseTexture = EditorGUIUtility.TrTextContent("Blue Noise Texture", "");
            public static GUIContent Downsample = EditorGUIUtility.TrTextContent("Downsample", "With this option enabled, Unity downsamples the SSAO effect texture to improve performance. Each dimension of the texture is reduced by a factor of 2.");
            public static GUIContent AfterOpaque = EditorGUIUtility.TrTextContent("After Opaque", "With this option enabled, Unity calculates and apply SSAO after the opaque pass to improve performance on mobile platforms with tiled-based GPU architectures. This is not physically correct.");
            public static GUIContent Source = EditorGUIUtility.TrTextContent("Source", "The source of the normal vector values.\nDepth Normals: the feature uses the values generated in the Depth Normal prepass.\nDepth: the feature reconstructs the normal values using the depth buffer.\nIn the Deferred rendering path, the feature uses the G-buffer normals texture.");
            public static GUIContent NormalQuality = new GUIContent("Normal Quality", "The number of depth texture samples that Unity takes when computing the normals. Low:1 sample, Medium: 5 samples, High: 9 samples.");
            public static GUIContent Intensity = EditorGUIUtility.TrTextContent("Intensity", "The degree of darkness that Ambient Occlusion adds.");
            public static GUIContent DirectLightingStrength = EditorGUIUtility.TrTextContent("Direct Lighting Strength", "Controls how much the ambient occlusion affects direct lighting.");
            public static GUIContent Radius = EditorGUIUtility.TrTextContent("Radius", "The radius around a given point, where Unity calculates and applies the effect.");
            public static GUIContent Samples = EditorGUIUtility.TrTextContent("Sample Count", "The number of samples that Unity takes when calculating the obscurance value. Higher values have high performance impact.");
            public static GUIContent SinglePassBlur = EditorGUIUtility.TrTextContent("Single Pass Blur", "Single pass blur approximation");
            public static GUIContent FinalUpsample = EditorGUIUtility.TrTextContent("Final Upsample", "Final upsample step to perform after blurring downsampled targets.");
            public static GUIContent BlurType = EditorGUIUtility.TrTextContent("Blur Type", "Blur Algorithm to Use");
            public static GUIContent OnlyAO = EditorGUIUtility.TrTextContent("Only AO", "");
            public static GUIContent Falloff = EditorGUIUtility.TrTextContent("Falloff Distance", "");
        }

        private void Init()
        {
            SerializedProperty settings = serializedObject.FindProperty("m_Settings");
            m_BlueNoiseTexture = settings.FindPropertyRelative("BlueNoiseTexture");
            m_Source = settings.FindPropertyRelative("Source");
            m_AOAlgorithm = settings.FindPropertyRelative("AOAlgorithm");
            m_Downsample = settings.FindPropertyRelative("Downsample");
            m_AfterOpaque = settings.FindPropertyRelative("AfterOpaque");
            m_NormalQuality = settings.FindPropertyRelative("NormalSamples");
            m_Intensity = settings.FindPropertyRelative("Intensity");
            m_DirectLightingStrength = settings.FindPropertyRelative("DirectLightingStrength");
            m_Radius = settings.FindPropertyRelative("Radius");
            m_Samples = settings.FindPropertyRelative("Samples");
            m_SinglePassBlur = settings.FindPropertyRelative("SinglePassBlur");
            m_FinalUpsample = settings.FindPropertyRelative("FinalUpsample");
            m_BlurType = settings.FindPropertyRelative("BlurType");
            m_OnlyAO = settings.FindPropertyRelative("OnlyAO");
            m_Falloff = settings.FindPropertyRelative("Falloff");

            m_IsInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            if (!m_IsInitialized)
            {
                Init();
            }

            bool isDeferredRenderingMode = RendererIsDeferred();

            EditorGUILayout.PropertyField(m_BlueNoiseTexture, Styles.BlueNoiseTexture);


            EditorGUILayout.PropertyField(m_AOAlgorithm, Styles.AOAlgorithm);
            EditorGUILayout.PropertyField(m_OnlyAO, Styles.OnlyAO);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_Downsample, Styles.Downsample);
            EditorGUILayout.PropertyField(m_AfterOpaque, Styles.AfterOpaque);

            GUI.enabled = !isDeferredRenderingMode;
            EditorGUILayout.PropertyField(m_Source, Styles.Source);

            // We only enable this field when depth source is selected
            GUI.enabled = !isDeferredRenderingMode && m_Source.enumValueIndex == (int)ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth;
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_NormalQuality, Styles.NormalQuality);
            EditorGUI.indentLevel--;
            GUI.enabled = true;

            EditorGUILayout.Space();


            EditorGUILayout.PropertyField(m_BlurType, Styles.BlurType);
            EditorGUILayout.PropertyField(m_SinglePassBlur, Styles.SinglePassBlur);
            EditorGUILayout.PropertyField(m_FinalUpsample, Styles.FinalUpsample);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_Intensity, Styles.Intensity);
            EditorGUILayout.PropertyField(m_Radius, Styles.Radius);
            EditorGUILayout.PropertyField(m_Falloff, Styles.Falloff);
            m_DirectLightingStrength.floatValue = EditorGUILayout.Slider(Styles.DirectLightingStrength, m_DirectLightingStrength.floatValue, 0f, 1f);
            EditorGUILayout.PropertyField(m_Samples, Styles.Samples);

            m_Intensity.floatValue = Mathf.Clamp(m_Intensity.floatValue, 0f, m_Intensity.floatValue);
            m_Radius.floatValue = Mathf.Clamp(m_Radius.floatValue, 0f, m_Radius.floatValue);

            m_Falloff.floatValue = Mathf.Clamp(m_Falloff.floatValue, 0f, m_Falloff.floatValue);
        }

        private bool RendererIsDeferred()
        {
            ScreenSpaceAmbientOcclusion ssaoFeature = (ScreenSpaceAmbientOcclusion)this.target;
            UniversalRenderPipelineAsset pipelineAsset = (UniversalRenderPipelineAsset)GraphicsSettings.renderPipelineAsset;

            if (ssaoFeature == null || pipelineAsset == null)
                return false;

            // We have to find the renderer related to the SSAO feature, then test if it is in deferred mode.
            var rendererDataList = pipelineAsset.m_RendererDataList;
            for (int rendererIndex = 0; rendererIndex < rendererDataList.Length; ++rendererIndex)
            {
                ScriptableRendererData rendererData = (ScriptableRendererData)rendererDataList[rendererIndex];
                if (rendererData == null)
                    continue;

                var rendererFeatures = rendererData.rendererFeatures;
                foreach (var feature in rendererFeatures)
                {
                    if (feature is ScreenSpaceAmbientOcclusion && (ScreenSpaceAmbientOcclusion)feature == ssaoFeature)
                        return rendererData is UniversalRendererData && ((UniversalRendererData)rendererData).renderingMode == RenderingMode.Deferred;
                }
            }

            return false;
        }
    }
}
