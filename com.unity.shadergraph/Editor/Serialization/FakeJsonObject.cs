using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    [Serializable]
    public struct FakeJsonObject
    {
        [SerializeField]
        string m_Type;

        [SerializeField]
        string m_Id;

        public string id
        {
            get => m_Id;
            set => m_Id = value;
        }

        public string type
        {
            get => m_Type;
            set => m_Type = value;
        }

        public void Reset()
        {
            m_Id = null;
            m_Type = null;
        }
    }
}
