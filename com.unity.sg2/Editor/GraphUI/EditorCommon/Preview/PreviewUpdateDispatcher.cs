using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Is responsible for taking in change lists and sending requests for updates to the preview service
    /// </summary>
    public class PreviewUpdateDispatcher
    {
        HeadlessPreviewManager m_PreviewHandlerInstance;
        MainPreviewData m_MainPreviewData;
        ShaderGraphModel m_GraphModel;

        public void Initialize(ShaderGraphModel shaderGraphModel, IPreviewUpdateReceiver previewUpdateReceiver)
        {
            m_GraphModel = shaderGraphModel;
            m_MainPreviewData = shaderGraphModel.MainPreviewData;

            // Initialize the headless preview
            m_PreviewHandlerInstance = new HeadlessPreviewManager();
            m_PreviewHandlerInstance.Initialize(shaderGraphModel.DefaultContextName, m_MainPreviewData.MainPreviewSize);
            m_PreviewHandlerInstance.SetActiveGraph(shaderGraphModel.GraphHandler);
            m_PreviewHandlerInstance.SetActiveRegistry(shaderGraphModel.RegistryInstance.Registry);
            m_PreviewHandlerInstance.SetActiveTarget(shaderGraphModel.ActiveTarget);
            m_PreviewHandlerInstance.SetPreviewUpdateReceiver(previewUpdateReceiver);
        }

        public void OnPreviewListenerAdded(string listenerID, HeadlessPreviewManager.PreviewRenderMode previewRenderMode)
        {
            m_PreviewHandlerInstance.RequestPreviewUpdate(listenerID, previewRenderMode);
        }

        public void OnListenerConnectionChanged(string nodeName, bool wasNodeDeleted = false)
        {
            var impactedNodes = m_PreviewHandlerInstance.NotifyNodeFlowChanged(nodeName, wasNodeDeleted);
            foreach (var downstreamNode in impactedNodes)
            {
                m_PreviewHandlerInstance.RequestPreviewUpdate(downstreamNode);
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
                m_PreviewHandlerInstance.RequestPreviewUpdate(downstreamNode);
            }
        }

        public void OnLocalPropertyChanged(string nodeName, string propertyName, object newValue)
        {
            var impactedNodes = m_PreviewHandlerInstance.SetLocalProperty(nodeName, propertyName, newValue);
            foreach (var downstreamNode in impactedNodes)
            {
                m_PreviewHandlerInstance.RequestPreviewUpdate(downstreamNode);
            }
        }
    }
}
