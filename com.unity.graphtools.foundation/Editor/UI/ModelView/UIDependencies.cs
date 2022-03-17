using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Stores dependencies of a UI to other UI and additional models.
    /// </summary>
    /// <remarks>
    /// - A dependency exists between two UI when one UI needs to be updated when another UI changes.
    /// - A dependency exists between a UI and a model when one UI needs to be updated when the model changes.
    ///   There is already an intrinsic dependency between a UI and its Model. However we sometimes need to
    ///   make a UI dependent on additional models.
    /// </remarks>
    public class UIDependencies
    {
        static Dictionary<SerializableGUID, HashSet<IModelView>> s_ModelDependencies = new Dictionary<SerializableGUID, HashSet<IModelView>>();

        IModelView m_Owner;

        // Graph elements that we affect when we change.
        HashSet<(IModelView, DependencyTypes)> m_ForwardDependencies;
        // Graph elements that affect us when they change.
        HashSet<(IModelView, DependencyTypes)> m_BackwardDependencies;
        // Additional models that influence us.
        HashSet<IGraphElementModel> m_ModelDependencies;

        readonly EventCallback<CustomStyleResolvedEvent> m_OnBackwardDependencyCustomStyleResolved;
        readonly EventCallback<GeometryChangedEvent> m_OnBackwardDependencyGeometryChanged;
        readonly EventCallback<DetachFromPanelEvent> m_OnBackwardDependencyDetachedFromPanel;

        /// <summary>
        /// Initializes a new instance of the <see cref="UIDependencies"/> class.
        /// </summary>
        /// <param name="owner">The UI for which these dependencies are declared.</param>
        public UIDependencies(IModelView owner)
        {
            m_Owner = owner;
            m_OnBackwardDependencyCustomStyleResolved = OnBackwardDependencyCustomStyleResolved;
            m_OnBackwardDependencyGeometryChanged = OnBackwardDependencyGeometryChanged;
            m_OnBackwardDependencyDetachedFromPanel = OnBackwardDependencyDetachedFromPanel;
        }

        /// <summary>
        /// Removes all dependencies.
        /// </summary>
        public void ClearDependencyLists()
        {
            if (m_Owner.HasForwardsDependenciesChanged())
                m_ForwardDependencies?.Clear();

            if (m_BackwardDependencies != null && m_Owner.HasBackwardsDependenciesChanged())
            {
                foreach (var (graphElement, dependencyType) in m_BackwardDependencies)
                {
                    var ve = graphElement as VisualElement;
                    if (ve == null)
                        continue;
                    if (dependencyType.HasFlagFast(DependencyTypes.Style))
                        ve.UnregisterCallback(m_OnBackwardDependencyCustomStyleResolved);
                    if (dependencyType.HasFlagFast(DependencyTypes.Geometry))
                        ve.UnregisterCallback(m_OnBackwardDependencyGeometryChanged);
                    if (dependencyType.HasFlagFast(DependencyTypes.Removal))
                        ve.UnregisterCallback(m_OnBackwardDependencyDetachedFromPanel);
                }
                m_BackwardDependencies.Clear();
            }

            if (m_ModelDependencies != null && m_Owner.HasModelDependenciesChanged())
            {
                foreach (var model in m_ModelDependencies)
                {
                    RemoveModelDependency(model, m_Owner);
                }
                m_ModelDependencies.Clear();
            }
        }

        /// <summary>
        /// Asks the owner UI to update its dependencies.
        /// </summary>
        public void UpdateDependencyLists()
        {
            ClearDependencyLists();

            if (m_Owner.HasForwardsDependenciesChanged())
                m_Owner.AddForwardDependencies();

            if (m_Owner.HasBackwardsDependenciesChanged())
            {
                m_Owner.AddBackwardDependencies();
                if (m_BackwardDependencies != null)
                {
                    foreach (var (graphElement, dependencyType) in m_BackwardDependencies)
                    {
                        var ve = graphElement as VisualElement;
                        if (ve == null)
                            continue;
                        if (dependencyType.HasFlagFast(DependencyTypes.Style))
                            ve.RegisterCallback(m_OnBackwardDependencyCustomStyleResolved);
                        if (dependencyType.HasFlagFast(DependencyTypes.Geometry))
                            ve.RegisterCallback(m_OnBackwardDependencyGeometryChanged);
                        if (dependencyType.HasFlagFast(DependencyTypes.Removal))
                            ve.RegisterCallback(m_OnBackwardDependencyDetachedFromPanel);
                    }
                }
            }

            if (m_Owner.HasModelDependenciesChanged())
            {
                m_Owner.AddModelDependencies();
                if (m_ModelDependencies != null)
                {
                    foreach (var model in m_ModelDependencies)
                    {
                        AddModelDependency(model, m_Owner);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a <see cref="ModelView"/> to the forward dependencies list. A forward dependency is
        /// a UI that should be updated whenever this object's owner is updated.
        /// </summary>
        public void AddForwardDependency(IModelView dependency, DependencyTypes dependencyType)
        {
            if (m_ForwardDependencies == null)
                m_ForwardDependencies = new HashSet<(IModelView, DependencyTypes)>();

            m_ForwardDependencies.Add((dependency, dependencyType));
        }

        /// <summary>
        /// Adds a <see cref="ModelView"/> to the backward dependencies list. A backward dependency is
        /// a UI that causes this object's owner to be updated whenever it is updated.
        /// </summary>
        public void AddBackwardDependency(IModelView dependency, DependencyTypes dependencyType)
        {
            if (m_BackwardDependencies == null)
                m_BackwardDependencies = new HashSet<(IModelView, DependencyTypes)>();

            m_BackwardDependencies.Add((dependency, dependencyType));
        }

        /// <summary>
        /// Adds <see cref="IGraphElementModel"/> to the model dependencies list. A model dependency is
        /// a graph element model that causes this object's owner to be updated whenever it is updated.
        /// </summary>
        public void AddModelDependency(IGraphElementModel model)
        {
            if (m_ModelDependencies == null)
                m_ModelDependencies = new HashSet<IGraphElementModel>();

            m_ModelDependencies.Add(model);
            AddModelDependency(model, m_Owner);
        }

        void OnBackwardDependencyGeometryChanged(GeometryChangedEvent evt)
        {
            m_Owner.UpdateFromModel();
        }

        void OnBackwardDependencyCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            m_Owner.UpdateFromModel();
        }

        void OnBackwardDependencyDetachedFromPanel(DetachFromPanelEvent evt)
        {
            m_Owner.UpdateFromModel();
        }

        public void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (m_ForwardDependencies != null)
            {
                foreach (var(graphElement, dependencyType) in m_ForwardDependencies)
                {
                    if (dependencyType.HasFlagFast(DependencyTypes.Geometry))
                        graphElement.UpdateFromModel();
                }
            }
        }

        public void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (m_ForwardDependencies != null)
            {
                foreach (var(graphElement, dependencyType) in m_ForwardDependencies)
                {
                    if (dependencyType.HasFlagFast(DependencyTypes.Style))
                        graphElement.UpdateFromModel();
                }
            }
        }

        public void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            if (m_ForwardDependencies != null)
            {
                foreach (var(graphElement, dependencyType) in m_ForwardDependencies)
                {
                    if (dependencyType.HasFlagFast(DependencyTypes.Removal))
                        graphElement.UpdateFromModel();
                }
            }
        }

        /// <summary>
        /// Adds a dependency between a model and a UI.
        /// </summary>
        /// <param name="model">The model side of the dependency.</param>
        /// <param name="ui">The UI side of the dependency.</param>
        static void AddModelDependency(IGraphElementModel model, IModelView ui)
        {
            if (!s_ModelDependencies.TryGetValue(model.Guid, out var uiList))
            {
                uiList = new HashSet<IModelView>();
                s_ModelDependencies[model.Guid] = uiList;
            }

            uiList.Add(ui);
        }

        /// <summary>
        /// Removes a dependency between a model and a UI.
        /// </summary>
        /// <param name="model">The model side of the dependency.</param>
        /// <param name="ui">The UI side of the dependency.</param>
        static void RemoveModelDependency(IGraphElementModel model, IModelView ui)
        {
            if (s_ModelDependencies.TryGetValue(model.Guid, out var uiList))
            {
                uiList.Remove(ui);
            }
        }

        /// <summary>
        /// Gets the UIs that depends on a model. They need to be updated when the model changes.
        /// </summary>
        /// <param name="model">The model for which we're querying the UI.</param>
        public static IEnumerable<IModelView> GetModelDependencies(IGraphElementModel model)
        {
            return s_ModelDependencies.TryGetValue(model.Guid, out var uiList) ? uiList : Enumerable.Empty<IModelView>();
        }
    }
}
