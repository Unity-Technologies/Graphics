using System;

namespace UnityEditor.ShaderGraph
{
    public struct InputPort
    {
        internal int value { get; }
        internal int index => value - 1;

        internal InputPort(int value)
        {
            this.value = value;
        }

        internal bool isValid => value > 0;
    }

    public struct OutputPort
    {
        internal int value { get; }
        internal int index => value - 1;

        internal OutputPort(int value)
        {
            this.value = value;
        }

        internal bool isValid => value > 0;
    }
}
