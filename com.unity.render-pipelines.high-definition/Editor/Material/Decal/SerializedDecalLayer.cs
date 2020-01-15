using System;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    public struct SerializedDecalLayer
    {
        SerializedProperty m_ValueProperty;
        SerializedProperty m_RootProperty;

        public SerializedDecalLayer(SerializedProperty serializedProperty)
        {
            m_RootProperty = serializedProperty;
            if (serializedProperty == null) throw new ArgumentNullException(nameof(serializedProperty));

            m_ValueProperty = serializedProperty.FindPropertyRelative("m_Value");
            if (m_ValueProperty == null) throw new ArgumentException("Can't find property 'm_Value'.");
        }

        public DecalLayer value
        {
            get => (DecalLayer) m_ValueProperty.intValue;
            set => m_ValueProperty.intValue = (int)value;
        }

        public static explicit operator SerializedProperty(in SerializedDecalLayer v) => v.m_RootProperty;
    }
}
