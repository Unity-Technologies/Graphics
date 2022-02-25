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

        private SerializedProperty property = null;
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

        private void Init(SerializedProperty property)
        {
            SerializedProperty rendererProp =
                property.serializedObject
                    .FindProperty(nameof(UniversalRenderPipelineAsset.m_RendererDataReferenceList))
                    .GetArrayElementAtIndex(ScriptableRendererDataEditor.CurrentIndex);

            isDeferredRenderingMode = rendererProp.FindPropertyRelative("m_RenderingMode").intValue == (int)RenderingMode.Deferred;
            if (this.property == property)
                return;
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

        protected override void OnGUIRendererFeature(ref Rect position, SerializedProperty property, GUIContent label)
        {
            Init(property);

            DrawProperty(ref position, m_Downsample, Styles.Downsample);

            DrawProperty(ref position, m_AfterOpaque, Styles.AfterOpaque);

            if (!isDeferredRenderingMode)
            {
                DrawProperty(ref position, m_Source, Styles.Source);

                // We only enable this field when depth source is selected
                if (m_Source.enumValueIndex == (int)ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth)
                {
                    EditorGUI.indentLevel++;
                    DrawProperty(ref position, m_NormalQuality, Styles.NormalQuality);
                    EditorGUI.indentLevel--;
                }
            }

            DrawProperty(ref position, m_Intensity, Styles.Intensity);
            DrawProperty(ref position, m_Radius, Styles.Radius);
            DrawProperty(ref position, m_DirectLightingStrength, Styles.DirectLightingStrength);
            DrawProperty(ref position, m_SampleCount, Styles.SampleCount);

            m_Intensity.floatValue = Mathf.Clamp(m_Intensity.floatValue, 0f, m_Intensity.floatValue);
            m_Radius.floatValue = Mathf.Clamp(m_Radius.floatValue, 0f, m_Radius.floatValue);
        }
        /* This code might be useful later when renderers are polymorphic.
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
        }*/
    }
}
