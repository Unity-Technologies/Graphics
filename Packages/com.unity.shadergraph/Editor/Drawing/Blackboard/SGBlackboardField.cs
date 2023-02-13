using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Assertions;

using ContextualMenuManipulator = UnityEngine.UIElements.ContextualMenuManipulator;
using GraphDataStore = UnityEditor.ShaderGraph.DataStore<UnityEditor.ShaderGraph.GraphData>;

namespace UnityEditor.ShaderGraph.Drawing
{
    class SGBlackboardField : GraphElement, IInspectable, ISGControlledElement<ShaderInputViewController>, IDisposable
    {
        static readonly Texture2D k_ExposedIcon = Resources.Load<Texture2D>("GraphView/Nodes/BlackboardFieldExposed");
        static readonly string k_UxmlTemplatePath = "UXML/Blackboard/SGBlackboardField";
        static readonly string k_StyleSheetPath = "Styles/SGBlackboard";

        ShaderInputViewModel m_ViewModel;

        ShaderInputViewModel ViewModel
        {
            get => m_ViewModel;
            set => m_ViewModel = value;
        }

        VisualElement m_ContentItem;
        Pill m_Pill;
        Label m_TypeLabel;
        TextField m_TextField;
        internal TextField textField => m_TextField;

        Action m_ResetReferenceNameTrigger;
        List<Node> m_SelectedNodes = new List<Node>();

        public string text
        {
            get { return m_Pill.text; }
            set { m_Pill.text = value; }
        }

        public string typeText
        {
            get { return m_TypeLabel.text; }
            set { m_TypeLabel.text = value; }
        }

        public Texture icon
        {
            get { return m_Pill.icon; }
            set { m_Pill.icon = value; }
        }

        public bool highlighted
        {
            get { return m_Pill.highlighted; }
            set { m_Pill.highlighted = value; }
        }

        internal SGBlackboardField(ShaderInputViewModel viewModel)
        {
            ViewModel = viewModel;
            // Store ShaderInput in userData object
            userData = ViewModel.model;
            if (userData == null)
            {
                AssertHelpers.Fail("Could not initialize blackboard field as shader input was null.");
                return;
            }
            // Store the Model guid as viewDataKey as that is persistent
            viewDataKey = ViewModel.model.guid.ToString();

            var visualTreeAsset = Resources.Load<VisualTreeAsset>(k_UxmlTemplatePath);
            Assert.IsNotNull(visualTreeAsset);

            VisualElement mainContainer = visualTreeAsset.Instantiate();
            var styleSheet = Resources.Load<StyleSheet>(k_StyleSheetPath);
            Assert.IsNotNull(styleSheet);
            styleSheets.Add(styleSheet);

            mainContainer.AddToClassList("mainContainer");
            mainContainer.pickingMode = PickingMode.Ignore;

            m_ContentItem = mainContainer.Q("contentItem");
            m_Pill = mainContainer.Q<Pill>("pill");
            m_TypeLabel = mainContainer.Q<Label>("typeLabel");
            m_TextField = mainContainer.Q<TextField>("textField");
            m_TextField.style.display = DisplayStyle.None;

            // Handles the upgrade fix for the old color property deprecation
            if (shaderInput is AbstractShaderProperty property)
            {
                property.onAfterVersionChange += () =>
                {
                    this.typeText = property.GetPropertyTypeString();
                    this.m_InspectorUpdateDelegate();
                };
            }

            Add(mainContainer);

            RegisterCallback<MouseDownEvent>(OnMouseDownEvent);

            capabilities |= Capabilities.Selectable | Capabilities.Droppable | Capabilities.Deletable | Capabilities.Renamable;

            ClearClassList();
            AddToClassList("blackboardField");

            this.name = "SGBlackboardField";
            UpdateFromViewModel();

            // add the right click context menu
            IManipulator contextMenuManipulator = new ContextualMenuManipulator(AddContextMenuOptions);
            this.AddManipulator(contextMenuManipulator);
            this.AddManipulator(new SelectionDropper());
            this.AddManipulator(new ContextualMenuManipulator(BuildFieldContextualMenu));

            // When a display name is changed through the BlackboardPill, bind this callback to handle it with appropriate change action
            var textInputElement = m_TextField.Q(TextField.textInputUssName);
            textInputElement.RegisterCallback<FocusOutEvent>(e => { OnEditTextFinished(); }, TrickleDown.TrickleDown);

            ShaderGraphPreferences.onAllowDeprecatedChanged += UpdateTypeText;

            RegisterCallback<MouseEnterEvent>(evt => OnMouseHover(evt, ViewModel.model));
            RegisterCallback<MouseLeaveEvent>(evt => OnMouseHover(evt, ViewModel.model));
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);

            var blackboard = ViewModel.parentView.GetFirstAncestorOfType<SGBlackboard>();
            if (blackboard != null)
            {
                // These callbacks are used for the property dragging scroll behavior
                RegisterCallback<DragEnterEvent>(blackboard.OnDragEnterEvent);
                RegisterCallback<DragExitedEvent>(blackboard.OnDragExitedEvent);

                // These callbacks are used for the property dragging scroll behavior
                RegisterCallback<DragEnterEvent>(blackboard.OnDragEnterEvent);
                RegisterCallback<DragExitedEvent>(blackboard.OnDragExitedEvent);
            }
        }

        void AddContextMenuOptions(ContextualMenuPopulateEvent evt)
        {
            // Checks if the reference name has been overridden and appends menu action to reset it, if so
            if (shaderInput.isRenamable &&
                !string.IsNullOrEmpty(shaderInput.overrideReferenceName))
            {
                evt.menu.AppendAction(
                    "Reset Reference",
                    e =>
                    {
                        var resetReferenceNameAction = new ResetReferenceNameAction();
                        resetReferenceNameAction.shaderInputReference = shaderInput;
                        ViewModel.requestModelChangeAction(resetReferenceNameAction);
                        m_ResetReferenceNameTrigger();
                    },
                    DropdownMenuAction.AlwaysEnabled);
            }

            if (shaderInput is ColorShaderProperty colorProp)
            {
                PropertyNodeView.AddMainColorMenuOptions(evt, colorProp, controller.graphData, m_InspectorUpdateDelegate);
            }


            if (shaderInput is Texture2DShaderProperty texProp)
            {
                PropertyNodeView.AddMainTextureMenuOptions(evt, texProp, controller.graphData, m_InspectorUpdateDelegate);
            }
        }

        internal void UpdateFromViewModel()
        {
            this.text = ViewModel.inputName;
            this.icon = ViewModel.isInputExposed ? k_ExposedIcon : null;
            this.typeText = ViewModel.inputTypeName;
        }

        ShaderInputViewController m_Controller;

        // --- Begin ISGControlledElement implementation
        public void OnControllerChanged(ref SGControllerChangedEvent e)
        {
        }

        public void OnControllerEvent(SGControllerEvent e)
        {
        }

        public ShaderInputViewController controller
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

        [Inspectable("Shader Input", null)]
        public ShaderInput shaderInput => ViewModel.model;

        public string inspectorTitle => ViewModel.inputName + " " + ViewModel.inputTypeName;

        public object GetObjectToInspect()
        {
            return shaderInput;
        }

        Action m_InspectorUpdateDelegate;

        public void SupplyDataToPropertyDrawer(IPropertyDrawer propertyDrawer, Action inspectorUpdateDelegate)
        {
            if (propertyDrawer is ShaderInputPropertyDrawer shaderInputPropertyDrawer)
            {
                // We currently need to do a halfway measure between the old way of handling stuff for property drawers (how FieldView and NodeView handle it)
                // and how we want to handle it with the new style of controllers and views. Ideally we'd just hand the property drawer a view model and thats it.
                // We've maintained all the old callbacks as they are in the PropertyDrawer to reduce possible halo changes and support PropertyNodeView functionality
                // Instead we supply different underlying methods for the callbacks in the new SGBlackboardField,
                // that way both code paths should work until we can refactor PropertyNodeView
                shaderInputPropertyDrawer.GetViewModel(
                    ViewModel,
                    controller.graphData,
                    ((triggerInspectorUpdate, modificationScope) =>
                    {
                        controller.DirtyNodes(modificationScope);
                        if (triggerInspectorUpdate)
                            inspectorUpdateDelegate();
                    }));

                m_ResetReferenceNameTrigger = shaderInputPropertyDrawer.ResetReferenceName;
                m_InspectorUpdateDelegate = inspectorUpdateDelegate;
            }
        }

        void OnMouseDownEvent(MouseDownEvent e)
        {
            if ((e.clickCount == 2) && e.button == (int)MouseButton.LeftMouse && IsRenamable())
            {
                OpenTextEditor();
                e.PreventDefault();
            }
            else
            {
                e.StopPropagation();
            }
        }

        void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            if (m_SelectedNodes.Any())
            {
                foreach (var node in m_SelectedNodes)
                {
                    node.RemoveFromClassList("hovered");
                }
                m_SelectedNodes.Clear();
            }
        }

        // TODO: Move to controller? Feels weird for this to be directly communicating with PropertyNodes etc.
        // Better way would be to send event to controller that notified of hover enter/exit and have other controllers be sent those events in turn
        void OnMouseHover(EventBase evt, ShaderInput input)
        {
            var graphView = ViewModel.parentView.GetFirstAncestorOfType<MaterialGraphView>();
            if (evt.eventTypeId == MouseEnterEvent.TypeId())
            {
                foreach (var node in graphView.nodes.ToList())
                {
                    if (input is AbstractShaderProperty property)
                    {
                        if (node.userData is PropertyNode propertyNode)
                        {
                            if (propertyNode.property == input)
                            {
                                m_SelectedNodes.Add(node);
                                node.AddToClassList("hovered");
                            }
                        }
                    }
                    else if (input is ShaderKeyword keyword)
                    {
                        if (node.userData is KeywordNode keywordNode)
                        {
                            if (keywordNode.keyword == input)
                            {
                                m_SelectedNodes.Add(node);
                                node.AddToClassList("hovered");
                            }
                        }
                    }
                    else if (input is ShaderDropdown dropdown)
                    {
                        if (node.userData is DropdownNode dropdownNode)
                        {
                            if (dropdownNode.dropdown == input)
                            {
                                m_SelectedNodes.Add(node);
                                node.AddToClassList("hovered");
                            }
                        }
                    }
                }
            }
            else if (evt.eventTypeId == MouseLeaveEvent.TypeId() && m_SelectedNodes.Any())
            {
                foreach (var node in m_SelectedNodes)
                {
                    node.RemoveFromClassList("hovered");
                }
                m_SelectedNodes.Clear();
            }
        }

        void UpdateTypeText()
        {
            if (shaderInput is AbstractShaderProperty asp)
            {
                typeText = asp.GetPropertyTypeString();
            }
        }

        internal void OpenTextEditor()
        {
            m_TextField.SetValueWithoutNotify(text);
            m_TextField.style.display = DisplayStyle.Flex;
            m_ContentItem.visible = false;
            m_TextField.Q(TextField.textInputUssName).Focus();
            m_TextField.SelectAll();
        }

        void OnEditTextFinished()
        {
            m_ContentItem.visible = true;
            m_TextField.style.display = DisplayStyle.None;

            if (text != m_TextField.text && String.IsNullOrWhiteSpace(m_TextField.text) == false && String.IsNullOrEmpty(m_TextField.text) == false)
            {
                var changeDisplayNameAction = new ChangeDisplayNameAction();
                changeDisplayNameAction.shaderInputReference = shaderInput;
                changeDisplayNameAction.newDisplayNameValue = m_TextField.text;
                ViewModel.requestModelChangeAction(changeDisplayNameAction);
                m_InspectorUpdateDelegate?.Invoke();
            }
            else
            {
                // Reset text field to original name
                m_TextField.value = text;
            }
        }

        protected virtual void BuildFieldContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Rename", (a) => OpenTextEditor(), DropdownMenuAction.AlwaysEnabled);
        }

        public void Dispose()
        {
            m_ResetReferenceNameTrigger = null;
            m_InspectorUpdateDelegate = null;

            UnregisterCallback<MouseDownEvent>(OnMouseDownEvent);
            UnregisterCallback<MouseEnterEvent>(evt => OnMouseHover(evt, ViewModel.model));
            UnregisterCallback<MouseLeaveEvent>(evt => OnMouseHover(evt, ViewModel.model));
            UnregisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            var blackboard = ViewModel.parentView.GetFirstAncestorOfType<SGBlackboard>();
            UnregisterCallback<DragEnterEvent>(blackboard.OnDragEnterEvent);
            UnregisterCallback<DragExitedEvent>(blackboard.OnDragExitedEvent);
            ShaderGraphPreferences.onAllowDeprecatedChanged -= UpdateTypeText;

            // Clear references
            m_SelectedNodes = null;
            m_ContentItem = null;
            m_Pill = null;
            m_TypeLabel = null;
            m_TextField = null;
            m_Controller = null;
            m_ViewModel = null;
            userData = null;
            styleSheets.Clear();
            Clear();
        }
    }
}
