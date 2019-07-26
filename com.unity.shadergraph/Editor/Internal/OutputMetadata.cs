using System;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    public struct OutputMetadata
    {
        int m_Index;
        string m_ReferenceName;

        internal OutputMetadata(int index, string referenceName)
        {
            m_Index = index;
            m_ReferenceName = referenceName;
        }

        public int index => m_Index;

        public string referenceName => m_ReferenceName;

        internal bool isValid => referenceName != null;
    }
}
