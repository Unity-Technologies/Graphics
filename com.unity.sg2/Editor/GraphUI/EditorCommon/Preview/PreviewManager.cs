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
        bool m_IsInitialized;

        public bool IsInitialized
        {
            get => m_IsInitialized;
            set => m_IsInitialized = value;
        }

        HeadlessPreviewManager m_PreviewHandlerInstance;

        GraphModelStateComponent m_GraphModelStateComponent;

        HashSet<string> m_DirtyNodes;

        ShaderGraphModel m_GraphModel;

        Dictionary<string, SerializableGUID> m_NodeLookupTable;

        MainPreviewView m_MainPreviewView;
        MainPreviewData m_MainPreviewData;

        string m_MainContextNodeName = new Defs.ShaderGraphContext().GetRegistryKey().Name;

        internal PreviewManager(GraphModelStateComponent graphModelStateComponent)
        {
            m_GraphModelStateComponent = graphModelStateComponent;

            m_PreviewHandlerInstance = new HeadlessPreviewManager();

            m_DirtyNodes = new HashSet<string>();
            m_NodeLookupTable = new Dictionary<string, SerializableGUID>();
        }

        internal void Initialize(ShaderGraphModel graphModel, MainPreviewView mainPreviewView, bool wasWindowCloseCancelled)
        {
            // Can be null when the editor window is opened to the onboarding page
            if (graphModel == null)
                return;

            m_IsInitialized = true;
            m_GraphModel = graphModel;

            // Initialize the main preview
            m_MainPreviewView = mainPreviewView;
            m_MainPreviewData = graphModel.MainPreviewData;
            m_MainPreviewView.Initialize(m_MainPreviewData);

            // Initialize the headless preview
            m_PreviewHandlerInstance.Initialize(m_MainContextNodeName,
                new Vector2(m_MainPreviewData.width, m_MainPreviewData.height));

            m_PreviewHandlerInstance.SetActiveGraph(m_GraphModel.GraphHandler);
            m_PreviewHandlerInstance.SetActiveRegistry(m_GraphModel.RegistryInstance.Registry);

            // Initialize preview data for any nodes that exist on graph load
            foreach (var nodeModel in m_GraphModel.NodeModels)
            {
                if(nodeModel is GraphDataContextNodeModel contextNode && IsMainContextNode(nodeModel))
                    OnNodeAdded(contextNode.graphDataName, contextNode.Guid);
                else if (nodeModel is GraphDataNodeModel graphDataNodeModel && graphDataNodeModel.HasPreview)
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

        static bool IsMainContextNode(IGraphElementModel nodeModel)
        {
            return nodeModel is GraphDataContextNodeModel contextNode && contextNode.graphDataName == new Defs.ShaderGraphContext().GetRegistryKey().Name;
        }

        public void Update()
        {
            var updatedNodes = new List<string>();

            foreach (string nodeName in m_DirtyNodes)
            {
                if (!m_NodeLookupTable.TryGetValue(nodeName, out var nodeGuid))
                    continue;

                m_GraphModel.TryGetModelFromGuid(nodeGuid, out var nodeModel);
                if (IsMainContextNode(nodeModel))
                {
                    var previewOutputState = m_PreviewHandlerInstance.RequestMainPreviewTexture(
                        m_MainPreviewData.width,
                        m_MainPreviewData.height,
                        m_MainPreviewData.mesh,
                        m_MainPreviewData.scale,
                        m_MainPreviewData.preventRotation,
                        m_MainPreviewData.rotation,
                        out var updatedTexture,
                        out var shaderMessages);
                    if (updatedTexture != m_MainPreviewView.mainPreviewTexture)
                    {
                        // Headless preview manager handles assigning correct texture in case of completion, error state, still updating etc.
                        // We just need to update the texture on this side regardless of what happens
                        m_MainPreviewView.mainPreviewTexture = updatedTexture;

                        // Node is updated, remove from dirty list
                        if (previewOutputState == HeadlessPreviewManager.PreviewOutputState.Complete)
                            updatedNodes.Add(nodeName);
                        if (previewOutputState == HeadlessPreviewManager.PreviewOutputState.ShaderError)
                        {
                            updatedNodes.Add(nodeName);
                            // TODO: Handle displaying main preview errors
                            foreach (var errorMessage in shaderMessages)
                            {
                                Debug.Log(errorMessage);
                            }
                        }
                    }

                }
                else if (nodeModel is GraphDataNodeModel graphDataNodeModel && graphDataNodeModel.IsPreviewExpanded)
                {
                    var previewOutputState = m_PreviewHandlerInstance.RequestNodePreviewTexture(nodeName, out var nodeRenderOutput, out var shaderMessages);
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

        public void OnPreviewMeshChanged()
        {
            OnNodeFlowChanged(m_MainContextNodeName);
        }

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
            // TODO: Dirty and notify any Upstream nodes
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
