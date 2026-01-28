#if ENABLE_UIELEMENTS_MODULE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
#define ENABLE_RENDERING_DEBUGGER_UI
#endif

#if ENABLE_RENDERING_DEBUGGER_UI

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering
{
    using ScheduledItemsDictionary = Dictionary<DebugUI.Widget, List<IVisualElementScheduledItem>>;

    public sealed partial class DebugManager
    {
        // This class keeps track of all VisualElementScheduledItems created by DebugUI widgets so they can be paused when the content is not visible,
        // and resumed when it becomes visible again. This greatly speeds up the UI because we don't process schedulers in inactive panels and closed foldouts.
        internal class SchedulerTracker
        {
            readonly ScheduledItemsDictionary[] m_ScheduledItems;

            public SchedulerTracker()
            {
                int numDictionaries = (int)DebugUI.Context.Count;
                m_ScheduledItems = new ScheduledItemsDictionary[numDictionaries];
                for (int i = 0; i < numDictionaries; i++)
                    m_ScheduledItems[i] = new ScheduledItemsDictionary();
            }

            internal ScheduledItemsDictionary GetScheduledItemsDictionary(DebugUI.Context context) => m_ScheduledItems[(int)context];

            public void SetEnabled(DebugUI.Context context, DebugUI.Widget widget, bool enabled)
            {
                var dict = GetScheduledItemsDictionary(context);
                if (dict.TryGetValue(widget, out var scheduledItems))
                {
                    foreach (var scheduledItem in scheduledItems)
                    {
                        if (enabled)
                            scheduledItem.Resume();
                        else
                            scheduledItem.Pause();
                    }
                }
            }

            // Any time a widget constructs internal widgets (like containers, tuples, history fields), those widgets need to be handled
            // here to ensure their schedulers are enabled/disabled when needed.
            void UpdateSchedulersRecursive(DebugUI.Context context, DebugUI.IContainer container, bool enabled)
            {
                foreach (var child in container.children)
                {
                    SetEnabled(context, child, enabled);

                    if (child is DebugUI.IContainer childContainer)
                    {
                        bool childContainerEnabled = enabled;
                        if (child is DebugUI.Foldout childFoldout)
                            childContainerEnabled = enabled && childFoldout.opened;
                        UpdateSchedulersRecursive(context, childContainer, childContainerEnabled);
                    }

                    if (child is DebugUI.ValueTuple childTuple)
                    {
                        foreach (var value in childTuple.values)
                            SetEnabled(context, value, enabled);
                    }

                    if (child is DebugUI.HistoryBoolField childHistoryBoolField)
                    {
                        foreach (var value in childHistoryBoolField.childWidgets)
                            SetEnabled(context, value, enabled);
                    }

                    if (child is DebugUI.HistoryEnumField childHistoryEnumField)
                    {
                        foreach (var value in childHistoryEnumField.childWidgets)
                            SetEnabled(context, value, enabled);
                    }
                }
            }

            public void SetHierarchyEnabled(DebugUI.Context context, DebugUI.IContainer container, bool enabled)
            {
                UpdateSchedulersRecursive(context, container, enabled);
            }

            public void Add(DebugUI.Context context, DebugUI.Widget widget, IVisualElementScheduledItem scheduledItem)
            {
                var dict = GetScheduledItemsDictionary(context);
                if (!dict.TryGetValue(widget, out var widgetScheduledItemList))
                {
                    widgetScheduledItemList = new List<IVisualElementScheduledItem>();
                    dict.Add(widget, widgetScheduledItemList);
                }
                widgetScheduledItemList.Add(scheduledItem);
            }

            public void Remove(DebugUI.Context context, DebugUI.Widget widget, VisualElement removedVisualElement)
            {
                var dict = GetScheduledItemsDictionary(context);
                if (dict.TryGetValue(widget, out var widgetScheduledItemList))
                {
                    for (int i = widgetScheduledItemList.Count - 1; i >= 0; i--)
                    {
                        var scheduledItem = widgetScheduledItemList[i];
                        if (scheduledItem.element == removedVisualElement)
                        {
                            scheduledItem.Pause();
                            widgetScheduledItemList.RemoveAt(i);
                        }
                    }
                    if (widgetScheduledItemList.Count == 0)
                        dict.Remove(widget);
                }
            }
        }
    }
}

#endif
