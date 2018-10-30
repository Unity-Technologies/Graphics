using UnityEditor;
using UnityEditor.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    [CustomEditor(typeof(LightweightRenderPipelineAsset))]
    public class LightweightRenderPipelineAssetEditor : Editor
    {
        internal class Styles
        {
            // Groups
            public static GUIContent generalSettingsText = EditorGUIUtility.TrTextContent("General");
            public static GUIContent qualitySettingsText = EditorGUIUtility.TrTextContent("Quality");
            public static GUIContent lightingSettingsText = EditorGUIUtility.TrTextContent("Lighting");
            public static GUIContent shadowSettingsText = EditorGUIUtility.TrTextContent("Shadows");
            public static GUIContent advancedSettingsText = EditorGUIUtility.TrTextContent("Advanced");

            // General
            public static GUIContent requireDepthTextureText = EditorGUIUtility.TrTextContent("Depth Texture", "If enabled the pipeline will generate camera's depth that can be bound in shaders as _CameraDepthTexture.");
            public static GUIContent requireOpaqueTextureText = EditorGUIUtility.TrTextContent("Opaque Texture", "If enabled the pipeline will copy the screen to texture after opaque objects are drawn. For transparent objects this can be bound in shaders as _CameraOpaqueTexture.");
            public static GUIContent opaqueDownsamplingText = EditorGUIUtility.TrTextContent("Opaque Downsampling", "The downsampling method that is used for the opaque texture");

            // Quality
            public static GUIContent hdrText = EditorGUIUtility.TrTextContent("HDR", "Controls the global HDR settings.");
            public static GUIContent msaaText = EditorGUIUtility.TrTextContent("Anti Aliasing (MSAA)", "Controls the global anti aliasing settings.");
            public static GUIContent renderScaleText = EditorGUIUtility.TrTextContent("Render Scale", "Scales the camera render target allowing the game to render at a resolution different than native resolution. UI is always rendered at native resolution. When VR is enabled, this is overridden by XRSettings.");

            // Main light
            public static GUIContent mainLightRenderingModeText = EditorGUIUtility.TrTextContent("Main Light", "Main light is the brightest directional light.");
            public static GUIContent supportsMainLightShadowsText = EditorGUIUtility.TrTextContent("Cast Shadows", "If enabled the main light can be a shadow casting light.");
            public static GUIContent mainLightShadowmapResolutionText = EditorGUIUtility.TrTextContent("Shadow Resolution", "Resolution of the main light shadowmap texture. If cascades are enabled, cascades will be packed into an atlas and this setting controls the maximum shadows atlas resolution.");

            // Additional lights
            public static GUIContent addditionalLightsRenderingModeText = EditorGUIUtility.TrTextContent("Additional Lights", "Additional lights support.");
            public static GUIContent perObjectLimit = EditorGUIUtility.TrTextContent("Per Object Limit", "Maximum amount of additional lights. These lights are sorted and culled per-object.");
            public static GUIContent supportsAdditionalShadowsText = EditorGUIUtility.TrTextContent("Cast Shadows", "If enabled shadows will be supported for spot lights.\n");
            public static GUIContent additionalLightsShadowmapResolution = EditorGUIUtility.TrTextContent("Shadow Resolution", "All additional lights are packed into a single shadowmap atlas. This setting controls the atlas size.");

            // Shadow settings
            public static GUIContent shadowDistanceText = EditorGUIUtility.TrTextContent("Distance", "Maximum shadow rendering distance.");
            public static GUIContent shadowCascadesText = EditorGUIUtility.TrTextContent("Cascades", "Number of cascade splits used in for directional shadows");
            public static GUIContent shadowDepthBias = EditorGUIUtility.TrTextContent("Depth Bias", "Controls the distance at which the shadows will be pushed away from the light. Useful for avoiding false self-shadowing artifacts.");
            public static GUIContent shadowNormalBias = EditorGUIUtility.TrTextContent("Normal Bias", "Controls distance at which the shadow casting surfaces will be shrunk along the surface normal. Useful for avoiding false self-shadowing artifacts.");
            public static GUIContent supportsSoftShadows = EditorGUIUtility.TrTextContent("Soft Shadows", "If enabled pipeline will perform shadow filtering. Otherwise all lights that cast shadows will fallback to perform a single shadow sample.");

            // Advanced settings
            public static GUIContent dynamicBatching = EditorGUIUtility.TrTextContent("Dynamic Batching", "If enabled the pipeline will batch drawcalls with few triangles together by copying their vertex buffers into a shared buffer on a per-frame basis.");
            public static GUIContent mixedLightingSupportLabel = EditorGUIUtility.TrTextContent("Mixed Lighting", "Support for mixed light mode.");

            public static GUIContent shaderVariantLogLevel = EditorGUIUtility.TrTextContent("Shader Variant Log Level", "Controls the level logging in of shader variants information is outputted when a build is performed. Information will appear in the Unity console when the build finishes.");

            // Dropdown menu options
            public static string[] mainLightOptions = { "None", "Pixel Lighting" };

            public static string[] shadowCascadeOptions = {"No Cascades", "Two Cascades", "Four Cascades"};
            public static string[] opaqueDownsamplingOptions = {"None", "2x (Bilinear)", "4x (Box)", "4x (Bilinear)"};

        }

        bool m_GeneralSettingsFoldout = false;
        bool m_QualitySettingsFoldout = false;
        bool m_LightingSettingsFoldout = false;
        bool m_ShadowSettingsFoldout = false;
        bool m_AdvancedSettingsFoldout = false;

        int k_MaxSupportedPerObjectLights = 4;
        float k_MinRenderScale = 0.1f;
        float k_MaxRenderScale = 4.0f;
        float k_MaxShadowBias = 10.0f;

        SerializedProperty m_RequireDepthTextureProp;
        SerializedProperty m_RequireOpaqueTextureProp;
        SerializedProperty m_OpaqueDownsamplingProp;

        SerializedProperty m_HDR;
        SerializedProperty m_MSAA;
        SerializedProperty m_RenderScale;

        SerializedProperty m_MainLightRenderingModeProp;
        SerializedProperty m_MainLightShadowsSupportedProp;
        SerializedProperty m_MainLightShadowmapResolutionProp;

        SerializedProperty m_AdditionalLightsRenderingModeProp;
        SerializedProperty m_AdditionalLightsPerObjectLimitProp;
        SerializedProperty m_AdditionalLightShadowsSupportedProp;
        SerializedProperty m_AdditionalLightShadowmapResolutionProp;

        SerializedProperty m_ShadowDistanceProp;
        SerializedProperty m_ShadowCascadesProp;
        SerializedProperty m_ShadowCascade2SplitProp;
        SerializedProperty m_ShadowCascade4SplitProp;
        SerializedProperty m_ShadowDepthBiasProp;
        SerializedProperty m_ShadowNormalBiasProp;

        SerializedProperty m_SoftShadowsSupportedProp;

        SerializedProperty m_SupportsDynamicBatching;
        SerializedProperty m_MixedLightingSupportedProp;

        SerializedProperty m_ShaderVariantLogLevel;

        internal static LightRenderingMode selectedLightRenderingMode;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawGeneralSettings();
            DrawQualitySettings();
            DrawLightingSettings();
            DrawShadowSettings();
            DrawAdvancedSettings();

            serializedObject.ApplyModifiedProperties();
        }

        void OnEnable()
        {
            m_RequireDepthTextureProp = serializedObject.FindProperty("m_RequireDepthTexture");
            m_RequireOpaqueTextureProp = serializedObject.FindProperty("m_RequireOpaqueTexture");
            m_OpaqueDownsamplingProp = serializedObject.FindProperty("m_OpaqueDownsampling");

            m_HDR = serializedObject.FindProperty("m_SupportsHDR");
            m_MSAA = serializedObject.FindProperty("m_MSAA");
            m_RenderScale = serializedObject.FindProperty("m_RenderScale");

            m_MainLightRenderingModeProp = serializedObject.FindProperty("m_MainLightRenderingMode");
            m_MainLightShadowsSupportedProp = serializedObject.FindProperty("m_MainLightShadowsSupported");
            m_MainLightShadowmapResolutionProp = serializedObject.FindProperty("m_MainLightShadowmapResolution");

            m_AdditionalLightsRenderingModeProp = serializedObject.FindProperty("m_AdditionalLightsRenderingMode");
            m_AdditionalLightsPerObjectLimitProp = serializedObject.FindProperty("m_AdditionalLightsPerObjectLimit");
            m_AdditionalLightShadowsSupportedProp = serializedObject.FindProperty("m_AdditionalLightShadowsSupported");
            m_AdditionalLightShadowmapResolutionProp = serializedObject.FindProperty("m_AdditionalLightsShadowmapResolution");

            m_ShadowDistanceProp = serializedObject.FindProperty("m_ShadowDistance");
            m_ShadowCascadesProp = serializedObject.FindProperty("m_ShadowCascades");
            m_ShadowCascade2SplitProp = serializedObject.FindProperty("m_Cascade2Split");
            m_ShadowCascade4SplitProp = serializedObject.FindProperty("m_Cascade4Split");
            m_ShadowDepthBiasProp = serializedObject.FindProperty("m_ShadowDepthBias");
            m_ShadowNormalBiasProp = serializedObject.FindProperty("m_ShadowNormalBias");
            m_SoftShadowsSupportedProp = serializedObject.FindProperty("m_SoftShadowsSupported");

            m_SupportsDynamicBatching = serializedObject.FindProperty("m_SupportsDynamicBatching");
            m_MixedLightingSupportedProp = serializedObject.FindProperty("m_MixedLightingSupported");

            m_ShaderVariantLogLevel = serializedObject.FindProperty("m_ShaderVariantLogLevel");
            selectedLightRenderingMode = (LightRenderingMode)m_AdditionalLightsRenderingModeProp.intValue;
        }

        void DrawGeneralSettings()
        {
            m_GeneralSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_GeneralSettingsFoldout, Styles.generalSettingsText);
            if (m_GeneralSettingsFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_RequireDepthTextureProp, Styles.requireDepthTextureText);
                EditorGUILayout.PropertyField(m_RequireOpaqueTextureProp, Styles.requireOpaqueTextureText);
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(!m_RequireOpaqueTextureProp.boolValue);
                EditorGUILayout.PropertyField(m_OpaqueDownsamplingProp, Styles.opaqueDownsamplingText);
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawQualitySettings()
        {
            m_QualitySettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_QualitySettingsFoldout, Styles.qualitySettingsText);
            if (m_QualitySettingsFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_HDR, Styles.hdrText);
                EditorGUILayout.PropertyField(m_MSAA, Styles.msaaText);
                EditorGUI.BeginDisabledGroup(XRGraphics.enabled);
                m_RenderScale.floatValue = EditorGUILayout.Slider(Styles.renderScaleText, m_RenderScale.floatValue, k_MinRenderScale, k_MaxRenderScale);
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawLightingSettings()
        {
            m_LightingSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_LightingSettingsFoldout, Styles.lightingSettingsText);
            if (m_LightingSettingsFoldout)
            {
                EditorGUI.indentLevel++;

                // Main Light
                bool disableGroup = false;
                EditorGUI.BeginDisabledGroup(disableGroup);
                CoreEditorUtils.DrawPopup(Styles.mainLightRenderingModeText, m_MainLightRenderingModeProp, Styles.mainLightOptions);
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel++;
                disableGroup |= !m_MainLightRenderingModeProp.boolValue;

                EditorGUI.BeginDisabledGroup(disableGroup);
                EditorGUILayout.PropertyField(m_MainLightShadowsSupportedProp, Styles.supportsMainLightShadowsText);
                EditorGUI.EndDisabledGroup();

                disableGroup |= !m_MainLightShadowsSupportedProp.boolValue;
                EditorGUI.BeginDisabledGroup(disableGroup);
                EditorGUILayout.PropertyField(m_MainLightShadowmapResolutionProp, Styles.mainLightShadowmapResolutionText);
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel--;
                EditorGUILayout.Space();

                // Additional light
                selectedLightRenderingMode = (LightRenderingMode)EditorGUILayout.EnumPopup(Styles.addditionalLightsRenderingModeText, selectedLightRenderingMode);
                m_AdditionalLightsRenderingModeProp.intValue = (int)selectedLightRenderingMode;
                EditorGUI.indentLevel++;

                disableGroup = m_AdditionalLightsRenderingModeProp.intValue != (int)LightRenderingMode.PerPixel;
                EditorGUI.BeginDisabledGroup(disableGroup);
                m_AdditionalLightsPerObjectLimitProp.intValue = EditorGUILayout.IntSlider(Styles.perObjectLimit, m_AdditionalLightsPerObjectLimitProp.intValue, 0, k_MaxSupportedPerObjectLights);
                EditorGUI.EndDisabledGroup();

                disableGroup |= m_AdditionalLightsPerObjectLimitProp.intValue == 0;
                EditorGUI.BeginDisabledGroup(disableGroup);
                EditorGUILayout.PropertyField(m_AdditionalLightShadowsSupportedProp, Styles.supportsAdditionalShadowsText);
                EditorGUI.EndDisabledGroup();

                disableGroup |= !m_AdditionalLightShadowsSupportedProp.boolValue;
                EditorGUI.BeginDisabledGroup(disableGroup);
                EditorGUILayout.PropertyField(m_AdditionalLightShadowmapResolutionProp, Styles.additionalLightsShadowmapResolution);
                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel--;
                EditorGUI.indentLevel--;

                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawShadowSettings()
        {
            m_ShadowSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShadowSettingsFoldout, Styles.shadowSettingsText);
            if (m_ShadowSettingsFoldout)
            {
                EditorGUI.indentLevel++;
                m_ShadowDistanceProp.floatValue = Mathf.Max(0.0f, EditorGUILayout.FloatField(Styles.shadowDistanceText, m_ShadowDistanceProp.floatValue));
                CoreEditorUtils.DrawPopup(Styles.shadowCascadesText, m_ShadowCascadesProp, Styles.shadowCascadeOptions);

                ShadowCascadesOption cascades = (ShadowCascadesOption)m_ShadowCascadesProp.intValue;
                if (cascades == ShadowCascadesOption.FourCascades)
                    CoreEditorUtils.DrawCascadeSplitGUI<Vector3>(ref m_ShadowCascade4SplitProp);
                else if (cascades == ShadowCascadesOption.TwoCascades)
                    CoreEditorUtils.DrawCascadeSplitGUI<float>(ref m_ShadowCascade2SplitProp);

                m_ShadowDepthBiasProp.floatValue = EditorGUILayout.Slider(Styles.shadowDepthBias, m_ShadowDepthBiasProp.floatValue, 0.0f, k_MaxShadowBias);
                m_ShadowNormalBiasProp.floatValue = EditorGUILayout.Slider(Styles.shadowNormalBias, m_ShadowNormalBiasProp.floatValue, 0.0f, k_MaxShadowBias);
                EditorGUILayout.PropertyField(m_SoftShadowsSupportedProp, Styles.supportsSoftShadows);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawAdvancedSettings()
        {
            m_AdvancedSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_AdvancedSettingsFoldout, Styles.advancedSettingsText);
            if (m_AdvancedSettingsFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_SupportsDynamicBatching, Styles.dynamicBatching);
                EditorGUILayout.PropertyField(m_MixedLightingSupportedProp, Styles.mixedLightingSupportLabel);
                EditorGUILayout.PropertyField(m_ShaderVariantLogLevel, Styles.shaderVariantLogLevel);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}
