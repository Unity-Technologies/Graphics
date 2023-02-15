using System;
using System.Linq;
using Unity.GraphToolsFoundation;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class SGBlockNodeModel : BlockNodeModel, IGraphDataOwner<SGBlockNodeModel>
    {
        [field: SerializeField, HideInInspector] public string ContextEntryName { get; set; }

        protected override PortModel CreatePort(PortDirection direction,
            PortOrientation orientation,
            string portName,
            PortType portType,
            TypeHandle dataType,
            string portId,
            PortModelOptions options,
            Attribute[] attributes)
        {
            return new SGPortModel(this, direction, orientation, portName ?? "", portType, dataType, portId, options, attributes);
        }

        protected override void OnDefineNode()
        {
            if (ContextNodeModel is not IGraphDataOwner<SGContextNodeModel> graphDataContext)
            {
                return;
            }

            if (!graphDataContext.TryGetNodeHandler(out var nodeHandler))
            {
                return;
            }

            var portHandler = nodeHandler.GetPort(ContextEntryName);
            var type = ShaderGraphExampleTypes.GetGraphType(portHandler);

            var input = this.AddDataInputPort(ContextEntryName, type);
            if (input.EmbeddedValue is BaseShaderGraphConstant sgConstant)
            {
                sgConstant.Initialize((SGGraphModel) GraphModel, graphDataContext.graphDataName, ContextEntryName);
            }
        }

        public override bool IsCompatibleWith(ContextNodeModel context)
        {
            if (context is SGContextNodeModel graphDataContext)
            {
                // Prevent moving between context types (i.e. vertex to fragment), which doesn't make sense
                if (ContextNodeModel != null && graphDataContext.graphDataName != graphDataName)
                {
                    return false;
                }

                // Prevent duplicate blocks for context entries
                return context.GraphElementModels
                    .OfType<SGBlockNodeModel>()
                    .Where(otherBlock => otherBlock != this)
                    .All(otherBlock => otherBlock.ContextEntryName != ContextEntryName);
            }

            // GTF wants us to maintain compatibility with a base ContextNodeModel for item library support
            // (see ContextNodeModel.InsertBlock).
            return base.IsCompatibleWith(context);
        }

        /// <summary>
        /// The identifier/unique name used to represent this entity and retrieve info. regarding it from CLDS.
        /// </summary>
        public string graphDataName => (ContextNodeModel as IGraphDataOwner<SGContextNodeModel>)?.graphDataName;

        /// <summary>
        /// The <see cref="RegistryKey"/> that represents the concrete type within the Registry, of this object.
        /// </summary>
        public RegistryKey registryKey => (ContextNodeModel as IGraphDataOwner<SGContextNodeModel>)?.registryKey ?? default;
    }
}
