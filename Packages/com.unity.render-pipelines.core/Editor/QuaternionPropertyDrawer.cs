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
            {
                bool isFinite = (!float.IsNaN(euler.x) && !float.IsInfinity(euler.x) &&
                    !float.IsNaN(euler.y) && !float.IsInfinity(euler.y) &&
                    !float.IsNaN(euler.z) && !float.IsInfinity(euler.z));
                if (isFinite)
                {
                    property.quaternionValue = Quaternion.Euler(euler.x, euler.y, euler.z);
                }
            }
        }
    }
}
