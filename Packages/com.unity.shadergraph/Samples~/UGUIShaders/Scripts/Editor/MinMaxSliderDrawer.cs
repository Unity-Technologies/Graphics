using UnityEngine;
using UnityEditor;

namespace Unity.UI.Shaders.Sample.Editor
{
    [CustomPropertyDrawer(typeof(MinMaxSliderAttribute))]
    public class MinMaxSliderDrawer : PropertyDrawer
    {
        MinMaxSliderAttribute range => (MinMaxSliderAttribute)attribute;

        Vector2 _value;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Vector2)
            {
                _value = property.vector2Value;
                EditorGUI.BeginChangeCheck();
                EditorGUI.MinMaxSlider(position, label, ref _value.x, ref _value.y, range.min, range.max);
                if (EditorGUI.EndChangeCheck())
                {
                    property.vector2Value = _value;
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }
    }
}
