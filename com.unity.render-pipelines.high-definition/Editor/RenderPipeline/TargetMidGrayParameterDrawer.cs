using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeParameterDrawer(typeof(TargetMidGrayParameter))]
    sealed class TargetMidGrayParameterDrawer : VolumeParameterDrawer
    {
        static readonly GUIContent[] s_MidGrayNames =
        {
            EditorGUIUtility.TrTextContent("Grey 12.5%"),
            EditorGUIUtility.TrTextContent("Grey 14.0%"),
            EditorGUIUtility.TrTextContent("Grey 18.0%")
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
