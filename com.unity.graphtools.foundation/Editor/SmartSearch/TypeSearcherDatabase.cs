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
        const string k_Class = "Classes";

        /// <summary>
        /// Creates a <see cref="SearcherDatabase"/> for types.
        /// </summary>
        /// <param name="types">Types to create the database from.</param>
        /// <returns>A database containing the types passed in parameter.</returns>
        public static SearcherDatabase ToSearcherDatabase(this IEnumerable<Type> types)
        {
            var searcherItems = new List<SearcherItem>();
            foreach (var type in types)
            {
                var typeHandle = type.GenerateTypeHandle();
                var meta = new TypeMetadata(typeHandle, type);
                if ((meta.IsClass || meta.IsValueType) && !meta.IsEnum)
                {
                    var path = k_Class + "/" + meta.Namespace.Replace(".", "/");
                    var classItem = new TypeSearcherItem(meta.FriendlyName, typeHandle) { CategoryPath = path};
                    searcherItems.Add(classItem);
                }
            }
            return new SearcherDatabase(searcherItems);
        }
    }
}
