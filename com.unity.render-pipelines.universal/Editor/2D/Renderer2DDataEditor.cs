using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [CustomEditor(typeof(Renderer2DData), true)]
    internal class Renderer2DDataEditor : Editor
    {
        class Styles
        {
            public static readonly GUIContent hdrEmulationScale = EditorGUIUtility.TrTextContent("HDR Emulation Scale", "Describes the scaling used by lighting to remap dynamic range between LDR and HDR");
            public static readonly GUIContent lightBlendStyles = EditorGUIUtility.TrTextContent("Light Blend Styles", "A Light Blend Style is a collection of properties that describe a particular way of applying lighting.");
            public static readonly GUIContent name = EditorGUIUtility.TrTextContent("Name");
            public static readonly GUIContent maskTextureChannel = EditorGUIUtility.TrTextContent("Mask Texture Channel", "Which channel of the mask texture will affect this Light Blend Style.");
            public static readonly GUIContent renderTextureScale = EditorGUIUtility.TrTextContent("Render Texture Scale", "The resolution of the lighting buffer relative to the screen resolution. 1.0 means full screen size.");
            public static readonly GUIContent blendMode = EditorGUIUtility.TrTextContent("Blend Mode", "How the lighting should be blended with the main color of the objects.");
            public static readonly GUIContent customBlendFactors = EditorGUIUtility.TrTextContent("Custom Blend Factors");
            public static readonly GUIContent blendFactorMultiplicative = EditorGUIUtility.TrTextContent("Multiplicative");
            public static readonly GUIContent blendFactorAdditive = EditorGUIUtility.TrTextContent("Additive");
        }
        struct LightBlendStyleProps

        {
            public SerializedProperty enabled;
            public SerializedProperty name;
            public SerializedProperty maskTextureChannel;
            public SerializedProperty renderTextureScale;
            public SerializedProperty blendMode;
            public SerializedProperty blendFactorMultiplicative;
            public SerializedProperty blendFactorAdditive;
        }

        SerializedProperty m_HDREmulationScale;
        SerializedProperty m_LightBlendStyles;
        LightBlendStyleProps[] m_LightBlendStylePropsArray;

        Analytics.Renderer2DAnalytics m_Analytics = Analytics.Renderer2DAnalytics.instance;
        Renderer2DData m_Renderer2DData;
        bool m_WasModified;

        int GetNumberOfUsedBlendingLayers(Renderer2DData rendererData)
        {
            int count = 0;
            foreach (var lightBlendStyle in rendererData.lightBlendStyles)
            {
                if (lightBlendStyle.enabled)
                    count++;
            }

            return count;
        }

        int GetBlendingModesUsed(Renderer2DData rendererData)
        {
            int modesUsed = 0;
            foreach (var lightBlendStyle in rendererData.lightBlendStyles)
            {
                if (lightBlendStyle.enabled)
                    modesUsed |= 1 << (int)lightBlendStyle.blendMode;
            }

            return modesUsed;
        }

        void SendModifiedAnalytics(Analytics.IAnalytics analytics)
        {
            if (m_WasModified)
            {
                Analytics.RendererAssetData modifiedData = new Analytics.RendererAssetData();
                modifiedData.instance_id = m_Renderer2DData.GetInstanceID();
                modifiedData.was_create_event = false;
                modifiedData.blending_layers_count = GetNumberOfUsedBlendingLayers(m_Renderer2DData);
                modifiedData.blending_modes_used = GetBlendingModesUsed(m_Renderer2DData);
                analytics.SendData(Analytics.AnalyticsDataTypes.k_Renderer2DDataString, modifiedData);
            }
        }

        void OnEnable()
        {
            m_WasModified = false;
            m_Renderer2DData = (Renderer2DData)serializedObject.targetObject;

            m_HDREmulationScale = serializedObject.FindProperty("m_HDREmulationScale");
            m_LightBlendStyles = serializedObject.FindProperty("m_LightBlendStyles");

            int numBlendStyles = m_LightBlendStyles.arraySize;
            m_LightBlendStylePropsArray = new LightBlendStyleProps[numBlendStyles];

            for (int i = 0; i < numBlendStyles; ++i)
            {
                SerializedProperty blendStyleProp = m_LightBlendStyles.GetArrayElementAtIndex(i);
                ref LightBlendStyleProps props = ref m_LightBlendStylePropsArray[i];

                props.enabled = blendStyleProp.FindPropertyRelative("enabled");
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
        }

        private void OnDestroy()
        {
            SendModifiedAnalytics(m_Analytics);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
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
                props.enabled.boolValue = EditorGUILayout.Toggle(props.enabled.boolValue);
                EditorGUILayout.EndHorizontal();

                if (blendStyleProp.isExpanded)
                {
                    EditorGUI.BeginDisabledGroup(!props.enabled.boolValue);
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
                    EditorGUI.EndDisabledGroup();
                }
            }

            EditorGUI.indentLevel--;
            m_WasModified |= serializedObject.hasModifiedProperties;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
