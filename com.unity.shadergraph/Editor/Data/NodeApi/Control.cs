namespace UnityEditor.ShaderGraph
{
    struct ControlState
    {
        public bool wasModified;
        public Identifier nodeId;
        public string label;
        public float value;
    }

    struct ControlRefSlice
    {
        public NodeTypeState nodeTypeState;
        public int startIndex;
        public int length;
    }

    public struct ControlRef
    {
        // TODO: Use versioning
        int m_Index;

        internal ControlRef(int index)
        {
            m_Index = index + 1;
        }

        internal int index => m_Index - 1;

        // TODO: Obviously something different
        public bool isValid => m_Index > 0;
    }
}
