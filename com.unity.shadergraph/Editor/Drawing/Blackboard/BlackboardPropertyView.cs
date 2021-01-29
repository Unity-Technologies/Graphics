using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Assertions;

using ContextualMenuManipulator = UnityEngine.UIElements.ContextualMenuManipulator;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardPropertyView : GraphElement, IInspectable, ISGControlledElement<ShaderInputViewController>
    {
        static readonly Texture2D k_ExposedIcon = Resources.Load<Texture2D>("GraphView/Nodes/BlackboardFieldExposed");
        static readonly string k_UxmlTemplatePath = "UXML/GraphView/BlackboardField";
        static readonly string k_StyleSheetPath = "Styles/Blackboard";

        ShaderInputViewModel m_ViewModel;

        ShaderInputViewModel ViewModel
        {
            get => m_ViewModel;
            set => m_ViewModel = value;
        }

        VisualElement m_ContentItem;
        Pill m_Pill;
        TextField m_TextField;
        Label m_TypeLabel;
        Label m_NameLabelField;

        ShaderInputPropertyDrawer.ChangeReferenceNameCallback m_ResetReferenceNameTrigger;

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

        internal BlackboardPropertyView(ShaderInputViewModel viewModel)
        {
            ViewModel = viewModel;
            // Store ShaderInput in userData object
            userData = ViewModel.Model;

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


            Add(mainContainer);

            RegisterCallback<MouseDownEvent>(OnMouseDownEvent);

            capabilities |= Capabilities.Selectable | Capabilities.Droppable | Capabilities.Deletable | Capabilities.Renamable;

            ClearClassList();
            AddToClassList("blackboardField");

            this.name = "BlackboardPropertyView";
            UpdateFromViewModel();

            // add the right click context menu
            IManipulator contextMenuManipulator = new ContextualMenuManipulator(AddContextMenuOptions);
            this.AddManipulator(contextMenuManipulator);
            this.AddManipulator(new SelectionDropper());
            this.AddManipulator(new ContextualMenuManipulator(BuildFieldContextualMenu));

            // When a display name is changed through the BlackboardPill, bind this callback to handle it with appropriate change action
            var textInputElement = m_TextField.Q(TextField.textInputUssName);
            textInputElement.RegisterCallback<FocusOutEvent>(e => { OnEditTextFinished(); });
            textInputElement.RegisterCallback<FocusOutEvent>(e =>
            {
                var changeDisplayNameAction = new ChangeDisplayNameAction();
                changeDisplayNameAction.ShaderInputReference = shaderInput;
                changeDisplayNameAction.NewDisplayNameValue = m_TextField.text;
                ViewModel.RequestModelChangeAction(changeDisplayNameAction);
            });

            ShaderGraphPreferences.onAllowDeprecatedChanged += UpdateTypeText;
        }

        ~BlackboardPropertyView()
        {
            ShaderGraphPreferences.onAllowDeprecatedChanged -= UpdateTypeText;
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
                        resetReferenceNameAction.ShaderInputReference = shaderInput;
                        ViewModel.RequestModelChangeAction(resetReferenceNameAction);
                        m_ResetReferenceNameTrigger(shaderInput.referenceName);
                    },
                    DropdownMenuAction.AlwaysEnabled);
            }
        }

        internal void UpdateFromViewModel()
        {
            this.text = ViewModel.InputName;
            this.icon = ViewModel.IsInputExposed ? k_ExposedIcon : null;
            this.typeText = ViewModel.InputTypeName;
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
                    Clear();
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
        ShaderInput shaderInput => ViewModel.Model;

        public string inspectorTitle => ViewModel.InputName + " " + ViewModel.InputTypeName;

        public object GetObjectToInspect()
        {
            return shaderInput;
        }

        public void SupplyDataToPropertyDrawer(IPropertyDrawer propertyDrawer, Action inspectorUpdateDelegate)
        {
            if (propertyDrawer is ShaderInputPropertyDrawer shaderInputPropertyDrawer)
            {
                shaderInputPropertyDrawer.GetViewModel(ViewModel);
                m_ResetReferenceNameTrigger = shaderInputPropertyDrawer._resetReferenceNameCallback;
                this.RegisterCallback<DetachFromPanelEvent>(evt => inspectorUpdateDelegate());
            }
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.eventTypeId == AttachToPanelEvent.TypeId())
            {
                int x = 0;
                x++;
                // TODO: Re-enable somehow (going to need to grab internal function which is gross but temporary at least)
                //if (ViewModel.ParentView is GraphView graphView)
                //    graphView.RestorePersitentSelectionForElement(this);
            }
        }

        void OnEditTextFinished()
        {
            m_ContentItem.visible = true;
            m_TextField.style.display = DisplayStyle.None;

            if (text != m_TextField.text)
            {
                var renameBlackboardItemAction = new RenameBlackboardItemAction();
                renameBlackboardItemAction.NewItemName = m_TextField.text;
                ViewModel.RequestModelChangeAction(renameBlackboardItemAction);
            }
        }

        void OnMouseDownEvent(MouseDownEvent e)
        {
            if ((e.clickCount == 2) && e.button == (int)MouseButton.LeftMouse && IsRenamable())
            {
                OpenTextEditor();
                e.PreventDefault();
            }
        }

        void UpdateTypeText()
        {
            if (shaderInput is AbstractShaderProperty asp)
            {
                typeText = asp.GetPropertyTypeString();
            }
        }

        public void OpenTextEditor()
        {
            m_TextField.SetValueWithoutNotify(text);
            m_TextField.style.display = DisplayStyle.Flex;
            m_ContentItem.visible = false;
            m_TextField.Q(TextField.textInputUssName).Focus();
            m_TextField.SelectAll();
        }

        protected virtual void BuildFieldContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Rename", (a) => OpenTextEditor(), DropdownMenuAction.AlwaysEnabled);
        }
    }
}
