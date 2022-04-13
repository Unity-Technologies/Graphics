using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Internal
{
    public struct GraphCode
    {
        public string code { get; internal set; }
        public ShaderGraphRequirements requirements { get; internal set; }
        public IEnumerable<AbstractShaderProperty> properties;
    }
}
