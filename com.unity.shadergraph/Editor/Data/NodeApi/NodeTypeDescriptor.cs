using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public struct NodeTypeDescriptor
    {
        public string path { get; set; }

        public string name { get; set; }

        public List<PortRef> inputs { get; set; }

        public List<PortRef> outputs { get; set; }
    }
}
