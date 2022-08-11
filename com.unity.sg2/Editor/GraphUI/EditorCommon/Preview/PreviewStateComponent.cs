using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// State component that holds the preview output data
    /// </summary>
    /// NOTES: Previously there was always a special deference provided
    /// to the main preview in terms of how its initialized and treated
    /// But if we treat main preview output as being backed by a generic INodeModel (currently GraphDataContextNodeModel,
    /// but in future will be ContextNodeModel or BlockNodeModel) we could then process it normally and unify that path as well
    /// Then we could even get ability to visualize main preview output for specific block nodes (Normal, Emission etc)
    /// instead of BaseColor
    public class PreviewStateComponent
        :   PersistedStateComponent<PreviewStateComponent.StateUpdater>,
            IPreviewUpdateReceiver
    {
        public class StateUpdater : BaseUpdater<PreviewStateComponent>
        {
            public void RegisterNewListener(string listenerID, IPreviewUpdateListener updateListener)
            {
                m_State.m_PreviewUpdateListeners.Add(listenerID, updateListener);
                m_State.m_PreviewVersionTrackers.Add(listenerID, 0);
                m_State.m_PreviewData.Add(listenerID, null);
                m_State.SetUpdateType(UpdateType.Partial);
            }

            public void RemoveListener(string listenerID)
            {
                m_State.m_PreviewUpdateListeners.Remove(listenerID);
                m_State.m_PreviewVersionTrackers.Remove(listenerID);
                m_State.m_PreviewData.Remove(listenerID);
            }

            public void UpdatePreviewData(string listenerID, Texture newTexture)
            {
                m_State.m_PreviewData[listenerID] = newTexture;
                m_State.m_PreviewVersionTrackers[listenerID]++;
                m_State.SetUpdateType(UpdateType.Partial);
            }

            /// <summary>
            /// Initializes the state component based on information from the graph model
            /// </summary>
            /// <param name="graphModel">The graph model for which we want to load a state component.</param>
            public void LoadStateForGraph(ShaderGraphModel graphModel)
            {
                // TODO: Persistence handling between domain reloads and editor sessions
                // PersistedStateComponentHelpers.SaveAndLoadPersistedStateForGraph(m_State, this, graphModel);

                // Initialize preview data for any nodes that exist on graph load
                foreach (var nodeModel in graphModel.NodeModels)
                {
                    if(nodeModel is GraphDataContextNodeModel contextNode && IsMainContextNode(nodeModel))
                        RegisterNewListener(contextNode.graphDataName, contextNode);
                    else if (nodeModel is GraphDataNodeModel graphDataNodeModel && graphDataNodeModel.HasPreview)
                        RegisterNewListener(graphDataNodeModel.graphDataName, graphDataNodeModel);
                }
            }

            static bool IsMainContextNode(IGraphElementModel nodeModel)
            {
                return nodeModel is GraphDataContextNodeModel contextNode
                    && contextNode.graphDataName == new Defs.ShaderGraphContext().GetRegistryKey().Name;
            }
        }

        Dictionary<string, IPreviewUpdateListener> m_PreviewUpdateListeners;
        Dictionary<string, int> m_PreviewVersionTrackers;
        Dictionary<string, Texture> m_PreviewData;

        public int GetListenerVersion(string listenerID)
        {
            m_PreviewVersionTrackers.TryGetValue(listenerID, out var versionResult);
            return versionResult;
        }

        public IPreviewUpdateListener GetListener(string listenerID)
        {
            m_PreviewUpdateListeners.TryGetValue(listenerID, out var previewUpdateListener);
            return previewUpdateListener;
        }

        public void UpdatePreviewData(string listenerID, Texture newTexture)
        {
            using (var updater = UpdateScope)
            {
                updater.UpdatePreviewData(listenerID, newTexture);
            }
        }
    }
}
