using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Editor script for a <c>UniversalRendererData</c> class.
    /// </summary>
    [CustomEditor(typeof(UniversalRendererData), true)]
    public class UniversalRendererDataEditor : ScriptableRendererDataEditor
    {
        private static class Styles
        {
            public static readonly GUIContent RendererTitle = EditorGUIUtility.TrTextContent("Universal Renderer", "Custom Universal Renderer for Universal RP.");
            public static readonly GUIContent PostProcessIncluded = EditorGUIUtility.TrTextContent("Enabled", "Enables the use of post processing effects within the scene. If disabled, Unity excludes post processing renderer Passes, shaders and textures from the build.");
            public static readonly GUIContent PostProcessLabel = EditorGUIUtility.TrTextContent("Data", "The asset containing references to shaders and Textures that the Renderer uses for post-processing.");
            public static readonly GUIContent FilteringSectionLabel = EditorGUIUtility.TrTextContent("Filtering", "Settings that controls and define which layers the renderer draws.");
            public static readonly GUIContent PrepassMask = EditorGUIUtility.TrTextContent("Prepass Layer Mask", "Controls which prepass layers this renderer draws. It applies to any prepass.");
            public static readonly GUIContent OpaqueMask = EditorGUIUtility.TrTextContent("Opaque Layer Mask", "Controls which opaque layers this renderer draws.");
            public static readonly GUIContent TransparentMask = EditorGUIUtility.TrTextContent("Transparent Layer Mask", "Controls which transparent layers this renderer draws.");

            public static readonly GUIContent RenderingSectionLabel = EditorGUIUtility.TrTextContent("Rendering", "Settings related to rendering and lighting.");
            public static readonly GUIContent RenderingModeLabel = EditorGUIUtility.TrTextContent("Rendering Path", "Select a rendering path.");
            public static readonly GUIContent DepthPrimingModeLabel = EditorGUIUtility.TrTextContent("Depth Priming Mode", "With depth priming enabled, Unity uses the depth buffer generated in the depth prepass to determine if a fragment should be rendered or skipped during the Base Camera opaque pass. Disabled: Unity does not perform depth priming. Auto: If there is a Render Pass that requires a depth prepass, Unity performs the depth prepass and depth priming. Forced: Unity performs the depth prepass and depth priming.");
            public static readonly GUIContent DepthPrimingModeInfo = EditorGUIUtility.TrTextContent("On Android, iOS, and Apple TV, Unity performs depth priming only in Forced mode.");
            public static readonly GUIContent DepthPrimingMSAAWarning = EditorGUIUtility.TrTextContent("Depth priming is not supported because MSAA is enabled.");
            public static readonly GUIContent CopyDepthModeLabel = EditorGUIUtility.TrTextContent("Depth Texture Mode", "Controls after which pass URP copies the scene depth. It has a significant impact on mobile devices bandwidth usage. It also allows to force a depth prepass to generate it.");
            public static readonly GUIContent DepthAttachmentFormat = EditorGUIUtility.TrTextContent("Depth Attachment Format", "Which format to use (if it is supported) when creating _CameraDepthAttachment.");
            public static readonly GUIContent DepthTextureFormat = EditorGUIUtility.TrTextContent("Depth Texture Format", "Which format to use (if it is supported) when creating _CameraDepthTexture.");
            public static readonly GUIContent RenderPassLabel = EditorGUIUtility.TrTextContent("Native RenderPass", "Enables URP to use RenderPass API.");

            public static readonly GUIContent RenderPassSectionLabel = EditorGUIUtility.TrTextContent("RenderPass", "This section contains properties related to render passes.");
            public static readonly GUIContent ShadowsSectionLabel = EditorGUIUtility.TrTextContent("Shadows", "This section contains properties related to rendering shadows.");
            public static readonly GUIContent PostProcessingSectionLabel = EditorGUIUtility.TrTextContent("Post-processing", "This section contains properties related to rendering post-processing.");

            public static readonly GUIContent OverridesSectionLabel = EditorGUIUtility.TrTextContent("Overrides", "This section contains Render Pipeline properties that this Renderer overrides.");

            public static readonly GUIContent accurateGbufferNormalsLabel = EditorGUIUtility.TrTextContent("Accurate G-buffer Normals", "Normals in G-buffer use octahedron encoding/decoding. This improves visual quality but might reduce performance.");
            public static readonly GUIContent defaultStencilStateLabel = EditorGUIUtility.TrTextContent("Default Stencil State", "Configure the stencil state for the opaque and transparent render passes.");
            public static readonly GUIContent shadowTransparentReceiveLabel = EditorGUIUtility.TrTextContent("Transparent Receive Shadows", "When disabled, none of the transparent objects will receive shadows.");
            public static readonly GUIContent invalidStencilOverride = EditorGUIUtility.TrTextContent("Error: When using the deferred rendering path, the Renderer requires the control over the 4 highest bits of the stencil buffer to store Material types. The current combination of the stencil override options prevents the Renderer from controlling the required bits. Try changing one of the options to Replace.");
            public static readonly GUIContent intermediateTextureMode = EditorGUIUtility.TrTextContent("Intermediate Texture", "Controls when URP renders via an intermediate texture.");
            public static readonly GUIContent deferredPlusIncompatibleWarning = EditorGUIUtility.TrTextContent("Deferred+ is only available with Render Graph. In compatibility mode, Deferred+ falls back to Forward+.");
        }

        SerializedProperty m_PrepassLayerMask;
        SerializedProperty m_OpaqueLayerMask;
        SerializedProperty m_TransparentLayerMask;
        SerializedProperty m_RenderingMode;
        SerializedProperty m_DepthPrimingMode;
        SerializedProperty m_CopyDepthMode;
        SerializedProperty m_DepthAttachmentFormat;
        SerializedProperty m_DepthTextureFormat;
        SerializedProperty m_AccurateGbufferNormals;
        SerializedProperty m_UseNativeRenderPass;
        SerializedProperty m_DefaultStencilState;
        SerializedProperty m_PostProcessData;
        SerializedProperty m_Shaders;
        SerializedProperty m_ShadowTransparentReceiveProp;
        SerializedProperty m_IntermediateTextureMode;

        List<string> m_DepthFormatStrings = new List<string>();

        private void OnEnable()
        {
            m_PrepassLayerMask = serializedObject.FindProperty("m_PrepassLayerMask");
            m_OpaqueLayerMask = serializedObject.FindProperty("m_OpaqueLayerMask");
            m_TransparentLayerMask = serializedObject.FindProperty("m_TransparentLayerMask");
            m_RenderingMode = serializedObject.FindProperty("m_RenderingMode");
            m_DepthPrimingMode = serializedObject.FindProperty("m_DepthPrimingMode");
            m_CopyDepthMode = serializedObject.FindProperty("m_CopyDepthMode");
            m_DepthAttachmentFormat = serializedObject.FindProperty("m_DepthAttachmentFormat");
            m_DepthTextureFormat = serializedObject.FindProperty("m_DepthTextureFormat");
            m_AccurateGbufferNormals = serializedObject.FindProperty("m_AccurateGbufferNormals");
            m_UseNativeRenderPass = serializedObject.FindProperty("m_UseNativeRenderPass");
            m_DefaultStencilState = serializedObject.FindProperty("m_DefaultStencilState");
            m_PostProcessData = serializedObject.FindProperty("postProcessData");
            m_Shaders = serializedObject.FindProperty("shaders");
            m_ShadowTransparentReceiveProp = serializedObject.FindProperty("m_ShadowTransparentReceive");
            m_IntermediateTextureMode = serializedObject.FindProperty("m_IntermediateTextureMode");
        }

        private void PopulateCompatibleDepthFormats(int renderingMode)
        {
            RenderPathCompatibility renderPathCompatibility = RenderPathCompatibility.All;
            switch (renderingMode)
            {
                case (int)RenderingMode.Forward:
                    renderPathCompatibility = RenderPathCompatibility.Forward;
                    break;
                case (int)RenderingMode.Deferred:
                    renderPathCompatibility = RenderPathCompatibility.Deferred;
                    break;
                case (int)RenderingMode.ForwardPlus:
                    renderPathCompatibility = RenderPathCompatibility.ForwardPlus;
                    break;
                case (int)RenderingMode.DeferredPlus:
                    renderPathCompatibility = RenderPathCompatibility.DeferredPlus;
                    break;
            }

            m_DepthFormatStrings.Clear();
            foreach (DepthFormat format in Enum.GetValues(typeof(DepthFormat)))
            {
                var field = typeof(DepthFormat).GetField(format.ToString());

                if (field.GetCustomAttributes(typeof(RenderPathCompatibleAttribute), false).Length > 0)
                {
                    var attribute = (RenderPathCompatibleAttribute)field.GetCustomAttributes(typeof(RenderPathCompatibleAttribute), false)[0];
                    if (attribute.renderPath.HasFlag(renderPathCompatibility))
                        m_DepthFormatStrings.Add(format.ToString());
                }
            }
        }

        private string[] GetCompatibleDepthFormats(int renderingMode)
        {
            if (m_DepthFormatStrings.Count == 0)
                PopulateCompatibleDepthFormats(renderingMode);
            return m_DepthFormatStrings.ToArray();
        }

        private DepthFormat GetDepthFormatAt(int index, int renderingMode)
        {
            if (m_DepthFormatStrings.Count == 0)
                PopulateCompatibleDepthFormats(renderingMode);

            if (index < 0 || index >= m_DepthFormatStrings.Count)
                return DepthFormat.Default;

            foreach (DepthFormat format in Enum.GetValues(typeof(DepthFormat)))
            {
                if (format.ToString() == m_DepthFormatStrings[index])
                    return format;
            }

            return DepthFormat.Default;
        }

        private int GetDepthFormatIndex(DepthFormat format, int renderingMode)
        {
            if (m_DepthFormatStrings.Count == 0)
                PopulateCompatibleDepthFormats(renderingMode);

            for (int i = 0; i < m_DepthFormatStrings.Count; i++)
            {
                if (m_DepthFormatStrings[i] == format.ToString())
                    return i;
            }

            return 0;
        }

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.FilteringSectionLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            if (GraphicsSettings.TryGetRenderPipelineSettings<RenderGraphSettings>(out var renderGraphSettings)
                && !renderGraphSettings.enableRenderCompatibilityMode)
            {
                EditorGUILayout.PropertyField(m_PrepassLayerMask, Styles.PrepassMask);
            }
            EditorGUILayout.PropertyField(m_OpaqueLayerMask, Styles.OpaqueMask);
            EditorGUILayout.PropertyField(m_TransparentLayerMask, Styles.TransparentMask);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.RenderingSectionLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            int depthFormatIndex = GetDepthFormatIndex((DepthFormat)m_DepthAttachmentFormat.intValue, m_RenderingMode.intValue);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_RenderingMode, Styles.RenderingModeLabel);

            if (EditorGUI.EndChangeCheck())
            {
                PopulateCompatibleDepthFormats(m_RenderingMode.intValue);
                depthFormatIndex = GetDepthFormatIndex((DepthFormat)m_DepthAttachmentFormat.intValue, m_RenderingMode.intValue);
            }

            if (m_RenderingMode.intValue == (int)RenderingMode.DeferredPlus && GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>().enableRenderCompatibilityMode)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(Styles.deferredPlusIncompatibleWarning.text, MessageType.Warning);
                EditorGUI.indentLevel--;
            }

            if (m_RenderingMode.intValue == (int)RenderingMode.Deferred || m_RenderingMode.intValue == (int)RenderingMode.DeferredPlus)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_AccurateGbufferNormals, Styles.accurateGbufferNormalsLabel, true);
                EditorGUI.indentLevel--;
            }

            if (m_RenderingMode.intValue == (int)RenderingMode.Forward || m_RenderingMode.intValue == (int)RenderingMode.ForwardPlus)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_DepthPrimingMode, Styles.DepthPrimingModeLabel);
                if (m_DepthPrimingMode.intValue != (int)DepthPrimingMode.Disabled)
                {
                    if (GraphicsSettings.currentRenderPipeline != null && GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset asset && asset.msaaSampleCount > 1)
                    {
                        EditorGUILayout.HelpBox(Styles.DepthPrimingMSAAWarning.text, MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(Styles.DepthPrimingModeInfo.text, MessageType.Info);
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(m_CopyDepthMode, Styles.CopyDepthModeLabel);

            depthFormatIndex = EditorGUILayout.Popup(Styles.DepthAttachmentFormat, depthFormatIndex, GetCompatibleDepthFormats(m_RenderingMode.intValue));
            m_DepthAttachmentFormat.intValue = (int)GetDepthFormatAt(depthFormatIndex, m_RenderingMode.intValue);

            EditorGUILayout.PropertyField(m_DepthTextureFormat, Styles.DepthTextureFormat);


            EditorGUI.indentLevel--;
            if (renderGraphSettings != null && renderGraphSettings.enableRenderCompatibilityMode)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Styles.RenderPassSectionLabel, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_UseNativeRenderPass, Styles.RenderPassLabel);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.ShadowsSectionLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_ShadowTransparentReceiveProp, Styles.shadowTransparentReceiveLabel);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.PostProcessingSectionLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            var postProcessIncluded = EditorGUILayout.Toggle(Styles.PostProcessIncluded, m_PostProcessData.objectReferenceValue != null);
            if (EditorGUI.EndChangeCheck())
            {
                m_PostProcessData.objectReferenceValue = postProcessIncluded ? PostProcessData.GetDefaultPostProcessData() : null;
            }
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_PostProcessData, Styles.PostProcessLabel);
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.OverridesSectionLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_DefaultStencilState, Styles.defaultStencilStateLabel, true);
            SerializedProperty overrideStencil = m_DefaultStencilState.FindPropertyRelative("overrideStencilState");

            bool usesDeferredLighting = m_RenderingMode.intValue == (int)RenderingMode.Deferred;
            usesDeferredLighting |= m_RenderingMode.intValue == (int)RenderingMode.DeferredPlus;

            if (overrideStencil.boolValue && usesDeferredLighting)
            {
                CompareFunction stencilFunction = (CompareFunction)m_DefaultStencilState.FindPropertyRelative("stencilCompareFunction").enumValueIndex;
                StencilOp stencilPass = (StencilOp)m_DefaultStencilState.FindPropertyRelative("passOperation").enumValueIndex;
                StencilOp stencilFail = (StencilOp)m_DefaultStencilState.FindPropertyRelative("failOperation").enumValueIndex;
                StencilOp stencilZFail = (StencilOp)m_DefaultStencilState.FindPropertyRelative("zFailOperation").enumValueIndex;
                bool invalidFunction = stencilFunction == CompareFunction.Disabled || stencilFunction == CompareFunction.Never;
                bool invalidOp = stencilPass != StencilOp.Replace && stencilFail != StencilOp.Replace && stencilZFail != StencilOp.Replace;

                if (invalidFunction || invalidOp)
                    EditorGUILayout.HelpBox(Styles.invalidStencilOverride.text, MessageType.Error, true);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Compatibility", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            {
                EditorGUILayout.PropertyField(m_IntermediateTextureMode, Styles.intermediateTextureMode);
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI(); // Draw the base UI, contains ScriptableRenderFeatures list
        }
    }
}
