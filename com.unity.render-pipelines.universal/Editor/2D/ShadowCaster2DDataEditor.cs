using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

namespace UnityEditor.Experimental.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(ShadowCaster2DData))]
    public class ShadowCaster2DDataEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PrefixLabel(position, label);
            EditorGUI.PropertyField(position, property.FindPropertyRelative("position"));
        }

    }
}
