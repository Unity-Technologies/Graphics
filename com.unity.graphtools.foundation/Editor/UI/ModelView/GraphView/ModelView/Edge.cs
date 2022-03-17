using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.InternalModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The UI for an <see cref="IEdgeModel"/>.
    /// </summary>
    public class Edge : GraphElement, IDisplaySmartSearchUI
    {
        public new static readonly string ussClassName = "ge-edge";
        public static readonly string ghostModifierUssClassName = ussClassName.WithUssModifier("ghost");

        public static readonly string edgeControlPartName = "edge-control";
        public static readonly string edgeBubblePartName = "edge-bubble";

        EdgeManipulator m_EdgeManipulator;

        EdgeControl m_EdgeControl;

        IModelView m_LastUsedFromPort;
        IModelView m_LastUsedToPort;
        IEdgeModel m_LastUsedEdgeModel;

        protected EdgeManipulator EdgeManipulator
        {
            get => m_EdgeManipulator;
            set => this.ReplaceManipulator(ref m_EdgeManipulator, value);
        }

        public IEdgeModel EdgeModel => Model as IEdgeModel;

        public bool IsGhostEdge => EdgeModel is IGhostEdge;

        public Vector2 From
        {
            get
            {
                var p = Vector2.zero;

                var port = EdgeModel.FromPort;
                if (port == null)
                {
                    if (EdgeModel is IGhostEdge ghostEdgeModel)
                    {
                        p = ghostEdgeModel.EndPoint;
                    }
                }
                else
                {
                    var ui = port.GetView<Port>(RootView);
                    if (ui == null)
                        return Vector2.zero;

                    p = ui.GetGlobalCenter();
                }

                return this.WorldToLocal(p);
            }
        }

        public Vector2 To
        {
            get
            {
                var p = Vector2.zero;

                var port = EdgeModel.ToPort;
                if (port == null)
                {
                    if (EdgeModel is GhostEdgeModel ghostEdgeModel)
                    {
                        p = ghostEdgeModel.EndPoint;
                    }
                }
                else
                {
                    var ui = port.GetView<Port>(RootView);
                    if (ui == null)
                        return Vector2.zero;

                    p = ui.GetGlobalCenter();
                }

                return this.WorldToLocal(p);
            }
        }

        public EdgeControl EdgeControl
        {
            get
            {
                if (m_EdgeControl == null)
                {
                    var edgeControlPart = PartList.GetPart(edgeControlPartName);
                    m_EdgeControl = edgeControlPart?.Root as EdgeControl;
                }

                return m_EdgeControl;
            }
        }

        public IPortModel Output => EdgeModel.FromPort;

        public IPortModel Input => EdgeModel.ToPort;

        /// <inheritdoc />
        public override bool ShowInMiniMap => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Edge"/> class.
        /// </summary>
        public Edge()
        {
            Layer = -1;

            EdgeManipulator = new EdgeManipulator();
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(EdgeControlPart.Create(edgeControlPartName, Model, this, ussClassName));
            PartList.AppendPart(EdgeBubblePart.Create(edgeBubblePartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
            EnableInClassList(ghostModifierUssClassName, IsGhostEdge);
            this.AddStylesheet("Edge.uss");
        }

        /// <inheritdoc />
        public override bool HasBackwardsDependenciesChanged()
        {
            return m_LastUsedFromPort != EdgeModel.FromPort?.GetView(RootView) || m_LastUsedToPort != EdgeModel.ToPort?.GetView(RootView);
        }

        /// <inheritdoc />
        public override bool HasModelDependenciesChanged() => m_LastUsedEdgeModel != EdgeModel;

        /// <inheritdoc/>
        public override void AddBackwardDependencies()
        {
            base.AddBackwardDependencies();

            // When the ports move, the edge should be redrawn.
            AddDependencies(EdgeModel.FromPort);
            AddDependencies(EdgeModel.ToPort);

            m_LastUsedFromPort = EdgeModel.FromPort.GetView(RootView);
            m_LastUsedToPort = EdgeModel.ToPort.GetView(RootView);

            void AddDependencies(IPortModel portModel)
            {
                if (portModel == null)
                    return;

                var ui = portModel.GetView(RootView);
                if (ui != null)
                {
                    // Edge color changes with port color.
                    Dependencies.AddBackwardDependency(ui, DependencyTypes.Style);
                }

                ui = portModel.NodeModel.GetView(RootView);
                if (ui != null)
                {
                    // Edge position changes with node position.
                    Dependencies.AddBackwardDependency(ui, DependencyTypes.Geometry);
                }

                ui = (portModel.NodeModel.Container as IGraphElementModel)?.GetView(GraphView);
                if (ui != null)
                {
                    // Edge position changes with container's position.
                    Dependencies.AddBackwardDependency(ui, DependencyTypes.Geometry);
                }
            }
        }

        /// <inheritdoc/>
        public override void AddModelDependencies()
        {
            var ui = EdgeModel.FromPort?.GetView<Port>(RootView);
            ui?.AddDependencyToEdgeModel(EdgeModel);

            ui = EdgeModel.ToPort?.GetView<Port>(RootView);
            ui?.AddDependencyToEdgeModel(EdgeModel);

            m_LastUsedEdgeModel = EdgeModel;
        }

        /// <inheritdoc />
        public override bool Overlaps(Rect rectangle)
        {
            return EdgeControl.Overlaps(this.ChangeCoordinatesTo(EdgeControl, rectangle));
        }

        /// <inheritdoc />
        public override bool ContainsPoint(Vector2 localPoint)
        {
            return EdgeControl.ContainsPoint(this.ChangeCoordinatesTo(EdgeControl, localPoint));
        }

        /// <inheritdoc />
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (!(evt.currentTarget is Edge edge))
                return;

            {
                int initialMenuItemCount = evt.menu.MenuItems().Count;

                if ((edge.EdgeModel.FromPort as IReorderableEdgesPortModel)?.HasReorderableEdges ?? false)
                {
                    if (initialMenuItemCount > 0)
                        evt.menu.AppendSeparator();

                    var siblingEdges = edge.EdgeModel.FromPort.GetConnectedEdges().ToList();
                    var siblingEdgesCount = siblingEdges.Count;

                    var index = siblingEdges.IndexOf(edge.EdgeModel);
                    evt.menu.AppendAction("Reorder Edge/Move First",
                        _ => ReorderEdges(ReorderEdgeCommand.ReorderType.MoveFirst),
                        siblingEdgesCount > 1 && index > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                    evt.menu.AppendAction("Reorder Edge/Move Up",
                        _ => ReorderEdges(ReorderEdgeCommand.ReorderType.MoveUp),
                        siblingEdgesCount > 1 && index > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                    evt.menu.AppendAction("Reorder Edge/Move Down",
                        _ => ReorderEdges(ReorderEdgeCommand.ReorderType.MoveDown),
                        siblingEdgesCount > 1 && index < siblingEdgesCount - 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                    evt.menu.AppendAction("Reorder Edge/Move Last",
                        _ => ReorderEdges(ReorderEdgeCommand.ReorderType.MoveLast),
                        siblingEdgesCount > 1 && index < siblingEdgesCount - 1 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                    void ReorderEdges(ReorderEdgeCommand.ReorderType reorderType)
                    {
                        GraphView.Dispatch(new ReorderEdgeCommand(edge.EdgeModel, reorderType));
                    }
                }
            }
        }

        /// <inheritdoc/>
        public virtual bool DisplaySmartSearch(Vector2 mousePosition)
        {
            var graphPosition = GraphView.ContentViewContainer.WorldToLocal(mousePosition);
            SearcherService.ShowEdgeNodes(
                GraphView.GraphModel.Stencil as Stencil,
                GraphView.GraphTool.Name,
                GraphView.GetType(),
                GraphView.GraphTool.Preferences,
                GraphView.GraphModel, EdgeModel, mousePosition, item =>
            {
                GraphView.Dispatch(CreateNodeCommand.OnEdge(item, EdgeModel, graphPosition));
            }, GraphView.Window);

            return true;
        }
    }
}
