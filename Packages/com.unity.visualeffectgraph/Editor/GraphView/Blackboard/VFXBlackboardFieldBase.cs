using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    abstract class VFXBlackboardFieldBase : GraphElement, IBlackBoardElementWithTitle
    {
        private VFXView m_View;
        private TextField m_TextField;

        protected Label m_Label;

        protected VFXView View => m_View;

        protected VFXBlackboardFieldBase(string dataKey)
        {
            viewDataKey = dataKey;
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        public abstract IParameterItem item { get; }
        public string text
        {
            get => m_Label.text;
            set => m_Label.text = value;
        }

        protected int maxTextLength { get; set; } = VFXParameterController.kMaxExposedNameLength;

        public virtual bool OpenTextEditor()
        {
            if (text.Length > maxTextLength)
            {
                var result = EditorUtility.DisplayDialog("Name is too long", "The name exceeds the character count limit. Do you want to proceed with renaming and truncate it, or cancel renaming?", "Rename", "Cancel");
                if (!result)
                {
                    return false;
                }
            }
            m_Label.style.display = DisplayStyle.None;
            m_TextField.value = text.Length <= maxTextLength ? text : text.Substring(0, maxTextLength);
            m_TextField.style.display = DisplayStyle.Flex;
            m_TextField.Q(TextField.textInputUssName).Focus();
            return true;
        }

        public override void OnSelected()
        {
            m_View.blackboard.UpdateSelection();
        }

        public override void OnUnselected()
        {
            m_View.blackboard.UpdateSelection();
        }

        protected void SetTextField(TextField textField)
        {
            m_TextField = textField;
            m_TextField.maxLength = maxTextLength;
            textField.RegisterCallback<KeyDownEvent>(OnTextFieldKeyPressed, TrickleDown.TrickleDown);
            textField.RegisterCallback<FocusOutEvent>(OnEditTextSucceed, TrickleDown.TrickleDown);
        }

        protected virtual void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.clickCount == 2 && evt.button == (int)MouseButton.LeftMouse)
            {
                OpenTextEditor();
                focusController.IgnoreEvent(evt);
                evt.StopPropagation();
            }
        }

        protected virtual void OnTextFieldKeyPressed(KeyDownEvent e)
        {
            switch (e.keyCode)
            {
                case KeyCode.Escape:
                    CleanupNameField();
                    e.StopPropagation();
                    break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    OnEditTextSucceed(m_TextField);
                    e.StopPropagation();
                    break;
            }
        }

        protected virtual void OnEditTextSucceed(TextField textField)
        {
            CleanupNameField();
        }

        protected virtual void CleanupNameField()
        {
            m_TextField.style.display = DisplayStyle.None;
            m_Label.style.display = DisplayStyle.Flex;
            GetFirstAncestorOfType<TreeView>().Focus();
        }

        private void OnEditTextSucceed(FocusOutEvent evt)
        {
            if (m_TextField.style.display != DisplayStyle.None)
                OnEditTextSucceed(m_TextField);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_View = GetFirstAncestorOfType<VFXView>();
            UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }
    }
}
