using System.Collections.Generic;
using UnityEngine.UI;

namespace UnityEngine.Experimental.Rendering.UI
{
    public class DebugUIHandlerBitField : DebugUIHandlerWidget
    {
        public Text nameLabel;
        public UIFoldout valueToggle;

        public List<DebugUIHandlerIndirectToggle> toggles;

        DebugUI.BitField m_Field;
        DebugUIHandlerContainer m_Container;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            base.SetWidget(widget);
            m_Field = CastWidget<DebugUI.BitField>();
            m_Container = GetComponent<DebugUIHandlerContainer>();
            nameLabel.text = m_Field.displayName;

            int toggleIndex = 0;
            foreach (var enumName in m_Field.enumNames)
            {
                if (toggleIndex >= toggles.Count)
                    continue;

                var toggle = toggles[toggleIndex];
                toggle.getter = GetValue;
                toggle.setter = SetValue;
                toggle.nextUIHandler = toggleIndex < (m_Field.enumNames.Length - 1) ? toggles[toggleIndex + 1] : null;
                toggle.previousUIHandler = toggleIndex > 0 ? toggles[toggleIndex - 1] : null;
                toggle.parentUIHandler = this;
                toggle.index = toggleIndex;
                toggle.nameLabel.text = enumName.text;
                toggle.Init();
                toggleIndex++;
            };

            for (; toggleIndex < toggles.Count; ++toggleIndex)
            {
                toggles[toggleIndex].transform.SetParent(null);
            }
        }

        bool GetValue(int index)
        {
            int intValue = System.Convert.ToInt32(m_Field.GetValue());
            return (intValue & (1 << index)) != 0;
        }

        void SetValue(int index, bool value)
        {
            int intValue = System.Convert.ToInt32(m_Field.GetValue());
            if (value)
                intValue |= m_Field.enumValues[index];
            else
                intValue &= ~m_Field.enumValues[index];
            m_Field.SetValue(System.Enum.ToObject(m_Field.enumType, intValue));
        }

        public override bool OnSelection(bool fromNext, DebugUIHandlerWidget previous)
        {
            if (fromNext || valueToggle.isOn == false)
            {
                nameLabel.color = colorSelected;
            }
            else if (valueToggle.isOn)
            {
                if (m_Container.IsDirectChild(previous))
                {
                    nameLabel.color = colorSelected;
                }
                else
                {
                    var lastItem = m_Container.GetLastItem();
                    DebugManager.instance.ChangeSelection(lastItem, false);
                }
            }

            return true;
        }

        public override void OnDeselection()
        {
            nameLabel.color = colorDefault;
        }

        public override void OnIncrement(bool fast)
        {
            valueToggle.isOn = true;
        }

        public override void OnDecrement(bool fast)
        {
            valueToggle.isOn = false;
        }

        public override void OnAction()
        {
            valueToggle.isOn = !valueToggle.isOn;
        }

        public override DebugUIHandlerWidget Next()
        {
            if (!valueToggle.isOn || m_Container == null)
                return base.Next();

            var firstChild = m_Container.GetFirstItem();

            if (firstChild == null)
                return base.Next();

            return firstChild;
        }
    }
}
