using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphProcessor : IGraphProcessor
    {
        const string k_RedirectMissingInputMessage = "Node has no input and default value will be 0.";

        public GraphProcessingResult ProcessGraph(IGraphModel graphModel)
        {
            var result = new GraphProcessingResult();

            foreach (var missingInput in graphModel
                .NodeModels
                .OfType<RedirectNodeModel>()
                .Where(r => !r.GetIncomingEdges().Any() && r.GetConnectedEdges().Any()))
            {
                result.AddWarning(k_RedirectMissingInputMessage, missingInput);
            }

            return result;
        }
    }
}
