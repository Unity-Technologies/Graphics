using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods to create <see cref="SearcherDatabase"/> for types.
    /// </summary>
    [PublicAPI]
    public static class TypeSearcherExtensions
    {
        /// <summary>
        /// Creates a <see cref="SearcherDatabase"/> for types.
        /// </summary>
        /// <param name="types">Types to create the database from.</param>
        /// <returns>A database containing the types passed in parameter.</returns>
        public static SearcherDatabase ToSearcherDatabase(this IEnumerable<Type> types)
        {
            List<SearcherItem> searcherItems = new List<SearcherItem>();
            foreach (var type in types)
            {
                var typeMetadata = new TypeMetadata(type.GenerateTypeHandle(), type);
                var classItem = new TypeSearcherItem(type.GenerateTypeHandle(), typeMetadata.FriendlyName);
                searcherItems.TryAddClassItem(classItem, typeMetadata);
            }
            return new SearcherDatabase(searcherItems);
        }
    }
}
