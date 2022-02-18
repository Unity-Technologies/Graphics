using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing.Views;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

using ContextualMenuManipulator = UnityEngine.UIElements.ContextualMenuManipulator;

namespace UnityEditor.ShaderGraph.Drawing
{
    sealed class SGBlackboardCategory : GraphElement, ISGControlledElement<BlackboardCategoryController>, ISelection, IComparable<SGBlackboardCategory>
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

        const string k_StylePath = "Styles/SGBlackboard";
        const string k_UxmlPath = "UXML/Blackboard/SGBlackboardCategory";

        VisualElement m_DragIndicator;
        VisualElement m_MainContainer;
        VisualElement m_Header;
        Label m_TitleLabel;
        Foldout m_Foldout;
        TextField m_TextField;
        internal TextField textField => m_TextField;
        VisualElement m_RowsContainer;
        int m_InsertIndex;
        SGBlackboard blackboard => m_ViewModel.parentView as SGBlackboard;

        bool m_IsDragInProgress;
        bool m_WasHoverExpanded;

        bool m_DroppedOnBottomEdge;
        bool m_DroppedOnTopEdge;

        bool m_RenameInProgress;

        public delegate bool CanAcceptDropDelegate(ISelectable selected);

        public CanAcceptDropDelegate canAcceptDrop { get; set; }

        int InsertionIndex(Vector2 pos)
        {
            // For an empty category can always just insert at the start
            if (this.childCount == 0)
                return 0;

            var blackboardRows = this.Query<SGBlackboardRow>().ToList();
            for (int index = 0; index < blackboardRows.Count; index++)
            {
                var blackboardRow = blackboardRows[index];
                var localPosition = this.ChangeCoordinatesTo(blackboardRow, pos);
                if (blackboardRow.ContainsPoint(localPosition))
                {
                    return index;
                }
            }
            return -1;
        }

        static VisualElement FindCategoryDirectChild(SGBlackboardCategory blackboardCategory, VisualElement element)
        {
            VisualElement directChild = element;

            while ((directChild != null) && (directChild != blackboardCategory))
            {
                if (directChild.parent == blackboardCategory)
                {
                    return directChild;
                }
                directChild = directChild.parent;
            }

            return null;
        }

        internal SGBlackboardCategory(BlackboardCategoryViewModel categoryViewModel, BlackboardCategoryController inController)
        {
            m_ViewModel = categoryViewModel;
            controller = inController;
            userData = controller.Model;

            // Setup VisualElement from Stylesheet and UXML file
            var tpl = Resources.Load(k_UxmlPath) as VisualTreeAsset;
            m_MainContainer = tpl.Instantiate();
            m_MainContainer.AddToClassList("mainContainer");

            m_Header = m_MainContainer.Q("categoryHeader");
            m_TitleLabel = m_MainContainer.Q<Label>("categoryTitleLabel");
            m_Foldout = m_MainContainer.Q<Foldout>("categoryTitleFoldout");
            m_RowsContainer = m_MainContainer.Q("rowsContainer");
            m_TextField = m_MainContainer.Q<TextField>("textField");
            m_TextField.style.display = DisplayStyle.None;

            hierarchy.Add(m_MainContainer);

            m_DragIndicator = m_MainContainer.Q("dragIndicator");
            m_DragIndicator.visible = false;

            hierarchy.Add(m_DragIndicator);

            capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Droppable | Capabilities.Deletable | Capabilities.Renamable | Capabilities.Copiable;

            ClearClassList();
            AddToClassList("blackboardCategory");

            // add the right click context menu
            IManipulator contextMenuManipulator = new ContextualMenuManipulator(AddContextMenuOptions);
            this.AddManipulator(contextMenuManipulator);

            // add drag and drop manipulator
            var selectionDropperManipulator = new SelectionDropper();
            this.AddManipulator(selectionDropperManipulator);

            RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            var textInputElement = m_TextField.Q(TextField.textInputUssName);
            textInputElement.RegisterCallback<FocusOutEvent>(e => { OnEditTextFinished(); }, TrickleDown.TrickleDown);
            // Register hover callbacks
            RegisterCallback<MouseEnterEvent>(OnHoverStartEvent);
            RegisterCallback<MouseLeaveEvent>(OnHoverEndEvent);
            // Register drag callbacks
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);

            var styleSheet = Resources.Load<StyleSheet>(k_StylePath);
            styleSheets.Add(styleSheet);

            m_InsertIndex = -1;

            // Update category title from view model
            title = m_ViewModel.name;
            this.viewDataKey = viewModel.associatedCategoryGuid;

            if (String.IsNullOrEmpty(title))
            {
                m_Foldout.visible = false;
                m_Foldout.RemoveFromHierarchy();
            }
            else
            {
                TryDoFoldout(m_ViewModel.isExpanded);
                m_Foldout.RegisterCallback<ChangeEvent<bool>>(OnFoldoutToggle);
            }

            // Remove the header element if this is the default category
            if (!controller.Model.IsNamedCategory())
                headerVisible = false;
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

        void SetDragIndicatorVisible(bool visible)
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

        void OnFoldoutToggle(ChangeEvent<bool> evt)
        {
            if (evt.previousValue != evt.newValue)
            {
                var isExpandedAction = new ChangeCategoryIsExpandedAction();
                if (selection.Contains(this)) // expand all selected if the foldout is part of a selection
                    isExpandedAction.categoryGuids = selection.OfType<SGBlackboardCategory>().Select(s => s.viewModel.associatedCategoryGuid).ToList();
                else
                    isExpandedAction.categoryGuids = new List<string>() { viewModel.associatedCategoryGuid };

                isExpandedAction.isExpanded = evt.newValue;
                isExpandedAction.editorPrefsBaseKey = blackboard.controller.editorPrefsBaseKey;
                viewModel.requestModelChangeAction(isExpandedAction);
            }
        }

        internal void TryDoFoldout(bool expand)
        {
            m_Foldout.SetValueWithoutNotify(expand);
            if (!expand)
            {
                m_DragIndicator.visible = true;
                m_RowsContainer.RemoveFromHierarchy();
            }
            else
            {
                m_DragIndicator.visible = false;
                m_MainContainer.Add(m_RowsContainer);
            }

            var key = $"{blackboard.controller.editorPrefsBaseKey}.{viewDataKey}.{ChangeCategoryIsExpandedAction.kEditorPrefKey}";
            EditorPrefs.SetBool(key, expand);
        }

        void OnMouseDownEvent(MouseDownEvent e)
        {
            // Handles double-click with left mouse, which should trigger a rename action on this category
            if ((e.clickCount == 2) && e.button == (int)MouseButton.LeftMouse && IsRenamable())
            {
                OpenTextEditor();
                e.PreventDefault();
            }
            else if (e.clickCount == 1 && e.button == (int)MouseButton.LeftMouse && IsRenamable())
            {
                // Select the child elements within this category (the field views)
                var fieldViews = this.Query<SGBlackboardField>();
                foreach (var child in fieldViews.ToList())
                {
                    this.AddToSelection(child);
                }
            }
        }

        internal void OpenTextEditor()
        {
            m_TextField.SetValueWithoutNotify(title);
            m_TextField.style.display = DisplayStyle.Flex;
            m_TitleLabel.visible = false;
            m_TextField.Q(TextField.textInputUssName).Focus();
            m_TextField.SelectAll();

            m_RenameInProgress = true;
        }

        void OnEditTextFinished()
        {
            m_TitleLabel.visible = true;
            m_TextField.style.display = DisplayStyle.None;

            if (title != m_TextField.text && String.IsNullOrWhiteSpace(m_TextField.text) == false)
            {
                var changeCategoryNameAction = new ChangeCategoryNameAction();
                changeCategoryNameAction.newCategoryNameValue = m_TextField.text;
                changeCategoryNameAction.categoryGuid = m_ViewModel.associatedCategoryGuid;
                m_ViewModel.requestModelChangeAction(changeCategoryNameAction);
            }
            else
            {
                // Reset text field to original name
                m_TextField.value = title;
            }

            m_RenameInProgress = false;
        }

        void OnHoverStartEvent(MouseEnterEvent evt)
        {
            AddToClassList("hovered");
            if (selection.OfType<SGBlackboardField>().Any()
                && controller.Model.IsNamedCategory()
                && m_IsDragInProgress
                && !viewModel.isExpanded)
            {
                m_WasHoverExpanded = true;
                TryDoFoldout(true);
            }
        }

        void OnHoverEndEvent(MouseLeaveEvent evt)
        {
            if (m_WasHoverExpanded && m_IsDragInProgress)
            {
                m_WasHoverExpanded = false;
                TryDoFoldout(false);
            }
            RemoveFromClassList("hovered");
        }

        void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;

            // Don't show drag indicator if selection has categories,
            // We don't want category drag & drop to be ambiguous with shader input drag & drop
            if (selection.OfType<SGBlackboardCategory>().Any())
            {
                SetDragIndicatorVisible(false);
                return;
            }

            // If can't find at least one blackboard field in the selection, don't update drag indicator
            if (selection.OfType<SGBlackboardField>().Any() == false)
            {
                SetDragIndicatorVisible(false);
                return;
            }

            m_IsDragInProgress = true;

            Vector2 localPosition = evt.localMousePosition;

            m_InsertIndex = InsertionIndex(localPosition);
            if (m_InsertIndex != -1)
            {
                float indicatorY = 0;
                bool inMoveRange = false;
                // When category is empty
                if (this.childCount == 0)
                {
                    // This moves the indicator to the bottom of the category in case of an empty category
                    indicatorY = this.layout.height * 0.9f;
                    m_DragIndicator.style.marginBottom = 8;
                    inMoveRange = true;
                }
                else
                {
                    m_DragIndicator.style.marginBottom = 0;

                    var relativePosition = new Vector2();
                    var childHeight = 0.0f;
                    VisualElement childAtInsertIndex = this[m_InsertIndex];
                    childHeight = childAtInsertIndex.layout.height;

                    relativePosition = this.ChangeCoordinatesTo(childAtInsertIndex, localPosition);

                    if (relativePosition.y > 0 && relativePosition.y < childHeight * 0.25f)
                    {
                        // Top Edge
                        inMoveRange = true;
                        indicatorY = childAtInsertIndex.ChangeCoordinatesTo(this, new Vector2(0, 0)).y;
                        m_DragIndicator.style.rotate = new StyleRotate(Rotate.None());
                        m_DroppedOnBottomEdge = false;
                        m_DroppedOnTopEdge = true;
                    }
                    else if (relativePosition.y > 0.75f * childHeight && relativePosition.y < childHeight)
                    {
                        // Bottom Edge
                        inMoveRange = true;
                        indicatorY = childAtInsertIndex.ChangeCoordinatesTo(this, new Vector2(0, 0)).y + childAtInsertIndex.layout.height;
                        //m_DragIndicator.style.rotate = new StyleRotate(new Rotate(-180));
                        m_DroppedOnBottomEdge = true;
                        m_DroppedOnTopEdge = false;
                    }
                }

                if (inMoveRange)
                {
                    SetDragIndicatorVisible(true);

                    m_DragIndicator.style.width = layout.width;
                    var newPosition = indicatorY;
                    m_DragIndicator.style.top = newPosition;
                }
                else
                    SetDragIndicatorVisible(true);
            }
            else
                SetDragIndicatorVisible(false);

            if (m_InsertIndex != -1)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            }
        }

        void OnDragPerformEvent(DragPerformEvent evt)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;

            m_IsDragInProgress = false;

            // Don't show drag indicator if selection has categories,
            // We don't want category drag & drop to be ambiguous with shader input drag & drop
            if (selection.OfType<SGBlackboardCategory>().Any())
            {
                SetDragIndicatorVisible(false);
                return;
            }

            if (m_InsertIndex == -1)
            {
                SetDragIndicatorVisible(false);
                return;
            }

            // Map of containing categories to the actual dragged elements within them
            SortedDictionary<SGBlackboardCategory, List<VisualElement>> draggedElements = new SortedDictionary<SGBlackboardCategory, List<VisualElement>>();

            foreach (ISelectable selectedElement in selection)
            {
                var draggedElement = selectedElement as VisualElement;
                if (draggedElement == null)
                    continue;

                if (this.Contains(draggedElement))
                {
                    if (draggedElements.ContainsKey(this))
                        draggedElements[this].Add(FindCategoryDirectChild(this, draggedElement));
                    else
                        draggedElements.Add(this, new List<VisualElement> { FindCategoryDirectChild(this, draggedElement) });
                }
                else
                {
                    var otherCategory = draggedElement.GetFirstAncestorOfType<SGBlackboardCategory>();
                    if (otherCategory != null)
                    {
                        if (draggedElements.ContainsKey(otherCategory))
                            draggedElements[otherCategory].Add(FindCategoryDirectChild(otherCategory, draggedElement));
                        else
                            draggedElements.Add(otherCategory, new List<VisualElement> { FindCategoryDirectChild(otherCategory, draggedElement) });
                    }
                }
            }

            if (draggedElements.Count == 0)
            {
                SetDragIndicatorVisible(false);
                return;
            }

            foreach (var categoryToChildrenTuple in draggedElements)
            {
                var containingCategory = categoryToChildrenTuple.Key;
                var childList = categoryToChildrenTuple.Value;
                // Sorts the dragged elements from their relative order in their parent
                childList.Sort((item1, item2) => containingCategory.IndexOf(item1).CompareTo(containingCategory.IndexOf(item2)));
            }

            int insertIndex = Mathf.Clamp(m_InsertIndex, 0, m_InsertIndex);

            bool adjustedInsertIndex = false;
            VisualElement lastInsertedElement = null;
            /* Handles moving elements within a category */
            foreach (var categoryToChildrenTuple in draggedElements)
            {
                var childList = categoryToChildrenTuple.Value;
                VisualElement firstChild = childList.First();
                foreach (var draggedElement in childList)
                {
                    var blackboardField = draggedElement.Q<SGBlackboardField>();
                    ShaderInput shaderInput = null;
                    if (blackboardField != null)
                        shaderInput = blackboardField.controller.Model;

                    // Skip if this field is not contained by this category as we handle that in the next loop below
                    if (shaderInput == null || !this.Contains(blackboardField))
                        continue;

                    VisualElement categoryDirectChild = draggedElement;
                    int indexOfDraggedElement = IndexOf(categoryDirectChild);

                    bool listEndInsertion = false;
                    // Only find index for the first item
                    if (draggedElement == firstChild)
                    {
                        adjustedInsertIndex = true;
                        // Handles case of inserting after last item in list
                        if (insertIndex == childCount - 1 && m_DroppedOnBottomEdge)
                        {
                            listEndInsertion = true;
                        }
                        // Handles case of inserting after any item except the last in list
                        else if (m_DroppedOnBottomEdge)
                            insertIndex++;

                        insertIndex = Mathf.Clamp(insertIndex, 0, childCount - 1);

                        if (insertIndex != indexOfDraggedElement)
                        {
                            // If ever placing it at end of list, make sure to place after last item
                            if (listEndInsertion)
                            {
                                categoryDirectChild.PlaceInFront(this[insertIndex]);
                            }
                            else
                            {
                                categoryDirectChild.PlaceBehind(this[insertIndex]);
                            }
                        }

                        lastInsertedElement = firstChild;
                    }
                    //  Place every subsequent row after that use PlaceInFront(), this prevents weird re-ordering issues as long as we can get the first index right
                    else
                    {
                        var indexOfFirstChild = this.IndexOf(lastInsertedElement);
                        categoryDirectChild.PlaceInFront(this[indexOfFirstChild]);
                        lastInsertedElement = categoryDirectChild;
                    }

                    if (insertIndex > childCount - 1 || listEndInsertion)
                        insertIndex = -1;

                    var moveShaderInputAction = new MoveShaderInputAction();
                    moveShaderInputAction.associatedCategoryGuid = viewModel.associatedCategoryGuid;
                    moveShaderInputAction.shaderInputReference = shaderInput;
                    moveShaderInputAction.newIndexValue = insertIndex;
                    m_ViewModel.requestModelChangeAction(moveShaderInputAction);

                    // Make sure to remove the element from the selection so it doesn't get re-handled by the blackboard as well, leads to duplicates
                    selection.Remove(blackboardField);

                    if (insertIndex > indexOfDraggedElement)
                        continue;

                    // If adding to the end of the list, we no longer need to increment the index
                    if (insertIndex != -1)
                        insertIndex++;
                }
            }

            /* Handles moving elements from one category to another (including between different graph windows) */
            // Handles case of inserting after item in list
            if (!adjustedInsertIndex)
            {
                if (m_DroppedOnBottomEdge)
                {
                    insertIndex++;
                }
                // Only ever do this for the first item
                else if (m_DroppedOnTopEdge && insertIndex == 0)
                {
                    insertIndex = Mathf.Clamp(insertIndex - 1, 0, childCount - 1);
                }
            }
            else if (lastInsertedElement != null)
            {
                insertIndex = this.IndexOf(lastInsertedElement) + 1;
            }

            foreach (var categoryToChildrenTuple in draggedElements)
            {
                var childList = categoryToChildrenTuple.Value;
                foreach (var draggedElement in childList)
                {
                    var blackboardField = draggedElement.Q<SGBlackboardField>();
                    ShaderInput shaderInput = null;
                    if (blackboardField != null)
                        shaderInput = blackboardField.controller.Model;
                    if (shaderInput == null)
                        continue;

                    // If the blackboard field is contained by this category its already been handled above, skip
                    if (this.Contains(blackboardField))
                        continue;

                    var addItemToCategoryAction = new AddItemToCategoryAction();
                    addItemToCategoryAction.categoryGuid = viewModel.associatedCategoryGuid;
                    addItemToCategoryAction.addActionSource = AddItemToCategoryAction.AddActionSource.DragDrop;
                    addItemToCategoryAction.itemToAdd = shaderInput;

                    // If adding to end of list, make the insert index -1 to ensure op goes through as expected
                    if (insertIndex > childCount - 1)
                        insertIndex = -1;

                    addItemToCategoryAction.indexToAddItemAt = insertIndex;
                    m_ViewModel.requestModelChangeAction(addItemToCategoryAction);

                    // Make sure to remove the element from the selection so it doesn't get re-handled by the blackboard as well, leads to duplicates
                    selection.Remove(blackboardField);

                    // If adding to the end of the list, we no longer need to increment the index
                    if (insertIndex != -1)
                        insertIndex++;
                }
            }

            SetDragIndicatorVisible(false);
        }

        void OnDragLeaveEvent(DragLeaveEvent evt)
        {
            SetDragIndicatorVisible(false);
        }

        internal void OnDragActionCanceled()
        {
            SetDragIndicatorVisible(false);
            m_IsDragInProgress = false;
        }

        public override void Select(VisualElement selectionContainer, bool additive)
        {
            // Don't add the un-named/default category to graph selections
            if (controller.Model.IsNamedCategory())
            {
                base.Select(selectionContainer, additive);
            }
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
            // Don't add the un-named/default category to graph selections,
            if (controller.Model.IsNamedCategory() == false && selectable == this)
                return;

            // Don't add to selection if a rename op is in progress
            if (m_RenameInProgress)
            {
                RemoveFromSelection(this);
                return;
            }

            var materialGraphView = m_ViewModel.parentView.GetFirstAncestorOfType<MaterialGraphView>();
            materialGraphView?.AddToSelection(selectable);
        }

        public void RemoveFromSelection(ISelectable selectable)
        {
            var materialGraphView = m_ViewModel.parentView.GetFirstAncestorOfType<MaterialGraphView>();

            // If we're de-selecting the category itself
            if (selectable == this)
            {
                materialGraphView?.RemoveFromSelection(selectable);
                // Also deselect the child elements within this category (the field views)
                var fieldViews = this.Query<SGBlackboardField>();
                foreach (var child in fieldViews.ToList())
                {
                    materialGraphView?.RemoveFromSelection(child);
                }
            }
            // If a category is unselected, only then can the children beneath it be deselected
            else if (selection.Contains(this) == false)
            {
                materialGraphView?.RemoveFromSelection(selectable);
            }
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

                return new List<ISelectable>();
            }
        }

        void RequestCategoryDelete()
        {
            var materialGraphView = blackboard.ParentView as MaterialGraphView;
            materialGraphView?.deleteSelection?.Invoke("Delete", GraphView.AskUser.DontAskUser);
        }

        void AddContextMenuOptions(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Delete", evt => RequestCategoryDelete());
            evt.menu.AppendAction("Rename", (a) => OpenTextEditor(), DropdownMenuAction.AlwaysEnabled);
            // Don't allow the default/un-named category to have right-click menu options
            if (controller.Model.IsNamedCategory())
            {
                evt.menu.AppendAction("Delete", evt => RequestCategoryDelete());
            }
        }

        public int CompareTo(SGBlackboardCategory other)
        {
            if (other == null)
                return 1;

            var thisBlackboard = this.blackboard;
            var otherBlackboard = other.blackboard;

            return thisBlackboard.IndexOf(this).CompareTo(otherBlackboard.IndexOf(other));
        }
    }
}
