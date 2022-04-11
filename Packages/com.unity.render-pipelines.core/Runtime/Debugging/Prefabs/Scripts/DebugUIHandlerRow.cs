using UnityEngine.UI;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for row widget.
    /// </summary>
    public class DebugUIHandlerRow : DebugUIHandlerFoldout
    {
        float m_Timer;

        /// <summary>
        /// OnEnable implementation.
        /// </summary>
        protected override void OnEnable()
        {
            m_Timer = 0f;
        }

        /// <summary>
        /// Update implementation.
        /// </summary>
        protected void Update()
        {
            var row = CastWidget<DebugUI.Table.Row>();
            var table = row.parent as DebugUI.Table;

            float refreshRate = 0.1f;
            bool refreshRow = m_Timer >= refreshRate;
            if (refreshRow)
                m_Timer -= refreshRate;
            m_Timer += Time.deltaTime;

            for (int i = 0; i < row.children.Count; i++)
            {
                var child = gameObject.transform.GetChild(1).GetChild(i).gameObject;
                var active = table.GetColumnVisibility(i);
                child.SetActive(active);
                if (active && refreshRow)
                {
                    if (child.TryGetComponent<DebugUIHandlerColor>(out var color))
                        color.UpdateColor();
                    if (child.TryGetComponent<DebugUIHandlerToggle>(out var toggle))
                        toggle.UpdateValueLabel();
                }
            }

            // Update previous and next ui handlers to pass over hidden volumes
            var item = gameObject.transform.GetChild(1).GetChild(0).gameObject;
            var itemWidget = item.GetComponent<DebugUIHandlerWidget>();
            DebugUIHandlerWidget previous = null;
            for (int i = 0; i < row.children.Count; i++)
            {
                itemWidget.previousUIHandler = previous;
                if (table.GetColumnVisibility(i))
                    previous = itemWidget;

                bool found = false;
                for (int j = i + 1; j < row.children.Count; j++)
                {
                    if (table.GetColumnVisibility(j))
                    {
                        var child = gameObject.transform.GetChild(1).GetChild(j).gameObject;
                        var childWidget = child.GetComponent<DebugUIHandlerWidget>();
                        itemWidget.nextUIHandler = childWidget;
                        item = child;
                        itemWidget = childWidget;
                        i = j - 1;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    itemWidget.nextUIHandler = null;
                    break;
                }
            }
        }
    }
}
