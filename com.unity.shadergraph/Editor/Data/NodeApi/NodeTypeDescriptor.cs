using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    public struct NodeTypeDescriptor
    {
        public string path { get; set; }

        public string name { get; set; }

        public void AddInput(int id, string displayName, PortValue value)
        {
            // TODO: something
        }

        public void AddOutput(int id, string displayName, PortValueType value)
        {
            // TODO: something
        }
    }
}
