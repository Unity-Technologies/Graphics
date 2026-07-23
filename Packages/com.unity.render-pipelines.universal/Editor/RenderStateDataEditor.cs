using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEditor.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(StencilStateData), true)]
    internal class StencilStateDataDrawer : PropertyDrawer
    {
        class Styles
        {
            public static readonly GUIContent overrideStencil =
                EditorGUIUtility.TrTextContent("Stencil", "Process and override the Stencil buffer values.");

            public static readonly GUIContent stencilValue = EditorGUIUtility.TrTextContent("Value",
                "For each pixel, the Compare function compares this value with the value in the Stencil buffer. The function writes this value to the buffer if the Pass property is set to Replace.");

            public static readonly GUIContent stencilReadMask = EditorGUIUtility.TrTextContent("Read Mask",
                "Bitmask applied to both the reference value and the value in the stencil buffer before the comparison.");

            public static readonly GUIContent stencilWriteMask = EditorGUIUtility.TrTextContent("Write Mask",
                "Bitmask applied when writing to the stencil buffer.");

            public static readonly GUIContent stencilFunction = EditorGUIUtility.TrTextContent("Compare Function",
                "For each pixel, Unity uses this function to compare the value in the Value property with the value in the Stencil buffer.");

            public static readonly GUIContent stencilPass =
                EditorGUIUtility.TrTextContent("Pass", "What happens to the stencil value when passing.");

            public static readonly GUIContent stencilFail =
                EditorGUIUtility.TrTextContent("Fail", "What happens to the stencil value when failing.");

            public static readonly GUIContent stencilZFail =
                EditorGUIUtility.TrTextContent("Z Fail", "What happens to the stencil value when failing Z testing.");
        }

        const int k_LinesWhenOverrideOff = 1;
        const int k_LinesWhenOverrideOn = 8;

        SerializedProperty m_OverrideStencil;
        SerializedProperty m_StencilIndex;
        SerializedProperty m_StencilReadMask;
        SerializedProperty m_StencilWriteMask;
        SerializedProperty m_StencilFunction;
        SerializedProperty m_StencilPass;
        SerializedProperty m_StencilFail;
        SerializedProperty m_StencilZFail;
        readonly List<SerializedObject> m_Properties = new List<SerializedObject>();

        void Init(SerializedProperty property)
        {
            m_OverrideStencil = property.FindPropertyRelative("overrideStencilState");
            m_StencilIndex = property.FindPropertyRelative("stencilReference");
            m_StencilReadMask = property.FindPropertyRelative("stencilReadMask");
            m_StencilWriteMask = property.FindPropertyRelative("stencilWriteMask");
            m_StencilFunction = property.FindPropertyRelative("stencilCompareFunction");
            m_StencilPass = property.FindPropertyRelative("passOperation");
            m_StencilFail = property.FindPropertyRelative("failOperation");
            m_StencilZFail = property.FindPropertyRelative("zFailOperation");

            m_Properties.Add(property.serializedObject);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            if (!m_Properties.Contains(property.serializedObject))
                Init(property);

            rect.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.PropertyField(rect, m_OverrideStencil, Styles.overrideStencil);
            if (!m_OverrideStencil.boolValue)
                return;

            EditorGUI.indentLevel++;

            rect.y += EditorUtils.Styles.defaultLineSpace;
            DrawClampedStencilField(rect, Styles.stencilValue, m_StencilIndex);

            rect.y += EditorUtils.Styles.defaultLineSpace;
            DrawClampedStencilField(rect, Styles.stencilReadMask, m_StencilReadMask);

            rect.y += EditorUtils.Styles.defaultLineSpace;
            DrawClampedStencilField(rect, Styles.stencilWriteMask, m_StencilWriteMask);

            rect.y += EditorUtils.Styles.defaultLineSpace;
            EditorGUI.PropertyField(rect, m_StencilFunction, Styles.stencilFunction);

            rect.y += EditorUtils.Styles.defaultLineSpace;
            EditorGUI.indentLevel++;
            EditorGUI.PropertyField(rect, m_StencilPass, Styles.stencilPass);
            rect.y += EditorUtils.Styles.defaultLineSpace;
            EditorGUI.PropertyField(rect, m_StencilFail, Styles.stencilFail);
            EditorGUI.indentLevel--;

            rect.y += EditorUtils.Styles.defaultLineSpace;
            EditorGUI.PropertyField(rect, m_StencilZFail, Styles.stencilZFail);

            EditorGUI.indentLevel--;
        }

        static void DrawClampedStencilField(Rect rect, GUIContent label, SerializedProperty property)
        {
            EditorGUI.BeginProperty(rect, label, property);
            EditorGUI.BeginChangeCheck();
            int value = EditorGUI.IntField(rect, label, property.intValue);
            if (EditorGUI.EndChangeCheck())
                property.intValue = Mathf.Clamp(value, 0, (int)StencilUsage.UserMask);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (m_Properties.Contains(property.serializedObject) && m_OverrideStencil != null && m_OverrideStencil.boolValue)
                return EditorUtils.Styles.defaultLineSpace * k_LinesWhenOverrideOn;
            return EditorUtils.Styles.defaultLineSpace * k_LinesWhenOverrideOff;
        }
    }
}
