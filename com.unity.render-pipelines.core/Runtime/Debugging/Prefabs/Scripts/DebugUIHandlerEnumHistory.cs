using System.Collections;
using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for enum with history widget.
    /// </summary>
    public class DebugUIHandlerEnumHistory : DebugUIHandlerEnumField
    {
        Text[] historyValues;
        const float k_XOffset = 230f;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            int historyDepth = (widget as DebugUI.HistoryEnumField)?.historyDepth ?? 0;
            historyValues = new Text[historyDepth];
            float columnOffset = historyDepth > 0 ? k_XOffset / (float)historyDepth : 0f;
            for (int index = 0; index < historyDepth; ++index)
            {
                var historyValue = Instantiate(valueLabel, transform);
                Vector3 pos = historyValue.transform.position;
                pos.x += (index + 1) * columnOffset;
                historyValue.transform.position = pos;
                var text = historyValue.GetComponent<Text>();
                text.color = new Color32(110, 110, 110, 255);
                historyValues[index] = text;
            }

            //this call UpdateValueLabel which will rely on historyToggles
            base.SetWidget(widget);
        }

        /// <summary>
        /// Update the label of the widget.
        /// </summary>
        public override void UpdateValueLabel()
        {
            int index = m_Field.currentIndex;

            // Fallback just in case, we may be handling sub/sectionned enums here
            if (index < 0)
                index = 0;

            valueLabel.text = m_Field.enumNames[index].text;

            DebugUI.HistoryEnumField field = m_Field as DebugUI.HistoryEnumField;
            int historyDepth = field?.historyDepth ?? 0;
            for (int indexHistory = 0; indexHistory < historyDepth; ++indexHistory)
            {
                if (indexHistory < historyValues.Length && historyValues[indexHistory] != null)
                    historyValues[indexHistory].text = field.enumNames[field.GetHistoryValue(indexHistory)].text;
            }

            if (isActiveAndEnabled)
                StartCoroutine(RefreshAfterSanitization());
        }

        IEnumerator RefreshAfterSanitization()
        {
            yield return null; //wait one frame
            m_Field.currentIndex = m_Field.getIndex();
            valueLabel.text = m_Field.enumNames[m_Field.currentIndex].text;
        }
    }
}
