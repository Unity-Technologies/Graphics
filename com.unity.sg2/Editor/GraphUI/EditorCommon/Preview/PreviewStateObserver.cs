﻿using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    ///  Observes PreviewStateComponent for changes in preview data, and notifies listeners to update themselves if so
    /// </summary>
    public class PreviewStateObserver : StateObserver
    {
        PreviewStateComponent m_PreviewStateComponent;
        ShaderGraphView m_ShaderGraphView;

        public PreviewStateObserver(
            PreviewStateComponent previewStateComponent,
            ShaderGraphView shaderGraphView)
            : base(previewStateComponent)
        {
            m_PreviewStateComponent = previewStateComponent;
            m_ShaderGraphView = shaderGraphView;
        }

        public override void Observe()
        {
            using (var previewObservation = this.ObserveState(m_PreviewStateComponent))
            {
                if (previewObservation.UpdateType != UpdateType.None)
                {
                    var changedListeners = m_PreviewStateComponent.GetChangedListeners();

                    // Update view models
                    foreach (var listener in changedListeners)
                    {
                        var newTexture = m_PreviewStateComponent.GetPreviewTexture(listener.ListenerID);
                        listener.HandlePreviewTextureUpdated(newTexture);
                    }

                    m_ShaderGraphView.HandlePreviewUpdates(changedListeners.Cast<IGraphElementModel>());

                    if(m_ShaderGraphView.Window is ShaderGraphEditorWindow shaderGraphEditorWindow)
                        shaderGraphEditorWindow.MainPreviewView.HandlePreviewUpdates();
                }
            }
        }
    }
}
