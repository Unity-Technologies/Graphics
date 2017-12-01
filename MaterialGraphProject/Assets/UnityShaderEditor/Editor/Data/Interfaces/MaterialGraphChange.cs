using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
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

    public class LayerAdded : GraphChange
    {
        public LayerAdded(LayeredShaderGraph.Layer layer)
        {
            this.layer = layer;
        }

        public LayeredShaderGraph.Layer layer { get; private set; }
    }

    public class LayerRemoved : GraphChange
    {
        public LayerRemoved(Guid id)
        {
            this.id = id;
        }

        public Guid id { get; private set; }
    }
}
