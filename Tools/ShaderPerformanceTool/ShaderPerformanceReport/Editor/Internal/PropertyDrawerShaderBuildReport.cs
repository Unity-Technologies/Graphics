using UnityEngine;

namespace UnityEditor.Experimental.ShaderTools.Internal
{
    [CustomPropertyDrawer(typeof(ShaderBuildReport))]
    class PropertyDrawerShaderBuildReport : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);
        }
    }
}
