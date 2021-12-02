using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [VolumeParameterDrawer(typeof(ColorParameter))]
    sealed class ColorParameterDrawer : VolumeParameterDrawer
    {
        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Color)
                return false;

            var o = parameter.GetObjectRef<ColorParameter>();

            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, title, value);
            value.colorValue = EditorGUI.ColorField(rect, title, value.colorValue, o.showEyeDropper, o.showAlpha, o.hdr);
            EditorGUI.EndProperty();
            return true;
        }
    }
}
