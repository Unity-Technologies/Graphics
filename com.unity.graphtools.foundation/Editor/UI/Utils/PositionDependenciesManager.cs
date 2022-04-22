using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class PositionDependenciesManager
    {
        const int k_AlignHorizontalOffset = 30;
        const int k_AlignVerticalOffset = 30;

        readonly GraphView m_GraphView;
        readonly Dictionary<SerializableGUID, Dictionary<SerializableGUID, IDependency>> m_DependenciesByNode = new Dictionary<SerializableGUID, Dictionary<SerializableGUID, IDependency>>();
        readonly Dictionary<SerializableGUID, Dictionary<SerializableGUID, IDependency>> m_PortalDependenciesByNode = new Dictionary<SerializableGUID, Dictionary<SerializableGUID, IDependency>>();
        readonly HashSet<INodeModel> m_ModelsToMove = new HashSet<INodeModel>();
        readonly HashSet<INodeModel> m_TempMovedModels = new HashSet<INodeModel>();

        Vector2 m_StartPos;
        Preferences m_Preferences;

        public PositionDependenciesManager(GraphView graphView, Preferences preferences)
        {
            m_GraphView = graphView;
            m_Preferences = preferences;
        }

        void AddEdgeDependency(INodeModel parent, IDependency child)
        {
            if (!m_DependenciesByNode.TryGetValue(parent.Guid, out var link))
                m_DependenciesByNode.Add(parent.Guid, new Dictionary<SerializableGUID, IDependency> { { child.DependentNode.Guid, child } });
            else
            {
                if (link.TryGetValue(child.DependentNode.Guid, out IDependency dependency))
                {
                    if (dependency is LinkedNodesDependency linked)
                        linked.Count++;
                    else
                        Debug.LogWarning($"Dependency between nodes {parent} && {child.DependentNode} registered both as a {dependency.GetType().Name} and a {nameof(LinkedNodesDependency)}");
                }
                else
                {
                    link.Add(child.DependentNode.Guid, child);
                }
            }
        }

        internal List<IDependency> GetDependencies(INodeModel parent)
        {
            if (!m_DependenciesByNode.TryGetValue(parent.Guid, out var link))
                return null;
            return link.Values.ToList();
        }

        internal List<IDependency> GetPortalDependencies(IEdgePortalModel parent)
        {
            if (!m_PortalDependenciesByNode.TryGetValue(parent.Guid, out var link))
                return null;
            return link.Values.ToList();
        }

        public void Remove(SerializableGUID a, SerializableGUID b)
        {
            SerializableGUID parent;
            SerializableGUID child;
            if (m_DependenciesByNode.TryGetValue(a, out var link) &&
                link.TryGetValue(b, out var dependency))
            {
                parent = a;
                child = b;
            }
            else if (m_DependenciesByNode.TryGetValue(b, out link) &&
                     link.TryGetValue(a, out dependency))
            {
                parent = b;
                child = a;
            }
            else
                return;

            if (dependency is LinkedNodesDependency linked)
            {
                linked.Count--;
                if (linked.Count <= 0)
                    link.Remove(child);
            }
            else
                link.Remove(child);
            if (link.Count == 0)
                m_DependenciesByNode.Remove(parent);
        }

        public void Clear()
        {
            foreach (var pair in m_DependenciesByNode)
                pair.Value.Clear();
            m_DependenciesByNode.Clear();

            foreach (var pair in m_PortalDependenciesByNode)
                pair.Value.Clear();
            m_PortalDependenciesByNode.Clear();
        }

        public void LogDependencies()
        {
            if (m_Preferences?.GetBool(BoolPref.DependenciesLogging) ?? false)
            {
                Log("Dependencies :" + String.Join("\r\n", m_DependenciesByNode.Select(n =>
                {
                    var s = String.Join(",", n.Value.Select(p => p.Key));
                    return $"{n.Key}: {s}";
                })));

                Log("Portal Dependencies :" + String.Join("\r\n", m_PortalDependenciesByNode.Select(n =>
                {
                    var s = String.Join(",", n.Value.Select(p => p.Key));
                    return $"{n.Key}: {s}";
                })));
            }
        }

        void Log(string message)
        {
            if (m_Preferences?.GetBool(BoolPref.DependenciesLogging) ?? false)
                Debug.Log(message);
        }

        void ProcessDependency(INodeModel nodeModel, Vector2 delta, Action<GraphElement, IDependency, Vector2, INodeModel> dependencyCallback)
        {
            Log($"ProcessDependency {nodeModel}");

            if (!m_DependenciesByNode.TryGetValue(nodeModel.Guid, out var link))
                return;

            foreach (var dependency in link)
            {
                if (m_ModelsToMove.Contains(dependency.Value.DependentNode))
                    continue;
                if (!m_TempMovedModels.Add(dependency.Value.DependentNode))
                {
                    Log($"Skip ProcessDependency {dependency.Value.DependentNode}");
                    continue;
                }

                var graphElement = dependency.Value.DependentNode.GetView<Node>(m_GraphView);
                if (graphElement != null)
                    dependencyCallback(graphElement, dependency.Value, delta, nodeModel);
                else
                    Log($"Cannot find ui node for model: {dependency.Value.DependentNode} dependency from {nodeModel}");

                ProcessDependency(dependency.Value.DependentNode, delta, dependencyCallback);
            }
        }

        void ProcessMovedNodes(Vector2 lastMousePosition, Action<GraphElement, IDependency, Vector2, INodeModel> dependencyCallback)
        {
            Profiler.BeginSample("GTF.ProcessMovedNodes");

            m_TempMovedModels.Clear();
            Vector2 delta = lastMousePosition - m_StartPos;
            foreach (INodeModel nodeModel in m_ModelsToMove)
                ProcessDependency(nodeModel, delta, dependencyCallback);

            Profiler.EndSample();
        }

        void ProcessDependencyModel(INodeModel nodeModel, GraphModelStateComponent.StateUpdater graphUpdater,
            Action<IDependency, INodeModel, GraphModelStateComponent.StateUpdater> dependencyCallback)
        {
            Log($"ProcessDependencyModel {nodeModel}");

            if (!m_DependenciesByNode.TryGetValue(nodeModel.Guid, out var link))
                return;

            foreach (var dependency in link)
            {
                if (m_ModelsToMove.Contains(dependency.Value.DependentNode))
                    continue;
                if (!m_TempMovedModels.Add(dependency.Value.DependentNode))
                {
                    Log($"Skip ProcessDependency {dependency.Value.DependentNode}");
                    continue;
                }

                dependencyCallback(dependency.Value, nodeModel, graphUpdater);
                ProcessDependencyModel(dependency.Value.DependentNode, graphUpdater, dependencyCallback);
            }
        }

        void ProcessMovedNodeModels(Action<IDependency, INodeModel, GraphModelStateComponent.StateUpdater> dependencyCallback, GraphModelStateComponent.StateUpdater graphUpdater)
        {
            Profiler.BeginSample("GTF.ProcessMovedNodeModel");

            m_TempMovedModels.Clear();
            foreach (INodeModel nodeModel in m_ModelsToMove)
                ProcessDependencyModel(nodeModel, graphUpdater, dependencyCallback);

            Profiler.EndSample();
        }

        public void UpdateNodeState()
        {
            var processed = new HashSet<SerializableGUID>();
            void SetNodeState(INodeModel nodeModel, ModelState state)
            {
                if (nodeModel.State == ModelState.Disabled)
                    state = ModelState.Disabled;

                var nodeUI = nodeModel.GetView<Node>(m_GraphView);
                if (nodeUI != null && state == ModelState.Enabled)
                {
                    nodeUI.EnableInClassList(Node.disabledModifierUssClassName, false);
                    nodeUI.EnableInClassList(Node.unusedModifierUssClassName, false);
                }

                Dictionary<SerializableGUID, IDependency> dependencies = null;

                if (nodeModel is IEdgePortalModel edgePortalModel)
                    m_PortalDependenciesByNode.TryGetValue(edgePortalModel.Guid, out dependencies);

                if ((dependencies == null || !dependencies.Any()) &&
                    !m_DependenciesByNode.TryGetValue(nodeModel.Guid, out dependencies))
                    return;

                foreach (var dependency in dependencies)
                {
                    if (processed.Add(dependency.Key))
                        SetNodeState(dependency.Value.DependentNode, state);
                }
            }

            var graphModel = m_GraphView.GraphModel;
            foreach (var nodeModel in graphModel.NodeAndBlockModels)
            {
                var node = nodeModel.GetView<Node>(m_GraphView);
                if (node == null)
                    continue;

                if (nodeModel.State == ModelState.Disabled)
                {
                    node.EnableInClassList(Node.disabledModifierUssClassName, true);
                    node.EnableInClassList(Node.unusedModifierUssClassName, false);
                }
                else
                {
                    node.EnableInClassList(Node.disabledModifierUssClassName, false);
                    node.EnableInClassList(Node.unusedModifierUssClassName, true);
                }
            }

            foreach (var root in graphModel.Stencil.GetEntryPoints())
            {
                SetNodeState(root, ModelState.Enabled);
            }
        }

        public void ProcessMovedNodes(Vector2 lastMousePosition)
        {
            ProcessMovedNodes(lastMousePosition, OffsetDependency);
        }

        static void OffsetDependency(GraphElement element, IDependency model, Vector2 delta, INodeModel _)
        {
            Vector2 prevPos = model.DependentNode.Position;
            var pos = prevPos + delta;
            element.SetPosition(pos);
        }

        IGraphModel m_GraphModel;

        public void StartNotifyMove(IReadOnlyList<IGraphElementModel> selection, Vector2 lastMousePosition)
        {
            m_StartPos = lastMousePosition;
            m_ModelsToMove.Clear();
            m_GraphModel = null;

            foreach (var element in selection)
            {
                if (element is INodeModel nodeModel)
                {
                    if (m_GraphModel == null)
                        m_GraphModel = nodeModel.GraphModel;
                    else
                        Assert.AreEqual(nodeModel.GraphModel, m_GraphModel);
                    m_ModelsToMove.Add(nodeModel);
                }
            }
        }

        public void CancelMove()
        {
            ProcessMovedNodes(Vector2.zero, (element, model, _, __) =>
            {
                element.SetPosition(model.DependentNode.Position);
            });
            m_ModelsToMove.Clear();
        }

        public void StopNotifyMove()
        {
            // case when drag and dropping a declaration to the graph
            if (m_GraphModel == null)
                return;

            using (var graphUpdater = m_GraphView.GraphViewModel.GraphModelState.UpdateScope)
            {
                ProcessMovedNodes(Vector2.zero, (element, model, _, __) =>
                {
                    model.DependentNode.Position = element.layout.position;
                    // ReSharper disable once AccessToDisposedClosure
                    graphUpdater.MarkChanged(model.DependentNode, ChangeHint.Layout);
                });
            }

            m_ModelsToMove.Clear();
        }

        void AlignDependency(IDependency dependency, INodeModel prev, GraphModelStateComponent.StateUpdater graphUpdater)
        {
            // Warning: Don't try to use the VisualElement.layout Rect as it is not up to date yet.
            // Use Node.GetPosition() when possible

            var parentUI = prev.GetView<Node>(m_GraphView);
            var depUI = dependency.DependentNode.GetView<Node>(m_GraphView);

            if (parentUI == null || depUI == null)
                return;

            switch (dependency)
            {
                case LinkedNodesDependency linked:

                    var input = linked.ParentPort.GetView<Port>(m_GraphView);
                    var output = linked.DependentPort.GetView<Port>(m_GraphView);

                    if (input?.Model != null && output?.Model != null &&
                        ((IPortModel)input.Model).Orientation == ((IPortModel)output.Model).Orientation)
                    {
                        var depOffset = input.parent.ChangeCoordinatesTo(parentUI.parent, input.layout.min) - output.parent.ChangeCoordinatesTo(depUI.parent, output.layout.min);
                        var parentOffset = parentUI.layout.min - prev.Position;

                        Vector2 position;
                        if (((IPortModel)input.Model).Orientation == PortOrientation.Horizontal)
                        {
                            position = new Vector2(
                                prev.Position.x + (linked.ParentPort.Direction == PortDirection.Output
                                    ? parentUI.layout.width + k_AlignHorizontalOffset
                                    : -k_AlignHorizontalOffset - depUI.layout.width),
                                depUI.layout.min.y + depOffset.y - parentOffset.y
                            );
                        }
                        else
                        {
                            position = new Vector2(
                                depUI.layout.min.x + depOffset.x - parentOffset.x,
                                prev.Position.y + (linked.ParentPort.Direction == PortDirection.Output
                                    ? parentUI.layout.height + k_AlignVerticalOffset
                                    : -k_AlignVerticalOffset - depUI.layout.height)
                            );
                        }

                        linked.DependentNode.Position = position;
                        graphUpdater.MarkChanged(linked.DependentNode, ChangeHint.Layout);
                    }
                    break;
            }
        }

        public void AlignNodes(bool follow, IReadOnlyList<IGraphElementModel> entryPoints, GraphModelStateComponent.StateUpdater graphUpdater)
        {
            HashSet<INodeModel> topMostModels = new HashSet<INodeModel>();

            topMostModels.Clear();

            bool anyEdge = false;
            foreach (var edgeModel in entryPoints.OfType<IEdgeModel>())
            {
                if (!edgeModel.GraphModel.Stencil.CreateDependencyFromEdge(edgeModel, out var dependency, out var parent))
                    continue;
                anyEdge = true;

                AlignDependency(dependency, parent, graphUpdater);
                topMostModels.Add(dependency.DependentNode);
            }

            if (anyEdge && !follow)
                return;

            if (!topMostModels.Any())
            {
                foreach (var nodeModel in entryPoints.OfType<INodeModel>())
                {
                    topMostModels.Add(nodeModel);
                }
            }

            if (!anyEdge && !follow)
            {
                // Align each top-most node then move dependencies by the same delta
                foreach (INodeModel model in topMostModels)
                {
                    if (!m_DependenciesByNode.TryGetValue(model.Guid, out var dependencies))
                        continue;
                    foreach (var dependency in dependencies)
                    {
                        AlignDependency(dependency.Value, model, graphUpdater);
                    }
                }
            }
            else
            {
                // Align recursively
                m_ModelsToMove.UnionWith(topMostModels);
                ProcessMovedNodeModels(AlignDependency, graphUpdater);
            }

            m_ModelsToMove.Clear();
            m_TempMovedModels.Clear();
        }

        public void AddPositionDependency(IEdgeModel model)
        {
            if (model.GraphModel.Stencil == null ||
                !model.GraphModel.Stencil.CreateDependencyFromEdge(model, out var dependency, out INodeModel parent))
                return;
            AddEdgeDependency(parent, dependency);
            LogDependencies();
        }

        public void AddPortalDependency(IEdgePortalModel model)
        {
            var stencil = model.GraphModel.Stencil;

            // Update all portals linked to this portal definition.
            foreach (var portalModel in stencil.GetLinkedPortals(model))
            {
                m_PortalDependenciesByNode[portalModel.Guid] =
                    stencil.GetPortalDependencies(portalModel)
                        .ToDictionary(p => p.Guid, p => (IDependency) new PortalNodesDependency { DependentNode = p });
            }
            LogDependencies();
        }

        public void RemovePortalDependency(INodeModel model)
        {
            foreach (var dependencies in m_PortalDependenciesByNode.Values)
            {
                dependencies.Remove(model.Guid);
            }

            m_PortalDependenciesByNode.Remove(model.Guid);
        }
    }
}
