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

        public delegate bool CanAcceptDropDelegate(ISelectable selected);

        public CanAcceptDropDelegate canAcceptDrop { get; set; }

        int InsertionIndex(Vector2 pos)
        {
            int index = BlackboardUtils.GetInsertionIndex(this, pos, Children());
            return Mathf.Clamp(index, 0, index);
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
            textInputElement.RegisterCallback<FocusOutEvent>(e => { OnEditTextFinished(); });
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
                m_RowsContainer.RemoveFromHierarchy();
            else
                m_MainContainer.Add(m_RowsContainer);

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
        }

        internal void OpenTextEditor()
        {
            m_TextField.SetValueWithoutNotify(title);
            m_TextField.style.display = DisplayStyle.Flex;
            m_TitleLabel.visible = false;
            m_TextField.Q(TextField.textInputUssName).Focus();
            m_TextField.SelectAll();
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
        }

        void OnHoverStartEvent(MouseEnterEvent evt)
        {
            AddToClassList("hovered");
        }

        void OnHoverEndEvent(MouseLeaveEvent evt)
        {
            RemoveFromClassList("hovered");
        }

        void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;

            VisualElement sourceItem = null;

            bool fieldInSelection = false;

            foreach (ISelectable selectedElement in selection)
            {
                sourceItem = selectedElement as VisualElement;
                if (sourceItem is SGBlackboardField blackboardField)
                    fieldInSelection = true;
                // Don't show drag indicator if selection has categories,
                // We don't want category drag & drop to be ambiguous with shader input drag & drop
                if (sourceItem is SGBlackboardCategory blackboardCategory)
                {
                    SetDragIndicatorVisible(false);
                    return;
                }
            }

            // If can't find at least one blackboard field in the selection, don't update drag indicator
            if (fieldInSelection == false)
            {
                SetDragIndicatorVisible(false);
                return;
            }

            Vector2 localPosition = evt.localMousePosition;

            m_InsertIndex = InsertionIndex(localPosition);
            if (m_InsertIndex != -1)
            {
                float indicatorY = 0;

                if (m_InsertIndex == childCount)
                {
                    // When category is empty
                    if (this.childCount == 0)
                    {
                        // This moves the indicator to the bottom of the category in case of an empty category
                        indicatorY = this.layout.height * 0.9f;
                    }
                    else
                    {
                        VisualElement lastChild = this[childCount - 1];
                        indicatorY = lastChild.ChangeCoordinatesTo(this, new Vector2(0, lastChild.layout.height + lastChild.resolvedStyle.marginBottom)).y;
                    }
                }
                else if (this.childCount > 0)
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
        }

        void OnDragPerformEvent(DragPerformEvent evt)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;

            foreach (ISelectable selectedElement in selection)
            {
                var sourceItem = selectedElement as VisualElement;

                // Don't show drag indicator if selection has categories,
                // We don't want category drag & drop to be ambiguous with shader input drag & drop
                if (sourceItem is SGBlackboardCategory blackboardCategory)
                {
                    SetDragIndicatorVisible(false);
                }
            }

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
                    if (selectedElement is VisualElement draggedElement)
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
                        addItemToCategoryAction.addActionSource = AddItemToCategoryAction.AddActionSource.DragDrop;
                        addItemToCategoryAction.itemToAdd = newShaderInput;
                        addItemToCategoryAction.indexToAddItemAt = m_InsertIndex;
                        m_ViewModel.requestModelChangeAction(addItemToCategoryAction);

                        // Make sure to remove the element from the selection so it doesn't get re-handled by the blackboard as well, leads to duplicates
                        selection.Remove(draggedElement as ISelectable);
                    }
                    m_InsertIndex++;
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
                            if (insertIndex == contentContainer.childCount)
                            {
                                insertIndex = contentContainer.childCount - 1;
                                categoryDirectChild.PlaceInFront(this[contentContainer.childCount - 1]);
                            }
                            else
                            {
                                categoryDirectChild.PlaceBehind(this[insertIndex]);
                            }

                            moveShaderInputAction.associatedCategoryGuid = viewModel.associatedCategoryGuid;
                            moveShaderInputAction.shaderInputReference = shaderInput;
                            moveShaderInputAction.newIndexValue = insertIndex;
                            m_ViewModel.requestModelChangeAction(moveShaderInputAction);
                        }
                    }

                    if (insertIndex > indexOfDraggedElement)     // No need to increment the insert index for the next dragged element if the current dragged element is above the current insert location.
                        continue;
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
            // Select the child elements within this category (the field views)
            var fieldViews = this.Query<SGBlackboardField>();
            foreach (var child in fieldViews.ToList())
            {
                this.AddToSelection(child);
            }
        }

        public override void OnUnselected()
        {
            RemoveFromClassList("selected");
        }

        public void AddToSelection(ISelectable selectable)
        {
            // Don't add the un-named/default category to graph selections
            if (controller.Model.IsNamedCategory() == false && selectable == this)
                return;

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
    }
}
