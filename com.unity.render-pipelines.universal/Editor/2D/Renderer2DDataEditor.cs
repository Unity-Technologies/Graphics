using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [CustomEditor(typeof(Renderer2DData), true)]
    internal class Renderer2DDataEditor : Editor
    {
        class Styles
        {
            public static readonly GUIContent transparencySortMode = EditorGUIUtility.TrTextContent("Transparency Sort Mode", "Default sorting mode used for transparent objects");
            public static readonly GUIContent transparencySortAxis = EditorGUIUtility.TrTextContent("Transparency Sort Axis", "Axis used for custom axis sorting mode");
            public static readonly GUIContent hdrEmulationScale = EditorGUIUtility.TrTextContent("HDR Emulation Scale", "Describes the scaling used by lighting to remap dynamic range between LDR and HDR");
            public static readonly GUIContent lightBlendStyles = EditorGUIUtility.TrTextContent("Light Blend Styles", "A Light Blend Style is a collection of properties that describe a particular way of applying lighting.");
            public static readonly GUIContent defaultMaterialType = EditorGUIUtility.TrTextContent("Default Material Type", "Material to use when adding new objects to a scene");
            public static readonly GUIContent defaultCustomMaterial = EditorGUIUtility.TrTextContent("Default Custom Material", "Material to use when adding new objects to a scene");

            public static readonly GUIContent name = EditorGUIUtility.TrTextContent("Name");
            public static readonly GUIContent maskTextureChannel = EditorGUIUtility.TrTextContent("Mask Texture Channel", "Which channel of the mask texture will affect this Light Blend Style.");
            public static readonly GUIContent renderTextureScale = EditorGUIUtility.TrTextContent("Render Texture Scale", "The resolution of the lighting buffer relative to the screen resolution. 1.0 means full screen size.");
            public static readonly GUIContent blendMode = EditorGUIUtility.TrTextContent("Blend Mode", "How the lighting should be blended with the main color of the objects.");
            public static readonly GUIContent customBlendFactors = EditorGUIUtility.TrTextContent("Custom Blend Factors");
            public static readonly GUIContent blendFactorMultiplicative = EditorGUIUtility.TrTextContent("Multiplicative");
            public static readonly GUIContent blendFactorAdditive = EditorGUIUtility.TrTextContent("Additive");
            public static readonly GUIContent useDepthStencilBuffer = EditorGUIUtility.TrTextContent("Use Depth/Stencil Buffer", "Uncheck this when you are certain you don't use any feature that requires the depth/stencil buffer (e.g. Sprite Mask). Not using the depth/stencil buffer may improve performance, especially on mobile platforms.");
            public static readonly GUIContent postProcessData = EditorGUIUtility.TrTextContent("Post-processing Data", "Resources (textures, shaders, etc.) required by post-processing effects.");
        }

        struct LightBlendStyleProps
        {
            public SerializedProperty name;
            public SerializedProperty maskTextureChannel;
            public SerializedProperty renderTextureScale;
            public SerializedProperty blendMode;
            public SerializedProperty blendFactorMultiplicative;
            public SerializedProperty blendFactorAdditive;
        }


        SerializedProperty m_TransparencySortMode;
        SerializedProperty m_TransparencySortAxis;
        SerializedProperty m_HDREmulationScale;
        SerializedProperty m_LightBlendStyles;
        LightBlendStyleProps[] m_LightBlendStylePropsArray;
        SerializedProperty m_UseDepthStencilBuffer;
        SerializedProperty m_PostProcessData;
        SerializedProperty m_DefaultMaterialType;
        SerializedProperty m_DefaultCustomMaterial;

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
            m_LightBlendStyles = serializedObject.FindProperty("m_LightBlendStyles");

            int numBlendStyles = m_LightBlendStyles.arraySize;
            m_LightBlendStylePropsArray = new LightBlendStyleProps[numBlendStyles];

            for (int i = 0; i < numBlendStyles; ++i)
            {
                SerializedProperty blendStyleProp = m_LightBlendStyles.GetArrayElementAtIndex(i);
                ref LightBlendStyleProps props = ref m_LightBlendStylePropsArray[i];

                props.name = blendStyleProp.FindPropertyRelative("name");
                props.maskTextureChannel = blendStyleProp.FindPropertyRelative("maskTextureChannel");
                props.renderTextureScale = blendStyleProp.FindPropertyRelative("renderTextureScale");
                props.blendMode = blendStyleProp.FindPropertyRelative("blendMode");
                props.blendFactorMultiplicative = blendStyleProp.FindPropertyRelative("customBlendFactors.multiplicative");
                props.blendFactorAdditive = blendStyleProp.FindPropertyRelative("customBlendFactors.additive");

                if (props.blendFactorMultiplicative == null)
                    props.blendFactorMultiplicative = blendStyleProp.FindPropertyRelative("customBlendFactors.modulate");
                if (props.blendFactorAdditive == null)
                    props.blendFactorAdditive = blendStyleProp.FindPropertyRelative("customBlendFactors.additve");
            }

            m_UseDepthStencilBuffer = serializedObject.FindProperty("m_UseDepthStencilBuffer");
            m_PostProcessData = serializedObject.FindProperty("m_PostProcessData");
            m_DefaultMaterialType = serializedObject.FindProperty("m_DefaultMaterialType");
            m_DefaultCustomMaterial = serializedObject.FindProperty("m_DefaultCustomMaterial");
        }

        private void OnDestroy()
        {
            SendModifiedAnalytics(m_Analytics);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            
            EditorGUILayout.PropertyField(m_TransparencySortMode, Styles.transparencySortMode);
            if(m_TransparencySortMode.intValue == (int)TransparencySortMode.CustomAxis)
                EditorGUILayout.PropertyField(m_TransparencySortAxis, Styles.transparencySortAxis);

            EditorGUILayout.PropertyField(m_HDREmulationScale, Styles.hdrEmulationScale);
            if (EditorGUI.EndChangeCheck() && m_HDREmulationScale.floatValue < 1.0f)
                m_HDREmulationScale.floatValue = 1.0f;

            EditorGUILayout.LabelField(Styles.lightBlendStyles);
            EditorGUI.indentLevel++;

            EditorGUI.BeginChangeCheck();
            int numBlendStyles = m_LightBlendStyles.arraySize;
            for (int i = 0; i < numBlendStyles; ++i)
            {
                SerializedProperty blendStyleProp = m_LightBlendStyles.GetArrayElementAtIndex(i);
                ref LightBlendStyleProps props = ref m_LightBlendStylePropsArray[i];
                
                EditorGUILayout.BeginHorizontal();
                blendStyleProp.isExpanded = EditorGUILayout.Foldout(blendStyleProp.isExpanded, props.name.stringValue, true);
                EditorGUILayout.EndHorizontal();

                if (blendStyleProp.isExpanded)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(props.name, Styles.name);
                    EditorGUILayout.PropertyField(props.maskTextureChannel, Styles.maskTextureChannel);
                    EditorGUILayout.PropertyField(props.renderTextureScale, Styles.renderTextureScale);
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

                    EditorGUI.indentLevel--;
                }
            }

            
            EditorGUI.indentLevel--;
            EditorGUILayout.PropertyField(m_UseDepthStencilBuffer, Styles.useDepthStencilBuffer);
            EditorGUILayout.PropertyField(m_PostProcessData, Styles.postProcessData);

            EditorGUILayout.PropertyField(m_DefaultMaterialType, Styles.defaultMaterialType);
            if(m_DefaultMaterialType.intValue == (int)Renderer2DData.Renderer2DDefaultMaterialType.Custom)
                EditorGUILayout.PropertyField(m_DefaultCustomMaterial, Styles.defaultCustomMaterial);

            m_WasModified |= serializedObject.hasModifiedProperties;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
