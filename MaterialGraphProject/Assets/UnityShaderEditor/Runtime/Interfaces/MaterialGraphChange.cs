using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    public class ShaderPropertyAdded : GraphChange
    {
        public ShaderPropertyAdded(IShaderProperty shaderProperty)
        {
            this.shaderProperty = shaderProperty;
        }

        public IShaderProperty shaderProperty { get; private set; }
    }

    public class ShaderPropertyRemoved : GraphChange
    {
        public ShaderPropertyRemoved(Guid guid)
        {
            this.guid = guid;
        }

        public Guid guid { get; private set; }
    }
}
