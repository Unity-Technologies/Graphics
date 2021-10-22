using System.Collections;
using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for toggle with history widget.
    /// </summary>
    public class DebugUIHandlerToggleHistory : DebugUIHandlerToggle
    {
        Toggle[] historyToggles;
        const float k_XOffset = 230f;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            int historyDepth = (widget as DebugUI.HistoryBoolField)?.historyDepth ?? 0;
            historyToggles = new Toggle[historyDepth];
            float columnOffset = historyDepth > 0 ? k_XOffset / (float)historyDepth : 0f;
            for (int index = 0; index < historyDepth; ++index)
            {
                var historyToggle = Instantiate(valueToggle, transform);
                Vector3 pos = historyToggle.transform.position;
                pos.x += (index + 1) * columnOffset;
                historyToggle.transform.position = pos;
                var background = historyToggle.transform.GetChild(0).GetComponent<Image>();
                background.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(-1, -1, 2, 2), Vector2.zero);
                background.color = new Color32(50, 50, 50, 120);
                var checkmark = background.transform.GetChild(0).GetComponent<Image>();
                checkmark.color = new Color32(110, 110, 110, 255);
                historyToggles[index] = historyToggle.GetComponent<Toggle>();
            }

            //this call UpdateValueLabel which will rely on historyToggles
            base.SetWidget(widget);
        }

        /// <summary>
        /// Update the label.
        /// </summary>
        internal protected override void UpdateValueLabel()
        {
            base.UpdateValueLabel();
            DebugUI.HistoryBoolField field = m_Field as DebugUI.HistoryBoolField;
            int historyDepth = field?.historyDepth ?? 0;
            for (int index = 0; index < historyDepth; ++index)
            {
                if (index < historyToggles.Length && historyToggles[index] != null)
                    historyToggles[index].isOn = field.GetHistoryValue(index);
            }

            if (isActiveAndEnabled)
                StartCoroutine(RefreshAfterSanitization());
        }

        IEnumerator RefreshAfterSanitization()
        {
            yield return null; //wait one frame
            valueToggle.isOn = m_Field.getter();
        }
    }
}
