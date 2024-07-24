using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXBlackboardField : VFXBlackboardFieldBase, IControlledElement
    {
        private readonly Label m_TypeLabel;
        private readonly Pill m_Pill;

        public VFXBlackboardRow owner { get; set; }

        public VFXBlackboardField(PropertyItem propertyItem) : base($"prop:{propertyItem.title}")
        {
            this.AddStyleSheetPath(Blackboard.StyleSheetPath);

            PropertyItem = propertyItem;
            RegisterCallback<MouseEnterEvent>(OnMouseHover);
            RegisterCallback<MouseLeaveEvent>(OnMouseHover);
            RegisterCallback<MouseCaptureOutEvent>(OnMouseHover);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            RegisterCallback<MouseDownEvent>(OnMouseDown);

            capabilities |= Capabilities.Deletable;
            m_Pill = new Pill();
            Add(m_Pill);
            m_Label = m_Pill.Q<Label>();
            m_Pill.pickingMode = PickingMode.Ignore;
            m_Pill.Q<Label>().pickingMode = PickingMode.Ignore;
            m_Pill.Q<TemplateContainer>().pickingMode = PickingMode.Ignore;

            m_TypeLabel = new Label { name = "typeLabel" };
            m_TypeLabel.pickingMode = PickingMode.Ignore;
            Add(m_TypeLabel);
            m_TextField = new TextField { name = "textField"};
            Add(m_TextField);
            m_TextField.style.display = DisplayStyle.None;

            m_TextField.RegisterCallback<KeyDownEvent>(OnTextFieldKeyPressed, TrickleDown.TrickleDown);
            m_TextField.RegisterCallback<FocusOutEvent>(OnEditTextSucceed, TrickleDown.TrickleDown);

            ClearClassList();
            AddToClassList("blackboardField");
        }

        public override IParameterItem item => PropertyItem;
        public PropertyItem PropertyItem { get; }

        Controller IControlledElement.controller => controller;
        public VFXParameterController controller => owner.controller;

        public void OnControllerChanged(ref ControllerChangedEvent e)
        {
        }

        public void SelfChange()
        {
            text = controller.exposedName;
            m_TypeLabel.text = controller.portType != null ? controller.portType.UserFriendlyName() : "null";

            if (controller.isOutput)
                m_Pill.icon = AssetDatabase.LoadAssetAtPath<Texture2D>(VisualEffectAssetEditorUtility.editorResourcesPath + "/VFX/output-dot.png");
            else if (controller.exposed)
                m_Pill.icon = AssetDatabase.LoadAssetAtPath<Texture2D>(VisualEffectAssetEditorUtility.editorResourcesPath + "/VFX/exposed-dot.png");
            else
                m_Pill.icon = null;

            m_Pill.tooltip = controller.model.tooltip;

            var isUsed = false;
            var slots = controller.isOutput ? controller.model.inputSlots : controller.model.outputSlots;
            foreach (var slot in slots)
            {
                if (slot.HasLink(true))
                {
                    isUsed = true;
                    break;
                }
            }

            if (isUsed)
                RemoveFromClassList("unused");
            else
                AddToClassList("unused");
        }

        public override void OpenTextEditor()
        {
            base.OpenTextEditor();
            m_TypeLabel.style.display = DisplayStyle.None;
            m_Pill.style.display = DisplayStyle.None;
        }

        protected override void OnEditTextSucceed(FocusOutEvent evt)
        {
            if (controller.exposedName != m_TextField.value)
            {
                controller.exposedName = m_TextField.value;
            }
            base.OnEditTextSucceed(evt);
        }

        protected override void CleanupNameField()
        {
            base.CleanupNameField();
            m_Pill.style.display = DisplayStyle.Flex;
            m_TypeLabel.style.display = DisplayStyle.Flex;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UpdateHover(View, false);
        }

        void OnMouseHover(EventBase evt)
        {
            Profiler.BeginSample("VFXBlackboardField.OnMouseOver");
            try
            {
                if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                    return;
                UpdateHover(View, evt.eventTypeId == MouseEnterEvent.TypeId());
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        void UpdateHover(VFXView view, bool isHovered)
        {
            view.Query<VFXParameterUI>().ForEach(parameter =>
            {
                if (parameter.controller.parentController == controller)
                {
                    if (isHovered)
                        parameter.AddToClassList("hovered");
                    else
                        parameter.RemoveFromClassList("hovered");
                }
            });
        }
    }
}
