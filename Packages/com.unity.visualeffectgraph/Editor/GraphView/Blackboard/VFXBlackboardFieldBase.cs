using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    abstract class VFXBlackboardFieldBase : GraphElement, IBlackBoardElementWithTitle
    {
        private VFXView m_View;

        protected Label m_Label;
        protected TextField m_TextField;

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

        public virtual void OpenTextEditor()
        {
            m_Label.style.display = DisplayStyle.None;
            m_TextField.value = text;
            m_TextField.style.display = DisplayStyle.Flex;
            m_TextField.Q(TextField.textInputUssName).Focus();
        }

        public override void OnSelected()
        {
            m_View.blackboard.UpdateSelection();
        }

        public override void OnUnselected()
        {
            m_View.blackboard.UpdateSelection();
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
                    OnEditTextSucceed(null);
                    e.StopPropagation();
                    break;
            }
        }

        protected virtual void OnEditTextSucceed(FocusOutEvent evt)
        {
            CleanupNameField();
        }

        protected virtual void CleanupNameField()
        {
            m_TextField.style.display = DisplayStyle.None;
            m_Label.style.display = DisplayStyle.Flex;
            GetFirstAncestorOfType<TreeView>().Focus();
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_View = GetFirstAncestorOfType<VFXView>();
            UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }
    }
}
