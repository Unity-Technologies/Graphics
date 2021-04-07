using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing.Views;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

using ContextualMenuManipulator = UnityEngine.UIElements.ContextualMenuManipulator;

namespace UnityEditor.ShaderGraph.Drawing
{
    sealed class SGBlackboardCategory : GraphElement, ISGControlledElement<BlackboardCategoryController>, ISelection
    {
        // --- Begin ISGControlledElement implementation
        public void OnControllerChanged(ref SGControllerChangedEvent e)
        {
        }

        public void OnControllerEvent(SGControllerEvent e)
        {
        }

        public BlackboardCategoryController controller
        {
            get => m_Controller;
            set
            {
                if (m_Controller != value)
                {
                    if (m_Controller != null)
                    {
                        m_Controller.UnregisterHandler(this);
                    }
                    m_Controller = value;

                    if (m_Controller != null)
                    {
                        m_Controller.RegisterHandler(this);
                    }
                }
            }
        }

        SGController ISGControlledElement.controller => m_Controller;

        // --- ISGControlledElement implementation

        BlackboardCategoryController m_Controller;

        BlackboardCategoryViewModel m_ViewModel;
        public BlackboardCategoryViewModel viewModel => m_ViewModel;

        VisualElement m_DragIndicator;
        VisualElement m_MainContainer;
        VisualElement m_Header;
        Label m_TitleLabel;
        VisualElement m_RowsContainer;
        int m_InsertIndex;
        SGBlackboard Blackboard => m_ViewModel.parentView as SGBlackboard;

        public delegate bool CanAcceptDropDelegate(ISelectable selected);

        public CanAcceptDropDelegate canAcceptDrop { get; set; }

        int InsertionIndex(Vector2 pos)
        {
            int index = -1;

            if (this.ContainsPoint(pos))
            {
                index = 0;

                foreach (VisualElement child in Children())
                {
                    Rect rect = child.layout;

                    if (pos.y > (rect.y + rect.height / 2))
                    {
                        ++index;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return index;
        }

        VisualElement FindCategoryDirectChild(VisualElement element)
        {
            VisualElement directChild = element;

            while ((directChild != null) && (directChild != this))
            {
                if (directChild.parent == this)
                {
                    return directChild;
                }
                directChild = directChild.parent;
            }

            return null;
        }

        internal SGBlackboardCategory(BlackboardCategoryViewModel categoryViewModel)
        {
            m_ViewModel = categoryViewModel;

            // Setup VisualElement from Stylesheet and UXML file
            var tpl = Resources.Load("UXML/GraphView/BlackboardSection") as VisualTreeAsset;
            m_MainContainer = tpl.Instantiate();
            m_MainContainer.AddToClassList("mainContainer");

            m_Header = m_MainContainer.Q("sectionHeader");
            m_TitleLabel = m_MainContainer.Q<Label>("sectionTitleLabel");
            m_RowsContainer = m_MainContainer.Q("rowsContainer");

            hierarchy.Add(m_MainContainer);

            m_DragIndicator = m_MainContainer.Q("dragIndicator");
            m_DragIndicator.visible = false;

            hierarchy.Add(m_DragIndicator);

            capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Droppable | Capabilities.Deletable | Capabilities.Renamable;

            ClearClassList();
            AddToClassList("blackboardSection");

            // add the right click context menu
            IManipulator contextMenuManipulator = new ContextualMenuManipulator(AddContextMenuOptions);
            this.AddManipulator(contextMenuManipulator);
            // add drag and drop manipulator
            //this.AddManipulator(new SelectionDropper());

            // Register hover callbacks
            RegisterCallback<MouseEnterEvent>(OnHoverStartEvent);
            RegisterCallback<MouseLeaveEvent>(OnHoverEndEvent);
            // Register drag callbacks
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);

            var styleSheet = Resources.Load<StyleSheet>($"Styles/Blackboard");
            styleSheets.Add(styleSheet);

            m_InsertIndex = -1;

            // Update category title from view model
            title = m_ViewModel.name;
            this.viewDataKey = viewModel.associatedCategoryGuid;
        }

        public override VisualElement contentContainer { get { return m_RowsContainer; } }

        public override string title
        {
            get => m_TitleLabel.text;
            set
            {
                m_TitleLabel.text = value;
                if (m_TitleLabel.text == String.Empty)
                {
                    AddToClassList("unnamed");
                }
                else
                {
                    RemoveFromClassList("unnamed");
                }
            }
        }

        public bool headerVisible
        {
            get { return m_Header.parent != null; }
            set
            {
                if (value == (m_Header.parent != null))
                    return;

                if (value)
                {
                    m_MainContainer.Add(m_Header);
                }
                else
                {
                    m_MainContainer.Remove(m_Header);
                }
            }
        }

        private void SetDragIndicatorVisible(bool visible)
        {
            m_DragIndicator.visible = visible;
        }

        public bool CategoryContains(List<ISelectable> selection)
        {
            if (selection == null)
                return false;

            // Look for at least one selected element in this category to accept drop
            foreach (ISelectable selected in selection)
            {
                VisualElement selectedElement = selected as VisualElement;

                if (selected != null && Contains(selectedElement))
                {
                    if (canAcceptDrop == null || canAcceptDrop(selected))
                        return true;
                }
            }

            return false;
        }

        void OnHoverStartEvent(MouseEnterEvent evt)
        {
            AddToClassList("hovered");
        }

        void OnHoverEndEvent(MouseLeaveEvent evt)
        {
            RemoveFromClassList("hovered");
        }

        private void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;

            VisualElement sourceItem = null;

            foreach (ISelectable selectedElement in selection)
            {
                sourceItem = selectedElement as VisualElement;

                if (sourceItem == null)
                    continue;
            }

            Vector2 localPosition = evt.localMousePosition;

            m_InsertIndex = InsertionIndex(localPosition);

            if (m_InsertIndex != -1)
            {
                float indicatorY = 0;

                if (m_InsertIndex == childCount)
                {
                    //when category is emapty
                    if (this.childCount == 0)
                    {
                        indicatorY = 0;
                    }
                    else
                    {
                        VisualElement lastChild = this[childCount - 1];

                        indicatorY = lastChild.ChangeCoordinatesTo(this, new Vector2(0, lastChild.layout.height + lastChild.resolvedStyle.marginBottom)).y;
                    }
                }
                else
                {
                    VisualElement childAtInsertIndex = this[m_InsertIndex];

                    indicatorY = childAtInsertIndex.ChangeCoordinatesTo(this, new Vector2(0, -childAtInsertIndex.resolvedStyle.marginTop)).y;
                }

                SetDragIndicatorVisible(true);

                m_DragIndicator.style.width = layout.width;
                var newPosition = indicatorY - m_DragIndicator.layout.height / 2;
                m_DragIndicator.style.top = newPosition;
            }
            else
            {
                SetDragIndicatorVisible(false);

                m_InsertIndex = -1;
            }

            if (m_InsertIndex != -1)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            }
            evt.StopPropagation();
        }

        private void OnDragPerformEvent(DragPerformEvent evt)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
            if (m_InsertIndex == -1)
            {
                SetDragIndicatorVisible(false);
                return;
            }

            if (!CategoryContains(selection))
            {
                List<VisualElement> draggedElements = new List<VisualElement>();
                foreach (ISelectable selectedElement in selection)
                {
                    var draggedElement = selectedElement as VisualElement;

                    if (draggedElement != null)
                    {
                        draggedElements.Add(draggedElement);
                    }
                }
                if (draggedElements.Count == 0)
                {
                    SetDragIndicatorVisible(false);
                    return;
                }

                foreach (var draggedElement in draggedElements)
                {
                    var addItemToCategoryAction = new AddItemToCategoryAction();
                    if (draggedElement.userData is ShaderInput newShaderInput)
                    {
                        addItemToCategoryAction.categoryGuid = viewModel.associatedCategoryGuid;
                        addItemToCategoryAction.itemToAdd = newShaderInput;
                        addItemToCategoryAction.indexToAddItemAt = m_InsertIndex;
                        m_ViewModel.requestModelChangeAction(addItemToCategoryAction);
                    }
                }
            }
            else
            {
                List<Tuple<VisualElement, VisualElement>> draggedElements = new List<Tuple<VisualElement, VisualElement>>();

                foreach (ISelectable selectedElement in selection)
                {
                    var draggedElement = selectedElement as VisualElement;

                    if (draggedElement != null && Contains(draggedElement))
                    {
                        draggedElements.Add(new Tuple<VisualElement, VisualElement>(FindCategoryDirectChild(draggedElement), draggedElement));
                    }
                }

                if (draggedElements.Count == 0)
                {
                    SetDragIndicatorVisible(false);
                    return;
                }

                // Sorts the dragged elements from their relative order in their parent
                draggedElements.Sort((pair1, pair2) => { return IndexOf(pair1.Item1).CompareTo(IndexOf(pair2.Item1)); });

                int insertIndex = m_InsertIndex;

                foreach (Tuple<VisualElement, VisualElement> draggedElement in draggedElements)
                {
                    VisualElement categoryDirectChild = draggedElement.Item1;
                    int indexOfDraggedElement = IndexOf(categoryDirectChild);

                    if (!((indexOfDraggedElement == insertIndex) || ((insertIndex - 1) == indexOfDraggedElement)))
                    {
                        var moveShaderInputAction = new MoveShaderInputAction();
                        if (draggedElement.Item2.userData is ShaderInput shaderInput)
                        {
                            moveShaderInputAction.shaderInputReference = shaderInput;
                            moveShaderInputAction.newIndexValue = m_InsertIndex;
                            m_ViewModel.requestModelChangeAction(moveShaderInputAction);

                            if (insertIndex == contentContainer.childCount)
                            {
                                categoryDirectChild.BringToFront();
                            }
                            else
                            {
                                categoryDirectChild.PlaceBehind(this[insertIndex]);
                            }
                        }
                    }

                    if (insertIndex > indexOfDraggedElement)     // No need to increment the insert index for the next dragged element if the current dragged element is above the current insert location.
                        continue;
                    insertIndex++;
                }
            }

            SetDragIndicatorVisible(false);
            evt.StopPropagation();
        }

        void OnDragLeaveEvent(DragLeaveEvent evt)
        {
            SetDragIndicatorVisible(false);
        }

        internal void OnDragActionCanceled()
        {
            SetDragIndicatorVisible(false);
        }

        public override void Select(VisualElement selectionContainer, bool additive)
        {
            // Don't add un-named categories to graph selections
            if (controller.Model.IsNamedCategory())
                base.Select(selectionContainer, additive);
        }

        public override void OnSelected()
        {
            AddToClassList("selected");
        }

        public override void OnUnselected()
        {
            RemoveFromClassList("selected");
        }

        public void AddToSelection(ISelectable selectable)
        {
            // Don't add un-named categories to graph selections
            if (controller.Model.IsNamedCategory() == false && selectable == this)
                return;

            var materialGraphView = m_ViewModel.parentView.GetFirstAncestorOfType<MaterialGraphView>();
            materialGraphView?.AddToSelection(selectable);
        }

        public void RemoveFromSelection(ISelectable selectable)
        {
            if (selectable == this)
                RemoveFromClassList("selected");
            var materialGraphView = m_ViewModel.parentView.GetFirstAncestorOfType<MaterialGraphView>();
            materialGraphView?.RemoveFromSelection(selectable);
        }

        public void ClearSelection()
        {
            RemoveFromClassList("selected");
            var materialGraphView = m_ViewModel.parentView.GetFirstAncestorOfType<MaterialGraphView>();
            materialGraphView?.ClearSelection();
        }

        public List<ISelectable> selection
        {
            get
            {
                var selectionProvider = m_ViewModel.parentView.GetFirstAncestorOfType<ISelectionProvider>();
                if (selectionProvider?.GetSelection != null)
                    return selectionProvider.GetSelection;
                else
                    return new List<ISelectable>();
            }
        }

        void RequestCategoryDelete()
        {
            var materialGraphView = Blackboard.ParentView as MaterialGraphView;
            materialGraphView?.deleteSelection?.Invoke("Delete", GraphView.AskUser.DontAskUser);
        }

        void AddContextMenuOptions(ContextualMenuPopulateEvent evt)
        {
            // Don't allow un-named sections to have right-click menu options
            if (controller.Model.IsNamedCategory())
            {
                evt.menu.AppendAction("Delete", evt => RequestCategoryDelete());
            }
        }
    }
}
