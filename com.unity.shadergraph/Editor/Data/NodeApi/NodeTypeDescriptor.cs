using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public struct NodeTypeDescriptor
    {
        public string path { get; set; }

        public string name { get; set; }

        public List<InputPort> inputs { get; set; }

        public List<OutputPort> outputs { get; set; }
    }
}
