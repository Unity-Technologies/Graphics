using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Holds the selected graph elements in the current view, for the current graph asset.
    /// </summary>
    [Serializable]
    public sealed class SelectionStateComponent : AssetViewStateComponent<SelectionStateComponent.StateUpdater>
    {
        /// <summary>
        /// An observer that updates the <see cref="SelectionStateComponent"/> when a graph is loaded.
        /// </summary>
        public class GraphAssetLoadedObserver : StateObserver
        {
            ToolStateComponent m_ToolStateComponent;
            SelectionStateComponent m_SelectionStateComponent;

            /// <summary>
            /// Initializes a new instance of the <see cref="GraphAssetLoadedObserver"/> class.
            /// </summary>
            public GraphAssetLoadedObserver(ToolStateComponent toolStateComponent, SelectionStateComponent selectionStateComponent)
                : base(new [] { toolStateComponent},
                    new IStateComponent[] { selectionStateComponent })
            {
                m_ToolStateComponent = toolStateComponent;
                m_SelectionStateComponent = selectionStateComponent;
            }

            /// <inheritdoc />
            public override void Observe()
            {
                using (var obs = this.ObserveState(m_ToolStateComponent))
                {
                    if (obs.UpdateType != UpdateType.None)
                    {
                        using (var updater = m_SelectionStateComponent.UpdateScope)
                        {
                            updater.SaveAndLoadStateForAsset(m_ToolStateComponent.AssetModel);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updater for <see cref="SelectionStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<SelectionStateComponent>
        {
            /// <summary>
            /// Marks graph elements as selected or unselected.
            /// </summary>
            /// <param name="graphElementModels">The graph elements to select or unselect.</param>
            /// <param name="select">True if the graph elements should be selected.
            /// False is the graph elements should be unselected.</param>
            public void SelectElements(IReadOnlyCollection<IGraphElementModel> graphElementModels, bool select)
            {
                // If m_SelectedModels is not null, we maintain it. Otherwise, we let GetSelection rebuild it.

                if (select)
                {
                    m_State.m_SelectedModels = m_State.m_SelectedModels?.Concat(graphElementModels).Distinct().ToList();

                    var guidsToAdd = graphElementModels.Select(x => x.Guid);
                    m_State.m_Selection = m_State.m_Selection.Concat(guidsToAdd).Distinct().ToList();
                }
                else
                {
                    foreach (var graphElementModel in graphElementModels)
                    {
                        if (m_State.m_Selection.Remove(graphElementModel.Guid))
                            m_State.m_SelectedModels?.Remove(graphElementModel);
                    }
                }

                m_State.CurrentChangeset.ChangedModels.UnionWith(graphElementModels);
                m_State.SetUpdateType(UpdateType.Partial);
            }

            /// <summary>
            /// Marks graph element as selected or unselected.
            /// </summary>
            /// <param name="graphElementModel">The graph element to select or unselect.</param>
            /// <param name="select">True if the graph element should be selected.
            /// False is the graph element should be unselected.</param>
            public void SelectElement(IGraphElementModel graphElementModel, bool select)
            {
                if (select)
                {
                    if (m_State.m_SelectedModels != null && !m_State.m_SelectedModels.Contains(graphElementModel))
                        m_State.m_SelectedModels.Add(graphElementModel);

                    var guid = graphElementModel.Guid;
                    if (!m_State.m_Selection.Contains(guid))
                        m_State.m_Selection.Add(guid);
                }
                else
                {
                    if (m_State.m_Selection.Remove(graphElementModel.Guid))
                        m_State.m_SelectedModels?.Remove(graphElementModel);
                }
            }

            /// <summary>
            /// Unselects all graph elements.
            /// </summary>
            public void ClearSelection(IGraphModel graphModel)
            {
                m_State.CurrentChangeset.ChangedModels.UnionWith(m_State.GetSelection(graphModel));
                m_State.SetUpdateType(UpdateType.Partial);

                // If m_SelectedModels is not null, we maintain it. Otherwise, we let GetSelection rebuild it.
                m_State.m_SelectedModels?.Clear();
                m_State.m_Selection.Clear();
            }

            /// <summary>
            /// Saves the state component and replaces it by the state component associated with <paramref name="assetModel"/>.
            /// </summary>
            /// <param name="assetModel">The asset model for which we want to load a state component.</param>
            public void SaveAndLoadStateForAsset(IGraphAssetModel assetModel)
            {
                PersistedStateComponentHelpers.SaveAndLoadAssetViewStateForAsset(m_State, this, assetModel);
            }
        }

        static IReadOnlyList<IGraphElementModel> s_EmptyList = new List<IGraphElementModel>();

        // Source of truth
        [SerializeField]
        List<SerializableGUID> m_Selection;

        // Cache of selected models, built using m_Selection, for use by GetSelection().
        List<IGraphElementModel> m_SelectedModels;

        ChangesetManager<SimpleChangeset<IGraphElementModel>> m_ChangesetManager = new ChangesetManager<SimpleChangeset<IGraphElementModel>>();

        /// <inheritdoc />
        protected override IChangesetManager ChangesetManager => m_ChangesetManager;

        SimpleChangeset<IGraphElementModel> CurrentChangeset => m_ChangesetManager.CurrentChangeset;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectionStateComponent" /> class.
        /// </summary>
        public SelectionStateComponent()
        {
            m_Selection = new List<SerializableGUID>();
            m_SelectedModels = null;
        }

        /// <summary>
        /// Gets a changeset that encompasses all changeset having a version larger than <paramref name="sinceVersion"/>.
        /// </summary>
        /// <param name="sinceVersion">The version from which to consider changesets.</param>
        /// <returns>The aggregated changeset.</returns>
        public SimpleChangeset<IGraphElementModel> GetAggregatedChangeset(uint sinceVersion)
        {
            return m_ChangesetManager.GetAggregatedChangeset(sinceVersion, CurrentVersion);
        }

        /// <summary>
        /// Gets the list of selected graph element models. If not done yet, this
        /// function resolves the list of models from a list of GUID, using the graph.
        /// </summary>
        /// <param name="graph">The graph containing the selected models.</param>
        /// <returns>A list of selected graph element models.</returns>
        public IReadOnlyList<IGraphElementModel> GetSelection(IGraphModel graph)
        {
            if (m_SelectedModels == null)
            {
                if (graph == null)
                {
                    return s_EmptyList;
                }

                m_SelectedModels = new List<IGraphElementModel>();
                foreach (var guid in m_Selection)
                {
                    if (graph.TryGetModelFromGuid(guid, out var model))
                    {
                        Debug.Assert(model != null);
                        m_SelectedModels.Add(model);
                    }
                }
            }

            return m_SelectedModels;
        }

        /// <summary>
        /// Checks is the graph element model is selected.
        /// </summary>
        /// <param name="model">The model to check.</param>
        /// <returns>True is the model is selected. False otherwise.</returns>
        public bool IsSelected(IGraphElementModel model)
        {
            return model != null && m_Selection.Contains(model.Guid);
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other)
        {
            base.Move(other);

            if (other is SelectionStateComponent selectionStateComponent)
            {
                m_Selection = selectionStateComponent.m_Selection;
                m_SelectedModels = null;

                selectionStateComponent.m_Selection = null;
                selectionStateComponent.m_SelectedModels = null;
            }
        }

        /// <inheritdoc />
        public override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();
            m_SelectedModels = null;
        }
    }
}
