using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A component to hold the editor state of the <see cref="IGraphModel"/>.
    /// </summary>
    public class GraphModelStateComponent : StateComponent<GraphModelStateComponent.StateUpdater>
    {
        /// <summary>
        /// An observer that updates the <see cref="GraphModelStateComponent"/> when a graph is loaded.
        /// </summary>
        public class GraphAssetLoadedObserver : StateObserver
        {
            ToolStateComponent m_ToolStateComponent;
            GraphModelStateComponent m_GraphModelStateComponent;

            /// <summary>
            /// Initializes a new instance of the <see cref="GraphAssetLoadedObserver"/> class.
            /// </summary>
            public GraphAssetLoadedObserver(ToolStateComponent toolStateComponent, GraphModelStateComponent graphModelStateComponent)
                : base(new [] { toolStateComponent},
                    new IStateComponent[] { graphModelStateComponent })
            {
                m_ToolStateComponent = toolStateComponent;
                m_GraphModelStateComponent = graphModelStateComponent;
            }

            /// <inheritdoc />
            public override void Observe()
            {
                using (var obs = this.ObserveState(m_ToolStateComponent))
                {
                    if (obs.UpdateType != UpdateType.None)
                    {
                        using (var updater = m_GraphModelStateComponent.UpdateScope)
                        {
                            updater.SaveAndLoadStateForGraph(m_ToolStateComponent.GraphModel);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updater for the <see cref="GraphModelStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<GraphModelStateComponent>
        {
            IGraphElementModel[] m_Single = new IGraphElementModel[1];

            /// <summary>
            /// Saves the current state and loads the state associated with <paramref name="graphModel"/>.
            /// </summary>
            /// <param name="graphModel">The graph asset for which to load the state component.</param>
            public void SaveAndLoadStateForGraph(IGraphModel graphModel)
            {
                m_State.m_CurrentGraph = new OpenedGraph(graphModel, null);
                m_State.SetUpdateType(UpdateType.Complete);
            }

            void SetDirty(bool somethingChanged)
            {
                if (somethingChanged)
                {
                    m_State.SetUpdateType(UpdateType.Partial);

                    var graphAsset = m_State.m_CurrentGraph.GetGraphAsset();
                    if (graphAsset != null)
                    {
                        graphAsset.Dirty = true;
                    }
                }
            }

            /// <summary>
            /// Marks graph element models as newly created.
            /// </summary>
            /// <param name="models">The newly created models.</param>
            public void MarkNew(IEnumerable<IGraphElementModel> models)
            {
                var somethingChanged = m_State.CurrentChangeset.AddNewModels(models);
                SetDirty(somethingChanged);
            }

            /// <summary>
            /// Marks a graph element model as newly created.
            /// </summary>
            /// <param name="model">The newly created model.</param>
            public void MarkNew(IGraphElementModel model)
            {
                m_Single[0] = model;
                MarkNew(m_Single);
            }

            /// <summary>
            /// Marks graph element models as changed.
            /// </summary>
            /// <param name="changes">The changed models.</param>
            public void MarkChanged(IReadOnlyDictionary<IGraphElementModel, IReadOnlyList<ChangeHint>> changes)
            {
                var somethingChanged = m_State.CurrentChangeset.AddChangedModels(changes);
                SetDirty(somethingChanged);
            }

            /// <summary>
            /// Marks graph element models as changed.
            /// </summary>
            /// <param name="models">The changed models.</param>
            /// <param name="changeHint">A hint about what changed on the models. The hint applies to all models.</param>
            public void MarkChanged(IEnumerable<IGraphElementModel> models, ChangeHint changeHint)
            {
                var somethingChanged = m_State.CurrentChangeset.AddChangedModels(models, changeHint);
                SetDirty(somethingChanged);
            }

            /// <summary>
            /// Marks graph element models as changed.
            /// </summary>
            /// <param name="models">The changed models.</param>
            /// <param name="changeHints">Hints about what changed on the models. Hints apply to all models.</param>
            public void MarkChanged(IEnumerable<IGraphElementModel> models, List<ChangeHint> changeHints = null)
            {
                var somethingChanged = m_State.CurrentChangeset.AddChangedModels(models, changeHints);
                SetDirty(somethingChanged);
            }

            /// <summary>
            /// Marks a graph element model as changed.
            /// </summary>
            /// <param name="model">The changed model.</param>
            /// <param name="changeHint">A hint about what changed on the model.</param>
            public void MarkChanged(IGraphElementModel model, ChangeHint changeHint)
            {
                m_Single[0] = model;
                MarkChanged(m_Single, changeHint);
            }

            public void MarkForRename(IGraphElementModel model)
            {
                if (model != null)
                {
                    m_State.CurrentChangeset.RenamedModel = model;

                    m_State.SetUpdateType(UpdateType.Partial);
                }
            }

            /// <summary>
            /// Marks a graph element model as changed.
            /// </summary>
            /// <param name="model">The changed model.</param>
            /// <param name="changeHints">Hints about what changed on the model.</param>
            public void MarkChanged(IGraphElementModel model, List<ChangeHint> changeHints = null)
            {
                m_Single[0] = model;
                MarkChanged(m_Single, changeHints);
            }

            /// <summary>
            /// Marks graph element models as deleted.
            /// </summary>
            /// <param name="models">The deleted models.</param>
            public void MarkDeleted(IEnumerable<IGraphElementModel> models)
            {
                var somethingChanged = m_State.CurrentChangeset.AddDeletedModels(models);
                SetDirty(somethingChanged);
            }

            /// <summary>
            /// Marks a graph element model as deleted.
            /// </summary>
            /// <param name="model">The deleted model.</param>
            public void MarkDeleted(IGraphElementModel model)
            {
                m_Single[0] = model;
                MarkDeleted(m_Single);
            }

            /// <summary>
            /// Marks a model as needing to be aligned.
            /// </summary>
            /// <param name="model">The model to align.</param>
            public void MarkModelToAutoAlign(IGraphElementModel model)
            {
                m_State.CurrentChangeset.AddModelToAutoAlign(model);
            }

            /// <summary>
            /// Tells the state component that the graph asset was modified externally.
            /// </summary>
            public void AssetChangedOnDisk()
            {
                m_State.SetUpdateType(UpdateType.Complete);
            }
        }

        /// <summary>
        /// The class that describes what changed in the <see cref="GraphModelStateComponent"/>.
        /// </summary>
        public class Changeset : IChangeset
        {
            static readonly List<ChangeHint> k_DefaultChangeHints = new List<ChangeHint> { ChangeHint.Unspecified };

            HashSet<IGraphElementModel> m_NewModels;
            Dictionary<IGraphElementModel, IReadOnlyList<ChangeHint>> m_ChangedModelsAndHints;
            HashSet<IGraphElementModel> m_DeletedModels;
            HashSet<IGraphElementModel> m_ModelsToAutoAlign;

            /// <summary>
            /// The new models.
            /// </summary>
            public IEnumerable<IGraphElementModel> NewModels => m_NewModels;

            /// <summary>
            /// The changed models and the hints about what changed.
            /// </summary>
            public IReadOnlyDictionary<IGraphElementModel, IReadOnlyList<ChangeHint>> ChangedModelsAndHints => m_ChangedModelsAndHints;

            /// <summary>
            /// The changed models.
            /// </summary>
            public IEnumerable<IGraphElementModel> ChangedModels => m_ChangedModelsAndHints.Keys;

            /// <summary>
            /// The deleted models.
            /// </summary>
            public IEnumerable<IGraphElementModel> DeletedModels => m_DeletedModels;

            /// <summary>
            /// The models that need to be aligned.
            /// </summary>
            public IEnumerable<IGraphElementModel> ModelsToAutoAlign => m_ModelsToAutoAlign;

            /// <summary>
            /// The models whose title will be focused for rename.
            /// </summary>
            public IGraphElementModel RenamedModel { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Changeset" /> class.
            /// </summary>
            public Changeset()
            {
                m_NewModels = new HashSet<IGraphElementModel>();
                m_ChangedModelsAndHints = new Dictionary<IGraphElementModel, IReadOnlyList<ChangeHint>>();
                m_DeletedModels = new HashSet<IGraphElementModel>();
                m_ModelsToAutoAlign = new HashSet<IGraphElementModel>();
            }

            /// <summary>
            /// Adds models to the list of new models.
            /// </summary>
            /// <param name="models">The models to add.</param>
            /// <returns>True if at least one model was added to the list of new models, false otherwise.</returns>
            public bool AddNewModels(IEnumerable<IGraphElementModel> models)
            {
                var somethingChanged = false;

                foreach (var model in models ?? Enumerable.Empty<IGraphElementModel>())
                {
                    if (model == null || m_DeletedModels.Contains(model))
                        continue;

                    m_ChangedModelsAndHints.Remove(model);
                    m_NewModels.Add(model);

                    somethingChanged = true;
                }

                return somethingChanged;
            }

            /// <summary>
            /// Adds models to the list of changed models, along with hints about the changes.
            /// </summary>
            /// <param name="changes">The models to add.</param>
            /// <returns>True if at least one model was added to the list of changed models, false otherwise.</returns>
            public bool AddChangedModels(IReadOnlyDictionary<IGraphElementModel, IReadOnlyList<ChangeHint>> changes)
            {
                var somethingChanged = false;

                foreach (var change in changes)
                {
                    if (change.Key == null ||
                        m_NewModels.Contains(change.Key) ||
                        m_DeletedModels.Contains(change.Key))
                        continue;

                    AddChangedModel(change.Key, change.Value);

                    somethingChanged = true;
                }

                return somethingChanged;

            }

            /// <summary>
            /// Adds models to the list of changed models, along with hints about the changes.
            /// </summary>
            /// <param name="models">The models to add.</param>
            /// <param name="changeHint">A hint about what changed on the models.</param>
            /// <returns>True if at least one model was added to the list of changed models, false otherwise.</returns>
            public bool AddChangedModels(IEnumerable<IGraphElementModel> models, ChangeHint changeHint = null)
            {
                var somethingChanged = false;
                changeHint ??= ChangeHint.Unspecified;

                foreach (var model in models ?? Enumerable.Empty<IGraphElementModel>())
                {
                    if (model == null ||
                        m_NewModels.Contains(model) ||
                        m_DeletedModels.Contains(model))
                        continue;

                    AddChangedModel(model, changeHint);

                    somethingChanged = true;
                }

                return somethingChanged;

            }

            /// <summary>
            /// Adds models to the list of changed models, along with hints about the changes.
            /// </summary>
            /// <param name="models">The models to add.</param>
            /// <param name="changeHints">Hints about what changed on the models. The hints apply to all models.</param>
            /// <returns>True if at least one model was added to the list of changed models, false otherwise.</returns>
            public bool AddChangedModels(IEnumerable<IGraphElementModel> models, List<ChangeHint> changeHints)
            {
                var somethingChanged = false;
                changeHints ??= k_DefaultChangeHints;

                foreach (var model in models ?? Enumerable.Empty<IGraphElementModel>())
                {
                    if (model == null ||
                        m_NewModels.Contains(model) ||
                        m_DeletedModels.Contains(model))
                        continue;

                    AddChangedModel(model, changeHints);

                    somethingChanged = true;
                }

                return somethingChanged;
            }

            void AddChangedModel(IGraphElementModel model, ChangeHint changeHint)
            {
                if (!m_ChangedModelsAndHints.TryGetValue(model, out var currentHints))
                {
                    m_ChangedModelsAndHints[model] = new List<ChangeHint> { changeHint };
                }
                else if (!currentHints.Contains(changeHint))
                {
                    ((List<ChangeHint>)currentHints).Add(changeHint);
                }
            }

            void AddChangedModel(IGraphElementModel model, IReadOnlyList<ChangeHint> changeHints)
            {
                if (!m_ChangedModelsAndHints.TryGetValue(model, out var currentHints))
                {
                    m_ChangedModelsAndHints[model] = changeHints as List<ChangeHint> ?? changeHints.ToList();
                }
                else
                {
                    ((List<ChangeHint>)currentHints).AddRange(changeHints);
                }
            }

            /// <summary>
            /// Adds models to the list of deleted models.
            /// </summary>
            /// <param name="models">The models to add.</param>
            /// <returns>True if at least one model was added to the list of deleted models, false otherwise.</returns>
            public bool AddDeletedModels(IEnumerable<IGraphElementModel> models)
            {
                var somethingChanged = false;
                foreach (var model in models ?? Enumerable.Empty<IGraphElementModel>())
                {
                    if (model == null)
                        continue;

                    m_NewModels.Remove(model);
                    m_ChangedModelsAndHints.Remove(model);

                    m_DeletedModels.Add(model);

                    somethingChanged = true;
                }

                return somethingChanged;
            }

            /// <summary>
            /// Adds a model to the list of models to auto-align.
            /// </summary>
            /// <param name="model">The model to add.</param>
            public void AddModelToAutoAlign(IGraphElementModel model)
            {
                m_ModelsToAutoAlign.Add(model);
            }

            /// <inheritdoc/>
            public void Clear()
            {
                m_NewModels.Clear();
                m_ChangedModelsAndHints.Clear();
                m_DeletedModels.Clear();
                m_ModelsToAutoAlign.Clear();
            }

            /// <inheritdoc/>
            public void AggregateFrom(IEnumerable<IChangeset> changesets)
            {
                Clear();

                foreach (var changeset in changesets.OfType<Changeset>())
                {
                    m_NewModels.UnionWith(changeset.m_NewModels);
                    m_DeletedModels.UnionWith(changeset.m_DeletedModels);
                    m_ModelsToAutoAlign.UnionWith(changeset.m_ModelsToAutoAlign);

                    foreach (var kv in changeset.m_ChangedModelsAndHints)
                    {
                        AddChangedModel(kv.Key, (List<ChangeHint>)kv.Value);
                    }
                }

                m_NewModels.RemoveWhere(m => m_DeletedModels.Contains(m));

                foreach (var model in m_ChangedModelsAndHints.Keys.ToList())
                {
                    if (m_NewModels.Contains(model) || m_DeletedModels.Contains(model))
                    {
                        m_ChangedModelsAndHints.Remove(model);
                    }
                }

                m_ModelsToAutoAlign.RemoveWhere(m => m_DeletedModels.Contains(m));

                foreach (var changeset in changesets.OfType<Changeset>())
                {
                    if (!m_DeletedModels.Contains(changeset.RenamedModel))
                    {
                        RenamedModel = changeset.RenamedModel;
                        break;
                    }
                }
            }
        }

        ChangesetManager<Changeset> m_ChangesetManager = new ChangesetManager<Changeset>();
        Changeset CurrentChangeset => m_ChangesetManager.CurrentChangeset;

        /// <inheritdoc />
        protected override IChangesetManager ChangesetManager => m_ChangesetManager;

        [SerializeField]
        OpenedGraph m_CurrentGraph;

        /// <summary>
        /// The <see cref="IGraphModel"/>.
        /// <remarks>This method is virtual for tests.</remarks>
        /// </summary>
        public virtual IGraphModel GraphModel => m_CurrentGraph.GetGraphModel();

        /// <summary>
        /// Gets a changeset that encompasses all changeset having a version larger than <paramref name="sinceVersion"/>.
        /// </summary>
        /// <param name="sinceVersion">The version from which to consider changesets.</param>
        /// <returns>The aggregated changeset.</returns>
        public Changeset GetAggregatedChangeset(uint sinceVersion)
        {
            return m_ChangesetManager.GetAggregatedChangeset(sinceVersion, CurrentVersion);
        }

        /// <inheritdoc/>
        protected override void Move(IStateComponent other)
        {
            base.Move(other);
            if (other is GraphModelStateComponent graphModelStateComponent)
            {
                m_CurrentGraph = graphModelStateComponent.m_CurrentGraph;
                graphModelStateComponent.m_CurrentGraph = default;
            }
        }

        /// <inheritdoc />
        public override void WillPerformUndoRedo(string undoString)
        {
            base.WillPerformUndoRedo(undoString);

            var obj = m_CurrentGraph.GetGraphAsset() as Object;
            if (obj != null)
            {
                Undo.RegisterCompleteObjectUndo(new[] { obj }, undoString);
            }
        }

        /// <inheritdoc />
        public override void UndoRedoPerformed()
        {
            base.UndoRedoPerformed();
            GraphModel?.UndoRedoPerformed();
        }
    }
}
