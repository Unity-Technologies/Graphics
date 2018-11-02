using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.ShaderGraph
{
    struct HlslSource
    {
        public HlslSourceType type;
        public string source;
    }

    public struct HlslSourceRef
    {
        public int id { get; }
        public bool isValid => id > 0;

        internal HlslSourceRef(int id)
        {
            this.id = id;
        }
    }

    public enum HlslSourceType
    {
        File,
        String
    }

    public struct HlslArgument
    {
        ArgumentUnion m_Union;
        public HlslArgumentType type { get; }
        public PortRef portRef => m_Union.portRef;
        public float vector1Value => m_Union.vector1Value;
        public HlslValueRef valueRef => m_Union.valueRef;

        internal HlslArgument(PortRef portRef) : this()
        {
            type = HlslArgumentType.Port;
            m_Union.portRef = portRef;
        }

        internal HlslArgument(float vector1Value) : this()
        {
            type = HlslArgumentType.Vector1;
            m_Union.vector1Value = vector1Value;
        }

        internal HlslArgument(HlslValueRef valueRef) : this()
        {
            type = HlslArgumentType.Value;
            m_Union.valueRef = valueRef;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct ArgumentUnion
        {
            [FieldOffset(0)]
            public PortRef portRef;
            [FieldOffset(0)]
            public float vector1Value;
            [FieldOffset(0)]
            public HlslValueRef valueRef;
        }
    }

    public enum HlslArgumentType
    {
        Port,
        Vector1,
        Value
    }

    public struct HlslValueRef
    {

    }

    public struct HlslFunctionDescriptor
    {
        public HlslSourceRef source { get; set; }
        public string name { get; set; }
        public HlslArgumentList arguments { get; set; }
        public PortRef returnValue { get; set; }
    }

    public struct HlslArgumentList : IEnumerable<HlslArgument>
    {
        List<HlslArgument> m_Arguments;

        public void Add(PortRef portRef)
        {
            m_Arguments = m_Arguments ?? new List<HlslArgument>();
            m_Arguments.Add(new HlslArgument(portRef));
        }

        public void Add(HlslValueRef valueRef)
        {
            m_Arguments = m_Arguments ?? new List<HlslArgument>();
            m_Arguments.Add(new HlslArgument(valueRef));
        }

        public void Add(float vector1Value)
        {
            m_Arguments = m_Arguments ?? new List<HlslArgument>();
            m_Arguments.Add(new HlslArgument(vector1Value));
        }

        public int Count => m_Arguments.Count;

        public IEnumerator<HlslArgument> GetEnumerator()
        {
            // TODO: Make non allocating
            m_Arguments = m_Arguments ?? new List<HlslArgument>();
            return m_Arguments.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
