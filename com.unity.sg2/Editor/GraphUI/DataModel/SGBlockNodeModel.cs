using System.Linq;
using Unity.GraphToolsFoundation;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class SGBlockNodeModel : BlockNodeModel, IGraphDataOwner
    {
        [field: SerializeField, HideInInspector] public string ContextEntryName { get; set; }

        protected override PortModel CreatePort(PortDirection direction,
            PortOrientation orientation,
            string portName,
            PortType portType,
            TypeHandle dataType,
            string portId,
            PortModelOptions options)
        {
            return new SGPortModel(this, direction, orientation, portName ?? "", portType, dataType, portId, options);
        }

        protected override void OnDefineNode()
        {
            if (ContextNodeModel is not SGContextNodeModel graphDataContext)
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
                sgConstant.BindTo(graphDataContext.graphDataName, ContextEntryName);
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

        // Implementation of IGraphDataOwner is forwarded to the owning context so we can still be treated like a
        // regular node.
        public string graphDataName => (ContextNodeModel as IGraphDataOwner)?.graphDataName;
        public RegistryKey registryKey => (ContextNodeModel as IGraphDataOwner)?.registryKey ?? default;
        public bool existsInGraphData => (ContextNodeModel as IGraphDataOwner)?.existsInGraphData ?? false;
    }
}
