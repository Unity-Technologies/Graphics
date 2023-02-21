using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

using PositionType = UnityEngine.UIElements.Position;

namespace UnityEditor.VFX.UI
{
    class VFXBlackboardCategory : GraphElement
    {
        Dictionary<VFXParameterController, VFXBlackboardRow> m_Parameters = new Dictionary<VFXParameterController, VFXBlackboardRow>();

        private VisualElement m_DragIndicator;
        private VisualElement m_MainContainer;
        private VisualElement m_Header;
        private Label m_TitleLabel;
        private VisualElement m_RowsContainer;
        public Button m_ExpandButton;
        private int m_InsertIndex;

        int InsertionIndex(Vector2 pos)
        {
            int index = 0;
            VisualElement owner = contentContainer != null ? contentContainer : this;
            Vector2 localPos = this.ChangeCoordinatesTo(owner, pos);

            if (owner.ContainsPoint(localPos))
            {
                foreach (VisualElement child in Children())
                {
                    Rect rect = child.layout;

                    if (localPos.y > (rect.y + rect.height / 2))
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

        public VFXBlackboardRow GetRowFromController(VFXParameterController controller)
        {
            VFXBlackboardRow row = null;

            m_Parameters.TryGetValue(controller, out row);

            return row;
        }

        public VFXBlackboardCategory()
        {
            var tpl = VFXView.LoadUXML("VFXBlackboardSection");

            m_MainContainer = tpl.CloneTree();
            m_MainContainer.AddToClassList("mainContainer");

            m_Header = m_MainContainer.Q<VisualElement>("sectionHeader");
            m_TitleLabel = m_MainContainer.Q<Label>("sectionTitleLabel");
            m_RowsContainer = m_MainContainer.Q<VisualElement>("rowsContainer");

            m_ExpandButton = m_Header.Q<Button>("expandButton");
            m_ExpandButton.clickable.clicked += ToggleExpand;

            hierarchy.Add(m_MainContainer);
            hierarchy.Add(m_MainContainer);


            m_DragIndicator = new VisualElement();

            m_DragIndicator.name = "dragIndicator";
            hierarchy.Add(m_DragIndicator);
            m_DragIndicator.style.display = DisplayStyle.None;

            ClearClassList();
            AddToClassList("blackboardSection");
            AddToClassList("selectable");

            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

            m_InsertIndex = -1;
        }

        TextField m_NameField;

        void OnTextFieldKeyPressed(KeyDownEvent e)
        {
            switch (e.keyCode)
            {
                case KeyCode.Escape:
                    CleanupNameField();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    OnEditTextSucceded();
                    break;
            }
        }

        void OnEditTextSucceded()
        {
            CleanupNameField();
            var blackboard = GetFirstAncestorOfType<VFXBlackboard>();
            if (blackboard != null)
            {
                blackboard.SetCategoryName(this, m_NameField.value);
            }
        }

        void CleanupNameField()
        {
            m_NameField.style.display = DisplayStyle.None;
        }

        public void SetSelectable()
        {
            capabilities |= Capabilities.Selectable | Capabilities.Droppable | Capabilities.Deletable;
            styleSheets.Add(VFXView.LoadStyleSheet("Selectable"));
            AddToClassList("selectable");
            hierarchy.Add(new VisualElement() { name = "selection-border", pickingMode = PickingMode.Ignore });

            //RegisterCallback<MouseDownEvent>(OnHeaderClicked);
            pickingMode = PickingMode.Position;

            this.AddManipulator(new SelectionDropper());

            m_NameField = new TextField() { name = "name-field" };
            m_Header.Add(m_NameField);
            m_Header.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            m_NameField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(e => { OnEditTextSucceded(); }, TrickleDown.TrickleDown);
            m_NameField.Q("unity-text-input").RegisterCallback<KeyDownEvent>(OnTextFieldKeyPressed, TrickleDown.TrickleDown);
            m_Header.pickingMode = PickingMode.Position;
            m_NameField.style.display = DisplayStyle.None;
        }

        void ToggleExpand()
        {
            var parent = GetFirstAncestorOfType<VFXBlackboard>();
            if (parent != null)
            {
                parent.SetCategoryExpanded(this, !expanded);
            }
        }

        bool m_Expanded;
        public bool expanded
        {
            get { return m_Expanded; }
            set
            {
                m_Expanded = value;
                if (m_Expanded)
                {
                    AddToClassList("expanded");
                }
                else
                {
                    RemoveFromClassList("expanded");
                }
            }
        }

        public override VisualElement contentContainer { get { return m_RowsContainer; } }

        public new string title
        {
            get { return m_TitleLabel.text; }
            set { m_TitleLabel.text = value; }
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
                    m_MainContainer.Insert(1, m_Header);
                }
                else
                {
                    m_MainContainer.Remove(m_Header);
                    expanded = true;
                }
            }
        }

        private void SetDragIndicatorVisible(bool visible)
        {
            if (visible)
            {
                m_DragIndicator.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_DragIndicator.style.display = DisplayStyle.None;
            }
        }

        private void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;

            if (selection == null)
            {
                SetDragIndicatorVisible(false);
                return;
            }

            if (selection.Any(t => !(t is VFXBlackboardField)))
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
                    if (childCount > 0)
                    {
                        VisualElement lastChild = this[childCount - 1];

                        indicatorY = lastChild.ChangeCoordinatesTo(this, new Vector2(0, lastChild.layout.height + lastChild.resolvedStyle.marginBottom)).y;
                    }
                    else
                    {
                        indicatorY = this.contentRect.height;
                    }
                }
                else
                {
                    VisualElement childAtInsertIndex = this[m_InsertIndex];

                    indicatorY = childAtInsertIndex.ChangeCoordinatesTo(this, new Vector2(0, -childAtInsertIndex.resolvedStyle.marginTop)).y;
                }

                SetDragIndicatorVisible(true);

                Rect dragLayout = m_DragIndicator.layout;

                m_DragIndicator.style.top = indicatorY - dragLayout.height / 2;
            }
            else
            {
                SetDragIndicatorVisible(false);

                m_InsertIndex = -1;
            }

            if (m_InsertIndex != -1)
            {
                DragAndDrop.visualMode = evt.ctrlKey ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
            }

            evt.StopPropagation();
        }

        private void OnDragPerformEvent(DragPerformEvent evt)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;

            if (selection == null)
            {
                SetDragIndicatorVisible(false);
                return;
            }

            if (selection.OfType<VFXBlackboardCategory>().Any())
                return;

            if (m_InsertIndex != -1)
            {
                var parent = GetFirstAncestorOfType<VFXBlackboard>();
                if (parent != null)
                    parent.OnMoveParameter(selection.OfType<VisualElement>().Select(t => t.GetFirstOfType<VFXBlackboardRow>()).Where(t => t != null), this, m_InsertIndex);
                SetDragIndicatorVisible(false);
                evt.StopPropagation();
                m_InsertIndex = -1;
            }
        }

        void OnDragLeaveEvent(DragLeaveEvent evt)
        {
            SetDragIndicatorVisible(false);
        }

        public new void Clear()
        {
            foreach (var param in m_Parameters)
            {
                param.Value.RemoveFromHierarchy();
            }

            m_Parameters.Clear();
        }

        private void OnMouseDownEvent(MouseDownEvent e)
        {
            if ((e.clickCount == 2) && e.button == (int)MouseButton.LeftMouse)
            {
                OpenTextEditor();
                e.PreventDefault();
            }
        }

        public void OpenTextEditor()
        {
            m_NameField.value = title;
            m_NameField.style.display = DisplayStyle.Flex;
            m_NameField.Q(TextField.textInputUssName).Focus();
            m_NameField.SelectAll();
        }

        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target == this && (capabilities & Capabilities.Selectable) != 0)
            {
                evt.menu.AppendAction("Rename", (a) => OpenTextEditor(), DropdownMenuAction.AlwaysEnabled);

                evt.menu.AppendAction("Delete", (a) => GetFirstAncestorOfType<VFXView>().controller.RemoveCategory(m_TitleLabel.text), DropdownMenuAction.AlwaysEnabled);
            }
        }

        public void SyncParameters(HashSet<VFXParameterController> controllers)
        {
            foreach (var removedControllers in m_Parameters.Where(t => !controllers.Contains(t.Key)).ToArray())
            {
                removedControllers.Value.RemoveFromHierarchy();
                m_Parameters.Remove(removedControllers.Key);
            }

            foreach (var addedController in controllers.Where(t => !m_Parameters.ContainsKey(t)).ToArray())
            {
                VFXBlackboardRow row = new VFXBlackboardRow();

                Add(row);

                row.controller = addedController;

                m_Parameters[addedController] = row;
            }

            if (m_Parameters.Count > 0)
            {
                var orderedParameters = m_Parameters.OrderBy(t => t.Key.order).Select(t => t.Value).ToArray();

                if (ElementAt(0) != orderedParameters[0])
                {
                    orderedParameters[0].SendToBack();
                }

                for (int i = 1; i < orderedParameters.Length; ++i)
                {
                    if (ElementAt(i) != orderedParameters[i])
                    {
                        orderedParameters[i].PlaceInFront(orderedParameters[i - 1]);
                    }
                }
            }
        }
    }
}
