using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// An observer that updates the <see cref="BlackboardView"/> state components when a graph is loaded.
    /// </summary>
    public class BlackboardGraphAssetLoadedObserver : StateObserver
    {
        ToolStateComponent m_ToolStateComponent;
        BlackboardViewStateComponent m_BlackboardViewStateComponent;
        SelectionStateComponent m_SelectionStateComponent;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardGraphAssetLoadedObserver"/> class.
        /// </summary>
        public BlackboardGraphAssetLoadedObserver(ToolStateComponent toolStateComponent, BlackboardViewStateComponent blackboardViewStateComponent, SelectionStateComponent selectionStateComponent)
            : base(new [] { toolStateComponent},
                new IStateComponent[] { blackboardViewStateComponent, selectionStateComponent })
        {
            m_ToolStateComponent = toolStateComponent;
            m_BlackboardViewStateComponent = blackboardViewStateComponent;
            m_SelectionStateComponent = selectionStateComponent;
        }

        /// <inheritdoc />
        public override void Observe()
        {
            using (var obs = this.ObserveState(m_ToolStateComponent))
            {
                if (obs.UpdateType != UpdateType.None)
                {
                    using (var updater = m_BlackboardViewStateComponent.UpdateScope)
                    {
                        updater.SaveAndLoadStateForAsset(m_ToolStateComponent.AssetModel);
                    }
                    using (var updater = m_SelectionStateComponent.UpdateScope)
                    {
                        updater.SaveAndLoadStateForAsset(m_ToolStateComponent.AssetModel);
                    }
                }
            }
        }
    }
}
