using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<SerializedUniversalRendererData>;

    enum ShowUIUniversalRendererData
    {
        General = 1 << 0,
        Lighting = 1 << 1,
        Shadow = 1 << 2,
        RendererFeatures = 1 << 3,
    }
    enum ShowAdditionalUIUniversalRendererData
    {
        Show = 1 << 0,
    }

    class SerializedUniversalRendererData
    {
        public SerializedProperty opaqueLayerMask;
        public SerializedProperty transparentLayerMask;
        public SerializedProperty renderingMode;
        public SerializedProperty depthPrimingMode;
        public SerializedProperty copyDepthMode;
        public SerializedProperty accurateGbufferNormals;
        public SerializedProperty clusteredRendering;
        public SerializedProperty tileSize;
        public SerializedProperty defaultStencilState;
        public SerializedProperty shaders;
        public SerializedProperty shadowTransparentReceiveProp;
        public SerializedProperty intermediateTextureMode;

        public SerializedProperty mainLightRenderingModeProp { get; }
        public SerializedProperty mainLightShadowsSupportedProp { get; }
        public SerializedProperty mainLightShadowmapResolutionProp { get; }
        public SerializedProperty additionalLightsRenderingModeProp { get; }
        public SerializedProperty additionalLightsPerObjectLimitProp { get; }
        public SerializedProperty additionalLightShadowsSupportedProp { get; }
        public SerializedProperty additionalLightShadowmapResolutionProp { get; }
        public SerializedProperty additionalLightsShadowResolutionTierLowProp { get; }
        public SerializedProperty additionalLightsShadowResolutionTierMediumProp { get; }
        public SerializedProperty additionalLightsShadowResolutionTierHighProp { get; }
        public SerializedProperty additionalLightCookieResolutionProp { get; }
        public SerializedProperty additionalLightCookieFormatProp { get; }
        public SerializedProperty reflectionProbeBlendingProp { get; }
        public SerializedProperty reflectionProbeBoxProjectionProp { get; }
        public SerializedProperty shadowDistanceProp { get; }
        public SerializedProperty shadowCascadeCountProp { get; }
        public SerializedProperty shadowCascade2SplitProp { get; }
        public SerializedProperty shadowCascade3SplitProp { get; }
        public SerializedProperty shadowCascade4SplitProp { get; }
        public SerializedProperty shadowCascadeBorderProp { get; }
        public SerializedProperty shadowDepthBiasProp { get; }
        public SerializedProperty shadowNormalBiasProp { get; }
        public SerializedProperty softShadowsSupportedProp { get; }
        public SerializedProperty conservativeEnclosingSphereProp { get; }

        public SerializedProperty mixedLightingSupportedProp { get; }
        public SerializedProperty supportsLightLayers { get; }


#if URP_ENABLE_CLUSTERED_UI
            public static bool s_EnableClusteredUI => true;
#else
        public static bool s_EnableClusteredUI => false;
#endif
        public EditorPrefBoolFlags<EditorUtils.Unit> state;


        public ScriptableRendererFeatureEditor rendererFeatureEditor;
        public Editor ownerEditor;
        public SerializedObject serializedObject;

        public ExpandedState<ShowUIUniversalRendererData, UniversalRendererData> k_showUI { get; }
        public AdditionalPropertiesState<ShowAdditionalUIUniversalRendererData, UniversalRendererData> k_showUIAdditional { get; }

        public SerializedUniversalRendererData(Editor ownerEditor, int index)
        {
            this.ownerEditor = ownerEditor;
            serializedObject = ownerEditor.serializedObject;

            opaqueLayerMask = serializedObject.FindProperty("m_OpaqueLayerMask");
            transparentLayerMask = serializedObject.FindProperty("m_TransparentLayerMask");
            renderingMode = serializedObject.FindProperty("m_RenderingMode");
            depthPrimingMode = serializedObject.FindProperty("m_DepthPrimingMode");
            copyDepthMode = serializedObject.FindProperty("m_CopyDepthMode");
            accurateGbufferNormals = serializedObject.FindProperty("m_AccurateGbufferNormals");
            clusteredRendering = serializedObject.FindProperty("m_ClusteredRendering");
            tileSize = serializedObject.FindProperty("m_TileSize");
            defaultStencilState = serializedObject.FindProperty("m_DefaultStencilState");
            shaders = serializedObject.FindProperty("shaders");
            shadowTransparentReceiveProp = serializedObject.FindProperty("m_ShadowTransparentReceive");
            intermediateTextureMode = serializedObject.FindProperty("m_IntermediateTextureMode");

            mainLightRenderingModeProp = serializedObject.FindProperty("m_MainLightRenderingMode");
            mainLightShadowsSupportedProp = serializedObject.FindProperty("m_MainLightShadowsSupported");
            mainLightShadowmapResolutionProp = serializedObject.FindProperty("m_MainLightShadowmapResolution");

            additionalLightsRenderingModeProp = serializedObject.FindProperty("m_AdditionalLightsRenderingMode");
            additionalLightsPerObjectLimitProp = serializedObject.FindProperty("m_AdditionalLightsPerObjectLimit");
            additionalLightShadowsSupportedProp = serializedObject.FindProperty("m_AdditionalLightShadowsSupported");
            additionalLightShadowmapResolutionProp = serializedObject.FindProperty("m_AdditionalLightsShadowmapResolution");

            additionalLightsShadowResolutionTierLowProp = serializedObject.FindProperty("m_AdditionalLightsShadowResolutionTierLow");
            additionalLightsShadowResolutionTierMediumProp = serializedObject.FindProperty("m_AdditionalLightsShadowResolutionTierMedium");
            additionalLightsShadowResolutionTierHighProp = serializedObject.FindProperty("m_AdditionalLightsShadowResolutionTierHigh");

            additionalLightCookieResolutionProp = serializedObject.FindProperty("m_AdditionalLightsCookieResolution");
            additionalLightCookieFormatProp = serializedObject.FindProperty("m_AdditionalLightsCookieFormat");

            reflectionProbeBlendingProp = serializedObject.FindProperty("m_ReflectionProbeBlending");
            reflectionProbeBoxProjectionProp = serializedObject.FindProperty("m_ReflectionProbeBoxProjection");

            shadowDistanceProp = serializedObject.FindProperty("m_ShadowDistance");

            shadowCascadeCountProp = serializedObject.FindProperty("m_ShadowCascadeCount");
            shadowCascade2SplitProp = serializedObject.FindProperty("m_Cascade2Split");
            shadowCascade3SplitProp = serializedObject.FindProperty("m_Cascade3Split");
            shadowCascade4SplitProp = serializedObject.FindProperty("m_Cascade4Split");
            shadowCascadeBorderProp = serializedObject.FindProperty("m_CascadeBorder");
            shadowDepthBiasProp = serializedObject.FindProperty("m_ShadowDepthBias");
            shadowNormalBiasProp = serializedObject.FindProperty("m_ShadowNormalBias");
            softShadowsSupportedProp = serializedObject.FindProperty("m_SoftShadowsSupported");
            conservativeEnclosingSphereProp = serializedObject.FindProperty("m_ConservativeEnclosingSphere");


            mixedLightingSupportedProp = serializedObject.FindProperty("m_MixedLightingSupported");
            supportsLightLayers = serializedObject.FindProperty("m_SupportsLightLayers");

            string Key = "Universal_Shadow_Setting_Unit:UI_State";
            state = new EditorPrefBoolFlags<EditorUtils.Unit>(Key);
            k_showUI = new(ShowUIUniversalRendererData.General, $"{index}_URP");
            k_showUIAdditional = new(0, $"{index}_URP");

            rendererFeatureEditor = new ScriptableRendererFeatureEditor(ownerEditor);
        }
    }

    [CustomEditor(typeof(UniversalRendererData), true)]
    public class UniversalRendererDataEditor : Editor
    {
        internal static class Styles
        {
            public static GUIContent generalSettingsText = EditorGUIUtility.TrTextContent("General", "Settings that affect the renderer");
            public static GUIContent lightingSettingsText = EditorGUIUtility.TrTextContent("Lighting", "Settings that affect the lighting in the Scene");
            public static GUIContent shadowSettingsText = EditorGUIUtility.TrTextContent("Shadows", "Settings that configure how shadows look and behave, and can be used to balance between the visual quality and performance of shadows.");
            public static GUIContent rendererFeatureSettingsText = EditorGUIUtility.TrTextContent("Renderer Features", "Settings that configure the renderer features used by the renderer.");


            public static readonly GUIContent RendererTitle = EditorGUIUtility.TrTextContent("Universal Renderer", "Custom Universal Renderer for Universal RP.");
            public static readonly GUIContent FilteringSectionLabel = EditorGUIUtility.TrTextContent("Filtering", "Settings that controls and define which layers the renderer draws.");
            public static readonly GUIContent OpaqueMask = EditorGUIUtility.TrTextContent("Opaque Layer Mask", "Controls which opaque layers this renderer draws.");
            public static readonly GUIContent TransparentMask = EditorGUIUtility.TrTextContent("Transparent Layer Mask", "Controls which transparent layers this renderer draws.");

            public static readonly GUIContent RenderingSectionLabel = EditorGUIUtility.TrTextContent("Rendering", "Settings related to rendering and lighting.");
            public static readonly GUIContent RenderingModeLabel = EditorGUIUtility.TrTextContent("Rendering Path", "Select a rendering path.");
            public static readonly GUIContent DepthPrimingModeLabel = EditorGUIUtility.TrTextContent("Depth Priming Mode", "With depth priming enabled, Unity uses the depth buffer generated in the depth prepass to determine if a fragment should be rendered or skipped during the Base Camera opaque pass. Disabled: Unity does not perform depth priming. Auto: If there is a Render Pass that requires a depth prepass, Unity performs the depth prepass and depth priming. Forced: Unity performs the depth prepass and depth priming.");
            public static readonly GUIContent DepthPrimingModeInfo = EditorGUIUtility.TrTextContent("On Android, iOS, and Apple TV, Unity performs depth priming only in the Forced mode. On tiled GPUs, which are common to those platforms, depth priming might reduce performance when combined with MSAA.");
            public static readonly GUIContent CopyDepthModeLabel = EditorGUIUtility.TrTextContent("Copy Depth Mode", "Controls after which pass URP copies the scene depth. It has a significant impact on mobile devices bandwidth usage.");
            public static readonly GUIContent RenderPassLabel = EditorGUIUtility.TrTextContent("Native RenderPass", "Enables URP to use RenderPass API. Has no effect on OpenGLES2");

            public static readonly GUIContent ShadowsSectionLabel = EditorGUIUtility.TrTextContent("Shadows", "This section contains properties related to rendering shadows.");

            public static readonly GUIContent OverridesSectionLabel = EditorGUIUtility.TrTextContent("Overrides", "This section contains Render Pipeline properties that this Renderer overrides.");

            public static readonly GUIContent accurateGbufferNormalsLabel = EditorGUIUtility.TrTextContent("Accurate G-buffer normals", "Normals in G-buffer use octahedron encoding/decoding. This improves visual quality but might reduce performance.");
            public static readonly GUIContent defaultStencilStateLabel = EditorGUIUtility.TrTextContent("Default Stencil State", "Configure the stencil state for the opaque and transparent render passes.");
            public static readonly GUIContent shadowTransparentReceiveLabel = EditorGUIUtility.TrTextContent("Transparent Receive Shadows", "When disabled, none of the transparent objects will receive shadows.");
            public static readonly GUIContent invalidStencilOverride = EditorGUIUtility.TrTextContent("Error: When using the deferred rendering path, the Renderer requires the control over the 4 highest bits of the stencil buffer to store Material types. The current combination of the stencil override options prevents the Renderer from controlling the required bits. Try changing one of the options to Replace.");
            public static readonly GUIContent clusteredRenderingLabel = EditorGUIUtility.TrTextContent("Clustered (experimental)", "(Experimental) Enables clustered rendering, allowing for more lights per object and more accurate light cullling.");
            public static readonly GUIContent intermediateTextureMode = EditorGUIUtility.TrTextContent("Intermediate Texture", "Controls when URP renders via an intermediate texture.");

            // Main light
            public static string[] mainLightOptions = { "Disabled", "Per Pixel" };
            public static GUIContent mainLightRenderingModeText = EditorGUIUtility.TrTextContent("Main Light", "Main light is the brightest directional light.");
            public static GUIContent supportsMainLightShadowsText = EditorGUIUtility.TrTextContent("Cast Shadows", "If enabled the main light can be a shadow casting light.");
            public static GUIContent mainLightShadowmapResolutionText = EditorGUIUtility.TrTextContent("Shadow Resolution", "Resolution of the main light shadowmap texture. If cascades are enabled, cascades will be packed into an atlas and this setting controls the maximum shadows atlas resolution.");

            // Additional lights
            public static GUIContent addditionalLightsRenderingModeText = EditorGUIUtility.TrTextContent("Additional Lights", "Additional lights support.");
            public static GUIContent perObjectLimit = EditorGUIUtility.TrTextContent("Per Object Limit", "Maximum amount of additional lights. These lights are sorted and culled per-object.");
            public static GUIContent supportsAdditionalShadowsText = EditorGUIUtility.TrTextContent("Cast Shadows", "If enabled shadows will be supported for spot lights.\n");
            public static GUIContent additionalLightsShadowmapResolution = EditorGUIUtility.TrTextContent("Shadow Atlas Resolution", "All additional lights are packed into a single shadowmap atlas. This setting controls the atlas size.");
            public static GUIContent additionalLightsShadowResolutionTiers = EditorGUIUtility.TrTextContent("Shadow Resolution Tiers", $"Additional Lights Shadow Resolution Tiers. Rounded to the next power of two, and clamped to be at least {UniversalAdditionalLightData.AdditionalLightsShadowMinimumResolution}.");
            public static GUIContent[] additionalLightsShadowResolutionTierNames =
            {
                new GUIContent("Low"),
                new GUIContent("Medium"),
                new GUIContent("High")
            };
            public static GUIContent additionalLightsCookieResolution = EditorGUIUtility.TrTextContent("Cookie Atlas Resolution", "All additional lights are packed into a single cookie atlas. This setting controls the atlas size.");
            public static GUIContent additionalLightsCookieFormat = EditorGUIUtility.TrTextContent("Cookie Atlas Format", "All additional lights are packed into a single cookie atlas. This setting controls the atlas format.");

            // Reflection Probes
            public static GUIContent reflectionProbesSettingsText = EditorGUIUtility.TrTextContent("Reflection Probes");
            public static GUIContent reflectionProbeBlendingText = EditorGUIUtility.TrTextContent("Probe Blending", "If enabled smooth transitions will be created between reflection probes.");
            public static GUIContent reflectionProbeBoxProjectionText = EditorGUIUtility.TrTextContent("Box Projection", "If enabled reflections appear based on the object’s position within the probe’s box, while still using a single probe as the source of the reflection.");

            // Additional lighting settings
            public static GUIContent mixedLightingSupportLabel = EditorGUIUtility.TrTextContent("Mixed Lighting", "Makes the render pipeline include mixed-lighting Shader Variants in the build.");
            public static GUIContent supportsLightLayers = EditorGUIUtility.TrTextContent("Light Layers", "When enabled, UniversalRP uses rendering layers instead of culling mask for the purpose of selecting how lights affect groups of geometry. For deferred rendering, an extra render target is allocated.");

            public static GUIContent lightlayersUnsupportedMessage =
                EditorGUIUtility.TrTextContent("Some Graphics API(s) in the Player Graphics APIs list are incompatible with Light Layers.  Switching to these Graphics APIs at runtime can cause issues: ");

            // Shadow settings
            public static GUIContent shadowWorkingUnitText = EditorGUIUtility.TrTextContent("Working Unit", "The unit in which Unity measures the shadow cascade distances. The exception is Max Distance, which will still be in meters.");
            public static GUIContent shadowDistanceText = EditorGUIUtility.TrTextContent("Max Distance", "Maximum shadow rendering distance.");
            public static GUIContent shadowCascadesText = EditorGUIUtility.TrTextContent("Cascade Count", "Number of cascade splits used for directional shadows.");
            public static GUIContent shadowDepthBias = EditorGUIUtility.TrTextContent("Depth Bias", "Controls the distance at which the shadows will be pushed away from the light. Useful for avoiding false self-shadowing artifacts.");
            public static GUIContent shadowNormalBias = EditorGUIUtility.TrTextContent("Normal Bias", "Controls distance at which the shadow casting surfaces will be shrunk along the surface normal. Useful for avoiding false self-shadowing artifacts.");
            public static GUIContent supportsSoftShadows = EditorGUIUtility.TrTextContent("Soft Shadows", "If enabled pipeline will perform shadow filtering. Otherwise all lights that cast shadows will fallback to perform a single shadow sample.");
            public static GUIContent conservativeEnclosingSphere = EditorGUIUtility.TrTextContent("Conservative Enclosing Sphere", "Enable this option to improve shadow frustum culling and prevent Unity from excessively culling shadows in the corners of the shadow cascades. Disable this option only for compatibility purposes of existing projects created in previous Unity versions.");
        }


        SerializedUniversalRendererData serialized;
        private void OnEnable()
        {
            serialized = new SerializedUniversalRendererData(this, 0);
        }



        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            CED.Group(
                CED.FoldoutGroup(Styles.generalSettingsText,
                    ShowUIUniversalRendererData.General, serialized.k_showUI,
                    FoldoutOption.SubFoldout, DrawGeneral),
                CED.AdditionalPropertiesFoldoutGroup(Styles.lightingSettingsText,
                    ShowUIUniversalRendererData.Lighting, serialized.k_showUI,
                    (ShowAdditionalUIUniversalRendererData)0, serialized.k_showUIAdditional,
                    DrawLighting, DrawLightingAdditional, FoldoutOption.SubFoldout),
                CED.AdditionalPropertiesFoldoutGroup(Styles.shadowSettingsText,
                    ShowUIUniversalRendererData.Shadow, serialized.k_showUI,
                    (ShowAdditionalUIUniversalRendererData)0, serialized.k_showUIAdditional,
                    DrawShadows, DrawShadowsAdditional, FoldoutOption.SubFoldout),
                CED.FoldoutGroup(Styles.rendererFeatureSettingsText,
                    ShowUIUniversalRendererData.RendererFeatures, serialized.k_showUI,
                    FoldoutOption.SubFoldout, DrawRendererFeatures)
            ).Draw(serialized, this);

            // Add a "Reload All" button in inspector when we are in developer's mode
            if (EditorPrefs.GetBool("DeveloperMode"))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serialized.shaders, true);

                if (GUILayout.Button("Reload All"))
                {
                    var resources = target as UniversalRendererData;
                    resources.shaders = null;
                    ResourceReloader.ReloadAllNullIn(target, UniversalRenderPipelineAsset.packagePath);
                }
            }
            serializedObject.ApplyModifiedProperties();

        }

        static void DrawGeneral(SerializedUniversalRendererData serialized, Editor ownerEditor)
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.FilteringSectionLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serialized.opaqueLayerMask, Styles.OpaqueMask);
            EditorGUILayout.PropertyField(serialized.transparentLayerMask, Styles.TransparentMask);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.RenderingSectionLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serialized.renderingMode, Styles.RenderingModeLabel);
            if (serialized.renderingMode.intValue == (int)RenderingMode.Deferred)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serialized.accurateGbufferNormals, Styles.accurateGbufferNormalsLabel, true);
                EditorGUI.indentLevel--;
            }

            if (serialized.renderingMode.intValue == (int)RenderingMode.Forward)
            {
                EditorGUI.indentLevel++;

                if (SerializedUniversalRendererData.s_EnableClusteredUI)
                {
                    EditorGUILayout.PropertyField(serialized.clusteredRendering, Styles.clusteredRenderingLabel);
                    EditorGUI.BeginDisabledGroup(!serialized.clusteredRendering.boolValue);
                    EditorGUILayout.PropertyField(serialized.tileSize);
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.PropertyField(serialized.depthPrimingMode, Styles.DepthPrimingModeLabel);
                if (serialized.depthPrimingMode.intValue != (int)DepthPrimingMode.Disabled)
                {
                    EditorGUILayout.HelpBox(Styles.DepthPrimingModeInfo.text, MessageType.Info);
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(serialized.copyDepthMode, Styles.CopyDepthModeLabel);
            EditorGUI.indentLevel--;



            EditorGUILayout.LabelField(Styles.OverridesSectionLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serialized.defaultStencilState, Styles.defaultStencilStateLabel, true);
            SerializedProperty overrideStencil = serialized.defaultStencilState.FindPropertyRelative("overrideStencilState");

            if (overrideStencil.boolValue && serialized.renderingMode.intValue == (int)RenderingMode.Deferred)
            {
                CompareFunction stencilFunction = (CompareFunction)serialized.defaultStencilState.FindPropertyRelative("stencilCompareFunction").enumValueIndex;
                StencilOp stencilPass = (StencilOp)serialized.defaultStencilState.FindPropertyRelative("passOperation").enumValueIndex;
                StencilOp stencilFail = (StencilOp)serialized.defaultStencilState.FindPropertyRelative("failOperation").enumValueIndex;
                StencilOp stencilZFail = (StencilOp)serialized.defaultStencilState.FindPropertyRelative("zFailOperation").enumValueIndex;
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
                EditorGUILayout.PropertyField(serialized.intermediateTextureMode, Styles.intermediateTextureMode);
            }
            EditorGUI.indentLevel--;
        }
        static void DrawLighting(SerializedUniversalRendererData serialized, Editor ownerEditor)
        {
            // Main Light
            bool disableGroup = false;
            EditorGUI.BeginDisabledGroup(disableGroup);
            CoreEditorUtils.DrawPopup(Styles.mainLightRenderingModeText, serialized.mainLightRenderingModeProp, Styles.mainLightOptions);
            EditorGUI.EndDisabledGroup();

            EditorGUI.indentLevel++;
            disableGroup |= !serialized.mainLightRenderingModeProp.boolValue;

            EditorGUI.BeginDisabledGroup(disableGroup);
            EditorGUILayout.PropertyField(serialized.mainLightShadowsSupportedProp, Styles.supportsMainLightShadowsText);
            EditorGUI.EndDisabledGroup();

            disableGroup |= !serialized.mainLightShadowsSupportedProp.boolValue;
            EditorGUI.BeginDisabledGroup(disableGroup);
            EditorGUILayout.PropertyField(serialized.mainLightShadowmapResolutionProp, Styles.mainLightShadowmapResolutionText);
            EditorGUI.EndDisabledGroup();

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            // Additional light
            EditorGUILayout.PropertyField(serialized.additionalLightsRenderingModeProp, Styles.addditionalLightsRenderingModeText);
            EditorGUI.indentLevel++;

            disableGroup = serialized.additionalLightsRenderingModeProp.intValue == (int)LightRenderingMode.Disabled;
            EditorGUI.BeginDisabledGroup(disableGroup);
            serialized.additionalLightsPerObjectLimitProp.intValue = EditorGUILayout.IntSlider(Styles.perObjectLimit, serialized.additionalLightsPerObjectLimitProp.intValue, 0, UniversalRenderPipeline.maxPerObjectLights);
            EditorGUI.EndDisabledGroup();

            disableGroup |= (serialized.additionalLightsPerObjectLimitProp.intValue == 0 || serialized.additionalLightsRenderingModeProp.intValue != (int)LightRenderingMode.PerPixel);
            EditorGUI.BeginDisabledGroup(disableGroup);
            EditorGUILayout.PropertyField(serialized.additionalLightShadowsSupportedProp, Styles.supportsAdditionalShadowsText);
            EditorGUI.EndDisabledGroup();

            disableGroup |= !serialized.additionalLightShadowsSupportedProp.boolValue;
            EditorGUI.BeginDisabledGroup(disableGroup);
            EditorGUILayout.PropertyField(serialized.additionalLightShadowmapResolutionProp, Styles.additionalLightsShadowmapResolution);
            DrawShadowResolutionTierSettings(serialized, ownerEditor);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            disableGroup = serialized.additionalLightsRenderingModeProp.intValue == (int)LightRenderingMode.Disabled;

            EditorGUI.BeginDisabledGroup(disableGroup);
            EditorGUILayout.PropertyField(serialized.additionalLightCookieResolutionProp, Styles.additionalLightsCookieResolution);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(disableGroup);
            EditorGUILayout.PropertyField(serialized.additionalLightCookieFormatProp, Styles.additionalLightsCookieFormat);
            EditorGUI.EndDisabledGroup();

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            // Reflection Probes
            EditorGUILayout.LabelField(Styles.reflectionProbesSettingsText);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serialized.reflectionProbeBlendingProp, Styles.reflectionProbeBlendingText);
            EditorGUILayout.PropertyField(serialized.reflectionProbeBoxProjectionProp, Styles.reflectionProbeBoxProjectionText);
            EditorGUI.indentLevel--;
        }

        static void DrawLightingAdditional(SerializedUniversalRendererData serialized, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(serialized.mixedLightingSupportedProp, Styles.mixedLightingSupportLabel);
            EditorGUILayout.PropertyField(serialized.supportsLightLayers, Styles.supportsLightLayers);

            if (serialized.supportsLightLayers.boolValue && !ValidateRendererGraphicsAPIsForLightLayers(out var unsupportedGraphicsApisMessage))
                EditorGUILayout.HelpBox(Styles.lightlayersUnsupportedMessage.text + unsupportedGraphicsApisMessage, MessageType.Warning, true);
        }

        static void DrawShadowResolutionTierSettings(SerializedUniversalRendererData serialized, Editor ownerEditor)
        {
            // UI code adapted from HDRP U.I logic implemented in com.unity.render-pipelines.high-definition/Editor/RenderPipeline/Settings/SerializedScalableSetting.cs )

            var rect = GUILayoutUtility.GetRect(0, float.Epsilon, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight);
            var contentRect = EditorGUI.PrefixLabel(rect, Styles.additionalLightsShadowResolutionTiers);

            EditorGUI.BeginChangeCheck();

            const int k_ShadowResolutionTiersCount = 3;
            var values = new[] { serialized.additionalLightsShadowResolutionTierLowProp, serialized.additionalLightsShadowResolutionTierMediumProp, serialized.additionalLightsShadowResolutionTierHighProp };

            var num = contentRect.width / (float)k_ShadowResolutionTiersCount;  // space allocated for every field including the label

            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0; // Reset the indentation

            float pixelShift = 0;  // Variable to keep track of the current pixel shift in the rectangle we were assigned for this whole section.
            for (var index = 0; index < k_ShadowResolutionTiersCount; ++index)
            {
                var labelWidth = Mathf.Clamp(EditorStyles.label.CalcSize(Styles.additionalLightsShadowResolutionTierNames[index]).x, 0, num);
                EditorGUI.LabelField(new Rect(contentRect.x + pixelShift, contentRect.y, labelWidth, contentRect.height), Styles.additionalLightsShadowResolutionTierNames[index]);
                pixelShift += labelWidth;           // We need to remove from the position the label size that we've just drawn and shift by it's length
                float spaceLeft = num - labelWidth; // The amount of space left for the field
                if (spaceLeft > 2) // If at least two pixels are left to draw this field, draw it, otherwise, skip
                {
                    var fieldSlot = new Rect(contentRect.x + pixelShift, contentRect.y, num - labelWidth, contentRect.height); // Define the rectangle for the field
                    int value = EditorGUI.DelayedIntField(fieldSlot, values[index].intValue);
                    values[index].intValue = Mathf.Max(UniversalAdditionalLightData.AdditionalLightsShadowMinimumResolution, Mathf.NextPowerOfTwo(value));
                }
                pixelShift += spaceLeft;  // Shift by the slot that was left for the field
            }

            EditorGUI.indentLevel = indentLevel;

            EditorGUI.EndChangeCheck();
        }

        static void DrawShadows(SerializedUniversalRendererData serialized, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(serialized.shadowTransparentReceiveProp, Styles.shadowTransparentReceiveLabel);
            serialized.shadowDistanceProp.floatValue = Mathf.Max(0.0f, EditorGUILayout.FloatField(Styles.shadowDistanceText, serialized.shadowDistanceProp.floatValue));
            EditorUtils.Unit unit = EditorUtils.Unit.Metric;
            if (serialized.shadowCascadeCountProp.intValue != 0)
            {
                EditorGUI.BeginChangeCheck();
                unit = (EditorUtils.Unit)EditorGUILayout.EnumPopup(Styles.shadowWorkingUnitText, serialized.state.value);
                if (EditorGUI.EndChangeCheck())
                {
                    serialized.state.value = unit;
                }
            }

            EditorGUILayout.IntSlider(serialized.shadowCascadeCountProp, UniversalRendererData.k_ShadowCascadeMinCount, UniversalRendererData.k_ShadowCascadeMaxCount, Styles.shadowCascadesText);

            int cascadeCount = serialized.shadowCascadeCountProp.intValue;
            EditorGUI.indentLevel++;

            bool useMetric = unit == EditorUtils.Unit.Metric;
            float baseMetric = serialized.shadowDistanceProp.floatValue;
            int cascadeSplitCount = cascadeCount - 1;

            DrawCascadeSliders(serialized, cascadeSplitCount, useMetric, baseMetric);

            EditorGUI.indentLevel--;
            DrawCascades(serialized, cascadeCount, useMetric, baseMetric);
            EditorGUI.indentLevel++;

            serialized.shadowDepthBiasProp.floatValue = EditorGUILayout.Slider(Styles.shadowDepthBias, serialized.shadowDepthBiasProp.floatValue, 0.0f, UniversalRenderPipeline.maxShadowBias);
            serialized.shadowNormalBiasProp.floatValue = EditorGUILayout.Slider(Styles.shadowNormalBias, serialized.shadowNormalBiasProp.floatValue, 0.0f, UniversalRenderPipeline.maxShadowBias);
            EditorGUILayout.PropertyField(serialized.softShadowsSupportedProp, Styles.supportsSoftShadows);

            EditorGUI.indentLevel--;
        }

        static void DrawShadowsAdditional(SerializedUniversalRendererData serialized, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(serialized.conservativeEnclosingSphereProp, Styles.conservativeEnclosingSphere);
        }


        static void DrawCascadeSliders(SerializedUniversalRendererData serialized, int splitCount, bool useMetric, float baseMetric)
        {
            Vector4 shadowCascadeSplit = Vector4.one;
            if (splitCount == 3)
                shadowCascadeSplit = new Vector4(serialized.shadowCascade4SplitProp.vector3Value.x, serialized.shadowCascade4SplitProp.vector3Value.y, serialized.shadowCascade4SplitProp.vector3Value.z, 1);
            else if (splitCount == 2)
                shadowCascadeSplit = new Vector4(serialized.shadowCascade3SplitProp.vector2Value.x, serialized.shadowCascade3SplitProp.vector2Value.y, 1, 0);
            else if (splitCount == 1)
                shadowCascadeSplit = new Vector4(serialized.shadowCascade2SplitProp.floatValue, 1, 0, 0);

            float splitBias = 0.001f;
            float invBaseMetric = baseMetric == 0 ? 0 : 1f / baseMetric;

            // Ensure correct split order
            shadowCascadeSplit[0] = Mathf.Clamp(shadowCascadeSplit[0], 0f, shadowCascadeSplit[1] - splitBias);
            shadowCascadeSplit[1] = Mathf.Clamp(shadowCascadeSplit[1], shadowCascadeSplit[0] + splitBias, shadowCascadeSplit[2] - splitBias);
            shadowCascadeSplit[2] = Mathf.Clamp(shadowCascadeSplit[2], shadowCascadeSplit[1] + splitBias, shadowCascadeSplit[3] - splitBias);


            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < splitCount; ++i)
            {
                float value = shadowCascadeSplit[i];

                float minimum = i == 0 ? 0 : shadowCascadeSplit[i - 1] + splitBias;
                float maximum = i == splitCount - 1 ? 1 : shadowCascadeSplit[i + 1] - splitBias;

                if (useMetric)
                {
                    float valueMetric = value * baseMetric;
                    valueMetric = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent($"Split {i + 1}", "The distance where this cascade ends and the next one starts."), valueMetric, 0f, baseMetric, null);

                    shadowCascadeSplit[i] = Mathf.Clamp(valueMetric * invBaseMetric, minimum, maximum);
                }
                else
                {
                    float valueProcentage = value * 100f;
                    valueProcentage = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent($"Split {i + 1}", "The distance where this cascade ends and the next one starts."), valueProcentage, 0f, 100f, null);

                    shadowCascadeSplit[i] = Mathf.Clamp(valueProcentage * 0.01f, minimum, maximum);
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                switch (splitCount)
                {
                    case 3:
                        serialized.shadowCascade4SplitProp.vector3Value = shadowCascadeSplit;
                        break;
                    case 2:
                        serialized.shadowCascade3SplitProp.vector2Value = shadowCascadeSplit;
                        break;
                    case 1:
                        serialized.shadowCascade2SplitProp.floatValue = shadowCascadeSplit.x;
                        break;
                }
            }

            var borderValue = serialized.shadowCascadeBorderProp.floatValue;

            EditorGUI.BeginChangeCheck();
            if (useMetric)
            {
                var lastCascadeSplitSize = splitCount == 0 ? baseMetric : (1.0f - shadowCascadeSplit[splitCount - 1]) * baseMetric;
                var invLastCascadeSplitSize = lastCascadeSplitSize == 0 ? 0 : 1f / lastCascadeSplitSize;
                float valueMetric = borderValue * lastCascadeSplitSize;
                valueMetric = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent("Last Border", "The distance of the last cascade."), valueMetric, 0f, lastCascadeSplitSize, null);

                borderValue = valueMetric * invLastCascadeSplitSize;
            }
            else
            {
                float valueProcentage = borderValue * 100f;
                valueProcentage = EditorGUILayout.Slider(EditorGUIUtility.TrTextContent("Last Border", "The distance of the last cascade."), valueProcentage, 0f, 100f, null);

                borderValue = valueProcentage * 0.01f;
            }

            if (EditorGUI.EndChangeCheck())
            {
                serialized.shadowCascadeBorderProp.floatValue = borderValue;
            }
        }

        static void DrawCascades(SerializedUniversalRendererData serialized, int cascadeCount, bool useMetric, float baseMetric)
        {
            var cascades = new ShadowCascadeGUI.Cascade[cascadeCount];

            Vector3 shadowCascadeSplit = Vector3.zero;
            if (cascadeCount == 4)
                shadowCascadeSplit = serialized.shadowCascade4SplitProp.vector3Value;
            else if (cascadeCount == 3)
                shadowCascadeSplit = serialized.shadowCascade3SplitProp.vector2Value;
            else if (cascadeCount == 2)
                shadowCascadeSplit.x = serialized.shadowCascade2SplitProp.floatValue;
            else
                shadowCascadeSplit.x = serialized.shadowCascade2SplitProp.floatValue;

            float lastCascadePartitionSplit = 0;
            for (int i = 0; i < cascadeCount - 1; ++i)
            {
                cascades[i] = new ShadowCascadeGUI.Cascade()
                {
                    size = i == 0 ? shadowCascadeSplit[i] : shadowCascadeSplit[i] - lastCascadePartitionSplit, // Calculate the size of cascade
                    borderSize = 0,
                    cascadeHandleState = ShadowCascadeGUI.HandleState.Enabled,
                    borderHandleState = ShadowCascadeGUI.HandleState.Hidden,
                };
                lastCascadePartitionSplit = shadowCascadeSplit[i];
            }

            // Last cascade is special
            var lastCascade = cascadeCount - 1;
            cascades[lastCascade] = new ShadowCascadeGUI.Cascade()
            {
                size = lastCascade == 0 ? 1.0f : 1 - shadowCascadeSplit[lastCascade - 1], // Calculate the size of cascade
                borderSize = serialized.shadowCascadeBorderProp.floatValue,
                cascadeHandleState = ShadowCascadeGUI.HandleState.Hidden,
                borderHandleState = ShadowCascadeGUI.HandleState.Enabled,
            };

            EditorGUI.BeginChangeCheck();
            ShadowCascadeGUI.DrawCascades(ref cascades, useMetric, baseMetric);
            if (EditorGUI.EndChangeCheck())
            {
                if (cascadeCount == 4)
                    serialized.shadowCascade4SplitProp.vector3Value = new Vector3(
                        cascades[0].size,
                        cascades[0].size + cascades[1].size,
                        cascades[0].size + cascades[1].size + cascades[2].size
                    );
                else if (cascadeCount == 3)
                    serialized.shadowCascade3SplitProp.vector2Value = new Vector2(
                        cascades[0].size,
                        cascades[0].size + cascades[1].size
                    );
                else if (cascadeCount == 2)
                    serialized.shadowCascade2SplitProp.floatValue = cascades[0].size;

                serialized.shadowCascadeBorderProp.floatValue = cascades[lastCascade].borderSize;
            }
        }
        static bool ValidateRendererGraphicsAPIsForLightLayers(out string unsupportedGraphicsApisMessage)
        {
            unsupportedGraphicsApisMessage = null;

            BuildTarget platform = EditorUserBuildSettings.activeBuildTarget;
            GraphicsDeviceType[] graphicsAPIs = PlayerSettings.GetGraphicsAPIs(platform);

            for (int apiIndex = 0; apiIndex < graphicsAPIs.Length; apiIndex++)
            {
                if (!RenderingUtils.SupportsLightLayers(graphicsAPIs[apiIndex]))
                {
                    if (unsupportedGraphicsApisMessage != null)
                        unsupportedGraphicsApisMessage += ", ";
                    unsupportedGraphicsApisMessage += System.String.Format("{0}", graphicsAPIs[apiIndex]);
                }
            }

            if (unsupportedGraphicsApisMessage != null)
                unsupportedGraphicsApisMessage += ".";

            return unsupportedGraphicsApisMessage == null;
        }

        static void DrawRendererFeatures(SerializedUniversalRendererData serialized, Editor ownerEditor)
        {
            serialized.rendererFeatureEditor.DrawRendererFeatures();
        }
    }
}
