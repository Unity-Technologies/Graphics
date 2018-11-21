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
        // TODO: Use versioning
        readonly int m_Index;

        internal int index => m_Index - 1;

        public bool isValid => index > 0;

        internal HlslSourceRef(int index)
        {
            m_Index = index + 1;
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
        public InputPortRef inputPortRef => m_Union.inputPortRef;
        public OutputPortRef outputPortRef => m_Union.outputPortRef;
        public float vector1Value => m_Union.vector1Value;
        public HlslValueRef valueRef => m_Union.valueRef;

        internal HlslArgument(InputPortRef portRef) : this()
        {
            type = HlslArgumentType.InputPort;
            m_Union.inputPortRef = portRef;
        }

        internal HlslArgument(OutputPortRef portRef) : this()
        {
            type = HlslArgumentType.OutputPort;
            m_Union.outputPortRef = portRef;
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
            public InputPortRef inputPortRef;
            [FieldOffset(0)]
            public OutputPortRef outputPortRef;
            [FieldOffset(0)]
            public float vector1Value;
            [FieldOffset(0)]
            public HlslValueRef valueRef;
        }
    }

    public enum HlslArgumentType
    {
        InputPort,
        OutputPort,
        Vector1,
        Value
    }

    struct HlslValue
    {
        // TODO: Support for more types
        public float value;
    }

    public struct HlslValueRef
    {
        // TODO: Use versioning
        readonly int m_Index;

        internal int index => m_Index - 1;

        public bool isValid => index > 0;

        internal HlslValueRef(int index)
        {
            m_Index = index + 1;
        }
    }

    public struct HlslFunctionDescriptor
    {
        public HlslSourceRef source { get; set; }
        public string name { get; set; }
        public HlslArgumentList arguments { get; set; }
        public OutputPortRef returnValue { get; set; }
    }

    public struct HlslArgumentList : IEnumerable<HlslArgument>
    {
        List<HlslArgument> m_Arguments;

        public void Add(InputPortRef portRef)
        {
            m_Arguments = m_Arguments ?? new List<HlslArgument>();
            m_Arguments.Add(new HlslArgument(portRef));
        }

        public void Add(OutputPortRef portRef)
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
