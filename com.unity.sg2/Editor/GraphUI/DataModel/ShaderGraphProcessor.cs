using System.Linq;
using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphProcessor : GraphProcessor
    {
        const string k_RedirectMissingInputMessage = "Node has no input and default value will be 0.";
        const string k_OutOfDateNodeMessage = "There is a newer version of this node available. Inspect node for details.";

        public override GraphProcessingResult ProcessGraph(GraphModel graphModel, GraphChangeDescription changes)
        {
            var result = new GraphProcessingResult();

            foreach (var node in graphModel.NodeModels)
            {
                switch (node)
                {
                    case RedirectNodeModel redirectNode when !redirectNode.GetIncomingEdges().Any() && redirectNode.GetConnectedWires().Any():
                        result.AddWarning(k_RedirectMissingInputMessage, node);
                        break;

                    case GraphDataNodeModel graphDataNodeModel:
                        if (graphDataNodeModel.currentVersion < graphDataNodeModel.latestAvailableVersion &&
                            graphDataNodeModel.dismissedUpgradeVersion < graphDataNodeModel.latestAvailableVersion)
                        {
                            result.AddWarning(k_OutOfDateNodeMessage, node);
                        }

                        break;
                }
            }

            return result;
        }
    }
}
