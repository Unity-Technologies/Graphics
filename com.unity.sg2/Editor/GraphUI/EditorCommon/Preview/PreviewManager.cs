using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.ShaderGraph.GraphUI
{
    using PreviewRenderMode = HeadlessPreviewManager.PreviewRenderMode;

    /// <summary>
    /// Manager class for node and master previews
    /// Layer that handles the interaction between GTF/Editor and the HeadlessPreviewManager
    /// </summary>
    public class PreviewManager
    {
        bool m_IsInitialized;

        public bool IsInitialized
        {
            get => m_IsInitialized;
            set => m_IsInitialized = value;
        }

        // Gets set to true when user selects the "Sprite" preview mesh in main preview
        bool m_LockMainPreviewRotation;

        public bool LockMainPreviewRotation
        {
            set => m_LockMainPreviewRotation = value;
        }

        HeadlessPreviewManager m_PreviewHandlerInstance;

        GraphModelStateComponent m_GraphModelStateComponent;

        HashSet<string> m_DirtyNodes;

        ShaderGraphModel m_GraphModel;

        Dictionary<string, SerializableGUID> m_NodeLookupTable;

        MainPreviewView m_MainPreviewView;
        MainPreviewData m_MainPreviewData;

        string m_MainContextNodeName = new Defs.ShaderGraphContext().GetRegistryKey().Name;

        // Defines how many times a node/main preview will try to update its preview before the update loop stops
        const int k_PreviewUpdateCycleMax = 50;
        Dictionary<string, int> m_CycleCountChecker = new ();

        int PreviewWidth => Mathf.FloorToInt(m_MainPreviewView.PreviewSize.x);
        int PreviewHeight => Mathf.FloorToInt(m_MainPreviewView.PreviewSize.y);

        internal void Initialize(
            GraphModelStateComponent graphModelStateComponent,
            ShaderGraphModel graphModel,
            MainPreviewView mainPreviewView,
            bool wasWindowCloseCancelled)
        {
            // Can be null when the editor window is opened to the onboarding page
            if (graphModel == null)
                return;

            m_GraphModelStateComponent = graphModelStateComponent;

            m_PreviewHandlerInstance = new HeadlessPreviewManager();

            m_DirtyNodes = new HashSet<string>();
            m_NodeLookupTable = new Dictionary<string, SerializableGUID>();

            m_IsInitialized = true;
            m_GraphModel = graphModel;

            // Initialize the main preview
            m_MainPreviewView = mainPreviewView;
            m_MainPreviewData = graphModel.MainPreviewData;

            // Initialize the headless preview
            m_PreviewHandlerInstance.Initialize(m_MainContextNodeName, m_MainPreviewView.PreviewSize);

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

        internal void UpdateReferencesAfterUndoRedo(
            GraphModelStateComponent graphModelStateComponent,
            ShaderGraphModel graphModel)
        {
            m_GraphModelStateComponent = graphModelStateComponent;
            m_GraphModel = graphModel;

            m_PreviewHandlerInstance.SetActiveGraph(m_GraphModel.GraphHandler);
        }

        /// <summary>
        ///  Called to reinitialize the textures for the previews when the graph is saved
        /// </summary>
        internal void HandleGraphReload(
            GraphModelStateComponent graphModelStateComponent,
            ShaderGraphModel graphModel,
            MainPreviewView mainPreviewView)
        {
            m_GraphModelStateComponent = graphModelStateComponent;
            m_GraphModel = graphModel;
            m_MainPreviewView = mainPreviewView;

            UpdateAllNodePreviewTextures();
        }

        internal void UpdateAllNodePreviewTextures()
        {
            using (var stateUpdater = m_GraphModelStateComponent.UpdateScope)
            {
                foreach (var (nodeName, nodeGuid) in m_NodeLookupTable)
                {
                    m_GraphModel.TryGetModelFromGuid(nodeGuid, out var nodeModel);
                    if (nodeModel is GraphDataNodeModel graphDataNodeModel)
                    {
                        m_PreviewHandlerInstance.RequestNodePreviewTexture(nodeName, out var nodeRenderOutput, out var shaderMessages, graphDataNodeModel.NodePreviewMode);
                        graphDataNodeModel.OnPreviewTextureUpdated(nodeRenderOutput);

                        stateUpdater.MarkChanged(graphDataNodeModel);
                    }
                }
            }
        }

        // Called after an undo/redo to cleanup any stale references to data that no longer exists on graph
        internal void PostUndoRedoConsistencyCheck()
        {
            var keys = m_NodeLookupTable.Keys.ToList();
            var values = m_NodeLookupTable.Values.ToList();
            // Remove any nodes from the preview manager that are no longer on graph
            for(var index = 0; index < m_NodeLookupTable.Count; index++)
            {
                var nodeGuid = values[index];
                var nodeName = keys[index];
                m_GraphModel.TryGetModelFromGuid(nodeGuid, out var nodeModel);
                if(nodeModel == null)
                    OnNodeFlowChanged(nodeName, true);
            }

            // Add any nodes to the preview manager that are newly on graph
            foreach (var graphNodeModel in m_GraphModel.NodeModels)
            {
                if(!m_NodeLookupTable.ContainsValue(graphNodeModel.Guid)
                    && graphNodeModel is GraphDataNodeModel graphDataNodeModel)
                    OnNodeAdded(graphDataNodeModel.graphDataName, graphDataNodeModel.Guid);
            }
        }

        // Called after a port value or blackboard value change is undone/redone to handle updating preview values accordingly
        internal void HandleConstantValueUndoRedo(BaseShaderGraphConstant oldConstant)
        {
            var nodeWriter = m_GraphModel.GraphHandler.GetNode(oldConstant.NodeName);
            // Validate that if this constant was associated with a node, that node wasn't deleted
            Assert.IsNotNull(nodeWriter);

            var propertyContextName = Registry.ResolveKey<PropertyContext>().Name;

            // Port values changes on a node being undone/redone
            m_NodeLookupTable.TryGetValue(oldConstant.NodeName, out var nodeGuid);
            m_GraphModel.TryGetModelFromGuid(nodeGuid, out var nodeModel);
            if (nodeModel is GraphDataNodeModel graphDataNodeModel
                && graphDataNodeModel.graphDataName != propertyContextName)
            {
                foreach (var currentPortConstant in graphDataNodeModel.InputConstantsById.Values)
                {
                    if (currentPortConstant is BaseShaderGraphConstant updatedConstant
                        && updatedConstant.PortName == oldConstant.PortName)
                    {
                        OnLocalPropertyChanged(oldConstant.NodeName, oldConstant.PortName, currentPortConstant.ObjectValue);
                        return;
                    }
                }
            }
            else // TODO: Blackboard value changes are not piped through correctly
            {
                // Blackboard item changes being undone/redone
                foreach (var declarationModel in m_GraphModel.VariableDeclarations)
                {
                    if (declarationModel is GraphDataVariableDeclarationModel graphDataVariableDeclarationModel
                        && oldConstant.PortName == graphDataVariableDeclarationModel.graphDataName)
                    {
                        OnGlobalPropertyChanged(oldConstant.PortName, declarationModel.InitializationModel.ObjectValue);
                        return;
                    }
                }
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
                // TODO: Main preview doesnt get removed from dirty nodes when a vector3 node is connected currrently
                if (IsMainContextNode(nodeModel))
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
                    if (updatedTexture != m_MainPreviewView.mainPreviewTexture)
                    {
                        // Headless preview manager handles assigning correct texture in case of completion, error state, still updating etc.
                        // We just need to update the texture on this side regardless of what happens
                        m_MainPreviewView.mainPreviewTexture = updatedTexture;
                    }

                    HandlePreviewUpdateRequest(previewOutputState, updatedNodes, nodeName, shaderMessages, m_CycleCountChecker);
                }
                else if (nodeModel is GraphDataNodeModel graphDataNodeModel && graphDataNodeModel.IsPreviewExpanded)
                {
                    var previewOutputState = m_PreviewHandlerInstance.RequestNodePreviewTexture(nodeName, out var nodeRenderOutput, out var shaderMessages);
                    if (nodeRenderOutput != graphDataNodeModel.PreviewTexture)
                    {
                        // Headless preview manager handles assigning correct texture in case of completion, error state, still updating etc.
                        // We just need to update the texture on this side regardless of what happens
                        graphDataNodeModel.OnPreviewTextureUpdated(nodeRenderOutput);

                        using(var graphUpdater = m_GraphModelStateComponent.UpdateScope)
                        {
                            graphUpdater.MarkChanged(nodeModel);
                        }
                    }
                    HandlePreviewUpdateRequest(previewOutputState, updatedNodes, nodeName, shaderMessages, m_CycleCountChecker);
                }
            }

            // Clean up any nodes that were successfully updated from the dirty list and cycle checker
            foreach (var updatedNode in updatedNodes)
            {
                m_DirtyNodes.Remove(updatedNode);
                m_CycleCountChecker.Remove(updatedNode);
            }
        }

        static void HandlePreviewUpdateRequest(
            HeadlessPreviewManager.PreviewOutputState previewOutputState,
            List<string> updatedNodes,
            string nodeName,
            ShaderMessage[] shaderMessages,
            Dictionary<string, int> cycleCountChecker)
        {
            switch (previewOutputState)
            {
                // Node is updated, remove from dirty list
                case HeadlessPreviewManager.PreviewOutputState.Complete:
                    updatedNodes.Add(nodeName);
                    break;
                case HeadlessPreviewManager.PreviewOutputState.ShaderError:
                    updatedNodes.Add(nodeName);

                    // TODO: Handle displaying main preview errors
                    foreach (var errorMessage in shaderMessages)
                        Debug.Log(errorMessage);
                    break;
                case HeadlessPreviewManager.PreviewOutputState.Updating:
                    if(cycleCountChecker.ContainsKey(nodeName))
                        cycleCountChecker[nodeName]++;
                    else
                        cycleCountChecker[nodeName] = 1;

                    // Node is cycling, toss it out of the update loop and print to log
                    if (cycleCountChecker[nodeName] > k_PreviewUpdateCycleMax)
                    {
                        updatedNodes.Add(nodeName);
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

        public void OnNodeAdded(String nodeName, SerializableGUID nodeGuid)
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
                // Need to do this check because we can get the property context as a connected node but we don't want to process it like any other node
                if(downstreamNode != m_GraphModel.BlackboardContextName)
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
