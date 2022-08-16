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
    /// NOTES:
    ///
    /// 1) Previously there was always a special deference provided
    /// to the main preview in terms of how its initialized and treated
    /// But if we treat main preview output as being backed by a generic INodeModel (currently GraphDataContextNodeModel,
    /// but in future will be ContextNodeModel or BlockNodeModel) we could then process it normally and unify that path as well
    /// Then we could even get ability to visualize main preview output for specific block nodes (Normal, Emission etc)
    /// instead of BaseColor
    ///
    /// 2) Issues with edge deletion
    ///
    /// 3) Blackboard item data hanging around in CLDS after deletion
    ///
    /// 4)
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

            public void ClearState()
            {
                m_State.m_PreviewUpdateListeners = new();
                m_State.m_PreviewVersionTrackers = new();
                m_State.m_PreviewData = new();
            }

            /// <summary>
            /// Initializes the state component based on information from the graph model
            /// </summary>
            /// <param name="graphModel">The graph model for which we want to load a state component.</param>
            public void LoadStateForGraph(ShaderGraphModel graphModel)
            {
                // TODO: Persistence handling between domain reloads and editor sessions
                // PersistedStateComponentHelpers.SaveAndLoadPersistedStateForGraph(m_State, this, graphModel);

                // NOTE: Currently we clear state on every save/load and undo/redo but a caching system would be great
                ClearState();

                // Initialize preview data for any nodes that exist on graph load
                foreach (var nodeModel in graphModel.NodeModels)
                {
                    switch (nodeModel)
                    {
                        case GraphDataContextNodeModel contextNode when contextNode.IsMainContextNode():
                            RegisterNewListener(contextNode.graphDataName, contextNode);
                            break;
                        case GraphDataNodeModel graphDataNodeModel when graphDataNodeModel.HasPreview:
                            RegisterNewListener(graphDataNodeModel.graphDataName, graphDataNodeModel);
                            break;
                    }
                }
            }
        }

        Dictionary<string, IPreviewUpdateListener> m_PreviewUpdateListeners;
        Dictionary<string, int> m_PreviewVersionTrackers;
        Dictionary<string, Texture> m_PreviewData;

        public PreviewStateComponent()
        {
            m_PreviewUpdateListeners = new();
            m_PreviewVersionTrackers = new();
            m_PreviewData = new();
        }

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

        public Texture GetPreviewTexture(string listenerID)
        {
            m_PreviewData.TryGetValue(listenerID, out var previewTexture);
            return previewTexture;
        }

        public List<IPreviewUpdateListener> GetChangedListeners()
        {
            var changedListeners = new List<IPreviewUpdateListener>();
            foreach (var (listenerID, listener) in m_PreviewUpdateListeners)
            {
                if (listener.CurrentVersion != GetListenerVersion(listenerID))
                {
                    changedListeners.Add(listener);
                }
            }

            return changedListeners;
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
