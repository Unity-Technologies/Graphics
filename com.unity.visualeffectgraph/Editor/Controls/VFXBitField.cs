using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace UnityEditor.VFX
{ 

    public class BitFieldAttribute : System.Attribute
    {

    }
}

namespace UnityEditor.VFX.UIElements
{

    abstract class VFXBitField<T,U> : VFXControl<U>
    {
        protected VisualElement[] m_Buttons;
        protected VisualElement m_Background;
        protected Texture2D m_BitImage;
        protected Texture2D m_BitBkgndImage;
        protected Label m_Label;

        public VFXBitField()
        {
            m_Buttons = new VisualElement[Marshal.SizeOf(typeof(T)) * 8];
            m_Background = new VisualElement() { name = "background" };

            m_Label = new Label() { name = "tip" };
            Add(m_Label);

            Add(m_Background);
            var buttonContainer = new VisualElement() { name = "button-container" ,pickingMode = PickingMode.Ignore};
            Add(buttonContainer);
            for (int i = 0; i < m_Buttons.Length; ++i)
            {
                var button = new VisualElement();
                button.style.flexGrow = button.style.flexShrink = 1;
                button.style.marginRight = 1;
                SetupListener(button, i);
                buttonContainer.Add(button);
                m_Buttons[i] = button;
            }

            VisualElement backgroundItem = null;
            for (int i = 0; i < m_Buttons.Length; ++i)
            {
                backgroundItem = new VisualElement();
                backgroundItem.style.flexGrow = backgroundItem.style.flexShrink = 1;
                if( i != m_Buttons.Length -1)
                    backgroundItem.style.paddingLeft = 1;
                SetupBkgnd(backgroundItem, i);
                m_Background.Add(backgroundItem);
            }

            m_Buttons[m_Buttons.Length - 1].style.marginRight = 0;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        public void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Check All", CheckAll, DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Check None", CheckNone, DropdownMenuAction.AlwaysEnabled);
        }

        protected abstract void CheckAll(DropdownMenuAction a);

        protected abstract void CheckNone(DropdownMenuAction a);

        void SetupListener(VisualElement button, int index)
        {
            button.AddManipulator(new Clickable(() => ValueToggled(index)));
            button.RegisterCallback<MouseEnterEvent>(e => m_Label.text = index.ToString());
            button.RegisterCallback<MouseLeaveEvent>(e => m_Label.text = "");
        }
        void SetupBkgnd(VisualElement button, int index)
        {
            button.RegisterCallback<MouseEnterEvent>(e => m_Label.text = index.ToString());
            button.RegisterCallback<MouseLeaveEvent>(e => m_Label.text = "");
        }

        protected abstract void ValueToggled(int i);

        static readonly CustomStyleProperty<Texture2D> s_BitImage = new CustomStyleProperty<Texture2D>("--bit-image");
        static readonly CustomStyleProperty<Texture2D> s_BitBkgndImage = new CustomStyleProperty<Texture2D>("--bit-bkgnd-image");
        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            var customStyle = e.customStyle;
            customStyle.TryGetValue(s_BitImage, out m_BitImage);
            customStyle.TryGetValue(s_BitBkgndImage, out m_BitBkgndImage);

            for(int i = 0; i < m_Background.childCount -1; ++i)
                m_Background.ElementAt(i).style.backgroundImage = m_BitBkgndImage;

            ValueToGUI(true);
        }

        bool m_Indeterminate;

        public override bool indeterminate
        {
            get
            {
                return m_Indeterminate;
            }
            set
            {
                m_Indeterminate = value;
                foreach( var button in m_Buttons)
                {
                    button.visible = !m_Indeterminate;
                }
            }
        }
    }

    class VFX32BitField : VFXBitField<uint,long>
    {
        protected override void ValueToGUI(bool force)
        {
            uint value = (uint)this.value;
            for (int i = 0; i < m_Buttons.Length; ++i)
            {
                m_Buttons[i].style.backgroundImage = (value & 1u<<i) != 0? m_BitImage : null;
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
