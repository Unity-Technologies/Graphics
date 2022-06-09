using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class TargetSettingsListPropertyField<T> : ListPropertyField
    {
        /// <summary>
        /// Callback to populate the dropdown menu options when the '+' footer button to add an item is clicked
        /// </summary>
        Func<IList<object>> m_GetAddItemData;

        /// <summary>
        /// Callback to invoke when the list needs to display a string label for a list item
        /// </summary>
        Func<object, string> m_GetAddItemMenuString;

        /// <summary>
        /// Callback to invoke when an item is selected from the dropdown menu, to add to the list
        /// </summary>
        GenericMenu.MenuFunction2 m_OnAddItemClicked;

        /// <summary>
        /// Callback to invoke when an item is removed from the list using the '-' footer button
        /// </summary>
        Action m_OnItemRemoved;

        /// <summary>
        /// Whether the list should allow duplicates
        /// </summary>
        bool m_MakeOptionsUnique;

        /// <summary>
        /// Reference to the actual list itself
        /// </summary>
        IList<T> m_ListItems;

        public TargetSettingsListPropertyField(
            ICommandTarget commandTarget,
            IList<T> listItems,
            Func<IList<object>> getAddItemData,
            Func<object, string> getAddItemMenuString,
            GenericMenu.MenuFunction2 onAddItemClicked,
            Action<IEnumerable<object>> onSelectionChanged,
            Action onItemRemoved,
            bool makeOptionsUnique,
            bool makeListReorderable)
            : base(
                commandTarget,
                (IList) listItems,
                makeItem: () => new Label(),
                bindItem: (e, i) => ((Label)e).text = listItems.Count == 0 ? "Empty" : getAddItemMenuString(listItems[i]))
        {
            m_ListItems = listItems;

            listView.reorderable = makeListReorderable;
            if (makeListReorderable)
            {
                listView.reorderMode = ListViewReorderMode.Animated;
            }

            listView.selectionChanged += onSelectionChanged;
            listView.itemsAdded += OnItemsAdded;
            listView.itemsRemoved += OnItemsRemoved;

            Add(listView);

            /* Store references to callbacks and other info. needed later */

            m_GetAddItemData = getAddItemData;
            m_GetAddItemMenuString = getAddItemMenuString;
            m_OnAddItemClicked = onAddItemClicked;
            m_OnItemRemoved = onItemRemoved;

            m_MakeOptionsUnique = makeOptionsUnique;

            m_ListItems = listItems;
        }

        void OnItemsAdded(IEnumerable<int> obj)
        {
            // Create the dropdown menu, add items and show it
            GenericMenu menu = new GenericMenu();

            var addItemOptions = m_GetAddItemData();

            if (m_MakeOptionsUnique)
            {
                foreach (var item in addItemOptions)
                {
                    var existsAlready = m_ListItems.Any(existingItem => m_GetAddItemMenuString(existingItem) == m_GetAddItemMenuString(item));
                    if (!existsAlready)
                        menu.AddItem(new GUIContent(m_GetAddItemMenuString(item)), false, m_OnAddItemClicked, userData: item);
                    else
                        menu.AddDisabledItem(new GUIContent(m_GetAddItemMenuString(item)), false);
                }
            }
            else
            {
                foreach (var item in addItemOptions)
                {
                    menu.AddItem(new GUIContent(m_GetAddItemMenuString(item)), false, m_OnAddItemClicked, userData: item);
                }
            }

            menu.ShowAsContext();
        }

        void OnItemsRemoved(IEnumerable<int> obj)
        {
            m_OnItemRemoved();
        }
    }
}
