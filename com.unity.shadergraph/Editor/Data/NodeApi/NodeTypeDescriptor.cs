using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    public struct NodeTypeDescriptor
    {
        public string path { get; set; }

        public string name { get; set; }

        List<InputPortDescriptor> m_InputPortDescriptors;

        List<OutputPortDescriptor> m_OutputPortDescriptors;

        public PortRef AddInput(int id, string displayName, PortValue value)
        {
            m_InputPortDescriptors = m_InputPortDescriptors ?? new List<InputPortDescriptor>();
            m_InputPortDescriptors.Add(new InputPortDescriptor { id = id, displayName = displayName, value = value });
            return new PortRef { id = id };
        }

        public PortRef AddOutput(int id, string displayName, PortValueType type)
        {
            m_OutputPortDescriptors = m_OutputPortDescriptors ?? new List<OutputPortDescriptor>();
            m_OutputPortDescriptors.Add(new OutputPortDescriptor { id = id, displayName = displayName, type = type });
            return new PortRef { id = id };
        }
    }
}
