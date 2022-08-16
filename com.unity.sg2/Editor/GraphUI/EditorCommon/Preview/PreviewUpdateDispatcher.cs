using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Is responsible for taking in change lists and sending requests for updates to the preview service
    /// </summary>
    public class PreviewUpdateDispatcher
    {
        HeadlessPreviewManager m_PreviewHandlerInstance;
        MainPreviewData m_MainPreviewData;
        // TODO: Should this communicate with the graph model through an interface layer?
        // Would allow for anyone (in theory) to hook up their own graph model and use our preview dispatcher
        ShaderGraphModel m_GraphModel;

        // TODO: Remove
        IVisualElementScheduler m_Scheduler;

        /// <summary>
        /// Provides this preview update dispatcher with necessary resources for initialization
        /// </summary>
        /// <param name="shaderGraphModel"> Source of the GraphData needed </param>
        /// <param name="previewUpdateReceiver"></param>
        public void Initialize(ShaderGraphModel shaderGraphModel, IPreviewUpdateReceiver previewUpdateReceiver, IVisualElementScheduler scheduler)
        {
            m_GraphModel = shaderGraphModel;
            m_MainPreviewData = shaderGraphModel.MainPreviewData;

            m_Scheduler = scheduler;

            // Initialize the headless preview
            m_PreviewHandlerInstance = new HeadlessPreviewManager();
            m_PreviewHandlerInstance.Initialize(shaderGraphModel.DefaultContextName, m_MainPreviewData.MainPreviewSize);
            m_PreviewHandlerInstance.SetActiveGraph(shaderGraphModel.GraphHandler);
            m_PreviewHandlerInstance.SetActiveRegistry(shaderGraphModel.RegistryInstance.Registry);
            m_PreviewHandlerInstance.SetActiveTarget(shaderGraphModel.ActiveTarget);
            m_PreviewHandlerInstance.SetPreviewUpdateReceiver(previewUpdateReceiver);

            // Request preview data for any nodes that exist on graph load
            // TODO: Think about how to auto-persist this so we don't need to recalculate every time
            foreach (var nodeModel in m_GraphModel.NodeModels)
            {
                switch (nodeModel)
                {
                    case GraphDataContextNodeModel contextNode when contextNode.IsMainContextNode():
                        m_PreviewHandlerInstance.RequestPreviewUpdate(contextNode.graphDataName, m_Scheduler, contextNode.NodePreviewMode);
                        break;
                    case GraphDataNodeModel graphDataNodeModel when graphDataNodeModel.HasPreview:
                        m_PreviewHandlerInstance.RequestPreviewUpdate(graphDataNodeModel.graphDataName, m_Scheduler, graphDataNodeModel.NodePreviewMode);
                        break;
                }
            }
        }

        public void OnListenerAdded(string listenerID, HeadlessPreviewManager.PreviewRenderMode previewRenderMode)
        {
            // TODO: Time-node support: can we get that data here? And then poll for updates in the preview dispatcher as needed
            m_PreviewHandlerInstance.RequestPreviewUpdate(listenerID, m_Scheduler, previewRenderMode);
        }

        public void OnListenerConnectionChanged(string nodeName, bool wasNodeDeleted = false)
        {
            var impactedNodes = m_PreviewHandlerInstance.NotifyNodeFlowChanged(nodeName, wasNodeDeleted);

            // If a node was deleted we don't want to issue update calls for it thereafter
            if (wasNodeDeleted)
                impactedNodes.Remove(nodeName);

            foreach (var downstreamNode in impactedNodes)
            {
                m_PreviewHandlerInstance.RequestPreviewUpdate(downstreamNode, m_Scheduler);
            }
        }

        public void OnGlobalPropertyChanged(string propertyName, object newValue)
        {
            var linkedVariableNodes =  m_GraphModel.GetLinkedVariableNodes(propertyName);

            var variableNodeNames = new List<string>();
            foreach(var node in linkedVariableNodes)
            {
                var nodeModel = node as GraphDataVariableNodeModel;
                variableNodeNames.Add(nodeModel.graphDataName);
            }

            var impactedNodes = m_PreviewHandlerInstance.SetGlobalProperty(propertyName, newValue, variableNodeNames);
            foreach (var downstreamNode in impactedNodes)
            {
                m_PreviewHandlerInstance.RequestPreviewUpdate(downstreamNode, m_Scheduler);
            }
        }

        public void OnLocalPropertyChanged(string nodeName, string propertyName, object newValue)
        {
            var impactedNodes = m_PreviewHandlerInstance.SetLocalProperty(nodeName, propertyName, newValue);
            foreach (var downstreamNode in impactedNodes)
            {
                m_PreviewHandlerInstance.RequestPreviewUpdate(downstreamNode, m_Scheduler);
            }
        }
    }
}
