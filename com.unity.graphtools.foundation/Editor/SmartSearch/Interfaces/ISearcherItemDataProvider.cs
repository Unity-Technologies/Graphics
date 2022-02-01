using UnityEditor.GraphToolsFoundation.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface to provide custom data in a <see cref="SearcherItem"/>
    /// </summary>
    public interface ISearcherItemDataProvider
    {
        ISearcherItemData Data { get; }
    }
}
