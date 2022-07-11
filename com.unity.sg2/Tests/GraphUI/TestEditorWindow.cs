using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.VersionControl;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    public class TestEditorWindow : ShaderGraphEditorWindow
    {
        public BlackboardView blackboardView => m_BlackboardView;

        public PreviewManager previewManager => m_PreviewManager;
        protected override GraphView CreateGraphView()
        {
            GraphTool.Preferences.SetInitialSearcherSize(SearcherService.Usage.CreateNode, new Vector2(425, 100), 2.0f);

            var testGraphView = new TestGraphView(this, GraphTool, GraphTool.Name);
            m_PreviewManager = new PreviewManager(testGraphView.GraphViewModel.GraphModelState);
            m_GraphViewStateObserver = new GraphViewStateObserver(testGraphView.GraphViewModel.GraphModelState, m_PreviewManager);
            GraphTool.ObserverManager.RegisterObserver(m_GraphViewStateObserver);

            // TODO (Brett) Command registration or state handler creation belongs here.
            // Example: graphView.RegisterCommandHandler<SetNumberOfInputPortCommand>(SetNumberOfInputPortCommand.DefaultCommandHandler);

            return testGraphView;
        }

        /// <summary>
        /// Returns first instance of node with this name
        /// Uses INodeModel.Title as the name to compare against
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        public INodeModel GetNodeModelFromGraphByName(string nodeName)
        {
            var nodeModels = GraphView.GraphModel.NodeModels;
            foreach (var nodeModel in nodeModels)
            {
                if (nodeModel is NodeModel concreteNodeModel && concreteNodeModel.Title == nodeName && !concreteNodeModel.Destroyed)
                    return concreteNodeModel;
            }

            return null;
        }


        /// <summary>
        /// Returns all instances of node with this name
        /// Uses INodeModel.Title as the name to compare against
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        public List<INodeModel> GetNodeModelsFromGraphByName(string nodeName)
        {
            var outNodeModels = new List<INodeModel>();
            var nodeModels = GraphView.GraphModel.NodeModels;
            foreach (var nodeModel in nodeModels)
            {
                if (nodeModel is NodeModel concreteNodeModel && concreteNodeModel.Title == nodeName)
                    outNodeModels.Add(nodeModel);
            }

            return outNodeModels;
        }

        public IEdgeModel GetEdgeModelFromGraphByName(string sourceNodeName, string destinationNodeName)
        {
            var edgeModels = GraphView.GraphModel.EdgeModels;
            foreach (var edgeModel in edgeModels)
            {
                var fromPortNodeModel = (NodeModel)edgeModel.FromPort.NodeModel;
                var toPortNodeModel = (NodeModel)edgeModel.ToPort.NodeModel;

                if (fromPortNodeModel.DisplayTitle == sourceNodeName
                    && toPortNodeModel.DisplayTitle == destinationNodeName)
                {
                    return edgeModel;
                }
            }

            return null;
        }

        public List<IEdgeModel> GetEdgeModelsFromGraphByName(string sourceNodeName, string destinationNodeName)
        {
            var outEdgeModels = new List<IEdgeModel>();
            var edgeModels = GraphView.GraphModel.EdgeModels;
            foreach (var edgeModel in edgeModels)
            {
                var fromPortNodeModel = (NodeModel)edgeModel.FromPort.NodeModel;
                var toPortNodeModel = (NodeModel)edgeModel.ToPort.NodeModel;

                if (fromPortNodeModel.DisplayTitle == sourceNodeName
                    && toPortNodeModel.DisplayTitle == destinationNodeName)
                {
                    outEdgeModels.Add(edgeModel);
                }
            }

            return outEdgeModels;
        }
    }
}
