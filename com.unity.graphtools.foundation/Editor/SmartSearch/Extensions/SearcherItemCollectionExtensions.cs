using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods for collections of <see cref="SearcherItem"/>s.
    /// </summary>
    [PublicAPI]
    public static class SearcherItemCollectionExtensions
    {
        const string k_Enums = "Enumerations";
        const string k_Class = "Classes";
        const string k_Graphs = "Graphs";

        /// <summary>
        /// Adds a <see cref="SearcherItem"/> in a hierarchy at a certain path.
        /// </summary>
        /// <param name="items">Hierarchy where to add the item.</param>
        /// <param name="item">Item to add.</param>
        /// <param name="path">Path where to add the item.</param>
        public static void AddAtPath(this List<SearcherItem> items, SearcherItem item, string path = "")
        {
            if (!string.IsNullOrEmpty(path))
            {
                SearcherItem parent = items.GetItemFromPath(path);
                parent.AddChild(item);
            }
            else
            {
                items.Add(item);
            }
        }

        internal static bool TryAddEnumItem(
            this List<SearcherItem> items,
            SearcherItem itemToAdd,
            ITypeMetadata meta,
            string parentName = ""
        )
        {
            if (meta.IsEnum)
            {
                items.AddAtPath(itemToAdd, parentName + "/" + k_Enums);
                return true;
            }

            return false;
        }

        internal static bool TryAddClassItem(
            this List<SearcherItem> items,
            SearcherItem itemToAdd,
            ITypeMetadata meta,
            string parentName = ""
        )
        {
            if ((meta.IsClass || meta.IsValueType) && !meta.IsEnum)
            {
                var path = BuildPath(parentName + "/" + k_Class, meta);
                items.AddAtPath(itemToAdd, path);
                return true;
            }

            return false;
        }

        static string BuildPath(string parentName, ITypeMetadata meta)
        {
            return parentName + "/" + meta.Namespace.Replace(".", "/");
        }

        /// <summary>
        /// Finds a <see cref="SearcherItem"/> at a specific path in a hierarchy.
        /// </summary>
        /// <param name="items">Hierarchy of <see cref="SearcherItem"/>s to search.</param>
        /// <param name="path">Expected path of the item.</param>
        /// <returns>The item matching the path.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the item was not found.</exception>
        [NotNull]
        public static SearcherItem GetItemFromPath(this List<SearcherItem> items, string path)
        {
            Assert.IsFalse(string.IsNullOrEmpty(path));

            string[] hierarchy = path.Split('/');
            SearcherItem item = null;
            SearcherItem parent = null;

            for (var i = 0; i < hierarchy.Length; ++i)
            {
                string s = hierarchy[i];

                if (i == 0 && s == "/" || s == string.Empty)
                    continue;

                List<SearcherItem> children = parent != null ? parent.Children : items;
                item = children.Find(x => x.Name == s);

                if (item == null)
                {
                    item = new SearcherItem(s);

                    if (parent != null)
                    {
                        parent.AddChild(item);
                    }
                    else
                    {
                        children.Add(item);
                    }
                }

                parent = item;
            }

            return item ?? throw new InvalidOperationException(
                "[SearcherItemUtility.GetItemFromPath] : Returned item cannot be null"
            );
        }
    }
}
