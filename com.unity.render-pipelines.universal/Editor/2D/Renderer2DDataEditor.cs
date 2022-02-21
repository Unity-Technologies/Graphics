using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<CachedRenderer2DDataEditor>;

    struct LightBlendStyleProps
    {
        public SerializedProperty name;
        public SerializedProperty maskTextureChannel;
        public SerializedProperty blendMode;
        public SerializedProperty blendFactorMultiplicative;
        public SerializedProperty blendFactorAdditive;
    }

    enum ShowUIRenderer2DData
    {
        All = 1 << 0,
        General = 1 << 1,
        LightingRenderTexture = 1 << 2,
        LightingBlendStyles = 1 << 3,
        CameraSortingLayerTexture = 1 << 4,
        RendererFeatures = 1 << 5,

    }

    class CachedRenderer2DDataEditor : CachedScriptableRendererDataEditor, IDisposable
    {
        public SerializedProperty HDREmulationScale;
        public SerializedProperty lightRenderTextureScale;
        public SerializedProperty lightBlendStyles;
        public LightBlendStyleProps[] lightBlendStylePropsArray;
        public SerializedProperty useDepthStencilBuffer;
        public SerializedProperty maxLightRenderTextureCount;
        public SerializedProperty maxShadowRenderTextureCount;

        public SerializedProperty useCameraSortingLayersTexture;
        public SerializedProperty cameraSortingLayersTextureBound;
        public SerializedProperty cameraSortingLayerDownsamplingMethod;


        public Analytics.Renderer2DAnalytics m_Analytics = Analytics.Renderer2DAnalytics.instance;
        public bool m_WasModified;

        public CachedRenderer2DDataEditor(SerializedProperty serializedProperty)
            : base(serializedProperty)
        {

            m_WasModified = false;

            HDREmulationScale = serializedProperty.FindPropertyRelative("m_HDREmulationScale");
            lightRenderTextureScale = serializedProperty.FindPropertyRelative("m_LightRenderTextureScale");
            lightBlendStyles = serializedProperty.FindPropertyRelative("m_LightBlendStyles");
            maxLightRenderTextureCount = serializedProperty.FindPropertyRelative("m_MaxLightRenderTextureCount");
            maxShadowRenderTextureCount = serializedProperty.FindPropertyRelative("m_MaxShadowRenderTextureCount");

            useCameraSortingLayersTexture = serializedProperty.FindPropertyRelative("m_UseCameraSortingLayersTexture");
            cameraSortingLayersTextureBound = serializedProperty.FindPropertyRelative("m_CameraSortingLayersTextureBound");
            cameraSortingLayerDownsamplingMethod = serializedProperty.FindPropertyRelative("m_CameraSortingLayerDownsamplingMethod");

            int numBlendStyles = lightBlendStyles.arraySize;
            lightBlendStylePropsArray = new LightBlendStyleProps[numBlendStyles];

            for (int i = 0; i < numBlendStyles; ++i)
            {
                SerializedProperty blendStyleProp = lightBlendStyles.GetArrayElementAtIndex(i);
                ref LightBlendStyleProps props = ref lightBlendStylePropsArray[i];

                props.name = blendStyleProp.FindPropertyRelative("name");
                props.maskTextureChannel = blendStyleProp.FindPropertyRelative("maskTextureChannel");
                props.blendMode = blendStyleProp.FindPropertyRelative("blendMode");
                props.blendFactorMultiplicative = blendStyleProp.FindPropertyRelative("customBlendFactors.multiplicative");
                props.blendFactorAdditive = blendStyleProp.FindPropertyRelative("customBlendFactors.additive");

                if (props.blendFactorMultiplicative == null)
                    props.blendFactorMultiplicative = blendStyleProp.FindPropertyRelative("customBlendFactors.modulate");
                if (props.blendFactorAdditive == null)
                    props.blendFactorAdditive = blendStyleProp.FindPropertyRelative("customBlendFactors.additve");
            }

            useDepthStencilBuffer = serializedProperty.FindPropertyRelative("m_UseDepthStencilBuffer");

            rendererFeatureEditor = new ScriptableRendererFeatureEditor(serializedProperty.FindPropertyRelative(nameof(ScriptableRendererData.m_RendererFeatures)));


        }

        void SendModifiedAnalytics(Analytics.IAnalytics analytics)
        {
            if (m_WasModified)
            {
                Analytics.RendererAssetData modifiedData = new Analytics.RendererAssetData();
                modifiedData.was_create_event = false;
                modifiedData.blending_layers_count = 0;
                modifiedData.blending_modes_used = 0;
                analytics.SendData(Analytics.AnalyticsDataTypes.k_Renderer2DDataString, modifiedData);
            }
        }
        public void Dispose()
        {
            SendModifiedAnalytics(m_Analytics);
        }
    }

    [CustomPropertyDrawer(typeof(Renderer2DData), false)]
    internal class Renderer2DDataEditor : ScriptableRendererDataEditor
    {
        class Styles
        {
            public static readonly GUIContent generalHeader = EditorGUIUtility.TrTextContent("General");
            public static readonly GUIContent lightRenderTexturesHeader = EditorGUIUtility.TrTextContent("Light Render Textures");
            public static readonly GUIContent lightBlendStylesHeader = EditorGUIUtility.TrTextContent("Light Blend Styles", "A Light Blend Style is a collection of properties that describe a particular way of applying lighting.");
            public static readonly GUIContent postProcessHeader = EditorGUIUtility.TrTextContent("Post-processing");
            public static readonly GUIContent rendererFeaturesHeader = EditorGUIUtility.TrTextContent("Renderer Features");

            public static readonly GUIContent hdrEmulationScale = EditorGUIUtility.TrTextContent("HDR Emulation Scale", "Describes the scaling used by lighting to remap dynamic range between LDR and HDR");
            public static readonly GUIContent lightRTScale = EditorGUIUtility.TrTextContent("Render Scale", "The resolution of intermediate light render textures, in relation to the screen resolution. 1.0 means full-screen size.");
            public static readonly GUIContent maxLightRTCount = EditorGUIUtility.TrTextContent("Max Light Render Textures", "How many intermediate light render textures can be created and utilized concurrently. Higher value usually leads to better performance on mobile hardware at the cost of more memory.");
            public static readonly GUIContent maxShadowRTCount = EditorGUIUtility.TrTextContent("Max Shadow Render Textures", "How many intermediate shadow render textures can be created and utilized concurrently. Higher value usually leads to better performance on mobile hardware at the cost of more memory.");

            public static readonly GUIContent name = EditorGUIUtility.TrTextContent("Name");
            public static readonly GUIContent maskTextureChannel = EditorGUIUtility.TrTextContent("Mask Texture Channel", "Which channel of the mask texture will affect this Light Blend Style.");
            public static readonly GUIContent blendMode = EditorGUIUtility.TrTextContent("Blend Mode", "How the lighting should be blended with the main color of the objects.");
            public static readonly GUIContent useDepthStencilBuffer = EditorGUIUtility.TrTextContent("Depth/Stencil Buffer", "Uncheck this when you are certain you don't use any feature that requires the depth/stencil buffer (e.g. Sprite Mask). Not using the depth/stencil buffer may improve performance, especially on mobile platforms.");
            public static readonly GUIContent postProcessIncluded = EditorGUIUtility.TrTextContent("Enabled", "Turns post-processing on (check box selected) or off (check box cleared). If you clear this check box, Unity excludes post-processing render Passes, shaders, and textures from the build.");
            public static readonly GUIContent postProcessData = EditorGUIUtility.TrTextContent("Data", "The asset containing references to shaders and Textures that the Renderer uses for post-processing.");

            public static readonly GUIContent cameraSortingLayerTextureHeader = EditorGUIUtility.TrTextContent("Camera Sorting Layer Texture", "Layers from back most to selected bounds will be rendered to _CameraSortingLayerTexture");
            public static readonly GUIContent cameraSortingLayerTextureBound = EditorGUIUtility.TrTextContent("Foremost Sorting Layer", "Layers from back most to selected bounds will be rendered to _CameraSortingLayerTexture");
            public static readonly GUIContent cameraSortingLayerDownsampling = EditorGUIUtility.TrTextContent("Downsampling Method", "Method used to copy _CameraSortingLayerTexture");
        }

        protected override CachedScriptableRendererDataEditor Init(SerializedProperty property)
        {
            return new CachedRenderer2DDataEditor(property);
        }
        protected override void OnGUI(CachedScriptableRendererDataEditor cachedEditorData, SerializedProperty property)
        {
            DrawHeader(
                cachedEditorData as CachedRenderer2DDataEditor,
                DrawRenderer);
        }

        void DrawRenderer(CachedRenderer2DDataEditor cachedEditorData, Editor ownerEditor)
        {
            int index = cachedEditorData.index.intValue;
            var rendererState = RenderersFoldoutStates.GetRendererState(index);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(cachedEditorData.name);
            CED.Group(
                CED.FoldoutGroup(Styles.generalHeader,
                    (int)ShowUIRenderer2DData.General, rendererState,
                    FoldoutOption.SubFoldout | FoldoutOption.Indent, DrawGeneral),
                CED.FoldoutGroup(Styles.lightRenderTexturesHeader,
                    (int)ShowUIRenderer2DData.LightingRenderTexture, rendererState,
                    FoldoutOption.SubFoldout | FoldoutOption.Indent, DrawLightRenderTextures),
                CED.FoldoutGroup(Styles.lightBlendStylesHeader,
                    (int)ShowUIRenderer2DData.LightingBlendStyles, rendererState,
                    FoldoutOption.SubFoldout | FoldoutOption.Indent, DrawLightBlendStyles),
                CED.FoldoutGroup(Styles.cameraSortingLayerDownsampling,
                    (int)ShowUIRenderer2DData.CameraSortingLayerTexture, rendererState,
                    FoldoutOption.SubFoldout | FoldoutOption.Indent, DrawCameraSortingLayerTexture),
                CED.FoldoutGroup(Styles.rendererFeaturesHeader,
                    (int)ShowUIRenderer2DData.RendererFeatures, rendererState,
                    FoldoutOption.SubFoldout | FoldoutOption.Indent, DrawRendererFeatures)
            ).Draw(cachedEditorData, null);
            if (EditorGUI.EndChangeCheck())
            {
                cachedEditorData.m_WasModified |= true;
            }
        }

        private static void DrawGeneral(CachedRenderer2DDataEditor cachedEditorData, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(cachedEditorData.useDepthStencilBuffer, Styles.useDepthStencilBuffer);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(cachedEditorData.HDREmulationScale, Styles.hdrEmulationScale);
            if (EditorGUI.EndChangeCheck() && cachedEditorData.HDREmulationScale.floatValue < 1.0f)
                cachedEditorData.HDREmulationScale.floatValue = 1.0f;

            EditorGUILayout.Space();
        }

        private static void DrawLightRenderTextures(CachedRenderer2DDataEditor cachedEditorData, Editor ownerEditor)
        {
            EditorGUILayout.PropertyField(cachedEditorData.lightRenderTextureScale, Styles.lightRTScale);
            EditorGUILayout.PropertyField(cachedEditorData.maxLightRenderTextureCount, Styles.maxLightRTCount);
            EditorGUILayout.PropertyField(cachedEditorData.maxShadowRenderTextureCount, Styles.maxShadowRTCount);

            EditorGUILayout.Space();
        }

        private static void DrawLightBlendStyles(CachedRenderer2DDataEditor cachedEditorData, Editor ownerEditor)
        {
            int numBlendStyles = cachedEditorData.lightBlendStyles.arraySize;
            for (int i = 0; i < numBlendStyles; ++i)
            {
                ref LightBlendStyleProps props = ref cachedEditorData.lightBlendStylePropsArray[i];

                EditorGUILayout.PropertyField(props.name, Styles.name);
                EditorGUILayout.PropertyField(props.maskTextureChannel, Styles.maskTextureChannel);
                EditorGUILayout.PropertyField(props.blendMode, Styles.blendMode);

                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
        }

        public static void DrawCameraSortingLayerTexture(CachedRenderer2DDataEditor cachedEditorData, Editor ownerEditor)
        {
            SortingLayer[] sortingLayers = SortingLayer.layers;
            string[] optionNames = new string[sortingLayers.Length + 1];
            int[] optionIds = new int[sortingLayers.Length + 1];
            optionNames[0] = "Disabled";
            optionIds[0] = -1;

            int currentOptionIndex = 0;
            for (int i = 0; i < sortingLayers.Length; i++)
            {
                optionNames[i + 1] = sortingLayers[i].name;
                optionIds[i + 1] = sortingLayers[i].id;
                if (sortingLayers[i].id == cachedEditorData.cameraSortingLayersTextureBound.intValue)
                    currentOptionIndex = i + 1;
            }


            int selectedOptionIndex = !cachedEditorData.useCameraSortingLayersTexture.boolValue ? 0 : currentOptionIndex;
            selectedOptionIndex = EditorGUILayout.Popup(Styles.cameraSortingLayerTextureBound, selectedOptionIndex, optionNames);

            cachedEditorData.useCameraSortingLayersTexture.boolValue = selectedOptionIndex != 0;
            cachedEditorData.cameraSortingLayersTextureBound.intValue = optionIds[selectedOptionIndex];

            EditorGUI.BeginDisabledGroup(!cachedEditorData.useCameraSortingLayersTexture.boolValue);
            EditorGUILayout.PropertyField(cachedEditorData.cameraSortingLayerDownsamplingMethod, Styles.cameraSortingLayerDownsampling);
            EditorGUI.EndDisabledGroup();
        }

        private static void DrawRendererFeatures(CachedRenderer2DDataEditor cachedEditorData, Editor ownerEditor)
        {
            EditorGUILayout.Space();
            cachedEditorData.rendererFeatureEditor.DrawRendererFeatures();

        }
    }
}
