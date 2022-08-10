using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class PreviewStateObserver : StateObserver
    {
        PreviewStateComponent m_PreviewStateComponent;

        public PreviewStateObserver(PreviewStateComponent previewStateComponent)
            : base(previewStateComponent)
        {
            m_PreviewStateComponent = previewStateComponent;
        }

        public override void Observe()
        {
            using (var previewObservation = this.ObserveState(m_PreviewStateComponent))
            {
                if (previewObservation.UpdateType != UpdateType.None)
                {

                }
            }
        }
    }
}
