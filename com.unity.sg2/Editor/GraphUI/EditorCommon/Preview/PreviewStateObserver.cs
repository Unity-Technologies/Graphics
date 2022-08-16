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
        GraphModelStateComponent m_GraphModelStateComponent;

        public PreviewStateObserver(PreviewStateComponent previewStateComponent,
                                    GraphModelStateComponent graphModelStateComponent)
            : base(new [] {previewStateComponent},
                new [] { graphModelStateComponent})
        {
            m_PreviewStateComponent = previewStateComponent;
            m_GraphModelStateComponent = graphModelStateComponent;
        }

        public override void Observe()
        {
            using (var updater = m_GraphModelStateComponent.UpdateScope)
            using (var previewObservation = this.ObserveState(m_PreviewStateComponent))
            {
                if (previewObservation.UpdateType != UpdateType.None)
                {
                    foreach (var listener in m_PreviewStateComponent.GetChangedListeners())
                    {
                        var newTexture = m_PreviewStateComponent.GetPreviewTexture(listener.ListenerID);
                        listener.HandlePreviewTextureUpdated(newTexture);

                        if (listener is IGraphElementModel graphElementModel)
                        {
                            updater.MarkChanged(graphElementModel);
                        }
                    }
                }
            }
        }
    }
}
