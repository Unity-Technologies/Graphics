using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [CustomEditor(typeof(Renderer2DData), true)]
    internal class Renderer2DDataEditor : Editor
    {
        class Styles
        {
            public static readonly GUIContent generalHeader = EditorGUIUtility.TrTextContent("General");
            public static readonly GUIContent lightRenderTexturesHeader = EditorGUIUtility.TrTextContent("Light Render Textures");
            public static readonly GUIContent lightBlendStylesHeader = EditorGUIUtility.TrTextContent("Light Blend Styles", "A Light Blend Style is a collection of properties that describe a particular way of applying lighting.");

            public static readonly GUIContent transparencySortMode = EditorGUIUtility.TrTextContent("Transparency Sort Mode", "Default sorting mode used for transparent objects");
            public static readonly GUIContent transparencySortAxis = EditorGUIUtility.TrTextContent("Transparency Sort Axis", "Axis used for custom axis sorting mode");
            public static readonly GUIContent hdrEmulationScale = EditorGUIUtility.TrTextContent("HDR Emulation Scale", "Describes the scaling used by lighting to remap dynamic range between LDR and HDR");
            public static readonly GUIContent lightRTScale = EditorGUIUtility.TrTextContent("Render Scale", "The resolution of intermediate light render textures, in relation to the screen resolution. 1.0 means full-screen size.");
            public static readonly GUIContent maxLightRTCount = EditorGUIUtility.TrTextContent("Max Render Textures", "How many intermediate light render textures can be created and utilized concurrently. Higher value usually leads to better performance on mobile hardware at the cost of more memory.");
            public static readonly GUIContent defaultMaterialType = EditorGUIUtility.TrTextContent("Default Material Type", "Material to use when adding new objects to a scene");
            public static readonly GUIContent defaultCustomMaterial = EditorGUIUtility.TrTextContent("Default Custom Material", "Material to use when adding new objects to a scene");

            public static readonly GUIContent name = EditorGUIUtility.TrTextContent("Name");
            public static readonly GUIContent maskTextureChannel = EditorGUIUtility.TrTextContent("Mask Texture Channel", "Which channel of the mask texture will affect this Light Blend Style.");
            public static readonly GUIContent blendMode = EditorGUIUtility.TrTextContent("Blend Mode", "How the lighting should be blended with the main color of the objects.");
            public static readonly GUIContent customBlendFactors = EditorGUIUtility.TrTextContent("Custom Blend Factors");
            public static readonly GUIContent blendFactorMultiplicative = EditorGUIUtility.TrTextContent("Multiplicative");
            public static readonly GUIContent blendFactorAdditive = EditorGUIUtility.TrTextContent("Additive");
            public static readonly GUIContent useDepthStencilBuffer = EditorGUIUtility.TrTextContent("Depth/Stencil Buffer", "Uncheck this when you are certain you don't use any feature that requires the depth/stencil buffer (e.g. Sprite Mask). Not using the depth/stencil buffer may improve performance, especially on mobile platforms.");

            public static readonly GUIContent enableBatching = EditorGUIUtility.TrTextContent("Enable Light Batching", "Enable Batching for Shape / Parametric Lights.");

            public static readonly GUIContent cameraSortingLayerTextureBound = EditorGUIUtility.TrTextContent("Camera Sorting Layers Texture Bound", "Layers from back most to selected bounds will be rendered to _CameraSortingLayersTexture");
            public static readonly GUIContent cameraSortingLayerDownsampling = EditorGUIUtility.TrTextContent("Camera Sorting Layers Downsampling Method", "Method used to copy _CameraSortingLayersTexture");
        }

        struct LightBlendStyleProps
        {
            public SerializedProperty name;
            public SerializedProperty maskTextureChannel;
            public SerializedProperty blendMode;
            public SerializedProperty blendFactorMultiplicative;
            public SerializedProperty blendFactorAdditive;
        }

        SerializedProperty m_TransparencySortMode;
        SerializedProperty m_TransparencySortAxis;
        SerializedProperty m_HDREmulationScale;
        SerializedProperty m_LightRenderTextureScale;
        SerializedProperty m_LightBlendStyles;
        LightBlendStyleProps[] m_LightBlendStylePropsArray;
        SerializedProperty m_UseDepthStencilBuffer;
        SerializedProperty m_DefaultMaterialType;
        SerializedProperty m_DefaultCustomMaterial;
        SerializedProperty m_MaxLightRenderTextureCount;
        SerializedProperty m_EnableBatchingProp;

        SerializedProperty m_UseCameraSortingLayersTexture;
        SerializedProperty m_CameraSortingLayersTextureBound;
        SerializedProperty m_CameraSortingLayerDownsamplingMethod;

        SavedBool m_GeneralFoldout;
        SavedBool m_LightRenderTexturesFoldout;
        SavedBool m_LightBlendStylesFoldout;

        Analytics.Renderer2DAnalytics m_Analytics = Analytics.Renderer2DAnalytics.instance;
        Renderer2DData m_Renderer2DData;
        bool m_WasModified;

        void SendModifiedAnalytics(Analytics.IAnalytics analytics)
        {
            if (m_WasModified)
            {
                Analytics.RendererAssetData modifiedData = new Analytics.RendererAssetData();
                modifiedData.instance_id = m_Renderer2DData.GetInstanceID();
                modifiedData.was_create_event = false;
                modifiedData.blending_layers_count = 0;
                modifiedData.blending_modes_used = 0;
                analytics.SendData(Analytics.AnalyticsDataTypes.k_Renderer2DDataString, modifiedData);
            }
        }

        void OnEnable()
        {
            m_WasModified = false;
            m_Renderer2DData = (Renderer2DData)serializedObject.targetObject;

            m_TransparencySortMode = serializedObject.FindProperty("m_TransparencySortMode");
            m_TransparencySortAxis = serializedObject.FindProperty("m_TransparencySortAxis");
            m_HDREmulationScale = serializedObject.FindProperty("m_HDREmulationScale");
            m_LightRenderTextureScale = serializedObject.FindProperty("m_LightRenderTextureScale");
            m_LightBlendStyles = serializedObject.FindProperty("m_LightBlendStyles");
            m_MaxLightRenderTextureCount = serializedObject.FindProperty("m_MaxLightRenderTextureCount");
            m_EnableBatchingProp = serializedObject.FindProperty("m_EnableBatching");

            m_CameraSortingLayersTextureBound = serializedObject.FindProperty("m_CameraSortingLayersTextureBound");
            m_UseCameraSortingLayersTexture = serializedObject.FindProperty("m_UseCameraSortingLayersTexture");
            m_CameraSortingLayerDownsamplingMethod = serializedObject.FindProperty("m_CameraSortingLayerDownsamplingMethod");

            int numBlendStyles = m_LightBlendStyles.arraySize;
            m_LightBlendStylePropsArray = new LightBlendStyleProps[numBlendStyles];

            for (int i = 0; i < numBlendStyles; ++i)
            {
                SerializedProperty blendStyleProp = m_LightBlendStyles.GetArrayElementAtIndex(i);
                ref LightBlendStyleProps props = ref m_LightBlendStylePropsArray[i];

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

            m_UseDepthStencilBuffer = serializedObject.FindProperty("m_UseDepthStencilBuffer");
            m_DefaultMaterialType = serializedObject.FindProperty("m_DefaultMaterialType");
            m_DefaultCustomMaterial = serializedObject.FindProperty("m_DefaultCustomMaterial");

            m_GeneralFoldout = new SavedBool($"{target.GetType()}.GeneralFoldout", true);
            m_LightRenderTexturesFoldout = new SavedBool($"{target.GetType()}.LightRenderTexturesFoldout", true);
            m_LightBlendStylesFoldout = new SavedBool($"{target.GetType()}.LightBlendStylesFoldout", true);
        }

        private void OnDestroy()
        {
            SendModifiedAnalytics(m_Analytics);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawGeneral();
            DrawLightRenderTextures();
            DrawLightBlendStyles();
            DrawCameraSortingLayerTexture();

            m_WasModified |= serializedObject.hasModifiedProperties;
            serializedObject.ApplyModifiedProperties();
        }

        public void DrawCameraSortingLayerTexture()
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
                if (sortingLayers[i].id == m_CameraSortingLayersTextureBound.intValue)
                    currentOptionIndex = i + 1;
            }


            int selectedOptionIndex = !m_UseCameraSortingLayersTexture.boolValue ? 0 : currentOptionIndex;
            selectedOptionIndex = EditorGUILayout.Popup(Styles.cameraSortingLayerTextureBound, selectedOptionIndex, optionNames);

            m_UseCameraSortingLayersTexture.boolValue = selectedOptionIndex != 0;
            m_CameraSortingLayersTextureBound.intValue = optionIds[selectedOptionIndex];

            EditorGUI.BeginDisabledGroup(!m_UseCameraSortingLayersTexture.boolValue);
            EditorGUILayout.PropertyField(m_CameraSortingLayerDownsamplingMethod, Styles.cameraSortingLayerDownsampling);
            EditorGUI.EndDisabledGroup();
        }

        private void DrawGeneral()
        {
            CoreEditorUtils.DrawSplitter();
            m_GeneralFoldout.value = CoreEditorUtils.DrawHeaderFoldout(Styles.generalHeader, m_GeneralFoldout.value);
            if (!m_GeneralFoldout.value)
                return;

            EditorGUILayout.PropertyField(m_TransparencySortMode, Styles.transparencySortMode);

            using (new EditorGUI.DisabledGroupScope(m_TransparencySortMode.intValue != (int)TransparencySortMode.CustomAxis))
                EditorGUILayout.PropertyField(m_TransparencySortAxis, Styles.transparencySortAxis);

            EditorGUILayout.PropertyField(m_DefaultMaterialType, Styles.defaultMaterialType);
            if (m_DefaultMaterialType.intValue == (int)Renderer2DData.Renderer2DDefaultMaterialType.Custom)
                EditorGUILayout.PropertyField(m_DefaultCustomMaterial, Styles.defaultCustomMaterial);

            EditorGUILayout.PropertyField(m_UseDepthStencilBuffer, Styles.useDepthStencilBuffer);
            EditorGUILayout.PropertyField(m_EnableBatchingProp, Styles.enableBatching);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_HDREmulationScale, Styles.hdrEmulationScale);
            if (EditorGUI.EndChangeCheck() && m_HDREmulationScale.floatValue < 1.0f)
                m_HDREmulationScale.floatValue = 1.0f;

            EditorGUILayout.Space();
        }

        private void DrawLightRenderTextures()
        {
            CoreEditorUtils.DrawSplitter();
            m_LightRenderTexturesFoldout.value = CoreEditorUtils.DrawHeaderFoldout(Styles.lightRenderTexturesHeader, m_LightRenderTexturesFoldout.value);
            if (!m_LightRenderTexturesFoldout.value)
                return;

            EditorGUILayout.PropertyField(m_LightRenderTextureScale, Styles.lightRTScale);
            EditorGUILayout.PropertyField(m_MaxLightRenderTextureCount, Styles.maxLightRTCount);

            EditorGUILayout.Space();
        }

        private void DrawLightBlendStyles()
        {
            CoreEditorUtils.DrawSplitter();
            m_LightBlendStylesFoldout.value = CoreEditorUtils.DrawHeaderFoldout(Styles.lightBlendStylesHeader, m_LightBlendStylesFoldout.value);
            if (!m_LightBlendStylesFoldout.value)
                return;

            int numBlendStyles = m_LightBlendStyles.arraySize;
            for (int i = 0; i < numBlendStyles; ++i)
            {
                ref LightBlendStyleProps props = ref m_LightBlendStylePropsArray[i];

                EditorGUILayout.PropertyField(props.name, Styles.name);
                EditorGUILayout.PropertyField(props.maskTextureChannel, Styles.maskTextureChannel);
                EditorGUILayout.PropertyField(props.blendMode, Styles.blendMode);

                if (props.blendMode.intValue == (int)Light2DBlendStyle.BlendMode.Custom)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField(Styles.customBlendFactors, GUILayout.MaxWidth(200.0f));
                    EditorGUI.indentLevel--;

                    int oldIndentLevel = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;

                    EditorGUIUtility.labelWidth = 80.0f;
                    EditorGUILayout.PropertyField(props.blendFactorMultiplicative, Styles.blendFactorMultiplicative, GUILayout.MinWidth(110.0f));

                    GUILayout.Space(10.0f);

                    EditorGUIUtility.labelWidth = 50.0f;
                    EditorGUILayout.PropertyField(props.blendFactorAdditive, Styles.blendFactorAdditive, GUILayout.MinWidth(90.0f));

                    EditorGUIUtility.labelWidth = 0.0f;
                    EditorGUI.indentLevel = oldIndentLevel;
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }

            EditorGUILayout.Space();
        }
    }
}
