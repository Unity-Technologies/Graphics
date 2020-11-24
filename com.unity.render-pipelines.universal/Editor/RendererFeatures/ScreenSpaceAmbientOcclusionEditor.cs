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
            public static GUIContent Source = EditorGUIUtility.TrTextContent("Source", "This option determines whether the ambient occlusion reconstructs the normal from depth or is given it from a DepthNormal/Deferred Gbuffer texture.");
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
        }
    }
}
