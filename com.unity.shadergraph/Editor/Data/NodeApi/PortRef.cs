using System;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public struct PortRef
    {
        internal int value { get; }
        internal int index => value - 1;
        internal bool isInput { get; }

        internal PortRef(int value, bool isInput)
        {
            this.value = value;
            this.isInput = isInput;
        }

        internal bool isValid => value > 0;
    }
}
