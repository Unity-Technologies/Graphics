using System;
using System.Collections.Generic;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public abstract class AbstractMasterNode : AbstractMaterialNode, IMasterNode
    {
        protected override bool generateDefaultInputs { get { return false; } }

        public override IEnumerable<ISlot> GetInputsWithNoConnection()
        {
            return new List<ISlot>();
        }

        public override bool hasPreview
        {
            get { return true; }
        }
        
        public override bool allowedInSubGraph
        {
            get { return false; }
        }

        public bool isActiveMaster
        {
            get
            {
                var graph = owner as MaterialGraph;
                return graph != null && graph.masterNode == this;
            }
            set
            {
                var graph = owner as MaterialGraph;
                if (graph == null || value == isActiveMaster)
                    return;
                graph.masterNode = value ? this : null;
            }
        }

        public bool canBeActiveMaster
        {
            get { return owner is MaterialGraph; }
        }

        public abstract string GetFullShader(GenerationMode mode, out List<PropertyGenerator.TextureInfo> configuredTextures);
        public abstract string GetSubShader(GenerationMode mode, PropertyGenerator shaderPropertiesVisitor);
    }
}
