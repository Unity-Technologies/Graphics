using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// <see cref="SearcherItem"/> representing a Type.
    /// </summary>
    public sealed class TypeSearcherItem : SearcherItem, ISearcherItemDataProvider
    {
        /// <summary>
        /// <see cref="TypeHandle"/> of the item.
        /// </summary>
        public TypeHandle Type => ((TypeSearcherItemData)Data).Type;

        /// <summary>
        /// Custom data for the searcher item.
        /// </summary>
        public ISearcherItemData Data { get; }

        /// <summary>
        /// Initializes a new instance of the TypeSearcherItem class.
        /// </summary>
        /// <param name="type">The type represented by the item.</param>
        /// <param name="name">The name to give this item in the search.</param>
        /// <param name="children">Other items to nest under this one in the search</param>
        public TypeSearcherItem(TypeHandle type, string name, List<SearcherItem> children = null)
            : base(name, string.Empty, children)
        {
            Data = new TypeSearcherItemData(type);
        }
    }
}
