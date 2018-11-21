using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public struct NodeTypeDescriptor
    {
        public string path { get; set; }

        public string name { get; set; }

        public List<InputPortRef> inputs { get; set; }

        public List<OutputPortRef> outputs { get; set; }
    }
}
