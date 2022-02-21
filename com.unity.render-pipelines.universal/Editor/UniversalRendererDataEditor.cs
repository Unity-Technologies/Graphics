using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<CachedUniversalRendererDataEditor>;

    enum ShowUIUniversalRendererData
    {
        All = 1 << 0,
        General = 1 << 1,
        Lighting = 1 << 2,
        Shadow = 1 << 3,
        RendererFeatures = 1 << 4,
    }

    class CachedUniversalRendererDataEditor : CachedScriptableRendererDataEditor
    {
        public SerializedProperty depthTextureProp;
        public SerializedProperty opaqueTextureProp;
        public SerializedProperty opaqueTextureDownsamplingProp;
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


        public CachedUniversalRendererDataEditor(SerializedProperty serializedProperty)
            : base(serializedProperty)
        {
            depthTextureProp = serializedProperty.FindPropertyRelative("m_RequireDepthTexture");
            opaqueTextureProp = serializedProperty.FindPropertyRelative("m_RequireOpaqueTexture");
            opaqueTextureDownsamplingProp = serializedProperty.FindPropertyRelative("m_OpaqueDownsampling");

            opaqueLayerMask = serializedProperty.FindPropertyRelative("m_OpaqueLayerMask");
            transparentLayerMask = serializedProperty.FindPropertyRelative("m_TransparentLayerMask");
            renderingMode = serializedProperty.FindPropertyRelative("m_RenderingMode");
            depthPrimingMode = serializedProperty.FindPropertyRelative("m_DepthPrimingMode");
            copyDepthMode = serializedProperty.FindPropertyRelative("m_CopyDepthMode");
            accurateGbufferNormals = serializedProperty.FindPropertyRelative("m_AccurateGbufferNormals");
            clusteredRendering = serializedProperty.FindPropertyRelative("m_ClusteredRendering");
            tileSize = serializedProperty.FindPropertyRelative("m_TileSize");
            defaultStencilState = serializedProperty.FindPropertyRelative("m_DefaultStencilState");
            shaders = serializedProperty.FindPropertyRelative("shaders");
            shadowTransparentReceiveProp = serializedProperty.FindPropertyRelative("m_ShadowTransparentReceive");
            intermediateTextureMode = serializedProperty.FindPropertyRelative("m_IntermediateTextureMode");

            mainLightRenderingModeProp = serializedProperty.FindPropertyRelative("m_MainLightRenderingMode");
            mainLightShadowsSupportedProp = serializedProperty.FindPropertyRelative("m_MainLightShadowsSupported");
            mainLightShadowmapResolutionProp = serializedProperty.FindPropertyRelative("m_MainLightShadowmapResolution");

            additionalLightsRenderingModeProp = serializedProperty.FindPropertyRelative("m_AdditionalLightsRenderingMode");
            additionalLightsPerObjectLimitProp = serializedProperty.FindPropertyRelative("m_AdditionalLightsPerObjectLimit");
            additionalLightShadowsSupportedProp = serializedProperty.FindPropertyRelative("m_AdditionalLightShadowsSupported");
            additionalLightShadowmapResolutionProp = serializedProperty.FindPropertyRelative("m_AdditionalLightsShadowmapResolution");

            additionalLightsShadowResolutionTierLowProp = serializedProperty.FindPropertyRelative("m_AdditionalLightsShadowResolutionTierLow");
            additionalLightsShadowResolutionTierMediumProp = serializedProperty.FindPropertyRelative("m_AdditionalLightsShadowResolutionTierMedium");
            additionalLightsShadowResolutionTierHighProp = serializedProperty.FindPropertyRelative("m_AdditionalLightsShadowResolutionTierHigh");

            additionalLightCookieResolutionProp = serializedProperty.FindPropertyRelative("m_AdditionalLightsCookieResolution");
            additionalLightCookieFormatProp = serializedProperty.FindPropertyRelative("m_AdditionalLightsCookieFormat");

            reflectionProbeBlendingProp = serializedProperty.FindPropertyRelative("m_ReflectionProbeBlending");
            reflectionProbeBoxProjectionProp = serializedProperty.FindPropertyRelative("m_ReflectionProbeBoxProjection");

            shadowDistanceProp = serializedProperty.FindPropertyRelative("m_ShadowDistance");

            shadowCascadeCountProp = serializedProperty.FindPropertyRelative("m_ShadowCascadeCount");
            shadowCascade2SplitProp = serializedProperty.FindPropertyRelative("m_Cascade2Split");
            shadowCascade3SplitProp = serializedProperty.FindPropertyRelative("m_Cascade3Split");
            shadowCascade4SplitProp = serializedProperty.FindPropertyRelative("m_Cascade4Split");
            shadowCascadeBorderProp = serializedProperty.FindPropertyRelative("m_CascadeBorder");
            shadowDepthBiasProp = serializedProperty.FindPropertyRelative("m_ShadowDepthBias");
            shadowNormalBiasProp = serializedProperty.FindPropertyRelative("m_ShadowNormalBias");
            softShadowsSupportedProp = serializedProperty.FindPropertyRelative("m_SoftShadowsSupported");
            conservativeEnclosingSphereProp = serializedProperty.FindPropertyRelative("m_ConservativeEnclosingSphere");


            mixedLightingSupportedProp = serializedProperty.FindPropertyRelative("m_MixedLightingSupported");
            supportsLightLayers = serializedProperty.FindPropertyRelative("m_SupportsLightLayers");

            string Key = "Universal_Shadow_Setting_Unit:UI_State";
            state = new EditorPrefBoolFlags<EditorUtils.Unit>(Key);
        }
    }

    /// <summary>
    /// PropertyDrawer script for a <c>UniversalRendererData</c> class.
    /// </summary>
    [CustomPropertyDrawer(typeof(UniversalRendererData), false)]
    public class UniversalRendererDataEditor : ScriptableRendererDataEditor
    {
        internal static class Styles
        {
            public static GUIContent generalSettingsText = EditorGUIUtility.TrTextContent("General", "Settings that affect the renderer");
            public static GUIContent lightingSettingsText = EditorGUIUtility.TrTextContent("Lighting", "Settings that affect the lighting in the Scene");
            public static GUIContent shadowSettingsText = EditorGUIUtility.TrTextContent("Shadows", "Settings that configure how shadows look and behave, and can be used to balance between the visual quality and performance of shadows.");
            public static GUIContent rendererFeatureSettingsText = EditorGUIUtility.TrTextContent("Renderer Features", "Settings that configure the renderer features used by the renderer.");

            public static GUIContent requireDepthTextureText = EditorGUIUtility.TrTextContent("Depth Texture", "If enabled the pipeline will generate camera's depth that can be bound in shaders as _CameraDepthTexture.");
            public static GUIContent requireOpaqueTextureText = EditorGUIUtility.TrTextContent("Opaque Texture", "If enabled the pipeline will copy the screen to texture after opaque objects are drawn. For transparent objects this can be bound in shaders as _CameraOpaqueTexture.");
            public static GUIContent opaqueDownsamplingText = EditorGUIUtility.TrTextContent("Opaque Downsampling", "The downsampling method that is used for the opaque texture");

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

            // Additional lights
            public static GUIContent addditionalLightsRenderingModeText = EditorGUIUtility.TrTextContent("Additional Lights", "Additional lights support.");
            public static GUIContent perObjectLimit = EditorGUIUtility.TrTextContent("Per Object Limit", "Maximum amount of additional lights. These lights are sorted and culled per-object.");
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
            public static string supportsMainLightShadowsWithoutMainLightHelpText = "Will not be able to cast main light shadows because main light is turned off.";
            public static GUIContent supportsMainLightShadowsText = EditorGUIUtility.TrTextContent("Main Light Shadows", "If enabled the main light can be a shadow casting light.");
            public static GUIContent mainLightShadowmapResolutionText = EditorGUIUtility.TrTextContent("Shadow Resolution", "Resolution of the main light shadowmap texture. If cascades are enabled, cascades will be packed into an atlas and this setting controls the maximum shadows atlas resolution.");

            public static string supportsAdditionalLightShadowsWithoutMainLightHelpText = "Will not be able to cast additional light shadows because additional light is not Per Pixel.";
            public static GUIContent supportsAdditionalShadowsText = EditorGUIUtility.TrTextContent("Additional Light Shadows", "If enabled shadows will be supported for spot lights.\n");
            public static GUIContent additionalLightsShadowmapResolution = EditorGUIUtility.TrTextContent("Shadow Atlas Resolution", "All additional lights are packed into a single shadowmap atlas. This setting controls the atlas size.");
            public static GUIContent additionalLightsShadowResolutionTiers = EditorGUIUtility.TrTextContent("Shadow Resolution Tiers", $"Additional Lights Shadow Resolution Tiers. Rounded to the next power of two, and clamped to be at least {UniversalAdditionalLightData.AdditionalLightsShadowMinimumResolution}.");
            public static GUIContent[] additionalLightsShadowResolutionTierNames =
            {
                new GUIContent("Low"),
                new GUIContent("Medium"),
                new GUIContent("High")
            };

            public static GUIContent shadowWorkingUnitText = EditorGUIUtility.TrTextContent("Working Unit", "The unit in which Unity measures the shadow cascade distances. The exception is Max Distance, which will still be in meters.");
            public static GUIContent shadowDistanceText = EditorGUIUtility.TrTextContent("Max Distance", "Maximum shadow rendering distance.");
            public static GUIContent shadowCascadesText = EditorGUIUtility.TrTextContent("Cascade Count", "Number of cascade splits used for directional shadows.");
            public static GUIContent shadowDepthBias = EditorGUIUtility.TrTextContent("Depth Bias", "Controls the distance at which the shadows will be pushed away from the light. Useful for avoiding false self-shadowing artifacts.");
            public static GUIContent shadowNormalBias = EditorGUIUtility.TrTextContent("Normal Bias", "Controls distance at which the shadow casting surfaces will be shrunk along the surface normal. Useful for avoiding false self-shadowing artifacts.");
            public static GUIContent supportsSoftShadows = EditorGUIUtility.TrTextContent("Soft Shadows", "If enabled pipeline will perform shadow filtering. Otherwise all lights that cast shadows will fallback to perform a single shadow sample.");
            public static GUIContent conservativeEnclosingSphere = EditorGUIUtility.TrTextContent("Conservative Enclosing Sphere", "Enable this option to improve shadow frustum culling and prevent Unity from excessively culling shadows in the corners of the shadow cascades. Disable this option only for compatibility purposes of existing projects created in previous Unity versions.");
        }

        protected override CachedScriptableRendererDataEditor Init(SerializedProperty property)
        {
            return new CachedUniversalRendererDataEditor(property);
        }



        protected override void OnGUI(CachedScriptableRendererDataEditor cachedEditorData, SerializedProperty property)
        {
            DrawHeader(
                cachedEditorData as CachedUniversalRendererDataEditor,
                DrawRenderer, DrawRendererAdditional);
        }

        static void DrawRenderer(CachedUniversalRendererDataEditor cachedEditorData, Editor ownerEditor)
        {
            int index = cachedEditorData.index.intValue;
            var rendererState = RenderersFoldoutStates.GetRendererState(index);
            var rendererAdditionalShowState = RenderersFoldoutStates.GetAdditionalRenderersShowState();
            EditorGUILayout.PropertyField(cachedEditorData.name);
            CED.Group(
                CED.FoldoutGroup(Styles.generalSettingsText,
                    (int)ShowUIUniversalRendererData.General, rendererState,
                    FoldoutOption.SubFoldout | FoldoutOption.Indent, DrawGeneral),
                CED.AdditionalPropertiesFoldoutGroup(Styles.lightingSettingsText,
                    (int)ShowUIUniversalRendererData.Lighting, rendererState,
                    1 << cachedEditorData.index.intValue, rendererAdditionalShowState,
                    DrawLighting, DrawLightingAdditional, FoldoutOption.SubFoldout | FoldoutOption.Indent),
                CED.AdditionalPropertiesFoldoutGroup(Styles.shadowSettingsText,
                    (int)ShowUIUniversalRendererData.Shadow, rendererState,
                    1 << cachedEditorData.index.intValue, rendererAdditionalShowState,
                    DrawShadows, DrawShadowsAdditional, FoldoutOption.SubFoldout | FoldoutOption.Indent),
                CED.FoldoutGroup(Styles.rendererFeatureSettingsText,
                    (int)ShowUIUniversalRendererData.RendererFeatures, rendererState,
                    FoldoutOption.SubFoldout | FoldoutOption.Indent, DrawRendererFeatures)
            ).Draw(cachedEditorData, ownerEditor);
        }
        static void DrawRendererAdditional(CachedUniversalRendererDataEditor cachedEditorData, Editor ownerEditor) { }
        static void DrawGeneral(CachedUniversalRendererDataEditor cachedEditorData, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(cachedEditorData.depthTextureProp, Styles.requireDepthTextureText);
            EditorGUILayout.PropertyField(cachedEditorData.opaqueTextureProp, Styles.requireOpaqueTextureText);
            if (cachedEditorData.opaqueTextureProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(cachedEditorData.opaqueTextureDownsamplingProp, Styles.opaqueDownsamplingText);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.LabelField(Styles.FilteringSectionLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(cachedEditorData.opaqueLayerMask, Styles.OpaqueMask);
            EditorGUILayout.PropertyField(cachedEditorData.transparentLayerMask, Styles.TransparentMask);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Styles.RenderingSectionLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(cachedEditorData.renderingMode, Styles.RenderingModeLabel);
            if (cachedEditorData.renderingMode.intValue == (int)RenderingMode.Deferred)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(cachedEditorData.accurateGbufferNormals, Styles.accurateGbufferNormalsLabel, true);
                EditorGUI.indentLevel--;
            }
            else if (cachedEditorData.renderingMode.intValue == (int)RenderingMode.Forward)
            {
                EditorGUI.indentLevel++;

                if (CachedUniversalRendererDataEditor.s_EnableClusteredUI)
                {
                    EditorGUILayout.PropertyField(cachedEditorData.clusteredRendering, Styles.clusteredRenderingLabel);
                    if (cachedEditorData.clusteredRendering.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(cachedEditorData.tileSize);
                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUILayout.PropertyField(cachedEditorData.depthPrimingMode, Styles.DepthPrimingModeLabel);
                if (cachedEditorData.depthPrimingMode.intValue != (int)DepthPrimingMode.Disabled)
                {
                    EditorGUILayout.HelpBox(Styles.DepthPrimingModeInfo.text, MessageType.Info);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(cachedEditorData.copyDepthMode, Styles.CopyDepthModeLabel);
            EditorGUI.indentLevel--;



            EditorGUILayout.LabelField(Styles.OverridesSectionLabel, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(cachedEditorData.defaultStencilState, Styles.defaultStencilStateLabel, true);
            SerializedProperty overrideStencil = cachedEditorData.defaultStencilState.FindPropertyRelative("overrideStencilState");

            if (overrideStencil.boolValue && cachedEditorData.renderingMode.intValue == (int)RenderingMode.Deferred)
            {
                CompareFunction stencilFunction = (CompareFunction)cachedEditorData.defaultStencilState.FindPropertyRelative("stencilCompareFunction").enumValueIndex;
                StencilOp stencilPass = (StencilOp)cachedEditorData.defaultStencilState.FindPropertyRelative("passOperation").enumValueIndex;
                StencilOp stencilFail = (StencilOp)cachedEditorData.defaultStencilState.FindPropertyRelative("failOperation").enumValueIndex;
                StencilOp stencilZFail = (StencilOp)cachedEditorData.defaultStencilState.FindPropertyRelative("zFailOperation").enumValueIndex;
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
                EditorGUILayout.PropertyField(cachedEditorData.intermediateTextureMode, Styles.intermediateTextureMode);
            }
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel++;

            // Add a "Reload All" button in inspector when we are in developer's mode
            if (EditorPrefs.GetBool("DeveloperMode"))
            {
                EditorGUILayout.PropertyField(cachedEditorData.shaders, true);
                if (cachedEditorData.shaders.isExpanded)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUIUtility.fieldWidth);
                    if (GUILayout.Button("Reload All"))
                    {
                        cachedEditorData.shaders = null;
                        ResourceReloader.ReloadAllNullIn(cachedEditorData.data, UniversalRenderPipelineAsset.packagePath);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            EditorGUI.indentLevel--;
        }
        static void DrawLighting(CachedUniversalRendererDataEditor cachedEditorData, Editor ownerEditor)
        {
            // Main Light
            CoreEditorUtils.DrawPopup(Styles.mainLightRenderingModeText, cachedEditorData.mainLightRenderingModeProp, Styles.mainLightOptions);

            // Additional light
            EditorGUILayout.PropertyField(cachedEditorData.additionalLightsRenderingModeProp, Styles.addditionalLightsRenderingModeText);
            if (cachedEditorData.additionalLightsRenderingModeProp.intValue != (int)LightRenderingMode.Disabled)
            {
                EditorGUI.indentLevel++;
                cachedEditorData.additionalLightsPerObjectLimitProp.intValue = EditorGUILayout.IntSlider(Styles.perObjectLimit, cachedEditorData.additionalLightsPerObjectLimitProp.intValue, 0, UniversalRenderPipeline.maxPerObjectLights);
                EditorGUILayout.PropertyField(cachedEditorData.additionalLightCookieResolutionProp, Styles.additionalLightsCookieResolution);
                EditorGUILayout.PropertyField(cachedEditorData.additionalLightCookieFormatProp, Styles.additionalLightsCookieFormat);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();



            // Reflection Probes
            EditorGUILayout.LabelField(Styles.reflectionProbesSettingsText);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(cachedEditorData.reflectionProbeBlendingProp, Styles.reflectionProbeBlendingText);
            EditorGUILayout.PropertyField(cachedEditorData.reflectionProbeBoxProjectionProp, Styles.reflectionProbeBoxProjectionText);
            EditorGUI.indentLevel--;
        }

        static void DrawLightingAdditional(CachedUniversalRendererDataEditor cachedEditorData, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(cachedEditorData.mixedLightingSupportedProp, Styles.mixedLightingSupportLabel);
            EditorGUILayout.PropertyField(cachedEditorData.supportsLightLayers, Styles.supportsLightLayers);

            if (cachedEditorData.supportsLightLayers.boolValue && !ValidateRendererGraphicsAPIsForLightLayers(out var unsupportedGraphicsApisMessage))
                EditorGUILayout.HelpBox(Styles.lightlayersUnsupportedMessage.text + unsupportedGraphicsApisMessage, MessageType.Warning, true);
        }

        static void DrawShadowResolutionTierSettings(CachedUniversalRendererDataEditor cachedEditorData, Editor ownerEditor)
        {
            // UI code adapted from HDRP U.I logic implemented in com.unity.render-pipelines.high-definition/Editor/RenderPipeline/Settings/SerializedScalableSetting.cs )

            var rect = GUILayoutUtility.GetRect(0, float.Epsilon, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight);
            // Removing the added border when calling GetRect
            rect.x += 2.6f;
            var contentRect = EditorGUI.PrefixLabel(rect, Styles.additionalLightsShadowResolutionTiers);

            EditorGUI.BeginChangeCheck();

            const int k_ShadowResolutionTiersCount = 3;
            var values = new[] { cachedEditorData.additionalLightsShadowResolutionTierLowProp, cachedEditorData.additionalLightsShadowResolutionTierMediumProp, cachedEditorData.additionalLightsShadowResolutionTierHighProp };

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

        static void DrawShadows(CachedUniversalRendererDataEditor cachedEditorData, Editor ownerEditor)
        {
            //Main light shadow
            EditorGUILayout.PropertyField(cachedEditorData.mainLightShadowsSupportedProp, Styles.supportsMainLightShadowsText);
            if (cachedEditorData.mainLightShadowsSupportedProp.boolValue)
            {
                EditorGUI.indentLevel++;
                if (!cachedEditorData.mainLightRenderingModeProp.boolValue)
                {
                    EditorGUILayout.HelpBox(Styles.supportsMainLightShadowsWithoutMainLightHelpText, MessageType.Warning);
                }
                EditorGUILayout.PropertyField(cachedEditorData.mainLightShadowmapResolutionProp, Styles.mainLightShadowmapResolutionText);


                EditorUtils.Unit unit = EditorUtils.Unit.Metric;
                if (cachedEditorData.shadowCascadeCountProp.intValue != 0)
                {
                    EditorGUI.BeginChangeCheck();
                    unit = (EditorUtils.Unit)EditorGUILayout.EnumPopup(Styles.shadowWorkingUnitText, cachedEditorData.state.value);
                    if (EditorGUI.EndChangeCheck())
                    {
                        cachedEditorData.state.value = unit;
                    }
                }
                EditorGUILayout.IntSlider(cachedEditorData.shadowCascadeCountProp, UniversalRendererData.k_ShadowCascadeMinCount, UniversalRendererData.k_ShadowCascadeMaxCount, Styles.shadowCascadesText);

                int cascadeCount = cachedEditorData.shadowCascadeCountProp.intValue;

                bool useMetric = unit == EditorUtils.Unit.Metric;
                float baseMetric = cachedEditorData.shadowDistanceProp.floatValue;
                int cascadeSplitCount = cascadeCount - 1;

                DrawCascadeSliders(cachedEditorData, cascadeSplitCount, useMetric, baseMetric);

                DrawCascades(cachedEditorData, cascadeCount, useMetric, baseMetric);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space();
            }

            // Additional light shadows
            EditorGUILayout.PropertyField(cachedEditorData.additionalLightShadowsSupportedProp, Styles.supportsAdditionalShadowsText);
            if (cachedEditorData.additionalLightShadowsSupportedProp.boolValue)
            {
                EditorGUI.indentLevel++;
                if (cachedEditorData.additionalLightsRenderingModeProp.intValue != (int)LightRenderingMode.PerPixel)
                {
                    EditorGUILayout.HelpBox(Styles.supportsAdditionalLightShadowsWithoutMainLightHelpText, MessageType.Warning);
                }
                EditorGUILayout.PropertyField(cachedEditorData.additionalLightShadowmapResolutionProp, Styles.additionalLightsShadowmapResolution);
                DrawShadowResolutionTierSettings(cachedEditorData, ownerEditor);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            if (cachedEditorData.mainLightShadowsSupportedProp.boolValue || cachedEditorData.additionalLightShadowsSupportedProp.boolValue)
            {
                EditorGUI.indentLevel--;
                EditorGUILayout.BeginVertical("GroupBox");
                EditorGUILayout.PropertyField(cachedEditorData.shadowTransparentReceiveProp, Styles.shadowTransparentReceiveLabel);
                cachedEditorData.shadowDistanceProp.floatValue = Mathf.Max(0.0f, EditorGUILayout.FloatField(Styles.shadowDistanceText, cachedEditorData.shadowDistanceProp.floatValue));

                cachedEditorData.shadowDepthBiasProp.floatValue = EditorGUILayout.Slider(Styles.shadowDepthBias, cachedEditorData.shadowDepthBiasProp.floatValue, 0.0f, UniversalRenderPipeline.maxShadowBias);
                cachedEditorData.shadowNormalBiasProp.floatValue = EditorGUILayout.Slider(Styles.shadowNormalBias, cachedEditorData.shadowNormalBiasProp.floatValue, 0.0f, UniversalRenderPipeline.maxShadowBias);
                EditorGUILayout.PropertyField(cachedEditorData.softShadowsSupportedProp, Styles.supportsSoftShadows);

                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel++;
            }
        }

        static void DrawShadowsAdditional(CachedUniversalRendererDataEditor cachedEditorData, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(cachedEditorData.conservativeEnclosingSphereProp, Styles.conservativeEnclosingSphere);
        }

        static void DrawCascadeSliders(CachedUniversalRendererDataEditor cachedEditorData, int splitCount, bool useMetric, float baseMetric)
        {
            Vector4 shadowCascadeSplit = Vector4.one;
            if (splitCount == 3)
                shadowCascadeSplit = new Vector4(cachedEditorData.shadowCascade4SplitProp.vector3Value.x, cachedEditorData.shadowCascade4SplitProp.vector3Value.y, cachedEditorData.shadowCascade4SplitProp.vector3Value.z, 1);
            else if (splitCount == 2)
                shadowCascadeSplit = new Vector4(cachedEditorData.shadowCascade3SplitProp.vector2Value.x, cachedEditorData.shadowCascade3SplitProp.vector2Value.y, 1, 0);
            else if (splitCount == 1)
                shadowCascadeSplit = new Vector4(cachedEditorData.shadowCascade2SplitProp.floatValue, 1, 0, 0);

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
                        cachedEditorData.shadowCascade4SplitProp.vector3Value = shadowCascadeSplit;
                        break;
                    case 2:
                        cachedEditorData.shadowCascade3SplitProp.vector2Value = shadowCascadeSplit;
                        break;
                    case 1:
                        cachedEditorData.shadowCascade2SplitProp.floatValue = shadowCascadeSplit.x;
                        break;
                }
            }

            var borderValue = cachedEditorData.shadowCascadeBorderProp.floatValue;

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
                cachedEditorData.shadowCascadeBorderProp.floatValue = borderValue;
            }
        }

        static void DrawCascades(CachedUniversalRendererDataEditor cachedEditorData, int cascadeCount, bool useMetric, float baseMetric)
        {
            var cascades = new ShadowCascadeGUI.Cascade[cascadeCount];

            Vector3 shadowCascadeSplit = Vector3.zero;
            if (cascadeCount == 4)
                shadowCascadeSplit = cachedEditorData.shadowCascade4SplitProp.vector3Value;
            else if (cascadeCount == 3)
                shadowCascadeSplit = cachedEditorData.shadowCascade3SplitProp.vector2Value;
            else if (cascadeCount == 2)
                shadowCascadeSplit.x = cachedEditorData.shadowCascade2SplitProp.floatValue;
            else
                shadowCascadeSplit.x = cachedEditorData.shadowCascade2SplitProp.floatValue;

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
                borderSize = cachedEditorData.shadowCascadeBorderProp.floatValue,
                cascadeHandleState = ShadowCascadeGUI.HandleState.Hidden,
                borderHandleState = ShadowCascadeGUI.HandleState.Enabled,
            };

            EditorGUI.BeginChangeCheck();
            ShadowCascadeGUI.DrawCascades(ref cascades, useMetric, baseMetric);
            if (EditorGUI.EndChangeCheck())
            {
                if (cascadeCount == 4)
                    cachedEditorData.shadowCascade4SplitProp.vector3Value = new Vector3(
                        cascades[0].size,
                        cascades[0].size + cascades[1].size,
                        cascades[0].size + cascades[1].size + cascades[2].size
                    );
                else if (cascadeCount == 3)
                    cachedEditorData.shadowCascade3SplitProp.vector2Value = new Vector2(
                        cascades[0].size,
                        cascades[0].size + cascades[1].size
                    );
                else if (cascadeCount == 2)
                    cachedEditorData.shadowCascade2SplitProp.floatValue = cascades[0].size;

                cachedEditorData.shadowCascadeBorderProp.floatValue = cascades[lastCascade].borderSize;
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

        static void DrawRendererFeatures(CachedUniversalRendererDataEditor cachedEditorData, Editor ownerEditor)
        {
            cachedEditorData.rendererFeatureEditor.DrawRendererFeatures();
        }
    }
}
