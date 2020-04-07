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
            propertyName = root.FindPropertyRelative("m_PropertyName");
            propertyType = root.FindPropertyRelative("m_Type");
            propertyValue = root.FindPropertyRelative("m_Value");
            rangeLimits = root.FindPropertyRelative("m_RangeLimits");
        }
    }
}
