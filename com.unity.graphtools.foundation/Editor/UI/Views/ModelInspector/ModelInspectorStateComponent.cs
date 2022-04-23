using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// State component for the <see cref="ModelInspectorView"/>.
    /// </summary>
    [Serializable]
    public class ModelInspectorStateComponent : PersistedStateComponent<ModelInspectorStateComponent.StateUpdater>
    {
        /// <summary>
        /// Updater for the component.
        /// </summary>
        public class StateUpdater : BaseUpdater<ModelInspectorStateComponent>
        {
            /// <summary>
            /// Sets the models being inspected.
            /// </summary>
            /// <param name="models">The models being inspected.</param>
            /// <param name="graphModel">The graph model to which the inspected model belongs.</param>
            public void SetInspectedModels(IEnumerable<IModel> models, IGraphModel graphModel)
            {
                m_State.SetInspectedModel(models, graphModel);
                m_State.SetUpdateType(UpdateType.Complete);
            }

            /// <summary>
            /// Sets a section as collapsed or expanded.
            /// </summary>
            /// <param name="sectionModel">The section to modify.</param>
            /// <param name="collapsed">True if the section should be collapsed, false if it should be expanded.</param>
            public void SetSectionCollapsed(IInspectorSectionModel sectionModel, bool collapsed)
            {
                sectionModel.Collapsed = collapsed;
                m_State.CurrentChangeset.ChangedModels.Add(sectionModel);
                m_State.SetUpdateType(UpdateType.Partial);
            }

            /// <summary>
            /// Sets the inspector ScrollView scroll offset.
            /// </summary>
            /// <param name="scrollOffset">The horizontal and vertical offsets for the ScrollView.</param>
            public void SetScrollOffset(Vector2 scrollOffset)
            {
                m_State.GetInspectorModel().ScrollOffset = scrollOffset;
                m_State.SetUpdateType(UpdateType.Partial);
            }

            /// <summary>
            /// Saves the state component and replaces it by the state component associated with <paramref name="graphModel"/>.
            /// </summary>
            /// <param name="graphModel">The graph for which we want to load a state component.</param>
            public void SaveAndLoadStateForGraph(IGraphModel graphModel)
            {
                PersistedStateComponentHelpers.SaveAndLoadPersistedStateForGraph(m_State, this, graphModel);
            }
        }

        ChangesetManager<SimpleChangeset<IInspectorSectionModel>> m_ChangesetManager;

        /// <inheritdoc />
        protected override IChangesetManager ChangesetManager => m_ChangesetManager;

        SimpleChangeset<IInspectorSectionModel> CurrentChangeset => m_ChangesetManager.CurrentChangeset;

        [SerializeField]
        SerializedReferenceDictionary<SerializableGUID, IInspectorModel> m_InspectorModels;

        IGraphModel m_GraphModel;

        List<IModel> m_InspectedModels;

        /// <summary>
        /// The models being inspected.
        /// </summary>
        public IReadOnlyList<IModel> InspectedModels => m_InspectedModels;

        public ModelInspectorStateComponent()
        {
            m_InspectorModels = new SerializedReferenceDictionary<SerializableGUID, IInspectorModel>();
            m_ChangesetManager = new ChangesetManager<SimpleChangeset<IInspectorSectionModel>>();
            m_InspectedModels = new List<IModel>(); // Needed because of serialization.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelInspectorStateComponent"/> class.
        /// </summary>
        /// <param name="models">The models being inspected.</param>
        /// <param name="graphModel">The graph model to which the inspected model belongs.</param>
        public ModelInspectorStateComponent(IEnumerable<IModel> models, IGraphModel graphModel)
            : this()
        {
            SetInspectedModel(models, graphModel);
        }

        void SetInspectedModel(IEnumerable<IModel> models, IGraphModel graphModel)
        {
            m_InspectedModels = new List<IModel>(models.Where(m => m != null));
            m_GraphModel = graphModel;
        }

        /// <summary>
        /// Gets the inspector model.
        /// </summary>
        /// <returns>The inspector model.</returns>
        public IInspectorModel GetInspectorModel()
        {
            if (m_InspectedModels.Count == 0 || m_GraphModel?.Stencil == null)
                return null;

            if (m_InspectedModels.Count == 1)
            {
                if (!m_InspectorModels.TryGetValue(m_InspectedModels[0].Guid, out var inspectorModel))
                {
                    inspectorModel = m_GraphModel.Stencil.CreateInspectorModel(m_InspectedModels[0]);
                    m_InspectorModels[m_InspectedModels[0].Guid] = inspectorModel;
                }

                return inspectorModel;
            }

            return null;
        }

        /// <summary>
        /// Gets a changeset that encompasses all changeset having a version larger than <paramref name="sinceVersion"/>.
        /// </summary>
        /// <param name="sinceVersion">The version from which to consider changesets.</param>
        /// <returns>The aggregated changeset.</returns>
        public SimpleChangeset<IInspectorSectionModel> GetAggregatedChangeset(uint sinceVersion)
        {
            return m_ChangesetManager.GetAggregatedChangeset(sinceVersion, CurrentVersion);
        }

        /// <inheritdoc />
        protected override void Move(IStateComponent other)
        {
            base.Move(other);

            if (other is ModelInspectorStateComponent modelInspectorStateComponent)
            {
                m_InspectorModels = modelInspectorStateComponent.m_InspectorModels;
                modelInspectorStateComponent.m_InspectorModels = null;

                m_GraphModel = null;
                m_InspectedModels =  new List<IModel>();
            }
        }
    }
}
