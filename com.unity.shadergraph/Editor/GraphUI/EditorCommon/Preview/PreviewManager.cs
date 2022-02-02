using System;
using System.Collections.Generic;
using Editor.GraphUI.Utilities;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon.Preview
{
    using PreviewRenderMode = HeadlessPreviewManager.PreviewRenderMode;

    public class PreviewManager
    {
        HeadlessPreviewManager m_PreviewHandlerInstance;

        HashSet<string> m_DirtyNodes;

        ShaderGraphModel m_GraphModel;

        Dictionary<string, SerializableGUID> m_NodeLookupTable;

        public PreviewManager(ShaderGraphModel graphModel)
        {
            m_GraphModel = graphModel;

            m_PreviewHandlerInstance = new HeadlessPreviewManager();

            m_PreviewHandlerInstance.SetActiveGraph(m_GraphModel.GraphHandler);
            m_PreviewHandlerInstance.SetActiveRegistry(m_GraphModel.RegistryInstance);

            m_DirtyNodes = new HashSet<string>();
            m_NodeLookupTable = new Dictionary<string, SerializableGUID>();

            // Initialize preview data for any nodes that exist on graph load
            foreach (var nodeModel in graphModel.NodeModels)
            {
                if(nodeModel is GraphDataNodeModel graphDataNodeModel)
                    OnNodeAdded(graphDataNodeModel.graphDataName, graphDataNodeModel.Guid);
            }
        }

        public void Update()
        {
            var updatedNodes = new List<string>();
            foreach (string nodeName in m_DirtyNodes)
            {
                var nodeGuid = m_NodeLookupTable[nodeName];
                m_GraphModel.TryGetModelFromGuid(nodeGuid, out var nodeModel);
                if (nodeModel is GraphDataNodeModel graphDataNodeModel)
                {
                    var previewOutputState = m_PreviewHandlerInstance.RequestNodePreviewImage(nodeName, out var nodeRenderOutput, out var shaderMessages);
                    if (nodeRenderOutput != graphDataNodeModel.PreviewTexture)
                    {
                        // Headless preview manager handles assigning correct texture in case of completion, error state, still updating etc.
                        // We just need to update the texture on this side regardless of what happens
                        graphDataNodeModel.OnPreviewTextureUpdated(nodeRenderOutput);

                        // Node is updated, remove from dirty list
                        if (previewOutputState == HeadlessPreviewManager.PreviewOutputState.Complete)
                            updatedNodes.Add(nodeName);
                        if (previewOutputState == HeadlessPreviewManager.PreviewOutputState.ShaderError)
                        {
                            updatedNodes.Add(nodeName);
                            // TODO: Handle displaying error badges on nodes
                            foreach (var errorMessage in shaderMessages)
                            {
                                Debug.Log(errorMessage);
                            }
                        }
                    }
                }
            }

            // Clean up any nodes that were successfully updated from the dirty list
            foreach (var updatedNode in updatedNodes)
                m_DirtyNodes.Remove(updatedNode);
        }

        public void OnPreviewExpansionChanged(string nodeName, bool newExpansionState) { }

        public void OnPreviewModeChanged(string nodeName, PreviewRenderMode newPreviewMode) { }

        public void OnNodeFlowChanged(string nodeName)
        {
            m_DirtyNodes.Add(nodeName);
        }

        public void OnNodeAdded(String nodeName, SerializableGUID nodeGuid)
        {
            if (m_NodeLookupTable.ContainsKey(nodeName))
                return;

            // Add node to dirty list so preview image can be updated next frame
            m_DirtyNodes.Add(nodeName);
            m_NodeLookupTable.Add(nodeName, nodeGuid);
        }

        public void OnNodeRemoved(String nodeName)
        {
            m_DirtyNodes.Remove(nodeName);
            m_NodeLookupTable.Remove(nodeName);
        }

        public void OnGlobalPropertyChanged(string propertyName, object newValue) { }

        public void OnLocalPropertyChanged(string nodeName, string propertyName, object newValue)
        {
            m_DirtyNodes.Add(nodeName);
            m_PreviewHandlerInstance.SetLocalProperty(nodeName, propertyName, newValue);
        }
    }
}
