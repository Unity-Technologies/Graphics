using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public struct OutputMetadata
    {
        [SerializeField]
        int m_Index;

        [SerializeField]
        string m_ReferenceName;

        [SerializeField]
        int m_Id;

        internal OutputMetadata(int index, string referenceName,int id)
        {
            m_Index = index;
            m_ReferenceName = referenceName;
            m_Id = id;
        }

        public int index => m_Index;

        public int id => m_Id;

        public string referenceName => m_ReferenceName;

        internal bool isValid => referenceName != null;
    }
}
