using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// State component for the <see cref="ModelInspectorView"/>.
    /// </summary>
    public class ModelInspectorStateComponent : ViewStateComponent<ModelInspectorStateComponent.StateUpdater>
    {
        /// <summary>
        /// Updater for the component.
        /// </summary>
        public class StateUpdater : BaseUpdater<ModelInspectorStateComponent>
        {
            /// <summary>
            /// Sets the model being inspected.
            /// </summary>
            /// <param name="nodeModel"></param>
            public void SetModel(INodeModel nodeModel)
            {
                if (nodeModel != m_State.m_EditedNode)
                {
                    m_State.m_EditedNode = nodeModel;
                    m_State.SetUpdateType(UpdateType.Complete);
                }
            }
        }

        INodeModel m_EditedNode;

        /// <summary>
        /// The model being inspected.
        /// </summary>
        public INodeModel EditedNode => m_EditedNode;
    }
}
