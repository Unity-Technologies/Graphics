using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    sealed class ContextData
    {
        [SerializeField]
        Vector2 m_Position;

        public ContextData()
        {
        }

        public Vector2 position
        {
            get => m_Position;
            set => m_Position = value;
        }
    }
}
