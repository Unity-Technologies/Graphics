using System.Linq;
using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphProcessor : GraphProcessor
    {
        const string k_RedirectMissingInputMessage = "Node has no input and default value will be 0.";
        const string k_OutOfDateNodeMessage = "There is a newer version of this node available. Inspect node for details.";
        GraphModel m_GraphModel;

        public ShaderGraphProcessor(GraphModel graphModel)
        {
            m_GraphModel = graphModel;
        }

        public override BaseGraphProcessingResult ProcessGraph(GraphChangeDescription changes)
        {
            var result = new ErrorsAndWarningsResult();

            foreach (var node in m_GraphModel.NodeModels)
            {
                switch (node)
                {
                    case SGRedirectNodeModel redirectNode when !redirectNode.GetIncomingEdges().Any() && redirectNode.GetConnectedWires().Any():
                        result.AddWarning(k_RedirectMissingInputMessage, node);
                        break;

                    case SGNodeModel graphDataNodeModel:
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
