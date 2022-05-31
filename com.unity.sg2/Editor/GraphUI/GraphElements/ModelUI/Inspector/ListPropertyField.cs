using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class SGListViewController : ListViewController
    {
        public override void AddItems(int itemCount)
        {
            RaiseItemsAdded(new List<int>());
        }
    }

    class ListPropertyField<T> : BaseModelPropertyField
    {
        /// <summary>
        /// ListView this PropertyField wraps around
        /// </summary>
        public ListView listView => m_ListView;

        ListView m_ListView = new();

        /// <summary>
        /// Callback to populate the dropdown menu options when the '+' footer button to add an item is clicked
        /// </summary>
        Func<IList<string>> m_GetAddItemOptions;

        /// <summary>
        /// Callback to invoke when the list needs to display a string label for a list item
        /// </summary>
        Func<object, string> m_GetItemDisplayName;

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

        public ListPropertyField(
            ICommandTarget commandTarget,
            IList<T> listItems,
            Func<IList<string>> getAddItemOptions,
            Func<object, string> getItemDisplayName,
            GenericMenu.MenuFunction2 onAddItemClicked,
            Action<IEnumerable<object>> onSelectionChanged,
            Action onItemRemoved,
            bool makeOptionsUnique,
            bool makeListReorderable)
            : base(commandTarget)
        {
            /* Setup the ListView */

            m_ListView.SetViewController(new SGListViewController());

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
            m_ListView.selectionType = SelectionType.Single;
            m_ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            m_ListView.showAddRemoveFooter = true;

            m_ListView.reorderable = makeListReorderable;
            if (makeListReorderable)
            {
                m_ListView.reorderMode = ListViewReorderMode.Animated;
            }

            m_ListView.selectionChanged += onSelectionChanged;
            m_ListView.itemsAdded += OnItemsAdded;
            m_ListView.itemsRemoved += OnItemsRemoved;

            Add(m_ListView);

            /* Store references to callbacks and other info. needed later */

            m_GetAddItemOptions = getAddItemOptions;
            m_GetItemDisplayName = getItemDisplayName;
            m_OnAddItemClicked = onAddItemClicked;
            m_OnItemRemoved = onItemRemoved;

            m_MakeOptionsUnique = makeOptionsUnique;

            m_ListItems = listItems;
        }

        void OnItemsAdded(IEnumerable<int> obj)
        {
            // Create the dropdown menu, add items and show it
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
