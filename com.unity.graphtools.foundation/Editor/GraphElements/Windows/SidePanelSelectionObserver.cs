using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class SidePanelSelectionObserver : StateObserver
    {
        ToolStateComponent m_ToolState;
        SelectionStateComponent m_SelectionState;
        ModelInspectorStateComponent m_ModelInspectorState;

        public SidePanelSelectionObserver(ToolStateComponent toolState, SelectionStateComponent selectionState,
            ModelInspectorStateComponent modelInspectorState)
            : base(new IStateComponent[] { toolState, selectionState },
                new[] { modelInspectorState })
        {
            m_ToolState = toolState;
            m_SelectionState = selectionState;
            m_ModelInspectorState = modelInspectorState;
        }

        public override void Observe()
        {
            using (var toolObservation = this.ObserveState(m_ToolState))
            using (var selectionObservation = this.ObserveState(m_SelectionState))
            {
                if (selectionObservation.UpdateType != UpdateType.None || toolObservation.UpdateType != UpdateType.None)
                {
                    var graphModel = m_ToolState.GraphModel;
                    var lastSelectedNode = m_SelectionState.GetSelection(graphModel).OfType<INodeModel>().LastOrDefault();

                    using (var updater = m_ModelInspectorState.UpdateScope)
                    {
                        updater.SetModel(lastSelectedNode);
                    }
                }
            }
        }
    }
}
