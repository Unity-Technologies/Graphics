using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [CustomEditor(typeof(LightweightPipelineAsset))]
    public class LightweightPipelineAssetEditor : Editor
    {
        internal class Styles
        {
            public static GUIContent renderingLabel = new GUIContent("Rendering");
            public static GUIContent shadowLabel = new GUIContent("Shadows");

            public static GUIContent renderScaleLabel = new GUIContent("Render Scale", "Scales the camera render target allowing the game to render at a resolution different than native resolution. UI is always rendered at native resolution. When in VR mode, VR scaling configuration is used instead.");

            public static GUIContent maxPixelLightsLabel = new GUIContent("Pixel Lights",
                    "Controls the amount of pixel lights that run in fragment light loop. Lights are sorted and culled per-object.");

            public static GUIContent enableVertexLightLabel = new GUIContent("Vertex Lighting",
                    "If enabled shades additional lights exceeding the maximum number of pixel lights per-vertex up to the maximum of 8 lights.");

            public static GUIContent requireDepthTexture = new GUIContent("Depth Texture", "If enabled the pipeline will generate camera's depth that can be bound in shaders as _CameraDepthTexture.");

            public static GUIContent requireSoftParticles = new GUIContent("Soft Particles", "If enabled the pipeline will enable SOFT_PARTICLES keyword.");

            public static GUIContent requireOpaqueTexture = new GUIContent("Opaque Texture", "If enabled the pipeline will copy the screen to texture after opaque objects are drawn. For transparent objects this can be bound in shaders as _CameraOpaqueTexture.");

            public static GUIContent opaqueDownsampling = new GUIContent("Opaque Downsampling", "The downsampling method that is used for the opaque texture");
            public static GUIContent hdrContent = new GUIContent("HDR", "Controls the global HDR settings.");
            public static GUIContent msaaContent = new GUIContent("Anti Aliasing (MSAA)", "Controls the global anti aliasing settings.");
            public static GUIContent dynamicBatching = new GUIContent("Dynamic Batching", "If enabled the pipeline will batch drawcalls with few triangles together by copying their vertex buffers into a shared buffer on a per-frame basis.");

            public static GUIContent supportsSoftShadows = new GUIContent("Soft Shadows", "If enabled pipeline will perform shadow filtering. Otherwise all lights that cast shadows will fallback to perform a single shadow sample.");
            public static GUIContent supportsDirectionalShadows = new GUIContent("Directional Shadows", "If enabled shadows will be supported for directional lights.");

            public static GUIContent shadowDistance = new GUIContent("Distance", "Max shadow rendering distance.");

            public static GUIContent directionalShadowAtlasResolution = new GUIContent("Atlas Resolution",
                    "Resolution of the directional shadow map texture. If cascades are enabled, cascades will be packed into an atlas and this setting controls the max shadows atlas resolution.");

            public static GUIContent shadowCascades = new GUIContent("Cascades",
                    "Number of cascades used in directional lights shadows");

            public static GUIContent shadowCascadeSplit = new GUIContent("Cascades Split",
                    "Percentages to split shadow volume");

            public static GUIContent supportsLocalShadows = new GUIContent("Local Shadows", "If enabled shadows will be supported for spot lights.");

            public static GUIContent localShadowsAtlasResolution = new GUIContent("Atlas Resolution",
                    "All local lights are packed into a single atlas. This setting controls the atlas size.");

            public static string[] shadowCascadeOptions = {"No Cascades", "Two Cascades", "Four Cascades"};
            public static string[] opaqueDownsamplingOptions = {"None", "2x (Bilinear)", "4x (Box)", "4x (Bilinear)"};
        }

        public static class StrippingStyles
        {
            public static GUIContent strippingLabel = new GUIContent("Shader Stripping");
            public static GUIContent pipelineCapabilitiesLabel = new GUIContent("Pipeline Capabilities", "Select pipeline capabilities variants to be kept in the build.");
            public static string[] strippingOptions = {"Automatic", "Custom"};

            public static GUIContent localLightsLabel = new GUIContent("Additional Lights", "If enabled additional lights variants won't be stripped from build.");
            public static GUIContent vertexLightsLabel = new GUIContent("Vertex Lights", "If enabled vertex lights variants wont' be stripped from build.");
            public static GUIContent directionalShadowsLabel = new GUIContent("Directional Shadows", "If enabled directional shadows variants won't be stripped from build.");
            public static GUIContent localShadowsLabel = new GUIContent("Local Shadows", "If enabled local shadows variants won't be stripped from build.");
            public static GUIContent softShadowsLabel = new GUIContent("Soft Shadows", "If enabled soft shadows variants won't be stripped from build.");
        }

        AnimBool m_ShowSoftParticles = new AnimBool();
        AnimBool m_ShowOpaqueTextureScale = new AnimBool();

        int k_MaxSupportedPixelLights = 8;
        float k_MinRenderScale = 0.1f;
        float k_MaxRenderScale = 4.0f;
        SerializedProperty m_RenderScale;
        SerializedProperty m_MaxPixelLights;
        SerializedProperty m_SupportsVertexLightProp;
        SerializedProperty m_RequireDepthTextureProp;
        SerializedProperty m_RequireSoftParticlesProp;
        SerializedProperty m_RequireOpaqueTextureProp;
        SerializedProperty m_OpaqueDownsamplingProp;
        SerializedProperty m_HDR;
        SerializedProperty m_MSAA;
        SerializedProperty m_SupportsDynamicBatching;

        SerializedProperty m_SoftShadowsSupportedProp;
        SerializedProperty m_DirectionalShadowsSupportedProp;
        SerializedProperty m_ShadowDistanceProp;
        SerializedProperty m_DirectionalShadowAtlasResolutionProp;
        SerializedProperty m_ShadowCascadesProp;
        SerializedProperty m_ShadowCascade2SplitProp;
        SerializedProperty m_ShadowCascade4SplitProp;
        SerializedProperty m_LocalShadowSupportedProp;
        SerializedProperty m_LocalShadowsAtlasResolutionProp;

        SerializedProperty m_CustomShaderVariantStripSettingsProp;
        SerializedProperty m_KeepAdditionalLightsProp;
        SerializedProperty m_KeepVertexLightsProp;
        SerializedProperty m_KeepDirectionalShadowsProp;
        SerializedProperty m_KeepLocalShadowsProp;
        SerializedProperty m_KeepSoftShadowsProp;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UpdateAnimationValues();
            DrawRenderingSettings();
            DrawShadowSettings();
            DrawStrippingSettings();

            serializedObject.ApplyModifiedProperties();
        }

        void OnEnable()
        {
            m_RenderScale = serializedObject.FindProperty("m_RenderScale");
            m_MaxPixelLights = serializedObject.FindProperty("m_MaxPixelLights");
            m_SupportsVertexLightProp = serializedObject.FindProperty("m_SupportsVertexLight");
            m_RequireDepthTextureProp = serializedObject.FindProperty("m_RequireDepthTexture");
            m_RequireSoftParticlesProp = serializedObject.FindProperty("m_RequireSoftParticles");
            m_RequireOpaqueTextureProp = serializedObject.FindProperty("m_RequireOpaqueTexture");
            m_OpaqueDownsamplingProp = serializedObject.FindProperty("m_OpaqueDownsampling");
            m_HDR = serializedObject.FindProperty("m_SupportsHDR");
            m_MSAA = serializedObject.FindProperty("m_MSAA");
            m_SupportsDynamicBatching = serializedObject.FindProperty("m_SupportsDynamicBatching");

            m_DirectionalShadowsSupportedProp = serializedObject.FindProperty("m_DirectionalShadowsSupported");
            m_ShadowDistanceProp = serializedObject.FindProperty("m_ShadowDistance");
            m_DirectionalShadowAtlasResolutionProp = serializedObject.FindProperty("m_ShadowAtlasResolution");
            m_ShadowCascadesProp = serializedObject.FindProperty("m_ShadowCascades");
            m_ShadowCascade2SplitProp = serializedObject.FindProperty("m_Cascade2Split");
            m_ShadowCascade4SplitProp = serializedObject.FindProperty("m_Cascade4Split");
            m_LocalShadowSupportedProp = serializedObject.FindProperty("m_LocalShadowsSupported");
            m_LocalShadowsAtlasResolutionProp = serializedObject.FindProperty("m_LocalShadowsAtlasResolution");
            m_SoftShadowsSupportedProp = serializedObject.FindProperty("m_SoftShadowsSupported");

            m_CustomShaderVariantStripSettingsProp = serializedObject.FindProperty("m_CustomShaderVariantStrippingSettings");
            m_KeepAdditionalLightsProp = serializedObject.FindProperty("m_KeepAdditionalLightVariants");
            m_KeepVertexLightsProp = serializedObject.FindProperty("m_KeepVertexLightVariants");
            m_KeepDirectionalShadowsProp = serializedObject.FindProperty("m_KeepDirectionalShadowVariants");
            m_KeepLocalShadowsProp = serializedObject.FindProperty("m_KeepLocalShadowVariants");
            m_KeepSoftShadowsProp = serializedObject.FindProperty("m_KeepSoftShadowVariants");

            m_ShowSoftParticles.valueChanged.AddListener(Repaint);
            m_ShowSoftParticles.value = m_RequireSoftParticlesProp.boolValue;
            m_ShowOpaqueTextureScale.valueChanged.AddListener(Repaint);
            m_ShowOpaqueTextureScale.value = m_RequireOpaqueTextureProp.boolValue;
        }

        void OnDisable()
        {
            m_ShowSoftParticles.valueChanged.RemoveListener(Repaint);
            m_ShowOpaqueTextureScale.valueChanged.RemoveListener(Repaint);
        }

        void UpdateAnimationValues()
        {
            m_ShowSoftParticles.target = m_RequireDepthTextureProp.boolValue;
            m_ShowOpaqueTextureScale.target = m_RequireOpaqueTextureProp.boolValue;
        }

        void DrawAnimatedProperty(SerializedProperty prop, GUIContent content, AnimBool animation)
        {
            using (var group = new EditorGUILayout.FadeGroupScope(animation.faded))
                if (group.visible)
                    EditorGUILayout.PropertyField(prop, content);
        }

        void DrawAnimatedPopup(SerializedProperty prop, GUIContent content, string[] options, AnimBool animation)
        {
            using (var group = new EditorGUILayout.FadeGroupScope(animation.faded))
                if (group.visible)
                    CoreEditorUtils.DrawPopup(content, prop, options);
        }

        void DrawRenderingSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.renderingLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.renderScaleLabel);
            m_RenderScale.floatValue = EditorGUILayout.Slider(m_RenderScale.floatValue, k_MinRenderScale, k_MaxRenderScale);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.maxPixelLightsLabel);
            m_MaxPixelLights.intValue = EditorGUILayout.IntSlider(m_MaxPixelLights.intValue, 0, k_MaxSupportedPixelLights);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(m_SupportsVertexLightProp, Styles.enableVertexLightLabel);
            EditorGUILayout.PropertyField(m_RequireDepthTextureProp, Styles.requireDepthTexture);
            DrawAnimatedProperty(m_RequireSoftParticlesProp, Styles.requireSoftParticles, m_ShowSoftParticles);
            EditorGUILayout.PropertyField(m_RequireOpaqueTextureProp, Styles.requireOpaqueTexture);
            DrawAnimatedPopup(m_OpaqueDownsamplingProp, Styles.opaqueDownsampling, Styles.opaqueDownsamplingOptions, m_ShowOpaqueTextureScale);
            EditorGUILayout.PropertyField(m_HDR, Styles.hdrContent);
            EditorGUILayout.PropertyField(m_MSAA, Styles.msaaContent);
            EditorGUILayout.PropertyField(m_SupportsDynamicBatching, Styles.dynamicBatching);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        void DrawShadowSettings()
        {
            EditorGUILayout.LabelField(Styles.shadowLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(m_DirectionalShadowsSupportedProp, Styles.supportsDirectionalShadows);
            bool directionalShadows = m_DirectionalShadowsSupportedProp.boolValue;
            if (directionalShadows)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_DirectionalShadowAtlasResolutionProp, Styles.directionalShadowAtlasResolution);
                m_ShadowDistanceProp.floatValue = Mathf.Max(0.0f,
                        EditorGUILayout.FloatField(Styles.shadowDistance, m_ShadowDistanceProp.floatValue));
                CoreEditorUtils.DrawPopup(Styles.shadowCascades, m_ShadowCascadesProp, Styles.shadowCascadeOptions);

                ShadowCascades cascades = (ShadowCascades)m_ShadowCascadesProp.intValue;
                if (cascades == ShadowCascades.FOUR_CASCADES)
                    CoreEditorUtils.DrawCascadeSplitGUI<Vector3>(ref m_ShadowCascade4SplitProp);
                else if (cascades == ShadowCascades.TWO_CASCADES)
                    CoreEditorUtils.DrawCascadeSplitGUI<float>(ref m_ShadowCascade2SplitProp);

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            EditorGUILayout.PropertyField(m_LocalShadowSupportedProp, Styles.supportsLocalShadows);
            bool localShadows = m_LocalShadowSupportedProp.boolValue;
            if (localShadows)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_LocalShadowsAtlasResolutionProp, Styles.localShadowsAtlasResolution);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            if (directionalShadows || localShadows)
                EditorGUILayout.PropertyField(m_SoftShadowsSupportedProp, Styles.supportsSoftShadows);

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }

        void DrawStrippingSettings()
        {
            EditorGUILayout.LabelField(StrippingStyles.strippingLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            CoreEditorUtils.DrawPopup(StrippingStyles.pipelineCapabilitiesLabel, m_CustomShaderVariantStripSettingsProp, StrippingStyles.strippingOptions);
            if (m_CustomShaderVariantStripSettingsProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_KeepAdditionalLightsProp, StrippingStyles.localLightsLabel);
                EditorGUILayout.PropertyField(m_KeepVertexLightsProp, StrippingStyles.vertexLightsLabel);
                EditorGUILayout.PropertyField(m_KeepDirectionalShadowsProp, StrippingStyles.directionalShadowsLabel);
                EditorGUILayout.PropertyField(m_KeepLocalShadowsProp, StrippingStyles.localShadowsLabel);
                EditorGUILayout.PropertyField(m_KeepSoftShadowsProp, StrippingStyles.softShadowsLabel);
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            EditorGUILayout.Space();
        }
    }
}
