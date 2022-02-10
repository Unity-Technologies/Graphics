using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    // SearcherTreeUtility contains a helper function that takes a flat list of SearcherItems and constructs a SearcherItems tree using their names as paths.
    //
    // For example:
    // List<SearcherItem> items = new List<SearcherItem>();
    // items.Add(new SearcherItem("Fantasy/J. R. R. Tolkien/The Fellowship of the Ring"));
    // items.Add(new SearcherItem("Fantasy/J. R. R. Tolkien/The Two Towers"));
    // items.Add(new SearcherItem("Fantasy/J. R. R. Tolkien/The Return of the King"));
    // items.Add(new SearcherItem("Health & Fitness/Becoming a Supple Leopard"));
    // items.Add(new SearcherItem("Some Uncategorized Book"));
    //
    // List<SearcherItem> itemsTree = SearcherTreeUtility.CreateFromFlatList(items);
    //
    // Will return the follow hierarchy:
    // - Fantasy
    // - - J. R. R. Tolkien
    // - - - The Fellowship of the Ring
    // - - - The Two Towers
    // - - - The Return of the King
    // - Health & Fitness
    // - - Becoming a Supple Leopard
    // - Some Uncategorized Book
    //
    // Where the first level of SearcherItems is directly inside the list.
    // Note that this will also break the names into their final path component.
    [PublicAPI]
    public static class SearcherTreeUtility
    {
        public static List<SearcherItem> CreateFromFlatList(List<SearcherItem> items)
        {
            List<SearcherItem> searchList = new List<SearcherItem>();
            for (int i = 0; i < items.Count; ++i)
            {
                SearcherItem item = items[i];
                string[] pathParts = item.Name.Split('/');
                SearcherItem searchNode = FindNodeByName(searchList, pathParts[0]);
                if (searchNode == null)
                {
                    searchNode = new SearcherItem(pathParts[0]);
                    searchList.Add(searchNode);
                }
                AddItem(searchNode, item, pathParts);
            }
            return searchList;
        }

        private static void AddItem(SearcherItem root, SearcherItem item, string[] pathParts)
        {
            string itemFullPath = item.Name;
            string itemName = pathParts[pathParts.Length - 1];
            string currentPath = string.Empty;
            SearcherItem currentSearchNode = root;

            for (int i = 1; i < pathParts.Length; ++i)
            {
                SearcherItem node = FindNodeByName(currentSearchNode.Children, pathParts[i]);
                if (node == null)
                {
                    node = new SearcherItem(pathParts[i]);
                    currentSearchNode.AddChild(node);
                }
                currentSearchNode = node;
            }
            // Set the user data to the final node, which is guaranteed to correspond to the item.
            currentSearchNode.UserData = item.UserData;
            currentSearchNode.Icon = item.Icon;
        }

        private static SearcherItem FindNodeByName(IList<SearcherItem> searchList, string name)
        {
            for (int i = 0; i < searchList.Count; ++i)
            {
                if (searchList[i].Name.Equals(name))
                {
                    return searchList[i];
                }
            }
            return null;
        }
    }
}
