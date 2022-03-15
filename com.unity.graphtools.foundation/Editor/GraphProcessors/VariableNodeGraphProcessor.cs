using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    class VariableNodeGraphProcessor : IGraphProcessor
    {
        /// <inheritdoc />
        public GraphProcessingResult ProcessGraph(IGraphModel graphModel, GraphChangeDescription changes)
        {
            var res = new GraphProcessingResult();
            foreach (var variableNodeModel in graphModel.NodeModels.OfType<IVariableNodeModel>().Where(v =>
                    !((Stencil)graphModel.Stencil).CanAllowVariableInGraph(v.VariableDeclarationModel, graphModel)))
            {
                res.AddError("This variable node is not allowed on the graph.", variableNodeModel);
            }

            return res;
        }
    }
}
