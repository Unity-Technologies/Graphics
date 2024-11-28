using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Displays UI for a VrsLut lookup table. Each entry is a
    /// ShadingRateFragmentSize enum value that maps to a Color.
    /// </summary>
    [CustomPropertyDrawer(typeof(VrsLut))]
    sealed class VrsLutDrawer : PropertyDrawer
    {
        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var foldout = new Foldout()
            {
                text = property.displayName,
                value = property.isExpanded,
            };

            var vrsLutData = GetVrsLutData(property);
            VrsLutDataGUI(foldout.contentContainer, vrsLutData);

            VisualElement root = new();
            root.Add(foldout);
            return root;
        }

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
                return (GetVrsLutData(property).arraySize + 1) * EditorGUIUtility.singleLineHeight;

            return EditorGUIUtility.singleLineHeight;
        }

        void VrsLutDataGUI(VisualElement contentContainer, SerializedProperty vrsLutData)
        {
            foreach (var fragmentSizeInfo in shadingRateFragmentSizeFields)
            {
                var fragmentSizeValue = (ShadingRateFragmentSize) fragmentSizeInfo.GetValue(null);
                var inspectorNameAttribute = fragmentSizeInfo.GetCustomAttribute<InspectorNameAttribute>();
                var displayName = inspectorNameAttribute == null ? ObjectNames.NicifyVariableName(fragmentSizeValue.ToString()) : inspectorNameAttribute.displayName;
                var lutProp = vrsLutData.GetArrayElementAtIndex((int) fragmentSizeValue);
                var propertyField = new PropertyField(lutProp, displayName);
                contentContainer.Add(propertyField);
            }
        }

        static SerializedProperty GetVrsLutData(SerializedProperty property) => property.FindPropertyRelative("m_Data");

        static FieldInfo[] shadingRateFragmentSizeFields => typeof(ShadingRateFragmentSize).GetFields(BindingFlags.Static | BindingFlags.Public);
    }
}
