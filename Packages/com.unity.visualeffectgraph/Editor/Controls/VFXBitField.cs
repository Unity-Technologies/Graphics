using UnityEngine.UIElements;

using System.Runtime.InteropServices;
namespace UnityEditor.VFX
{
    class BitFieldAttribute : System.Attribute
    {
    }
}

namespace UnityEditor.VFX.UI
{
    abstract class VFXBitField<T, U> : VFXControl<U>
    {
        private Label m_Label;
        private bool m_Indeterminate;

        protected VisualElement[] m_Buttons;

        protected VFXBitField()
        {
            m_Buttons = new VisualElement[Marshal.SizeOf(typeof(T)) * 8];
            for (int i = 0; i < m_Buttons.Length; ++i)
            {
                var button = new VisualElement { name = "bit-button"};
                SetupListener(button, i);
                Add(button);
                m_Buttons[i] = button;
            }
            m_Buttons[0].AddToClassList("first");
            m_Buttons[^1].AddToClassList("last");

            m_Label = new Label { name = "tip" };
            Add(m_Label);

            RegisterCallback<MouseLeaveEvent>(e => m_Label.text = "");
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Check All", CheckAll, DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Check None", CheckNone, DropdownMenuAction.AlwaysEnabled);
        }

        protected abstract void CheckAll(DropdownMenuAction a);

        protected abstract void CheckNone(DropdownMenuAction a);

        void SetupListener(VisualElement button, int index)
        {
            button.AddManipulator(new Clickable(() => this.ValueToggled(index)));
            button.RegisterCallback<MouseEnterEvent>(e => m_Label.text = index.ToString());
        }

        protected abstract void ValueToggled(int i);

        public override bool indeterminate
        {
            get => m_Indeterminate;
            set
            {
                m_Indeterminate = value;
                foreach (var button in m_Buttons)
                {
                    button.SetEnabled(!m_Indeterminate);
                }
            }
        }
    }

    class VFX32BitField : VFXBitField<uint, long>
    {
        protected override void ValueToGUI(bool force)
        {
            uint bitMask = (uint)this.value;
            for (int i = 0; i < m_Buttons.Length; ++i)
            {
                if ((bitMask & 1u << i) != 0)
                {
                    m_Buttons[i].AddToClassList("bit-set");
                }
                else
                {
                    m_Buttons[i].RemoveFromClassList("bit-set");
                }
            }
        }

        protected override void ValueToggled(int i)
        {
            value = value ^ (1u << i);
        }

        protected override void CheckAll(DropdownMenuAction a)
        {
            value = 0xFFFFFFFF;
        }

        protected override void CheckNone(DropdownMenuAction a)
        {
            value = 0;
        }
    }
}
