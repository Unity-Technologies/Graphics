using UnityEditor;

namespace UnityEditor.Rendering.HighDefinition.Compositor
{
    internal class SerializedShaderProperty
    {
        public SerializedProperty PropertyName;
        public SerializedProperty PropertyType;
        public SerializedProperty PropertyValue;
        public SerializedProperty RangeLimits;

        public SerializedShaderProperty(SerializedProperty root)
        {
            PropertyName = root.FindPropertyRelative("m_PropertyName");
            PropertyType = root.FindPropertyRelative("m_Type");
            PropertyValue = root.FindPropertyRelative("m_Value");
            RangeLimits = root.FindPropertyRelative("m_RangeLimits");
        }
    }
}
