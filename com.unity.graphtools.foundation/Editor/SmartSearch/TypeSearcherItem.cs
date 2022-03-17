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
        /// <param name="name">The name used to search the item.</param>
        /// <param name="type">The type represented by the item.</param>=
        public TypeSearcherItem(string name, TypeHandle type)
            :base(name)
        {
            Data = new TypeSearcherItemData(type);
        }
    }
}
