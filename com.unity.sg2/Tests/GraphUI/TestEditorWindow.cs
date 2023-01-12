using System;
using System.Collections.Generic;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class TestEditorWindow : ShaderGraphEditorWindow
    {
        public BlackboardView blackboardView => m_BlackboardView;

        void OnDestroy()
        {
            previewUpdateDispatcher.Cleanup();
        }

        protected override GraphView CreateGraphView()
        {
            GraphTool.Preferences.SetInitialItemLibrarySize(ItemLibraryService.Usage.CreateNode, new Vector2(425, 100), 2.0f);

            var testGraphView = new TestGraphView(this, GraphTool, GraphTool.Name, m_PreviewUpdateDispatcher);
            return testGraphView;
        }

        /// <summary>
        /// Returns first instance of node with this name
        /// Uses AbstractNodeModel.Title as the name to compare against
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        internal AbstractNodeModel GetNodeModelFromGraphByName(string nodeName)
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
        /// Uses AbstractNodeModel.Title as the name to compare against
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        internal List<AbstractNodeModel> GetNodeModelsFromGraphByName(string nodeName)
        {
            var outNodeModels = new List<AbstractNodeModel>();
            var nodeModels = GraphView.GraphModel.NodeModels;
            foreach (var nodeModel in nodeModels)
            {
                if (nodeModel is NodeModel concreteNodeModel && concreteNodeModel.Title == nodeName)
                    outNodeModels.Add(nodeModel);
            }

            return outNodeModels;
        }

        internal WireModel GetEdgeModelFromGraphByName(string sourceNodeName, string destinationNodeName)
        {
            var edgeModels = GraphView.GraphModel.WireModels;
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

        internal List<WireModel> GetEdgeModelsFromGraphByName(string sourceNodeName, string destinationNodeName)
        {
            var outEdgeModels = new List<WireModel>();
            var edgeModels = GraphView.GraphModel.WireModels;
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
