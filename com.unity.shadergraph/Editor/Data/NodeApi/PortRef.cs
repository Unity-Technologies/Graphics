using System;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public struct InputPortRef
    {
        internal int value { get; }
        internal int index => value - 1;

        internal InputPortRef(int value)
        {
            this.value = value;
        }

        internal bool isValid => value > 0;
    }

    [Serializable]
    public struct OutputPortRef
    {
        internal int value { get; }
        internal int index => value - 1;

        internal OutputPortRef(int value)
        {
            this.value = value;
        }

        internal bool isValid => value > 0;
    }
}
