using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Rendering.UI
{
    /// <summary>
    /// DebugUIHandler for container widget.
    /// </summary>
    [CoreRPHelpURL("Rendering-Debugger")]
    public class DebugUIHandlerContainer : MonoBehaviour
    {
        /// <summary>Content holder.</summary>
        [SerializeField]
        public RectTransform contentHolder;

        internal DebugUIHandlerWidget GetFirstItem()
        {
            if (contentHolder.childCount == 0)
                return null;

            var items = GetActiveChildren();

            if (items.Count == 0)
                return null;

            return items[0];
        }

        internal DebugUIHandlerWidget GetLastItem()
        {
            if (contentHolder.childCount == 0)
                return null;

            var items = GetActiveChildren();

            if (items.Count == 0)
                return null;

            return items[items.Count - 1];
        }

        internal bool IsDirectChild(DebugUIHandlerWidget widget)
        {
            if (contentHolder.childCount == 0)
                return false;

            return GetActiveChildren()
                .Count(x => x == widget) > 0;
        }

        List<DebugUIHandlerWidget> GetActiveChildren()
        {
            var list = new List<DebugUIHandlerWidget>();

            foreach (Transform t in contentHolder)
            {
                if (!t.gameObject.activeInHierarchy)
                    continue;

                if (t.TryGetComponent<DebugUIHandlerWidget>(out var c))
                    list.Add(c);
            }

            return list;
        }
    }
}
