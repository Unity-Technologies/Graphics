using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeParameterDrawer(typeof(TargetMidGrayParameter))]
    sealed class TargetMidGrayParameterDrawer : VolumeParameterDrawer
    {
        static readonly GUIContent[] s_MidGrayNames =
        {
            L10n.TextContent("Grey 12.5%", null, null, null),
            L10n.TextContent("Grey 14.0%", null, null, null),
            L10n.TextContent("Grey 18.0%", null, null, null)
        };

        public override bool OnGUI(SerializedDataParameter parameter, GUIContent title)
        {
            var value = parameter.value;

            if (value.propertyType != SerializedPropertyType.Enum)
                return false;

            value.intValue = EditorGUILayout.Popup(title, value.intValue, s_MidGrayNames);

            return true;
        }
    }
}
