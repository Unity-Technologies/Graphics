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

        GameObject GetChild(int index)
        {
            if (index < 0)
                return null;

            if (gameObject.transform != null)
            {
                var firstChild = gameObject.transform.GetChild(1);
                if (firstChild != null && firstChild.childCount > index)
                {
                    return firstChild.GetChild(index).gameObject;
                }
            }

            return null;
        }

        bool TryGetChild(int index, out GameObject child)
        {
            child = GetChild(index);
            return child != null;
        }

        bool IsActive(DebugUI.Table table, int index, GameObject child)
        {
            if (!table.GetColumnVisibility(index))
                return false;

            var valueChild = child.transform.Find("Value");
            if (valueChild != null && valueChild.TryGetComponent<Text>(out var text))
                return !string.IsNullOrEmpty(text.text);

            return true;
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
                if (!TryGetChild(i, out var child))
                    continue;

                bool active = IsActive(table, i, child);
                if (child != null)
                    child.SetActive(active);
                if (active && refreshRow)
                {
                    if (child.TryGetComponent<DebugUIHandlerColor>(out var color))
                        color.UpdateColor();
                    if (child.TryGetComponent<DebugUIHandlerToggle>(out var toggle))
                        toggle.UpdateValueLabel();
                    if (child.TryGetComponent<DebugUIHandlerObjectList>(out var list))
                        list.UpdateValueLabel();
                }
            }

            // Update previous and next ui handlers to skip hidden volumes
            var itemWidget = GetChild(0).GetComponent<DebugUIHandlerWidget>();
            DebugUIHandlerWidget previous = null;
            for (int i = 0; i < row.children.Count; i++)
            {
                itemWidget.previousUIHandler = previous;
                if (!TryGetChild(i, out var child))
                    continue;

                if (IsActive(table, i, child))
                    previous = itemWidget;

                bool found = false;
                for (int j = i + 1; j < row.children.Count; j++)
                {
                    if (!TryGetChild(j, out var innerChild))
                        continue;

                    if (IsActive(table, j, innerChild))
                    {
                        var childWidget = child.GetComponent<DebugUIHandlerWidget>();
                        itemWidget.nextUIHandler = childWidget;
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
