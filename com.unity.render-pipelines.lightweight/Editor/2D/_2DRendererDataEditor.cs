using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;

namespace UnityEditor.Experimental.Rendering.LWRP
{
    [CustomEditor(typeof(_2DRendererData), true)]
    internal class _2DRendererDataEditor : Editor
    {
        class Styles
        {
            public static readonly GUIContent hdrEmulationScale = EditorGUIUtility.TrTextContent("HDR Emulation Scale", "Describes the scaling used by lighting to remap dynamic range between LDR and HDR");
            public static readonly GUIContent lightOperations = EditorGUIUtility.TrTextContent("Light Operations", "A Light Operation is a collection of properties that describe a particular way of applying lighting.");
            public static readonly GUIContent name = EditorGUIUtility.TrTextContent("Name");
            public static readonly GUIContent maskTextureChannel = EditorGUIUtility.TrTextContent("Mask Texture Channel", "Which channel of the mask texture will affect this Light Operation.");
            public static readonly GUIContent renderTextureScale = EditorGUIUtility.TrTextContent("Render Texture Scale", "The resolution of the lighting buffer relative to the screen resolution. 1.0 means full screen size.");
            public static readonly GUIContent blendMode = EditorGUIUtility.TrTextContent("Blend Mode", "How the lighting should be blended with the main color of the objects.");
            public static readonly GUIContent customBlendFactors = EditorGUIUtility.TrTextContent("Custom Blend Factors");
            public static readonly GUIContent blendFactorMultiplicative = EditorGUIUtility.TrTextContent("Multiplicative");
            public static readonly GUIContent blendFactorAdditive = EditorGUIUtility.TrTextContent("Additive");
        }

        struct LightOperationProps
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
        SerializedProperty m_LightOperations;
        LightOperationProps[] m_LightOperationPropsArray;

        void OnEnable()
        {
            m_HDREmulationScale = serializedObject.FindProperty("m_HDREmulationScale");
            m_LightOperations = serializedObject.FindProperty("m_LightOperations");

            int numLightOps = m_LightOperations.arraySize;
            m_LightOperationPropsArray = new LightOperationProps[numLightOps];

            for (int i = 0; i < numLightOps; ++i)
            {
                SerializedProperty lightOpProp = m_LightOperations.GetArrayElementAtIndex(i);
                ref LightOperationProps props = ref m_LightOperationPropsArray[i];

                props.enabled = lightOpProp.FindPropertyRelative("enabled");
                props.name = lightOpProp.FindPropertyRelative("name");
                props.maskTextureChannel = lightOpProp.FindPropertyRelative("maskTextureChannel");
                props.renderTextureScale = lightOpProp.FindPropertyRelative("renderTextureScale");
                props.blendMode = lightOpProp.FindPropertyRelative("blendMode");
                props.blendFactorMultiplicative = lightOpProp.FindPropertyRelative("customBlendFactors.multiplicative");
                props.blendFactorAdditive = lightOpProp.FindPropertyRelative("customBlendFactors.additive");

                if (props.blendFactorMultiplicative == null)
                    props.blendFactorMultiplicative = lightOpProp.FindPropertyRelative("customBlendFactors.modulate");
                if (props.blendFactorAdditive == null)
                    props.blendFactorAdditive = lightOpProp.FindPropertyRelative("customBlendFactors.additve");
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_HDREmulationScale, Styles.hdrEmulationScale);
            if (EditorGUI.EndChangeCheck() && m_HDREmulationScale.floatValue < 1.0f)
                m_HDREmulationScale.floatValue = 1.0f;

            EditorGUILayout.LabelField(Styles.lightOperations);
            EditorGUI.indentLevel++;

            int numLightOps = m_LightOperations.arraySize;
            for (int i = 0; i < numLightOps; ++i)
            {
                SerializedProperty lightOpProp = m_LightOperations.GetArrayElementAtIndex(i);
                ref LightOperationProps props = ref m_LightOperationPropsArray[i];
                
                EditorGUILayout.BeginHorizontal();
                lightOpProp.isExpanded = EditorGUILayout.Foldout(lightOpProp.isExpanded, props.name.stringValue, true);
                props.enabled.boolValue = EditorGUILayout.Toggle(props.enabled.boolValue);
                EditorGUILayout.EndHorizontal();

                if (lightOpProp.isExpanded)
                {
                    EditorGUI.BeginDisabledGroup(!props.enabled.boolValue);
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(props.name, Styles.name);
                    EditorGUILayout.PropertyField(props.maskTextureChannel, Styles.maskTextureChannel);
                    EditorGUILayout.PropertyField(props.renderTextureScale, Styles.renderTextureScale);
                    EditorGUILayout.PropertyField(props.blendMode, Styles.blendMode);

                    if (props.blendMode.intValue == (int)_2DLightOperationDescription.BlendMode.Custom)
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

            serializedObject.ApplyModifiedProperties();
        }
    }
}
