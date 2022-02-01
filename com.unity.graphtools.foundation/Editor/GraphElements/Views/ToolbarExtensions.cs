using System;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods related to toolbar elements.
    /// </summary>
    public static class ToolbarExtensions
    {
        public static void CreateOrUpdateItem(this ToolbarBreadcrumbs breadcrumbs, int index, string itemLabel, Action<int> clickedEvent)
        {
            if (index >= breadcrumbs.childCount)
            {
                breadcrumbs.PushItem(itemLabel, () =>
                {
                    int i = index;
                    clickedEvent?.Invoke(i);
                });
            }
            else
            {
                if (breadcrumbs.ElementAt(index) is ToolbarButton item)
                {
                    item.text = itemLabel;
                }
                else
                {
                    Debug.LogError("Trying to update an element that is not a ToolbarButton");
                }
            }
        }

        public static void TrimItems(this ToolbarBreadcrumbs breadcrumbs, int countToKeep)
        {
            while (breadcrumbs.childCount > countToKeep)
                breadcrumbs.PopItem();
        }

        public static void ChangeClickEvent(this ToolbarButton button, Action newClickEvent)
        {
            if (button.clickable != null)
                button.RemoveManipulator(button.clickable);

            if (newClickEvent != null)
            {
                button.clickable = new Clickable(newClickEvent);
                button.AddManipulator(button.clickable);
            }
            else
            {
                button.clickable = null;
            }
        }
    }
}
