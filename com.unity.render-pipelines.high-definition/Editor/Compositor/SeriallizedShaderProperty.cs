using UnityEditor;

namespace UnityEditor.Rendering.HighDefinition.Compositor
{
    internal class SerializedShaderProperty
    {
        public SerializedProperty propertyName;
        public SerializedProperty propertyType;
        public SerializedProperty propertyValue;
        public SerializedProperty rangeLimits;

        public SerializedShaderProperty(SerializedProperty root)
        {
            propertyName = root.FindPropertyRelative("propertyName");
            propertyType = root.FindPropertyRelative("propertyType");
            propertyValue = root.FindPropertyRelative("value");
            rangeLimits = root.FindPropertyRelative("rangeLimits");
        }
    }
}
