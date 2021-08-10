using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// Separate persistent DebugUIHandler for value tuple widget.
    /// </summary>
    public class DebugUIHandlerValueTuplePersistent : DebugUIHandlerValueTuple
    {
        internal override void SetWidget(DebugUI.Widget widget)
        {
            m_Widget = widget;
            m_Field = CastWidget<DebugUI.ValueTuple>();
            nameLabel.text = m_Field.displayName;

            int numElements = 1;
            valueElements = new Text[numElements];
            valueElements[0] = valueLabel;
        }

        internal override void UpdateValueLabels()
        {
            string text = "";
            for (int index = 0; index < m_Field.numElements; ++index)
            {
                text += $"{m_Field.values[index].GetValueString()} / ";
            }
            if (text.Length > 3)
                text = text.Remove(text.Length - 3, 3); // remove last " / "
            valueElements[0].text = text;
        }
    }
}
