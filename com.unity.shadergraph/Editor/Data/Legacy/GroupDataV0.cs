using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Legacy
{
    [Serializable]
    class GroupDataV0
    {
        [SerializeField]
        string m_GuidSerialized = default;

        [SerializeField]
        string m_Title = default;

        [SerializeField]
        Vector2 m_Position = default;

        public string guidSerialized => m_GuidSerialized;

        public string title => m_Title;

        public Vector2 position => m_Position;
    }
}
