using UnityEngine;

namespace UnityEditor.Rendering
{
    [CustomPropertyDrawer(typeof(Quaternion))]
    class QuaternionPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var euler = property.quaternionValue.eulerAngles;
            EditorGUI.BeginChangeCheck();
            var w = EditorGUIUtility.wideMode;
            EditorGUIUtility.wideMode = true;
            euler = EditorGUI.Vector3Field(position, label, euler);
            EditorGUIUtility.wideMode = w;
            if (EditorGUI.EndChangeCheck())
                property.quaternionValue = Quaternion.Euler(euler);
        }
    }
}
