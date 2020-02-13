using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    sealed class ContextData
    {
        [SerializeField]
        string m_ContextName;

        [SerializeField]
        Vector2 m_Position;

        public ContextData()
        {
        }

        public string contextName => m_ContextName;

        public Vector2 position
        {
            get => m_Position;
            set => m_Position = value;
        }

        public static ContextData Copy(ContextData other)
        {
            return new ContextData()
            {
                m_ContextName = other.contextName,
                m_Position = other.position,
            };
        }

        // Define both Contexts statically
        // Contexts should be defined via some sort of Descriptor API
        // But currently we only have two and they only have one Port type
        public static ContextData Vertex => new ContextData()
        {
            m_ContextName = "Vertex",
        };

        public static ContextData Fragment => new ContextData()
        {
            m_ContextName = "Fragment",
        };
    }
}
