using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX.Utility;

namespace UnityEditor.VFX.Utility
{
    [CustomPropertyDrawer(typeof(ExposedProperty))]
    class ExposedPropertyDrawer : PropertyDrawer
    {
        const string k_PropertyName = "m_Name";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new PropertyField(property.FindPropertyRelative(k_PropertyName), ObjectNames.NicifyVariableName(property.name));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property.FindPropertyRelative(k_PropertyName), new GUIContent(ObjectNames.NicifyVariableName(property.name)));
        }
    }
}
