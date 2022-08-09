using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class PreviewStateComponent : PersistedStateComponent<PreviewStateComponent.StateUpdater>
    {
        public class StateUpdater : BaseUpdater<PreviewStateComponent>
        {

        }
    }
}
