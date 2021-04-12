using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(ScreenSpaceAmbientOcclusion))]
    internal class ScreenSpaceAmbientOcclusionEditor : Editor
    {
        #region Serialized Properties
        private SerializedProperty m_VolumeSettings;
        #endregion

        private bool m_IsInitialized = false;

        // Structs
        private struct Styles
        {
            public static GUIContent Volume = EditorGUIUtility.TrTextContent("Volume", "");
            public static GUIContent Downsample = EditorGUIUtility.TrTextContent("Downsample", "With this option enabled, Unity downsamples the SSAO effect texture to improve performance. Each dimension of the texture is reduced by a factor of 2.");
            public static GUIContent AfterOpaque = EditorGUIUtility.TrTextContent("After Opaque", "With this option enabled, Unity calculates and apply SSAO after the opaque pass to improve performance on mobile platforms with tiled-based GPU architectures. This is not physically correct.");
            public static GUIContent Source = EditorGUIUtility.TrTextContent("Source", "This option determines whether the ambient occlusion reconstructs the normal from depth or is given by a Normals texture. In deferred rendering mode, Gbuffer Normals texture is always used.");
            public static GUIContent NormalQuality = new GUIContent("Normal Quality", "The options in this field define the number of depth texture samples that Unity takes when computing the normals. Low: 1 sample, Medium: 5 samples, High: 9 samples.");
            public static GUIContent Intensity = EditorGUIUtility.TrTextContent("Intensity", "The degree of darkness that Ambient Occlusion adds.");
            public static GUIContent DirectLightingStrength = EditorGUIUtility.TrTextContent("Direct Lighting Strength", "Controls how much the ambient occlusion affects direct lighting.");
            public static GUIContent Radius = EditorGUIUtility.TrTextContent("Radius", "The radius around a given point, where Unity calculates and applies the effect.");
            public static GUIContent SampleCount = EditorGUIUtility.TrTextContent("Sample Count", "The number of samples that Unity takes when calculating the obscurance value. Higher values have high performance impact.");
        }

        private void Init()
        {
            m_VolumeSettings = serializedObject.FindProperty("m_VolumeSettings");
            if (m_VolumeSettings.objectReferenceValue == null)
            {
                serializedObject.Update();

                ScriptableObject component = CreateInstance(typeof(ScreenSpaceAmbientOcclusionVolume));
                component.name = $"New{typeof(ScreenSpaceAmbientOcclusionVolume)}";
                //Undo.RegisterCreatedObjectUndo(component, "Add SSAO");

                // Store this new effect as a sub-asset so we can reference it safely afterwards
                // Only when we're not dealing with an instantiated asset
                string ssaoRendererPath = AssetDatabase.GetAssetPath(target);
                ScriptableRendererData renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(ssaoRendererPath);
                if (EditorUtility.IsPersistent(renderer))
                {
                    AssetDatabase.AddObjectToAsset(component, renderer);
                }

                m_VolumeSettings.objectReferenceValue = component;
                EditorUtility.SetDirty(renderer);
                serializedObject.ApplyModifiedProperties();
            }

            m_IsInitialized = true;
        }

        public override void OnInspectorGUI()
        {
            if (!m_IsInitialized)
            {
                Init();
            }

            EditorGUILayout.PropertyField(m_VolumeSettings, Styles.Volume);
            EditorGUILayout.Space(10f);

            var volComp = m_VolumeSettings.objectReferenceValue as VolumeComponent;
            var editor = VolumeComponentListEditor.CreateSingleEditor(volComp, m_VolumeSettings, this);
            editor.OnInternalInspectorGUI();
/*
            bool isDeferredRenderingMode = RendererIsDeferred();

            EditorGUILayout.PropertyField(m_Downsample, Styles.Downsample);

            EditorGUILayout.PropertyField(m_AfterOpaque, Styles.AfterOpaque);

            GUI.enabled = !isDeferredRenderingMode;
            EditorGUILayout.PropertyField(m_Source, Styles.Source);

            // We only enable this field when depth source is selected
            GUI.enabled = !isDeferredRenderingMode && m_Source.enumValueIndex == (int)ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth;
            EditorGUILayout.PropertyField(m_NormalQuality, Styles.NormalQuality);
            GUI.enabled = true;

            EditorGUILayout.PropertyField(m_Intensity, Styles.Intensity);
            EditorGUILayout.PropertyField(m_Radius, Styles.Radius);
            m_DirectLightingStrength.floatValue = EditorGUILayout.Slider(Styles.DirectLightingStrength, m_DirectLightingStrength.floatValue, 0f, 1f);
            m_SampleCount.intValue = EditorGUILayout.IntSlider(Styles.SampleCount, m_SampleCount.intValue, 4, 20);

            m_Intensity.floatValue = Mathf.Clamp(m_Intensity.floatValue, 0f, m_Intensity.floatValue);
            m_Radius.floatValue = Mathf.Clamp(m_Radius.floatValue, 0f, m_Radius.floatValue);
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

            return false;*/
        }
    }
}
