using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A GraphElement to display a <see cref="IGroupModel"/>.
    /// </summary>
    public class BlackboardVariableGroup : GraphElement
    {
        /// <summary>
        /// The name for the selection border.
        /// </summary>
        public static readonly string selectionBorderElementName = "selection-border";

        /// <summary>
        /// The uss class for this element.
        /// </summary>
        public static new readonly string ussClassName = "ge-blackboard-variable-group";

        /// <summary>
        /// The uss class for the title container.
        /// </summary>
        public static readonly string titleContainerUssClassName = ussClassName.WithUssElement("title-container");

        /// <summary>
        /// The uss class name for the collapse toggle button.
        /// </summary>
        public static readonly string titleToggleUssClassName = ussClassName.WithUssElement("foldout");

        /// <summary>
        /// The uss class for the bottom line.
        /// </summary>
        public static readonly string bottomLineUssClassName = ussClassName.WithUssElement("bottom-line");

        /// <summary>
        /// The uss class for the drag indicator.
        /// </summary>
        public static readonly string dragIndicatorUssClassName = ussClassName.WithUssElement("drag-indicator");

        /// <summary>
        /// The uss class for the selection border.
        /// </summary>
        public static readonly string selectionBorderUssClassName = ussClassName.WithUssElement(selectionBorderElementName);

        /// <summary>
        /// The uss class for the element with the collapsed modifier.
        /// </summary>
        public static readonly string collapsedUssClassName = ussClassName.WithUssModifier("collapsed");
        /// <summary>
        /// The uss class for the element with the collapsed modifier.
        /// </summary>
        public static readonly string evenUssClassName = ussClassName.WithUssModifier("even");

        /// <summary>
        /// The Label displaying the title.
        /// </summary>
        protected VisualElement m_TitleLabel;

        /// <summary>
        /// The toggle button for collapsing the group.
        /// </summary>
        protected Toggle m_TitleToggle;

        /// <summary>
        /// The element containing the group's items representations.
        /// </summary>
        protected VisualElement m_ItemsContainer;

        /// <summary>
        /// The drag indicator element.
        /// </summary>
        protected VisualElement m_DragIndicator;

        /// <summary>
        /// The selection border element.
        /// </summary>
        protected SelectionBorder m_SelectionBorder;

        /// <summary>
        /// The title element.
        /// </summary>
        protected VisualElement m_Title;

        /// <summary>
        /// The bottom line element.
        /// </summary>
        protected VisualElement m_BottomLine;

        SelectionDropper m_SelectionDropper;

        VisualElement m_ContentContainer;

        /// <summary>
        /// The name of the title part.
        /// </summary>
        protected static readonly string TitlePartName = "title-part";

        /// <summary>
        /// The name of the items part.
        /// </summary>
        protected static readonly string ItemsPartName = "items-part";

        IGroupModel GroupModel => Model as IGroupModel;

        /// <summary>
        /// The selection dropper assigned to this element.
        /// </summary>
        protected SelectionDropper SelectionDropper
        {
            get => m_SelectionDropper;
            set => this.ReplaceManipulator(ref m_SelectionDropper, value);
        }

        /// <summary>
        /// The <see cref="BlackboardSection"/> that contains this group.
        /// </summary>
        protected virtual BlackboardSection Section => GetFirstAncestorOfType<BlackboardSection>();

        /// <inheritdoc />
        public override VisualElement contentContainer => m_ContentContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardVariableGroup"/> class.
        /// </summary>
        public BlackboardVariableGroup()
        {
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);

            m_SelectionBorder = new SelectionBorder { name = selectionBorderElementName };
            m_SelectionBorder.AddToClassList(selectionBorderUssClassName);

            m_ContentContainer = new VisualElement();

            hierarchy.Add(m_ContentContainer);
            hierarchy.Add(m_SelectionBorder);
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            AddToClassList(ussClassName);

            base.BuildElementUI();

            m_Title = new VisualElement();
            m_Title.AddToClassList(titleContainerUssClassName);

            m_TitleToggle = new Toggle();
            m_TitleToggle.RegisterCallback<ChangeEvent<bool>>(OnTitleToggle);
            m_TitleToggle.AddToClassList(titleToggleUssClassName);

            m_Title.Add(m_TitleToggle);

            Add(m_Title);

            m_DragIndicator = new VisualElement { name = "drag-indicator" };
            m_DragIndicator.AddToClassList(dragIndicatorUssClassName);
            Add(m_DragIndicator);
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.AppendPart(EditableTitlePart.Create(TitlePartName, Model, this, ussClassName));
            PartList.AppendPart(BlackboardVariableGroupItemsPart.Create(ItemsPartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            m_TitleLabel = PartList.GetPart(TitlePartName).Root;

            m_Title.Insert(1, m_TitleLabel);

            m_ItemsContainer = PartList.GetPart(ItemsPartName).Root;
        }

        void OnTitleToggle(ChangeEvent<bool> e)
        {
            using (var stateUpdater = GraphView.BlackboardViewState.UpdateScope)
            {
                stateUpdater.SetVariableGroupModelExpanded(GroupModel, e.newValue);
            }
        }

        int GetDepth()
        {
            int cpt = 0;
            var current = GroupModel.Group;
            while (current != null)
            {
                cpt++;
                current = current.Group;
            }

            return cpt;
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            if (Model.IsDroppable() && m_SelectionDropper == null)
                SelectionDropper = new SelectionDropper();

            base.UpdateElementFromModel();

            if (m_ItemsContainer != null)
            {
                int depth = GetDepth();

                EnableInClassList(evenUssClassName,depth % 2 == 0);
            }

            var expanded = GraphView.BlackboardViewState.GetVariableGroupExpanded(GroupModel);

            m_TitleToggle.SetValueWithoutNotify(expanded);

            EnableInClassList(collapsedUssClassName, !expanded);
        }

        /// <summary>
        /// Computes the index of the element at the given position.
        /// </summary>
        /// <param name="localPos"> The position in this element coordinates.</param>
        /// <returns>The index of the element at the given position.</returns>
        protected int InsertionIndex(Vector2 localPos)
        {
            int index = 0;

            foreach (VisualElement child in m_ItemsContainer.Children())
            {
                Rect rect = child.parent.ChangeCoordinatesTo(this,child.layout);

                if (localPos.y > (rect.y + rect.height / 2))
                {
                    ++index;
                }
                else
                {
                    break;
                }
            }

            return index;
        }

        /// <summary>
        /// Handles <see cref="DragUpdatedEvent"/>.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected virtual void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            BlackboardSection section = GetFirstOfType<BlackboardSection>();
            if (section == null)
                return;

            var draggedObjects = DragAndDrop.GetGenericData("DragSelection") as List<IGraphElementModel>;

            if (!CanAcceptDrop(draggedObjects))
            {
                HideDragIndicator();
                return;
            }

            int insertIndex = InsertionIndex(evt.localMousePosition);

            float indicatorY;
            if (m_ItemsContainer.childCount == 0)
                indicatorY = m_ItemsContainer.ChangeCoordinatesTo(this, Vector2.zero).y;
            else if (insertIndex >= m_ItemsContainer.childCount)
            {
                VisualElement lastChild = m_ItemsContainer[m_ItemsContainer.childCount - 1];

                indicatorY = lastChild.ChangeCoordinatesTo(this,
                    new Vector2(0, lastChild.layout.height + lastChild.resolvedStyle.marginBottom)).y;
            }
            else if (insertIndex == -1)
            {
                VisualElement childAtInsertIndex = m_ItemsContainer[0];

                indicatorY = childAtInsertIndex.ChangeCoordinatesTo(this,
                    new Vector2(0, -childAtInsertIndex.resolvedStyle.marginTop)).y;
            }
            else
            {
                VisualElement childAtInsertIndex = m_ItemsContainer[insertIndex];

                indicatorY = childAtInsertIndex.ChangeCoordinatesTo(this,
                    new Vector2(0, -childAtInsertIndex.resolvedStyle.marginTop)).y;
            }

            ShowDragIndicator(indicatorY);
            DragAndDrop.visualMode = DragAndDropVisualMode.Move;

            evt.StopPropagation();
        }

        /// <summary>
        /// handles <see cref="DragPerformEvent"/>.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected virtual void OnDragPerformEvent(DragPerformEvent evt)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<IGraphElementModel>;

            if (selection != null && CanAcceptDrop(selection))
            {
                int insertIndex = InsertionIndex(evt.localMousePosition);
                OnItemDropped(insertIndex, selection);
            }

            HideDragIndicator();
            evt.StopPropagation();
        }

        /// <summary>
        /// Returns whether this element can accept elements as its items.
        /// </summary>
        /// <param name="draggedObjects">The dragged elements.</param>
        /// <returns>Whether this element can accept elements as its items.</returns>
        public virtual bool CanAcceptDrop(List<IGraphElementModel> draggedObjects)
        {
            if (draggedObjects == null)
                return false;

            if (draggedObjects.OfType<IGroupItemModel>().All(t => t.GetSection() != GroupModel.GetSection()))
                return false;

            foreach (var obj in draggedObjects)
            {
                if (obj is IGroupModel vgm && GroupModel.IsIn(vgm))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Build the contextual menu.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            int index = InsertionIndex(this.WorldToLocal((evt.mousePosition)));

            int fromSelectionIndex = index;
            while (fromSelectionIndex >= 0 && fromSelectionIndex < GroupModel.Items.Count - 1)
            {
                GraphView.GetSelection().Contains(GroupModel.Items[fromSelectionIndex]);
                ++fromSelectionIndex;
            }

            if (fromSelectionIndex >= GroupModel.Items.Count)
                fromSelectionIndex = -1;
            if (index >= GroupModel.Items.Count)
                index = -1;

            if (GraphView.GetSelection().OfType<IGroupItemModel>().Any(t => !GroupModel.IsIn(t)))
            {
                evt.menu.AppendAction("Create Group From Selection", e =>
                {
                    View.Dispatch(new BlackboardGroupCreateCommand(GroupModel, fromSelectionIndex >= 0 ? GroupModel.Items[fromSelectionIndex] : null, null, GraphView.GetSelection().OfType<IGroupItemModel>().Where(t => !GroupModel.IsIn(t)).ToList()));
                });
            }
            evt.menu.AppendAction("Create Group", e =>
            {
                View.Dispatch(new BlackboardGroupCreateCommand(GroupModel, index >= 0 ? GroupModel.Items[index] : null));
            });
        }

        void HideDragIndicator()
        {
            GetFirstAncestorOfType<BlackboardVariableGroup>()?.HideDragIndicator();
            m_DragIndicator.style.visibility = Visibility.Hidden;
        }

        void ShowDragIndicator(float yPosition)
        {
            GetFirstAncestorOfType<BlackboardVariableGroup>()?.HideDragIndicator();


            m_DragIndicator.style.visibility = Visibility.Visible;
            m_DragIndicator.style.left = 0;
            m_DragIndicator.style.top = yPosition - m_DragIndicator.resolvedStyle.height / 2;
            m_DragIndicator.style.width = layout.width;
        }

        /// <summary>
        /// Handles a drop of elements at the given index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="elements">The dropped <see cref="GraphElementModel"/>s.</param>
        protected void OnItemDropped(int index, IEnumerable<IGraphElementModel> elements)
        {
            var droppedModels = elements.OfType<IGroupItemModel>().Where(t => t.GetSection() == GroupModel.GetSection()).ToList();
            if (!droppedModels.Any())
                return;

            IGroupItemModel insertAfterModel = null;

            if (m_ItemsContainer.childCount != 0)
            {
                if (index >= m_ItemsContainer.childCount)
                    insertAfterModel = (m_ItemsContainer[m_ItemsContainer.childCount - 1] as ModelUI)?.Model as IGroupItemModel;
                else if (index > 0)
                    insertAfterModel = (m_ItemsContainer[index -1] as ModelUI)?.Model as IGroupItemModel;
            }

            View.Dispatch(new ReorderGraphVariableDeclarationCommand((IGroupModel)Model, insertAfterModel, droppedModels));
        }

        /// <summary>
        /// Handles the <see cref="DragLeaveEvent"/>.
        /// </summary>
        /// <param name="evt"></param>
        protected virtual void OnDragLeaveEvent(DragLeaveEvent evt)
        {
            HideDragIndicator();
        }
    }
}
