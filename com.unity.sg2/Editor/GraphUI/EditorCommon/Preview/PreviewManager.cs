using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    using PreviewRenderMode = HeadlessPreviewManager.PreviewRenderMode;

    public class PreviewManager
    {
        bool m_isInitialized = false;

        public bool IsInitialized
        {
            get => m_isInitialized;
            set => m_isInitialized = value;
        }

        HeadlessPreviewManager m_PreviewHandlerInstance;

        GraphModelStateComponent m_GraphModelStateComponent;

        HashSet<string> m_DirtyNodes;

        ShaderGraphModel m_GraphModel;

        Dictionary<string, SerializableGUID> m_NodeLookupTable;

        public PreviewManager(GraphModelStateComponent graphModelStateComponent)
        {
            m_GraphModelStateComponent = graphModelStateComponent;

            m_PreviewHandlerInstance = new HeadlessPreviewManager();

            m_DirtyNodes = new HashSet<string>();
            m_NodeLookupTable = new Dictionary<string, SerializableGUID>();
        }

        public void Initialize(ShaderGraphModel graphModel, bool wasWindowCloseCancelled)
        {
            // Can be null when the editor window is opened to the onboarding page
            if (graphModel == null)
                return;

            m_isInitialized = true;
            m_GraphModel = graphModel;

            m_PreviewHandlerInstance.SetActiveGraph(m_GraphModel.GraphHandler);
            m_PreviewHandlerInstance.SetActiveRegistry(m_GraphModel.RegistryInstance);

            // Initialize preview data for any nodes that exist on graph load
            foreach (var nodeModel in m_GraphModel.NodeModels)
            {
                if (nodeModel is GraphDataNodeModel graphDataNodeModel)
                    OnNodeAdded(graphDataNodeModel.graphDataName, graphDataNodeModel.Guid);
            }

            // Call update once at graph load in order to handle updating all existing nodes
            Update();

            // Don't clear dirty state if the window close was cancelled with the graph in dirty state
            if (!wasWindowCloseCancelled)
            {
                // Mark dirty state as cleared afterwards to clear modification state from editor window tab
                m_GraphModel.Asset.Dirty = false;
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

                        using var graphUpdater = m_GraphModelStateComponent.UpdateScope;
                        {
                            graphUpdater.MarkChanged(nodeModel);
                        }

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
            var impactedNodes = m_PreviewHandlerInstance.NotifyNodeFlowChanged(nodeName);
            foreach (var downstreamNode in impactedNodes)
            {
                m_DirtyNodes.Add(downstreamNode);
            }
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

        public void OnGlobalPropertyChanged(string propertyName, object newValue)
        {
            Debug.LogWarning("UNIMPLEMENTED: Preview manager OnGlobalPropertyChanged");
        }

        public void OnLocalPropertyChanged(string nodeName, string propertyName, object newValue)
        {
            m_DirtyNodes.Add(nodeName);
            var impactedNodes = m_PreviewHandlerInstance.SetLocalProperty(nodeName, propertyName, newValue);
            foreach (var downstreamNode in impactedNodes)
            {
                m_DirtyNodes.Add(downstreamNode);
            }
        }
    }
}
