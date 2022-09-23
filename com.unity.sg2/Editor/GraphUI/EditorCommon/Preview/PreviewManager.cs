using System;
using System.Collections.Generic;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using Unity.GraphToolsFoundation;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI
{
    using PreviewRenderMode = PreviewService.PreviewRenderMode;

    /// <summary>
    /// Manager class for node and master previews
    /// Layer that handles the interaction between GTF/Editor and the HeadlessPreviewManager
    /// </summary>
    public class PreviewManager
    {
        // Gets set to true when user selects the "Sprite" preview mesh in main preview
        bool m_LockMainPreviewRotation;

        public bool LockMainPreviewRotation
        {
            set => m_LockMainPreviewRotation = value;
        }

        PreviewService m_PreviewHandlerInstance;

        GraphModelStateComponent m_GraphModelStateComponent;

        HashSet<string> m_DirtyNodes;

        ShaderGraphModel m_GraphModel;

        Dictionary<string, SerializableGUID> m_NodeLookupTable;

        MainPreviewView m_MainPreviewView;
        MainPreviewData m_MainPreviewData;

        // Defines how many times a node/main preview will try to update its preview before the update loop stops
        const int k_PreviewUpdateCycleMax = 50;
        Dictionary<string, int> m_CycleCountChecker;

        string m_MainContextNodeName;

        int PreviewWidth => Mathf.FloorToInt(0.0f);
        int PreviewHeight => Mathf.FloorToInt(0.0f);

        internal void Cleanup()
        {
            m_PreviewHandlerInstance.Cleanup();
        }

        // TODO: Could this be a list of IPreviewUpdateListeners instead?
        internal void Initialize(ShaderGraphModel graphModel, MainPreviewView mainPreviewView, bool wasWindowCloseCancelled)
        {
            m_GraphModel = graphModel;
            m_GraphModelStateComponent = m_GraphModel.graphModelStateComponent;
            m_NodeLookupTable = new Dictionary<string, SerializableGUID>();
            m_CycleCountChecker = new Dictionary<string, int>();
            m_PreviewHandlerInstance = new PreviewService();
            m_DirtyNodes = new HashSet<string>();

            // Initialize the main preview
            m_MainPreviewView = mainPreviewView;
            m_MainPreviewData = graphModel.MainPreviewData;
            m_MainContextNodeName = graphModel.DefaultContextName;

            // Initialize the headless preview
            m_PreviewHandlerInstance.Initialize(m_MainContextNodeName, graphModel.MainPreviewData.mainPreviewSize);
            m_PreviewHandlerInstance.SetActiveGraph(m_GraphModel.GraphHandler);
            m_PreviewHandlerInstance.SetActiveRegistry(m_GraphModel.RegistryInstance.Registry);
            m_PreviewHandlerInstance.SetActiveTarget(m_GraphModel.ActiveTarget);

            // Initialize preview data for any nodes that exist on graph load
            foreach (var nodeModel in m_GraphModel.NodeModels)
            {
                switch (nodeModel)
                {
                    //case GraphDataContextNodeModel contextNode when contextNode.IsMainContextNode():
                    //    OnNodeAdded(contextNode.graphDataName, contextNode.Guid);
                    //    break;
                    //case GraphDataNodeModel graphDataNodeModel when graphDataNodeModel.HasPreview:
                    //    OnNodeAdded(graphDataNodeModel.graphDataName, graphDataNodeModel.Guid);
                    //    break;
                }
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
                if (!m_NodeLookupTable.TryGetValue(nodeName, out var nodeGuid))
                    continue;

                m_GraphModel.TryGetModelFromGuid(nodeGuid, out var nodeModel);
                // TODO: Unify main preview and graph data node model update paths using IPreviewUpdateListener
                if (nodeModel is GraphDataContextNodeModel contextNode && contextNode.IsMainContextNode())
                {
                    var previewOutputState = m_PreviewHandlerInstance.RequestMainPreviewTexture(
                        PreviewWidth,
                        PreviewHeight,
                        m_MainPreviewData.mesh,
                        m_MainPreviewData.scale,
                        m_LockMainPreviewRotation,
                        m_MainPreviewData.rotation,
                        out var updatedTexture,
                        out var shaderMessages);

                    m_MainPreviewView.mainPreviewTexture = updatedTexture;
                    HandlePreviewUpdateRequest(previewOutputState, updatedNodes, nodeName, shaderMessages, m_CycleCountChecker);
                }
                else if (nodeModel is GraphDataNodeModel graphDataNodeModel && graphDataNodeModel.IsPreviewExpanded)
                {
                    var previewOutputState = m_PreviewHandlerInstance.RequestNodePreviewTexture(nodeName, out var nodeRenderOutput, out var shaderMessages);
                    graphDataNodeModel.OnPreviewTextureUpdated(nodeRenderOutput);

                    using var graphUpdater = m_GraphModelStateComponent.UpdateScope;
                    {
                        graphUpdater.MarkChanged(nodeModel);
                    }

                    HandlePreviewUpdateRequest(previewOutputState, updatedNodes, nodeName, shaderMessages, m_CycleCountChecker);
                }
            }

            // Clean up any nodes that were successfully updated from the dirty list
            foreach (var updatedNode in updatedNodes)
                m_DirtyNodes.Remove(updatedNode);

        }

        static void HandlePreviewUpdateRequest(
            PreviewService.PreviewOutputState previewOutputState,
            List<string> updatedNodes,
            string nodeName,
            ShaderMessage[] shaderMessages,
            Dictionary<string, int> cycleCountChecker)
        {
            switch (previewOutputState)
            {
                // Node is updated, remove from dirty list
                case PreviewService.PreviewOutputState.Complete:
                    updatedNodes.Add(nodeName);
                    break;
                case PreviewService.PreviewOutputState.ShaderError:
                {
                    updatedNodes.Add(nodeName);

                    // TODO: Handle displaying main preview errors
                    foreach (var errorMessage in shaderMessages)
                    {
                        Debug.Log(errorMessage.message);
                    }

                    break;
                }
                case PreviewService.PreviewOutputState.Updating:
                    if(cycleCountChecker.ContainsKey(nodeName))
                        cycleCountChecker[nodeName]++;
                    else
                        cycleCountChecker[nodeName] = 1;

                    // Node is cycling, toss it out of the update loop and print to log
                    if (cycleCountChecker[nodeName] > k_PreviewUpdateCycleMax)
                    {
                        updatedNodes.Add(nodeName);
                        cycleCountChecker.Remove(nodeName);
                        Debug.LogWarning("Node: " + nodeName + " was cycling infinitely trying to update its preview, forcibly removed from update loop.");
                    }
                    break;
            }
        }

        public Texture GetCachedMainPreviewTexture()
        {
            m_PreviewHandlerInstance.RequestMainPreviewTexture(
                PreviewWidth,
                PreviewHeight,
                m_MainPreviewData.mesh,
                m_MainPreviewData.scale,
                m_LockMainPreviewRotation,
                m_MainPreviewData.rotation,
                out var cachedMainPreviewTexture,
                out var shaderMessages);

            return cachedMainPreviewTexture;
        }


        /// <summary>
        /// This can be called when the main preview's mesh, zoom, rotation etc. changes and a re-render is required
        /// </summary>
        public void OnMainPreviewDataChanged()
        {
            m_DirtyNodes.Add(m_MainContextNodeName);
        }

        // TODO: Implement changing preview mode
        public void OnPreviewModeChanged(string nodeName, PreviewRenderMode newPreviewMode) { }


        /// <summary>
        /// Used to notify when a node's connections have been changed
        /// </summary>
        /// <param name="nodeName"> Name of node whose connections were modified </param>
        /// <param name="wasNodeDeleted"> Flag to set to true if this node was deleted in this modification </param>
        public void OnNodeFlowChanged(string nodeName, bool wasNodeDeleted = false)
        {
            if (wasNodeDeleted)
            {
                OnNodeRemoved(nodeName);
                m_PreviewHandlerInstance.NotifyNodeFlowChanged(nodeName, true);
            }
            else
            {
                m_DirtyNodes.Add(nodeName);
                var impactedNodes = m_PreviewHandlerInstance.NotifyNodeFlowChanged(nodeName);
                foreach (var downstreamNode in impactedNodes)
                {
                    m_DirtyNodes.Add(downstreamNode);
                }
            }
        }

        void OnNodeAdded(String nodeName, SerializableGUID nodeGuid)
        {
            if (m_NodeLookupTable.ContainsKey(nodeName))
                return;

            // Add node to dirty list so preview image can be updated next frame
            m_DirtyNodes.Add(nodeName);

            m_NodeLookupTable.Add(nodeName, nodeGuid);
        }

        void OnNodeRemoved(String nodeName)
        {
            m_DirtyNodes.Remove(nodeName);
            m_NodeLookupTable.Remove(nodeName);
            m_CycleCountChecker.Remove(nodeName);
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
                m_DirtyNodes.Add(downstreamNode);
            }
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

        /// <summary>
        /// Used by UI tests to validate preview results after UI driven changes
        /// </summary>
        /// <param name="nodeName"></param>
        internal Material GetPreviewMaterialForNode(string nodeName)
        {
            return m_PreviewHandlerInstance.RequestNodePreviewMaterial(nodeName);
        }
    }
}
