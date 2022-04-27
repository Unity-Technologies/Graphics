using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ReorderableListView<T> : BaseModelPropertyField
    {
        public ListView listView => m_ListView;
        ListView m_ListView = new();

        Func<IList<T>> m_GetAddItemOptions;

        Func<object, string> m_GetItemDisplayName;

        GenericMenu.MenuFunction2 m_OnAddItemClicked;

        Action m_OnItemRemoved;

        bool m_MakeOptionsUnique;

        IList<T> m_ListItems;

        public ReorderableListView(
            ICommandTarget commandTarget,
            IList<T> listItems,
            Func<IList<T>> getAddItemOptions,
            Func<object, string> getItemDisplayName,
            GenericMenu.MenuFunction2 onAddItemClicked,
            Action<IEnumerable<object>> onSelectionChanged,
            Action onItemRemoved,
            bool makeOptionsUnique)
            : base(commandTarget)
        {
            // The "makeItem" function will be called as needed
            // when the ListView needs more items to render
            Func<VisualElement> makeItem = () => new Label();

            // As the user scrolls through the list, the ListView object
            // will recycle elements created by the "makeItem"
            // and invoke the "bindItem" callback to associate
            // the element with the matching data item (specified as an index in the list)
            Action<VisualElement, int> bindItem = (e, i) =>
            {
                ((Label)e).text = listItems.Count == 0 ? "Empty" : getItemDisplayName(listItems[i]);
            };

            m_ListView.makeItem = makeItem;
            m_ListView.bindItem = bindItem;
            m_ListView.itemsSource = listItems.ToList();
            m_ListView.selectionType = SelectionType.Multiple;

            // Callback invoked when the user changes the selection inside the ListView
            m_ListView.selectionChanged += onSelectionChanged;

            m_ListView.itemsAdded += OnItemsAdded;
            m_ListView.itemsRemoved += OnItemsRemoved;
            m_ListView.reorderable = true;
            m_ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            m_ListView.showAddRemoveFooter = true;
            m_ListView.reorderMode = ListViewReorderMode.Animated;

            Add(m_ListView);

            m_GetAddItemOptions = getAddItemOptions;
            m_GetItemDisplayName = getItemDisplayName;
            m_OnAddItemClicked = onAddItemClicked;
            m_OnItemRemoved = onItemRemoved;

            m_MakeOptionsUnique = makeOptionsUnique;

            m_ListItems = listItems;
        }

        void OnItemsAdded(IEnumerable<int> obj)
        {
            // Now create the menu, add items and show it
            GenericMenu menu = new GenericMenu();

            var addItemOptions = m_GetAddItemOptions();

            if (m_MakeOptionsUnique)
            {
                foreach (var item in addItemOptions)
                {
                    var existsAlready = m_ListItems.Any(existingItem => m_GetItemDisplayName(existingItem) == m_GetItemDisplayName(item));
                    if(!existsAlready)
                        menu.AddItem(new GUIContent(m_GetItemDisplayName(item)), false, m_OnAddItemClicked, userData: item);
                    else
                        menu.AddDisabledItem(new GUIContent(m_GetItemDisplayName(item)), false);
                }
            }
            else
            {
                foreach (var item in addItemOptions)
                {
                    menu.AddItem(new GUIContent(m_GetItemDisplayName(item)), false, m_OnAddItemClicked, userData: item);
                }
            }

            menu.ShowAsContext();
        }

        void OnItemsRemoved(IEnumerable<int> obj)
        {
            m_OnItemRemoved();
        }

        public override bool UpdateDisplayedValue()
        {
            Debug.Log("Update Displayed Value");
            return true;
        }
    }
}
