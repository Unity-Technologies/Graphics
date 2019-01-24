using UnityEngine.UI;

namespace UnityEngine.Experimental.Rendering.UI
{
    public class DebugUIHandlerToggleHistory : DebugUIHandlerToggle
    {
        Toggle[] historyToggles;
        const float xDecal = 60f;

        internal override void SetWidget(DebugUI.Widget widget)
        {
            int historyDepth = (widget as DebugUI.HistoryBoolField)?.historyDepth ?? 0;
            historyToggles = new Toggle[historyDepth];
            for (int index = 0; index < historyDepth; ++index)
            {
                var historyToggle = Instantiate(valueToggle, transform);
                Vector3 pos = historyToggle.transform.position;
                pos.x += (index + 1) * xDecal;
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

        protected override void UpdateValueLabel()
        {
            base.UpdateValueLabel();
            DebugUI.HistoryBoolField field = m_Field as DebugUI.HistoryBoolField;
            int historyDepth = field?.historyDepth ?? 0;
            for (int index = 0; index < historyDepth; ++index)
            {
                if (index < historyToggles.Length && historyToggles[index] != null)
                    historyToggles[index].isOn = field.GetHistoryValue(index);
            }
        }
    }
}
