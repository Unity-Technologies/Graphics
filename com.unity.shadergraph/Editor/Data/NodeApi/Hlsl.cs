using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.ShaderGraph
{
    public struct HlslSource
    {
        public HlslSourceType type { get; private set; }
        public string value { get; private set; }

        public static HlslSource File(string source)
        {
            if (!System.IO.File.Exists(Path.GetFullPath(source)))
            {
                throw new ArgumentException($"Cannot open file at \"{source}\"");
            }
            
            return new HlslSource
            {
                type = HlslSourceType.File,
                value = source
            };
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
        public InputPort inputPort => m_Union.inputPort;
        public OutputPort outputPort => m_Union.outputPort;
        public float vector1Value => m_Union.vector1Value;
        public HlslValueRef valueRef => m_Union.valueRef;

        internal HlslArgument(InputPort port) : this()
        {
            type = HlslArgumentType.InputPort;
            m_Union.inputPort = port;
        }

        internal HlslArgument(OutputPort port) : this()
        {
            type = HlslArgumentType.OutputPort;
            m_Union.outputPort = port;
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
            public InputPort inputPort;
            [FieldOffset(0)]
            public OutputPort outputPort;
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
        public HlslSource source { get; set; }
        public string name { get; set; }
        public HlslArgumentList arguments { get; set; }
        public OutputPort returnValue { get; set; }
    }

    public struct HlslArgumentList : IEnumerable<HlslArgument>
    {
        List<HlslArgument> m_Arguments;

        public void Add(InputPort port)
        {
            m_Arguments = m_Arguments ?? new List<HlslArgument>();
            m_Arguments.Add(new HlslArgument(port));
        }

        public void Add(OutputPort port)
        {
            m_Arguments = m_Arguments ?? new List<HlslArgument>();
            m_Arguments.Add(new HlslArgument(port));
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
