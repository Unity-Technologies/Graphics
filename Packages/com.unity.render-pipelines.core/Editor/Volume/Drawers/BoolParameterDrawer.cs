using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [VolumeParameterDrawer(typeof(BoolParameter))]
    sealed class BoolParameterDrawer : VolumeParameterDrawer
    {
        private enum BoolEnum
        {
            Disabled = 0,
            Enabled = 1
        }

        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Boolean)
                return false;

            var o = parameter.GetObjectRef<BoolParameter>();
            if (o.displayType == BoolParameter.DisplayType.EnumPopup)
            {
                var enumValue = value.boolValue ? BoolEnum.Enabled: BoolEnum.Disabled;
                enumValue = (BoolEnum) EditorGUILayout.EnumPopup(title, enumValue);
                value.boolValue = enumValue == BoolEnum.Enabled;
            }
            else
            {
                EditorGUILayout.PropertyField(value, title);
            }
            return true;
        }
    }
}
