using System.Collections.Generic;
using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for Bitfield widget.
    /// </summary>
    public class DebugUIHandlerBitField : DebugUIHandlerWidget
    {
        /// <summary>Name of the widget.</summary>
        public Text nameLabel;
        /// <summary>Value toggle.</summary>
        public UIFoldout valueToggle;

        /// <summary>Toggles for the bitfield.</summary>
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

        /// <summary>
        /// OnSelection implementation.
        /// </summary>
        /// <param name="fromNext">True if the selection wrapped around.</param>
        /// <param name="previous">Previous widget.</param>
        /// <returns>True if the selection is allowed.</returns>
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

        /// <summary>
        /// OnDeselection implementation.
        /// </summary>
        public override void OnDeselection()
        {
            nameLabel.color = colorDefault;
        }

        /// <summary>
        /// OnIncrement implementation.
        /// </summary>
        /// <param name="fast">True if incrementing fast.</param>
        public override void OnIncrement(bool fast)
        {
            valueToggle.isOn = true;
        }

        /// <summary>
        /// OnDecrement implementation.
        /// </summary>
        /// <param name="fast">Trye if decrementing fast.</param>
        public override void OnDecrement(bool fast)
        {
            valueToggle.isOn = false;
        }

        /// <summary>
        /// OnAction implementation.
        /// </summary>
        public override void OnAction()
        {
            valueToggle.isOn = !valueToggle.isOn;
        }

        /// <summary>
        /// Next implementation.
        /// </summary>
        /// <returns>Next widget UI handler, parent if there is none.</returns>
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
