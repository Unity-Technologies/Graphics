using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace UnityEditor.VFX.UI
{
    interface IBlackBoardElementWithTitle
    {
        string text { get; }
        bool OpenTextEditor();
    }

    class VFXBlackboardCategory : VFXBlackboardFieldBase
    {
        public const int kMaxCategoryNameLength = 64;
        private readonly IParameterItem m_Category;

        public VFXBlackboardCategory(IParameterItem category) : base($"cat:{category.title}")
        {
            maxTextLength = kMaxCategoryNameLength;
            m_Category = category;

            var tpl = VFXView.LoadUXML("VFXBlackboardCategory");
            tpl.CloneTree(this);
            m_Label = this.Q<Label>("title");
            if (m_Category.canRename)
            {
                capabilities |= Capabilities.Deletable;
                var textField = this.Q<TextField>("titleEdit");
                textField.selectAllOnMouseUp = false;
                SetTextField(textField);
                RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            }
            else
            {
                capabilities &= ~(Capabilities.Deletable | Capabilities.Renamable | Capabilities.Copiable);
            }

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        protected override void OnEditTextSucceed(TextField textField)
        {
            base.OnEditTextSucceed(textField);
            if (this.title != textField.value)
            {
                GetFirstAncestorOfType<VFXBlackboard>()?.SetCategoryName(this, textField.value);
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
