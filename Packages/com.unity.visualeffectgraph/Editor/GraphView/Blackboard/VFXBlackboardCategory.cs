using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

using PositionType = UnityEngine.UIElements.Position;

namespace UnityEditor.VFX.UI
{
    interface IBlackBoardElementWithTitle
    {
        string text { get; }
        void OpenTextEditor();
    }

    class VFXBlackboardCategory : VFXBlackboardFieldBase
    {
        private readonly IParameterItem m_Category;

        public VFXBlackboardCategory(IParameterItem category) : base($"cat:{category.title}")
        {
            m_Category = category;

            var tpl = VFXView.LoadUXML("VFXBlackboardCategory");
            tpl.CloneTree(this);
            m_Label = this.Q<Label>("title");
            if (m_Category.canRename)
            {
                capabilities |= Capabilities.Deletable;
                m_TextField = this.Q<TextField>("titleEdit");
                m_TextField.selectAllOnMouseUp = false;
                m_TextField.RegisterCallback<KeyDownEvent>(OnTextFieldKeyPressed, TrickleDown.TrickleDown);
                m_TextField.RegisterCallback<FocusOutEvent>(OnEditTextSucceed, TrickleDown.TrickleDown);
                RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            }
            else
            {
                capabilities &= ~(Capabilities.Deletable | Capabilities.Renamable | Capabilities.Copiable);
            }

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        protected override void OnEditTextSucceed(FocusOutEvent evt)
        {
            base.OnEditTextSucceed(evt);
            if (this.title != m_TextField.value)
            {
                GetFirstAncestorOfType<VFXBlackboard>()?.SetCategoryName(this, m_TextField.value);
            }
        }

        public override IParameterItem item => category;
        public IParameterItem category => m_Category;

        public new string title
        {
            get => m_Label.text;
            set => m_Label.text = value;
        }

        private DropdownMenuAction.Status IsMenuVisible(DropdownMenuAction action)
        {
            switch (m_Category)
            {
                case OutputCategory: return DropdownMenuAction.Status.Disabled;
                case PropertyCategory { isRoot: false }: return DropdownMenuAction.Status.Normal;
                default: return DropdownMenuAction.Status.Disabled;
            }
        }

        private void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target == this)
            {
                evt.menu.AppendAction("Rename", (a) => OpenTextEditor(), IsMenuVisible);
                evt.menu.AppendAction("Duplicate %d", (a) => Duplicate(), IsMenuVisible);
                evt.menu.AppendAction("Delete", (a) => Delete(), IsMenuVisible);
                evt.menu.AppendSeparator(string.Empty);
            }
        }

        private void Delete()
        {
            GetFirstAncestorOfType<VFXView>().Delete();
        }

        private void Duplicate()
        {
            GetFirstAncestorOfType<VFXView>().DuplicateSelectionCallback();
        }
    }
}
