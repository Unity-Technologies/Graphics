using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;

// NOTICE: this is a rough attempt repalce blackboard sections with our own stuff.
namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardCateogrySection_V : GraphElement
    {
        InputCategory m_Category;
        GraphData m_Graph; // TODO: are we sure about this?

        VisualElement m_DragIndicator;
        VisualElement m_MainContainer;
        VisualElement m_Header;
        Label m_TitleLabel;
        VisualElement m_RowsContainer;
        public Button m_ExpandButton;
        TextField m_NameField;

        int m_InsertIndex;

        bool m_Expanded;

        public bool expanded
        {
            get { return m_Expanded; }
            set
            {
                m_Expanded = value;
                if (m_Expanded)
                {
                    m_Header.AddToClassList("expanded");
                }
                else
                {
                    m_Header.RemoveFromClassList("expanded");
                }
            }
        }

        public BlackboardCateogrySection_V(InputCategory category, GraphData graph)
        {
            m_Category = category;
            m_Graph = graph;

            // TODO: why is this necessary?
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

            var uxml = Resources.Load<VisualTreeAsset>("uxml/GraphView/BlackboardSection");
            m_MainContainer = uxml.CloneTree();

            m_MainContainer.AddToClassList("mainContainer");

            m_Header = m_MainContainer.Q<VisualElement>("sectionHeader");
            m_TitleLabel = m_MainContainer.Q<Label>("sectionTitleLabel");
            m_RowsContainer = m_MainContainer.Q<VisualElement>("rowsContainer");

            m_ExpandButton = m_Header.Q<Button>("expandButton");
            m_ExpandButton.clickable.clicked += ToggleExpand;

            hierarchy.Add(m_MainContainer);
            hierarchy.Add(m_MainContainer);

            title = m_Category.header;

            m_DragIndicator = new VisualElement();

            m_DragIndicator.name = "dragIndicator";
            // m_DragIndicator.style.position = PositionType.Absolute; // TODO:
            hierarchy.Add(m_DragIndicator);

            ClearClassList();
            AddToClassList("blackboardSection");
            AddToClassList("selectable");
            this.Q<VisualElement>("sectionHeader").AddToClassList("blackboardRow");

            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

            SetSelectable(); // TODO:

            m_InsertIndex = -1;
            expanded = true;
            RefreshSection();

            if (m_DragIndicator == null) Debug.Log("m_DragIndicator == null");
            if (m_MainContainer == null) Debug.Log("m_MainContainer == null");
            if (m_Header == null) Debug.Log("m_Header == null");
            if (m_TitleLabel == null) Debug.Log("m_TitleLabel == null");
            if (m_RowsContainer == null) Debug.Log("m_RowsContainer == null");
            if (m_ExpandButton == null) Debug.Log("m_ExpandButton == null");
            if (m_NameField == null) Debug.Log("m_NameField == null");
            // BlackboardProvider.needsUpdate = true;
        }

        public void RefreshSection()
        {
            Clear();
            if (!expanded)
            {
                return;
            }
            foreach (ShaderInput input in m_Category.inputs)
            {
                AddDisplayedInputRow(input);
            }
        }

        void AddDisplayedInputRow(ShaderInput input)
        {
            // TODO: double check that things cannot be added twice
//            if (m_InputRows.ContainsKey(input.guid))
//                return;

            BlackboardField field = null;
            BlackboardRow row = null;

            switch(input)
            {
                case AbstractShaderProperty property:
                {
                    var icon = (m_Graph.isSubGraph || (property.isExposable && property.generatePropertyBlock)) ? BlackboardProvider.exposedIcon : null;
                    field = new BlackboardField(icon, property.displayName, property.propertyType.ToString()) { userData = property };
                    var propertyView = new BlackboardFieldPropertyView(field, m_Graph, property);
                    row = new BlackboardRow(field, propertyView) { userData = input };

                    break;
                }
                case ShaderKeyword keyword:
                {
                    var icon = (m_Graph.isSubGraph || (keyword.isExposable && keyword.generatePropertyBlock)) ? BlackboardProvider.exposedIcon : null;
                    var typeText = KeywordUtil.IsBuiltinKeyword(keyword) ? "Built-in Keyword" : keyword.keywordType.ToString();
                    field = new BlackboardField(icon, keyword.displayName, typeText) { userData = keyword };
                    var keywordView = new BlackboardFieldKeywordView(field, m_Graph, keyword);
                    row = new BlackboardRow(field, keywordView);

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            this.Add(row);

            // TODO:
//            var pill = row.Q<Pill>();
//            pill.RegisterCallback<MouseEnterEvent>(evt => OnMouseHover(evt, input));
//            pill.RegisterCallback<MouseLeaveEvent>(evt => OnMouseHover(evt, input));
//            pill.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
//
//            var expandButton = row.Q<Button>("expandButton");
//            expandButton.RegisterCallback<MouseDownEvent>(evt => OnExpanded(evt, input), TrickleDown.TrickleDown);
//
//            m_InputRows[input.guid] = row;
//            m_InputRows[input.guid].expanded = SessionState.GetBool(input.guid.ToString(), true);
        }

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

//        public VFXBlackboardRow GetRowFromController(VFXParameterController controller)
//        {
//            VFXBlackboardRow row = null;
//
//            m_Parameters.TryGetValue(controller, out row);
//
//            return row;
//        }

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

        void CleanupNameField()
        {
            m_NameField.style.display = DisplayStyle.None;
        }

        public void SetSelectable()
        {
            capabilities |= Capabilities.Selectable | Capabilities.Droppable | Capabilities.Deletable;
            // styleSheets.Add(VFXView.LoadStyleSheet("Selectable")); // TODO:
            AddToClassList("selectable");
            hierarchy.Add(new VisualElement() {name = "selection-border", pickingMode = PickingMode.Ignore});

            //RegisterCallback<MouseDownEvent>(OnHeaderClicked);
            pickingMode = PickingMode.Position;

            this.AddManipulator(new SelectionDropper());

            m_NameField = new TextField() {name = "name-field"};
            m_Header.Add(m_NameField);
            m_Header.RegisterCallback<MouseDownEvent>(OnMouseDownEvent);
            m_NameField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(e => { OnEditTextSucceded(); });
            m_NameField.Q("unity-text-input").RegisterCallback<KeyDownEvent>(OnTextFieldKeyPressed);
            m_Header.pickingMode = PickingMode.Position;
            m_NameField.style.display = DisplayStyle.None;
        }

        void ToggleExpand()
        {
            // BlackboardProvider.needsUpdate = true;

            expanded = !expanded;
            // m_Graph.alteredCategories.Add(m_Category); // TODO: this hack...
            Debug.Log("I'm now going to expand... " + expanded.ToString());
        }

        public override VisualElement contentContainer
        {
            get { return m_RowsContainer; }
        }

        public new string title
        {
            get { return m_TitleLabel.text; }
            set { m_TitleLabel.text = value; }
        }

//        public bool headerVisible
//        {
//            get { return m_Header.parent != null; }
//            set
//            {
//                if (value == (m_Header.parent != null))
//                    return;
//
//                if (value)
//                {
//                    m_MainContainer.Insert(1, m_Header);
//                }
//                else
//                {
//                    m_MainContainer.Remove(m_Header);
//                    expanded = true;
//                }
//            }
//        }

        private void SetDragIndicatorVisible(bool visible)
        {
            if (visible && (m_DragIndicator.parent == null))
            {
                hierarchy.Add(m_DragIndicator);
                m_DragIndicator.visible = true;
            }
            else if ((visible == false) && (m_DragIndicator.parent != null))
            {
                hierarchy.Remove(m_DragIndicator);
            }
        }

        private void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            Debug.Log("OnDragUpdatedEvent");

            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;

            if (selection == null)
            {
                SetDragIndicatorVisible(false);
                return;
            }

            if (selection.Any(t => !(t is BlackboardCateogrySection)))
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
                m_DragIndicator.style.left = 0f;
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
            Debug.Log("OnDragPerformEvent");

            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;

            if (selection == null)
            {
                Debug.Log("selection == null");
                SetDragIndicatorVisible(false);
                return;
            }

            if (selection.OfType<BlackboardCateogrySection>().Any())
            {
                Debug.Log("selection.OfType<BlackboardCateogrySection_V>().Any()");
                // return;
            }

            Debug.Log("I want to move here! " + m_InsertIndex);

            if (m_InsertIndex != -1)
            {
//                var parent = GetFirstAncestorOfType<Blackboard>();
//                if (parent != null)
//                    parent.OnMoveParameter(selection.OfType<VisualElement>().Select(t => t.GetFirstOfType<BlackboardRow>()).Where(t=> t!= null), this, m_InsertIndex);
//                SetDragIndicatorVisible(false);
//                evt.StopPropagation();
//                m_InsertIndex = -1;
            }
        }

        void OnDragLeaveEvent(DragLeaveEvent evt)
        {
            Debug.Log("OnDragLeaveEvent");
            SetDragIndicatorVisible(false);
        }

        private void OnMouseDownEvent(MouseDownEvent e)
        {
            if ((e.clickCount == 2) && e.button == (int) MouseButton.LeftMouse)
            {
                OpenTextEditor();
                e.PreventDefault();
            }
        }

        public void OpenTextEditor()
        {
            Debug.Log("OpenTextEditor()");

            m_NameField.value = title;
            m_NameField.style.display = DisplayStyle.Flex;
            m_NameField.Q(TextField.textInputUssName).Focus();
            m_NameField.SelectAll();
        }

        void OnEditTextSucceded()
        {
            CleanupNameField();
            title = m_NameField.value;

            // Unsure why we would do this
//            var blackboard = GetFirstAncestorOfType<Blackboard>();
//            if (blackboard != null)
//            {
//                blackboard.SetCategoryName(this, m_NameField.value);
//            }
        }

        #region DropdownMenu
        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
        }
        #endregion

    }
}
