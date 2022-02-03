using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// UI for placemats.
    /// </summary>
    public class Placemat : GraphElement
    {
        enum MinSizePolicy
        {
            EnsureMinSize,
            DoNotEnsureMinSize
        }

        protected internal static readonly Vector2 defaultCollapsedSize = new Vector2(200, 42);
        protected static readonly int k_SelectRectOffset = 3;

        public new static readonly string ussClassName = "ge-placemat";
        public static readonly string collapsedModifierUssClassName = ussClassName.WithUssModifier("collapsed");
        public static readonly string selectionBorderElementName = "selection-border";
        public static readonly string titleContainerPartName = "title-container";
        public static readonly string collapseButtonPartName = "collapse-button";
        public static readonly string resizerPartName = "resizer";

        protected internal static readonly float bounds = 9.0f;
        protected internal static readonly float boundTop = 29.0f; // Current height of Title

        // The next two values need to be the same as USS... however, we can't get the values from there as we need them in a static
        // methods used to create new placemats
        protected static readonly float k_MinWidth = 200;
        protected static readonly float k_MinHeight = 100;

        protected VisualElement m_ContentContainer;

        protected HashSet<GraphElement> m_CollapsedElements = new HashSet<GraphElement>();

        public IPlacematModel PlacematModel => Model as IPlacematModel;

        public override VisualElement contentContainer => m_ContentContainer ?? this;

        /// <summary>
        /// The size of the placemat in its uncollapsed state.
        /// </summary>
        protected Vector2 UncollapsedSize => PlacematModel.PositionAndSize.size;

        /// <summary>
        /// The size of the placemat in its collapsed state.
        /// </summary>
        protected Vector2 CollapsedSize
        {
            get
            {
                var actualCollapsedSize = defaultCollapsedSize;
                if (UncollapsedSize.x < defaultCollapsedSize.x)
                    actualCollapsedSize.x = UncollapsedSize.x;

                return actualCollapsedSize;
            }
        }

        protected Rect EffectArea => PlacematModel.Collapsed ? new Rect(layout.position, UncollapsedSize) : layout;

        /// <summary>
        /// The graph elements that are currently being hidden by the placemat.
        /// </summary>
        protected IEnumerable<GraphElement> CollapsedElements
        {
            get => m_CollapsedElements;
            set
            {
                foreach (var collapsedElement in m_CollapsedElements)
                {
                    collapsedElement.style.visibility = StyleKeyword.Null;
                }

                m_CollapsedElements.Clear();

                if (value == null)
                    return;

                foreach (var collapsedElement in value)
                {
                    collapsedElement.style.visibility = Visibility.Hidden;
                    m_CollapsedElements.Add(collapsedElement);
                }
            }
        }

        public Placemat()
        {
            focusable = true;

            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        protected override void BuildPartList()
        {
            var editableTitlePart = EditableTitlePart.Create(titleContainerPartName, Model, this, ussClassName);
            PartList.AppendPart(editableTitlePart);
            var collapseButtonPart = CollapseButtonPart.Create(collapseButtonPartName, Model, this, ussClassName);
            editableTitlePart.PartList.AppendPart(collapseButtonPart);
            PartList.AppendPart(FourWayResizerPart.Create(resizerPartName, Model, this, ussClassName));
        }

        protected override void BuildElementUI()
        {
            var selectionBorder = new SelectionBorder { name = selectionBorderElementName };
            selectionBorder.AddToClassList(ussClassName.WithUssElement(selectionBorderElementName));
            Add(selectionBorder);
            m_ContentContainer = selectionBorder.ContentContainer;

            base.BuildElementUI();

            usageHints = UsageHints.DynamicTransform;
            AddToClassList(ussClassName);
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            var collapseButton = this.SafeQ(collapseButtonPartName);
            collapseButton?.RegisterCallback<ChangeEvent<bool>>(OnCollapseChangeEvent);

            this.AddStylesheet("Placemat.uss");
        }

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            style.backgroundColor = PlacematModel.Color;

            var newPos = PlacematModel.PositionAndSize.position;
            style.left = newPos.x;
            style.top = newPos.y;

            if (PlacematModel.Collapsed)
            {
                style.width = CollapsedSize.x;
                style.height = CollapsedSize.y;
            }
            else
            {
                style.width = UncollapsedSize.x;
                style.height = UncollapsedSize.y;
            }

            EnableInClassList(collapsedModifierUssClassName, PlacematModel.Collapsed);

            if (PlacematModel.Collapsed)
            {
                var collapsedElements = new List<GraphElement>();
                if (PlacematModel.HiddenElements != null)
                {
                    foreach (var elementModel in PlacematModel.HiddenElements)
                    {
                        var graphElement = elementModel.GetUI<GraphElement>(View);
                        if (graphElement != null)
                            collapsedElements.Add(graphElement);
                    }
                }

                GatherCollapsedEdges(collapsedElements);
                CollapsedElements = collapsedElements;
            }
            else
            {
                CollapsedElements = null;
            }
        }

        // PF FIXME: we can probably improve the performance of this.
        // Idea: build a bounding box of placemats affected by currentPlacemat and use this BB to intersect with nodes.
        // PF TODO: also revisit Placemat other recursive functions for perf improvements.
        protected static void GatherDependencies(Placemat currentPlacemat, IList<ModelUI> graphElements, ICollection<ModelUI> dependencies)
        {
            if (currentPlacemat.PlacematModel.Collapsed)
            {
                foreach (var cge in currentPlacemat.CollapsedElements)
                {
                    dependencies.Add(cge);
                    if (cge is Placemat placemat)
                        GatherDependencies(placemat, graphElements, dependencies);
                }

                return;
            }

            // We want gathering dependencies to work even if the placemat layout is not up to date, so we use the
            // currentPlacemat.PlacematModel.PositionAndSize to do our overlap test.
            var currRect = currentPlacemat.PlacematModel.PositionAndSize;
            var currentActivePlacematRect = RectUtils.Inflate(currRect, -k_SelectRectOffset, -k_SelectRectOffset,
                -k_SelectRectOffset, -k_SelectRectOffset);

            var currentPlacematZOrder = currentPlacemat.PlacematModel.GetZOrder();
            foreach (var elem in graphElements)
            {
                if (elem.layout.Overlaps(currentActivePlacematRect))
                {
                    var placemat = elem as Placemat;
                    if (placemat != null && placemat.PlacematModel.GetZOrder() > currentPlacematZOrder)
                    {
                        GatherDependencies(placemat, graphElements, dependencies);
                    }

                    if (placemat == null || placemat.PlacematModel.GetZOrder() > currentPlacematZOrder)
                    {
                        dependencies.Add(elem);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override bool HasForwardsDependenciesChanged()
        {
            // Would need to look into making this fast... Always re-add the dependencies for now.
            return true;
        }

        static readonly List<ModelUI> k_AddForwardDependenciesAllUIs = new List<ModelUI>();
        /// <inheritdoc/>
        public override void AddForwardDependencies()
        {
            GraphView.GraphModel.GraphElementModels
                .Where(ge => ge.IsSelectable() && !(ge is IEdgeModel))
                .GetAllUIsInList(GraphView, e => e.parent is GraphView.Layer, k_AddForwardDependenciesAllUIs);

            var dependencies = new List<ModelUI>();
            GatherDependencies(this, k_AddForwardDependenciesAllUIs, dependencies);
            k_AddForwardDependenciesAllUIs.Clear();

            var nodeModels = dependencies.Select(e => e.Model).OfType<INodeModel>();
            foreach (var edgeModel in nodeModels.SelectMany(n => n.GetConnectedEdges()))
            {
                var ui = edgeModel.GetUI(View);
                if (ui != null)
                {
                    // Edge models endpoints need to be updated when the placemat is collapsed/uncollapsed.
                    Dependencies.AddForwardDependency(ui, DependencyTypes.Geometry | DependencyTypes.Removal);
                }
            }
        }

        protected void OnCollapseChangeEvent(ChangeEvent<bool> evt)
        {
            this.CollapsePlacemat(evt.newValue);
        }

        public override void SetPosition(Rect newPos)
        {
            if (PlacematModel.Collapsed)
                newPos.size = CollapsedSize;

            base.SetPosition(newPos);
            style.height = newPos.height;
            style.width = newPos.width;
        }

        protected static IEnumerable<GraphElement> AllCollapsedElements(IEnumerable<GraphElement> collapsedElements)
        {
            if (collapsedElements != null)
            {
                foreach (var element in collapsedElements)
                {
                    switch (element)
                    {
                        case Placemat placemat when placemat.PlacematModel.Collapsed:
                        {
                            // TODO: evaluate performance of this recursive call.
                            foreach (var subElement in AllCollapsedElements(placemat.CollapsedElements))
                                yield return subElement;
                            yield return element;
                            break;
                        }
                        case Placemat placemat when !placemat.PlacematModel.Collapsed:
                            yield return element;
                            break;
                        case {} e when e.IsMovable():
                            yield return element;
                            break;
                    }
                }
            }
        }

        protected void GatherCollapsedEdges(ICollection<GraphElement> collapsedElements)
        {
            var allCollapsedNodes = AllCollapsedElements(collapsedElements)
                .Select(e => e.Model)
                .OfType<INodeModel>()
                .ToList();
            foreach (var edge in PlacematModel.GraphModel.EdgeModels)
            {
                if (AnyNodeIsConnectedToPort(allCollapsedNodes, edge.ToPort) && AnyNodeIsConnectedToPort(allCollapsedNodes, edge.FromPort))
                {
                    var edgeUI = edge.GetUI<GraphElement>(View);
                    if (!collapsedElements.Contains(edgeUI))
                    {
                        collapsedElements.Add(edgeUI);
                    }
                }
            }
        }

        static readonly List<ModelUI> k_GatherCollapsedElementsAllUIs = new List<ModelUI>();
        protected internal List<IGraphElementModel> GatherCollapsedElements()
        {
            List<GraphElement> collapsedElements = new List<GraphElement>();

            var graphView = GraphView;
            graphView.GraphModel.GraphElementModels
                .Where(ge => ge.IsSelectable() && !(ge is IEdgeModel))
                .GetAllUIsInList(GraphView, e => e.parent is GraphView.Layer, k_GatherCollapsedElementsAllUIs);

            var collapsedElementsElsewhere = new List<GraphElement>();
            RecurseGatherCollapsedElements(this, k_GatherCollapsedElementsAllUIs.OfType<GraphElement>(), collapsedElementsElsewhere);
            k_GatherCollapsedElementsAllUIs.Clear();

            var nodes = new HashSet<INodeModel>(AllCollapsedElements(collapsedElements).Select(e => e.Model).OfType<INodeModel>());

            for (var index = 0; index < graphView.GraphModel.EdgeModels.Count; index++)
            {
                var edge = graphView.GraphModel.EdgeModels[index];
                if (AnyNodeIsConnectedToPort(nodes, edge.ToPort) && AnyNodeIsConnectedToPort(nodes, edge.FromPort))
                    collapsedElements.Add(edge.GetUI<Edge>(graphView));
            }

            foreach (var ge in collapsedElementsElsewhere)
                collapsedElements.Remove(ge);

            return collapsedElements.Select(e => e.Model).ToList();

            void RecurseGatherCollapsedElements(Placemat currentPlacemat, IEnumerable<GraphElement> graphElementsParam,
                List<GraphElement> collapsedElementsElsewhereParam)
            {
                var currRect = currentPlacemat.EffectArea;
                var currentActivePlacematRect = RectUtils.Inflate(currRect, -k_SelectRectOffset, -k_SelectRectOffset, -k_SelectRectOffset, -k_SelectRectOffset);

                var currentPlacematZOrder = currentPlacemat.PlacematModel.GetZOrder();
                foreach (var elem in graphElementsParam)
                {
                    if (elem.layout.Overlaps(currentActivePlacematRect))
                    {
                        var placemat = elem as Placemat;
                        if (placemat != null && placemat.PlacematModel.GetZOrder() > currentPlacematZOrder)
                        {
                            if (placemat.PlacematModel.Collapsed)
                                foreach (var cge in placemat.CollapsedElements)
                                    collapsedElementsElsewhereParam.Add(cge);
                            else
                                RecurseGatherCollapsedElements(placemat, graphElementsParam, collapsedElementsElsewhereParam);
                        }

                        if (placemat == null || placemat.PlacematModel.GetZOrder() > currentPlacematZOrder)
                            if (elem.resolvedStyle.visibility == Visibility.Visible)
                                collapsedElements.Add(elem);
                    }
                }
            }
        }

        protected static bool AnyNodeIsConnectedToPort(IEnumerable<INodeModel> nodes, IPortModel port)
        {
            if (port.NodeModel == null)
            {
                return false;
            }

            foreach (var node in nodes)
            {
                if (node == port.NodeModel)
                    return true;
            }

            return false;
        }

#if UNITY_2022_1_OR_NEWER
        [EventInterest(typeof(PointerDownEvent))]
#endif
        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);
            if (evt is PointerDownEvent mde)
                if (mde.clickCount == 2 && mde.button == (int)MouseButton.LeftMouse)
                {
                    var models = new List<IGraphElementModel>();
                    ActOnGraphElementsOver(e => models.Add(e.Model));
                    GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, models));
                }
        }

        static readonly List<ModelUI> k_ActOnGraphElementsOverAllUIs = new List<ModelUI>();
        protected void ActOnGraphElementsOver(Action<GraphElement> act)
        {
            GraphView.GraphModel.GraphElementModels
                .Where(ge => ge.IsSelectable() && !(ge is IEdgeModel))
                .GetAllUIsInList(GraphView, e => e.parent is GraphView.Layer, k_ActOnGraphElementsOverAllUIs);

            foreach (var elem in k_ActOnGraphElementsOverAllUIs.OfType<GraphElement>())
            {
                if (elem.layout.Overlaps(layout))
                    act(elem);
            }

            k_ActOnGraphElementsOverAllUIs.Clear();
        }

        static readonly List<ModelUI> k_ActOnGraphElementsOver2AllUIs = new List<ModelUI>();
        protected internal bool ActOnGraphElementsOver(Func<GraphElement, bool> act, bool includePlacemats)
        {
            GraphView.GraphModel.GraphElementModels
                .Where(ge => ge.IsSelectable() && !(ge is IEdgeModel))
                .GetAllUIsInList(GraphView, e => e.parent is GraphView.Layer, k_ActOnGraphElementsOver2AllUIs);

            var retVal = RecurseActOnGraphElementsOver(this);
            k_ActOnGraphElementsOver2AllUIs.Clear();
            return retVal;

            bool RecurseActOnGraphElementsOver(Placemat currentPlacemat)
            {
                if (currentPlacemat.PlacematModel.Collapsed)
                {
                    var currentPlacematZOrder = currentPlacemat.PlacematModel.GetZOrder();
                    foreach (var elem in currentPlacemat.CollapsedElements)
                    {
                        var placemat = elem as Placemat;
                        if (placemat != null && placemat.PlacematModel.GetZOrder() > currentPlacematZOrder)
                            if (RecurseActOnGraphElementsOver(placemat))
                                return true;

                        if (placemat == null || (includePlacemats && placemat.PlacematModel.GetZOrder() > currentPlacematZOrder))
                            if (act(elem))
                                return true;
                    }
                }
                else
                {
                    var currRect = currentPlacemat.EffectArea;
                    var currentActivePlacematRect = new Rect(
                        currRect.x + k_SelectRectOffset,
                        currRect.y + k_SelectRectOffset,
                        currRect.width - 2 * k_SelectRectOffset,
                        currRect.height - 2 * k_SelectRectOffset);

                    var currentPlacematZOrder = currentPlacemat.PlacematModel.GetZOrder();
                    foreach (var elem in k_ActOnGraphElementsOver2AllUIs.OfType<GraphElement>())
                    {
                        if (elem.layout.Overlaps(currentActivePlacematRect))
                        {
                            var placemat = elem as Placemat;
                            if (placemat != null && placemat.PlacematModel.GetZOrder() > currentPlacematZOrder)
                                if (RecurseActOnGraphElementsOver(placemat))
                                    return true;

                            if (placemat == null || (includePlacemats && placemat.PlacematModel.GetZOrder() > currentPlacematZOrder))
                                if (elem.resolvedStyle.visibility != Visibility.Hidden)
                                    if (act(elem))
                                        return true;
                        }
                    }
                }

                return false;
            }
        }

        protected internal bool WillDragNode(GraphElement node)
        {
            if (PlacematModel.Collapsed)
                return AllCollapsedElements(CollapsedElements).Contains(node);

            return ActOnGraphElementsOver(t => node == t, true);
        }

        internal Rect ComputeGrowToFitElementsRect(List<GraphElement> elements = null)
        {
            if (elements == null)
                elements = GetNodesOverThisPlacemat();

            var pos = new Rect();
            if (elements.Count > 0 && ComputeElementBounds(ref pos, elements, MinSizePolicy.DoNotEnsureMinSize))
            {
                // We don't resize to be snug. In other words: we don't ever decrease in size.
                Rect currentRect = layout;
                if (pos.xMin > currentRect.xMin)
                    pos.xMin = currentRect.xMin;

                if (pos.xMax < currentRect.xMax)
                    pos.xMax = currentRect.xMax;

                if (pos.yMin > currentRect.yMin)
                    pos.yMin = currentRect.yMin;

                if (pos.yMax < currentRect.yMax)
                    pos.yMax = currentRect.yMax;

                MakeRectAtLeastMinimalSize(ref pos);
            }

            return pos;
        }

        internal Rect ComputeShrinkToFitElementsRect()
        {
            var elements = GetNodesOverThisPlacemat();
            var pos = new Rect();
            ComputeElementBounds(ref pos, elements);
            return pos;
        }

        Rect ComputeResizeToIncludeSelectedNodesRect()
        {
            var nodes = GraphView.GetSelection().
                OfType<INodeModel>().
                Select(n => n.GetUI<GraphElement>(View)).
                ToList();

            // Now include the selected nodes
            var pos = new Rect();
            if (ComputeElementBounds(ref pos, nodes, MinSizePolicy.DoNotEnsureMinSize))
            {
                // We don't resize to be snug: we only resize enough to contain the selected nodes.
                var currentRect = layout;
                if (pos.xMin > currentRect.xMin)
                    pos.xMin = currentRect.xMin;

                if (pos.xMax < currentRect.xMax)
                    pos.xMax = currentRect.xMax;

                if (pos.yMin > currentRect.yMin)
                    pos.yMin = currentRect.yMin;

                if (pos.yMax < currentRect.yMax)
                    pos.yMax = currentRect.yMax;

                MakeRectAtLeastMinimalSize(ref pos);
            }

            return pos;
        }

        internal void GetElementsToMove(bool moveOnlyPlacemat, HashSet<GraphElement> collectedElementsToMove)
        {
            if (PlacematModel.Collapsed)
            {
                var collapsedElements = AllCollapsedElements(CollapsedElements);
                foreach (var element in collapsedElements)
                {
                    collectedElementsToMove.Add(element);
                }
            }
            else if (!moveOnlyPlacemat)
            {
                ActOnGraphElementsOver(e =>
                {
                    collectedElementsToMove.Add(e);
                    return false;
                }, true);
            }
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (!(evt.currentTarget is Placemat placemat))
                return;

            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            evt.menu.AppendAction(placemat.PlacematModel.Collapsed ? "Expand Placemat" : "Collapse Placemat",
                a => placemat.CollapsePlacemat(!placemat.PlacematModel.Collapsed));

            // Gather nodes here so that we don't recycle this code in the resize functions.
            List<GraphElement> hoveringNodes = placemat.GetNodesOverThisPlacemat();

            evt.menu.AppendAction("Resize Placemat/Grow to Fit Nodes",
                a =>
                {
                    var newRect = placemat.ComputeGrowToFitElementsRect();
                    if (newRect != Rect.zero)
                        GraphView.Dispatch(new ChangeElementLayoutCommand(PlacematModel, newRect));
                },
                hoveringNodes.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Resize Placemat/Shrink to Fit Nodes",
                a =>
                {
                    var newRect = placemat.ComputeShrinkToFitElementsRect();
                    if (newRect != Rect.zero)
                        GraphView.Dispatch(new ChangeElementLayoutCommand(PlacematModel, newRect));
                },
                hoveringNodes.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Resize Placemat/Grow to Fit Selected Nodes",
                a =>
                {
                    var newRect = placemat.ComputeResizeToIncludeSelectedNodesRect();
                    GraphView.Dispatch(new ChangeElementLayoutCommand(PlacematModel, newRect));
                },
                s =>
                {
                    var selectedNodes = placemat.GraphView.GetSelection().Where(e => e is INodeModel);
                    return selectedNodes.Any() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
                });

            var placemats = GraphView.GraphModel.PlacematModels;

            // JOCE TODO: Check that *ALL* placemats are at the top or bottom. We should be able to do something otherwise.
            var placematIsTop = placemats.Last() == placemat.PlacematModel;
            var placematIsBottom = placemats.First() == placemat.PlacematModel;
            var canBeReordered = placemats.Count > 1;

            var selectedPlacemats = GraphView.GetSelection().OfType<IPlacematModel>().ToList();

            evt.menu.AppendAction("Reorder Placemats/Bring to Front",
                _ => GraphView.Dispatch(new ChangePlacematOrderCommand(ChangePlacematOrderCommand.PlacematOrderingAction.MoveTop, selectedPlacemats)),
                canBeReordered && !placematIsTop ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Reorder Placemats/Bring Forward",
                _ => GraphView.Dispatch(new ChangePlacematOrderCommand(ChangePlacematOrderCommand.PlacematOrderingAction.MoveForward, selectedPlacemats)),
                canBeReordered && !placematIsTop ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Reorder Placemats/Send Backward",
                _ => GraphView.Dispatch(new ChangePlacematOrderCommand(ChangePlacematOrderCommand.PlacematOrderingAction.MoveBackward, selectedPlacemats)),
                canBeReordered && !placematIsBottom ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendAction("Reorder Placemats/Send to Back",
                _ => GraphView.Dispatch(new ChangePlacematOrderCommand(ChangePlacematOrderCommand.PlacematOrderingAction.MoveBottom, selectedPlacemats)),
                canBeReordered && !placematIsBottom ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        protected List<GraphElement> GetNodesOverThisPlacemat()
        {
            var potentialElements = new List<ModelUI>();
            ActOnGraphElementsOver(e => potentialElements.Add(e));

            return potentialElements.OfType<GraphElement>().Where(e => e.Model is INodeModel).ToList();
        }

        protected void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            CollapsedElements = null;
        }

        protected internal bool GetPortCenterOverride(IPortModel port, out Vector2 overriddenPosition)
        {
            if (!PlacematModel.Collapsed || parent == null)
            {
                overriddenPosition = Vector2.zero;
                return false;
            }

            const int xOffset = 6;
            const int yOffset = 3;
            var halfSize = CollapsedSize * 0.5f;
            var offset = port.Orientation == PortOrientation.Horizontal
                ? new Vector2(port.Direction == PortDirection.Input ? -halfSize.x + xOffset : halfSize.x - xOffset, 0)
                : new Vector2(0, port.Direction == PortDirection.Input ? -halfSize.y + yOffset : halfSize.y - yOffset);

            overriddenPosition = parent.LocalToWorld(layout.center + offset);
            return true;
        }

        // Helper method that calculates how big a Placemat should be to fit the nodes on top of it currently.
        // Returns false if bounds could not be computed.
        protected internal static bool ComputeElementBounds(ref Rect pos, List<GraphElement> elements)
        {
            return ComputeElementBounds(ref pos, elements, MinSizePolicy.EnsureMinSize);
        }

        // Helper method that calculates how big a Placemat should be to fit the nodes on top of it currently.
        // Returns false if bounds could not be computed.
        static bool ComputeElementBounds(ref Rect pos, List<GraphElement> elements, MinSizePolicy ensureMinSize)
        {
            if (elements == null || elements.Count == 0)
                return false;

            float minX = Mathf.Infinity;
            float maxX = -Mathf.Infinity;
            float minY = Mathf.Infinity;
            float maxY = -Mathf.Infinity;

            foreach (var r in elements.Where(t => t.Model.IsMovable()).Select(n => n.layout))
            {
                if (r.xMin < minX)
                    minX = r.xMin;

                if (r.xMax > maxX)
                    maxX = r.xMax;

                if (r.yMin < minY)
                    minY = r.yMin;

                if (r.yMax > maxY)
                    maxY = r.yMax;
            }

            var width = maxX - minX + bounds * 2.0f;
            var height = maxY - minY + bounds * 2.0f + boundTop;

            pos = new Rect(
                minX - bounds,
                minY - (boundTop + bounds),
                width,
                height);

            if (ensureMinSize == MinSizePolicy.EnsureMinSize)
                MakeRectAtLeastMinimalSize(ref pos);

            return true;
        }

        static void MakeRectAtLeastMinimalSize(ref Rect r)
        {
            if (r.width < k_MinWidth)
                r.width = k_MinWidth;

            if (r.height < k_MinHeight)
                r.height = k_MinHeight;
        }
    }
}
