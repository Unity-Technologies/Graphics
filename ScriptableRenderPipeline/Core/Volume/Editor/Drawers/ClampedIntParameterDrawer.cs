using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    [VolumeParameterDrawer(typeof(ClampedIntParameter))]
    sealed class ClampedIntParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Integer)
                return false;

            var o = parameter.GetObjectRef<ClampedIntParameter>();

            if (o.clampMode == ParameterClampMode.MinMax)
            {
                EditorGUILayout.IntSlider(value, o.min, o.max, title);
                value.intValue = Mathf.Clamp(value.intValue, o.min, o.max);
            }
            else if (o.clampMode == ParameterClampMode.Min)
            {
                int v = EditorGUILayout.IntField(title, value.intValue);
                value.intValue = Mathf.Max(v, o.min);
            }
            else if (o.clampMode == ParameterClampMode.Max)
            {
                int v = EditorGUILayout.IntField(title, value.intValue);
                value.intValue = Mathf.Min(v, o.max);
            }
            else
            {
                return false;
            }

            return true;
        }
    }

    [VolumeParameterDrawer(typeof(InstantClampedIntParameter))]
    sealed class InstantClampedIntParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Integer)
                return false;

            var o = parameter.GetObjectRef<InstantClampedIntParameter>();

            if (o.clampMode == ParameterClampMode.MinMax)
            {
                EditorGUILayout.IntSlider(value, o.min, o.max, title);
                value.intValue = Mathf.Clamp(value.intValue, o.min, o.max);
            }
            else if (o.clampMode == ParameterClampMode.Min)
            {
                int v = EditorGUILayout.IntField(title, value.intValue);
                value.intValue = Mathf.Max(v, o.min);
            }
            else if (o.clampMode == ParameterClampMode.Max)
            {
                int v = EditorGUILayout.IntField(title, value.intValue);
                value.intValue = Mathf.Min(v, o.max);
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
