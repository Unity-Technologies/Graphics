using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<SerializedUniversalRenderPipelineAsset>;

    internal partial class UniversalRenderPipelineAssetUI
    {
        enum Expandable
        {
            Rendering = 1 << 1,
            Quality = 1 << 2,
            Lighting = 1 << 3,
            Shadows = 1 << 4,
            PostProcessing = 1 << 5,
#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
            AdaptivePerformance = 1 << 6,
#endif
        }

        enum ExpandableAdditional
        {
            Quality = 1 << 1,
        }

        internal static void RegisterEditor(UniversalRenderPipelineAssetEditor editor)
        {
            k_AdditionalPropertiesState.RegisterEditor(editor);
        }

        internal static void UnregisterEditor(UniversalRenderPipelineAssetEditor editor)
        {
            k_AdditionalPropertiesState.UnregisterEditor(editor);
        }

        [SetAdditionalPropertiesVisibility]
        internal static void SetAdditionalPropertiesVisibility(bool value)
        {
            if (value)
                k_AdditionalPropertiesState.ShowAll();
            else
                k_AdditionalPropertiesState.HideAll();
        }



        static readonly ExpandedState<Expandable, UniversalRenderPipelineAsset> k_ExpandedState = new(Expandable.Rendering, "URP");
        readonly static AdditionalPropertiesState<ExpandableAdditional, Light> k_AdditionalPropertiesState = new(0, "URP");

        public static readonly CED.IDrawer Inspector = CED.Group(
            CED.AdditionalPropertiesFoldoutGroup(Styles.qualitySettingsText, Expandable.Quality, k_ExpandedState, ExpandableAdditional.Quality, k_AdditionalPropertiesState, DrawQuality, DrawQualityAdditional),
            CED.FoldoutGroup(Styles.renderersSettingsText, Expandable.Rendering, k_ExpandedState, FoldoutOption.None, RendererOptionMenu, DrawRenderers)
        //CED.AdditionalPropertiesFoldoutGroup(Styles.lightingSettingsText, Expandable.Lighting, k_ExpandedState, ExpandableAdditional.Lighting, k_AdditionalPropertiesState, DrawLighting, DrawLightingAdditional),
        //CED.AdditionalPropertiesFoldoutGroup(Styles.shadowSettingsText, Expandable.Shadows, k_ExpandedState, ExpandableAdditional.Shadows, k_AdditionalPropertiesState, DrawShadows, DrawShadowsAdditional),
        //CED.AdditionalPropertiesFoldoutGroup(Styles.postProcessingSettingsText, Expandable.PostProcessing, k_ExpandedState, ExpandableAdditional.PostProcessing, k_AdditionalPropertiesState, DrawPostProcessing, DrawPostProcessingAdditional)
#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
            , CED.FoldoutGroup(Styles.adaptivePerformanceText, Expandable.AdaptivePerformance, k_ExpandedState, CED.Group(DrawAdaptivePerformance))
#endif
        );

        static void DrawRenderers(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            if (ownerEditor is UniversalRenderPipelineAssetEditor urpAssetEditor)
            {
                EditorGUILayout.Space();
                urpAssetEditor.rendererList.DoLayoutList();

                if (!serialized.asset.ValidateRendererData(-1))
                    EditorGUILayout.HelpBox(Styles.rendererMissingDefaultMessage.text, MessageType.Error, true);
                else if (!serialized.asset.ValidateRendererDataList(true))
                    EditorGUILayout.HelpBox(Styles.rendererMissingMessage.text, MessageType.Warning, true);
                else if (!ValidateRendererGraphicsAPIs(serialized.asset, out var unsupportedGraphicsApisMessage))
                    EditorGUILayout.HelpBox(Styles.rendererUnsupportedAPIMessage.text + unsupportedGraphicsApisMessage, MessageType.Warning, true);

                //EditorGUILayout.PropertyField(serialized.requireDepthTextureProp, Styles.requireDepthTextureText);
                //EditorGUILayout.PropertyField(serialized.requireOpaqueTextureProp, Styles.requireOpaqueTextureText);
                //EditorGUI.BeginDisabledGroup(!serialized.requireOpaqueTextureProp.boolValue);
                //EditorGUILayout.PropertyField(serialized.opaqueDownsamplingProp, Styles.opaqueDownsamplingText);
                //EditorGUI.EndDisabledGroup();
            }
        }

        static void RendererOptionMenu(Vector2 position)
        {
            GenericMenu menu = new GenericMenu();

            menu.AddItem(EditorGUIUtility.TrTextContent("Copy Settings"), false, () => { });
            menu.AddItem(EditorGUIUtility.TrTextContent("Paste Settings"), false, () => { });

            menu.DropDown(new Rect(position, Vector2.zero));
        }

        static void DrawQuality(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(serialized.hdr, Styles.hdrText);
            bool isHdrOn = serialized.hdr.boolValue;
            if (UniversalRenderPipelineGlobalSettings.instance.postProcessData != null)
            {
                if (!isHdrOn && UniversalRenderPipelineGlobalSettings.instance.colorGradingMode == ColorGradingMode.HighDynamicRange)
                    EditorGUILayout.HelpBox(Styles.colorGradingModeWarning, MessageType.Warning);
                else if (isHdrOn && UniversalRenderPipelineGlobalSettings.instance.colorGradingMode == ColorGradingMode.HighDynamicRange)
                    EditorGUILayout.HelpBox(Styles.colorGradingModeSpecInfo, MessageType.Info);
                if (isHdrOn && UniversalRenderPipelineGlobalSettings.instance.colorGradingMode == ColorGradingMode.HighDynamicRange && UniversalRenderPipelineGlobalSettings.instance.colorGradingLutSize < 32)
                    EditorGUILayout.HelpBox(Styles.colorGradingLutSizeWarning, MessageType.Warning);
            }
            EditorGUILayout.PropertyField(serialized.msaa, Styles.msaaText);
            //serialized.renderScale.floatValue = EditorGUILayout.Slider(Styles.renderScaleText, serialized.renderScale.floatValue, UniversalRenderPipeline.minRenderScale, UniversalRenderPipeline.maxRenderScale);
        }
        static void DrawQualityAdditional(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(serialized.srpBatcher, Styles.srpBatcher);
            EditorGUILayout.PropertyField(serialized.supportsDynamicBatching, Styles.dynamicBatching);
        }

        static bool ValidateRendererGraphicsAPIs(UniversalRenderPipelineAsset pipelineAsset, out string unsupportedGraphicsApisMessage)
        {
            // Check the list of Renderers against all Graphics APIs the player is built with.
            unsupportedGraphicsApisMessage = null;

            BuildTarget platform = EditorUserBuildSettings.activeBuildTarget;
            GraphicsDeviceType[] graphicsAPIs = PlayerSettings.GetGraphicsAPIs(platform);
            int rendererCount = pipelineAsset.m_RendererDataList.Length;

            for (int i = 0; i < rendererCount; i++)
            {
                ScriptableRenderer renderer = pipelineAsset.GetRenderer(i);
                if (renderer == null)
                    continue;

                GraphicsDeviceType[] unsupportedAPIs = renderer.unsupportedGraphicsDeviceTypes;

                for (int apiIndex = 0; apiIndex < unsupportedAPIs.Length; apiIndex++)
                {
                    if (System.Array.FindIndex(graphicsAPIs, element => element == unsupportedAPIs[apiIndex]) >= 0)
                        unsupportedGraphicsApisMessage += System.String.Format("{0} at index {1} does not support {2}.\n", renderer, i, unsupportedAPIs[apiIndex]);
                }
            }

            return unsupportedGraphicsApisMessage == null;
        }


#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
        static void DrawAdaptivePerformance(SerializedUniversalRenderPipelineAsset serialized, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(serialized.useAdaptivePerformance, Styles.useAdaptivePerformance);
        }
#endif
    }
}
