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
                m_State.SetUpdateType(UpdateType.Partial);
            }

            public void RemoveListener(string listenerID)
            {
                m_State.m_PreviewUpdateListeners.Remove(listenerID);
            }
        }

        Dictionary<string, IPreviewUpdateListener> m_PreviewUpdateListeners;

        Dictionary<string, Texture> m_PreviewData;

        public void UpdatePreviewData(string previewUpdateListenerID, Texture newTexture)
        {
            m_PreviewData[previewUpdateListenerID] = newTexture;
            SetUpdateType(UpdateType.Partial);
        }
    }
}
