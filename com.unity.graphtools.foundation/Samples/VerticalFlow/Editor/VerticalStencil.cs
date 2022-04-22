using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalStencil : Stencil
    {
        /// <inheritdoc />
        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphModel graphModel)
        {
            return new VerticalBlackboardGraphModel { GraphModel = graphModel };
        }

        /// <inheritdoc />
        public override IInspectorModel CreateInspectorModel(IModel inspectedModel)
        {
            return new InspectorModel(inspectedModel);
        }

        /// <inheritdoc />
        public override bool CanPasteNode(INodeModel originalModel, IGraphModel graph)
        {
            return originalModel is VerticalNodeModel || originalModel is VariableNodeModel;
        }

        public override  bool CanPasteVariable(IVariableDeclarationModel originalModel, IGraphModel graph)
        {
            return originalModel is VariableDeclarationModel && originalModel.DataType == TypeHandle.Float;
        }

        public static readonly string graphName = "VerticalFlow";
    }
}
