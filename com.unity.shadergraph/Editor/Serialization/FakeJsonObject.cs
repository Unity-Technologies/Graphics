using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    [Serializable]
    public class FakeJsonObject
    {
        [SerializeField]
        string m_Type;

        [SerializeField]
        string m_ObjectId;

        public string id
        {
            get => m_ObjectId;
            set => m_ObjectId = value;
        }

        public string type
        {
            get => m_Type;
            set => m_Type = value;
        }

        public void Reset()
        {
            m_ObjectId = null;
            m_Type = null;
        }
    }
}
