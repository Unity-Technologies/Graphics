using System.Collections.Generic;
using System.IO;

using UnityEditor.Experimental.GraphView;
using UnityEditor.VFX.Block;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXBlackboardAttributeField : VFXBlackboardFieldBase
    {
        private static readonly Dictionary<CustomAttributeUtility.Signature, string> s_TypeToIconPath = new()
        {
            { CustomAttributeUtility.Signature.Float, "Float" },
            { CustomAttributeUtility.Signature.Vector2, "Vector2" },
            { CustomAttributeUtility.Signature.Vector3, "Vector3" },
            { CustomAttributeUtility.Signature.Vector4, "Vector4" },
            { CustomAttributeUtility.Signature.Bool, "Boolean" },
            { CustomAttributeUtility.Signature.Uint, "Integer" },
            { CustomAttributeUtility.Signature.Int, "Integer" },
        };

        private readonly Pill m_Pill;

        public VFXBlackboardAttributeField(AttributeItem attribute) : base($"attr:{attribute.title}")
        {
            this.AddStyleSheetPath(Blackboard.StyleSheetPath);

            RegisterCallback<MouseEnterEvent>(OnMouseHover);
            RegisterCallback<MouseLeaveEvent>(OnMouseHover);
            RegisterCallback<MouseCaptureOutEvent>(OnMouseHover);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            m_Pill = new Pill();
            Add(m_Pill);
            m_Label = m_Pill.Q<Label>();
            m_Label.pickingMode = PickingMode.Ignore;
            m_Pill.pickingMode = PickingMode.Ignore;
            m_Pill.Q<TemplateContainer>().pickingMode = PickingMode.Ignore;

            capabilities |= Capabilities.Deletable;
            if (!attribute.isBuiltIn && attribute.isEditable)
            {
                RegisterCallback<MouseDownEvent>(OnMouseDown);

                m_TextField = new TextField { name = "textField"};
                Add(m_TextField);
                m_TextField.style.display = DisplayStyle.None;
                m_TextField.selectAllOnMouseUp = false;

                m_TextField.RegisterCallback<KeyDownEvent>(OnTextFieldKeyPressed, TrickleDown.TrickleDown);
                m_TextField.RegisterCallback<FocusOutEvent>(OnEditTextSucceed, TrickleDown.TrickleDown);
            }

            this.attribute = attribute;
            this.text = this.attribute.title;
            this.UpdateType(this.attribute.type);

            ClearClassList();
            AddToClassList("blackboardField");
        }

        public override IParameterItem item => attribute;
        public AttributeItem attribute { get; }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            UpdateHover(View, false);
        }

        private void OnMouseHover(EventBase evt)
        {
            Profiler.BeginSample("VFXBlackboardAttributeField.OnMouseOver");
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

        private void UpdateHover(VFXView view, bool isHovered)
        {
            try
            {
                view.Query<VFXNodeUI>().ForEach(attributeNode =>
                {
                    if (IsModelUsingAttribute(attributeNode.controller.model))
                    {
                        if (isHovered)
                            attributeNode.AddToClassList("hovered");
                        else
                            attributeNode.RemoveFromClassList("hovered");
                    }
                });
            }
            catch
            {
                // Never throw because of highlight
                // Attribute can be not found right after renaming
            }
        }

        public void UpdateType(CustomAttributeUtility.Signature newType)
        {
            this.m_Pill.icon = EditorGUIUtility.LoadIcon(Path.Combine(VisualEffectGraphPackageInfo.assetPackagePath, $"Editor/UIResources/VFX/types/{s_TypeToIconPath[newType]}@2x.png"));
        }

        public override void OpenTextEditor()
        {
            base.OpenTextEditor();
            m_Pill.style.display = DisplayStyle.None;
        }

        protected override void CleanupNameField()
        {
            base.CleanupNameField();
            m_Pill.style.display = DisplayStyle.Flex;
        }

        protected override void OnEditTextSucceed(FocusOutEvent evt)
        {
            if (m_TextField.style.display == DisplayStyle.Flex
                && text != m_TextField.value
                && View.controller.graph.TryRenameCustomAttribute(text, m_TextField.value))
            {
                attribute.title = m_TextField.value;
                text = m_TextField.value;
            }

            base.OnEditTextSucceed(evt);
        }

        private bool IsModelUsingAttribute(VFXModel model)
        {
            if (model is IVFXAttributeUsage attributeUsage)
            {
                foreach (var attr in attributeUsage.usedAttributes)
                {
                    if (VFXAttributeHelper.IsMatching(attr.name, attribute.title, true))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
