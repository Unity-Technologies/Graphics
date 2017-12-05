using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    [VolumeParameterDrawer(typeof(ClampedFloatParameter))]
    sealed class ClampedFloatParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Float)
                return false;

            var o = parameter.GetObjectRef<ClampedFloatParameter>();

            if (o.clampMode == ParameterClampMode.MinMax)
            {
                EditorGUILayout.Slider(value, o.min, o.max, title);
                value.floatValue = Mathf.Clamp(value.floatValue, o.min, o.max);
            }
            else if (o.clampMode == ParameterClampMode.Min)
            {
                float v = EditorGUILayout.FloatField(title, value.floatValue);
                value.floatValue = Mathf.Max(v, o.min);
            }
            else if (o.clampMode == ParameterClampMode.Max)
            {
                float v = EditorGUILayout.FloatField(title, value.floatValue);
                value.floatValue = Mathf.Min(v, o.max);
            }
            else
            {
                return false;
            }

            return true;
        }
    }

    [VolumeParameterDrawer(typeof(InstantClampedFloatParameter))]
    sealed class InstantClampedFloatParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Float)
                return false;

            var o = parameter.GetObjectRef<InstantClampedFloatParameter>();

            if (o.clampMode == ParameterClampMode.MinMax)
            {
                EditorGUILayout.Slider(value, o.min, o.max, title);
                value.floatValue = Mathf.Clamp(value.floatValue, o.min, o.max);
            }
            else if (o.clampMode == ParameterClampMode.Min)
            {
                float v = EditorGUILayout.FloatField(title, value.floatValue);
                value.floatValue = Mathf.Max(v, o.min);
            }
            else if (o.clampMode == ParameterClampMode.Max)
            {
                float v = EditorGUILayout.FloatField(title, value.floatValue);
                value.floatValue = Mathf.Min(v, o.max);
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
