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

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardPropertyView : GraphElement, IInspectable, ISGControlledElement<ShaderInputViewController>
    {
        static readonly Texture2D k_ExposedIcon = Resources.Load<Texture2D>("GraphView/Nodes/BlackboardFieldExposed");

        ShaderInputViewModel m_ViewModel;

        ShaderInputViewModel ViewModel
        {
            get => m_ViewModel;
            set => m_ViewModel = value;
        }

        private VisualElement m_ContentItem;
        private Pill m_Pill;
        private TextField m_TextField;
        private Label m_TypeLabel;

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

        public string inspectorTitle { get; }

        internal BlackboardPropertyView(ShaderInputViewModel viewModel)
        {
            ViewModel = viewModel;

            var visualTreeAsset = Resources.Load<VisualTreeAsset>("UXML/GraphView/BlackboardField");
            Assert.IsNotNull(visualTreeAsset);

            VisualElement mainContainer = visualTreeAsset.Instantiate();
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/ShaderGraphBlackboard"));

            mainContainer.AddToClassList("mainContainer");
            mainContainer.pickingMode = PickingMode.Ignore;

            m_ContentItem = mainContainer.Q("contentItem");
            m_Pill = mainContainer.Q<Pill>("pill");
            m_TypeLabel = mainContainer.Q<Label>("typeLabel");
            m_TextField = mainContainer.Q<TextField>("textField");
            m_TextField.style.display = DisplayStyle.None;

            var textInputElement = m_TextField.Q(TextField.textInputUssName);
            textInputElement.RegisterCallback<FocusOutEvent>(e => { OnEditTextFinished(); });

            Add(mainContainer);

            RegisterCallback<MouseDownEvent>(OnMouseDownEvent);

            capabilities |= Capabilities.Selectable | Capabilities.Droppable | Capabilities.Deletable | Capabilities.Renamable;

            ClearClassList();
            AddToClassList("blackboardField");

            this.text = ViewModel.InputName;
            this.icon = viewModel.IsInputExposed ? k_ExposedIcon : null;
            this.typeText = ViewModel.InputTypeName;

            this.AddManipulator(new SelectionDropper());
            this.AddManipulator(new ContextualMenuManipulator(BuildFieldContextualMenu));
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

        public object GetObjectToInspect()
        {
            throw new NotImplementedException();
        }

        public void SupplyDataToPropertyDrawer(IPropertyDrawer propertyDrawer, Action inspectorUpdateDelegate)
        {
            throw new NotImplementedException();
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.eventTypeId == AttachToPanelEvent.TypeId())
            {
                int x = 0;
                x++;
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
