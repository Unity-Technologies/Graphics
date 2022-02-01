using System;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface to define custom data for a searcher item.
    /// </summary>
    public interface ISearcherItemData
    {
    }

    /// <summary>
    /// Tag for specific searcher items.
    /// </summary>
    public enum CommonSearcherTags
    {
        StickyNote
    }

    /// <summary>
    /// Data for a Searcher Item tagged by a <see cref="CommonSearcherTags"/>.
    /// </summary>
    public readonly struct TagSearcherItemData : ISearcherItemData
    {
        public CommonSearcherTags Tag { get; }

        public TagSearcherItemData(CommonSearcherTags tag)
        {
            Tag = tag;
        }
    }

    /// <summary>
    /// Data for a Searcher Item linked to a type.
    /// </summary>
    public readonly struct TypeSearcherItemData : ISearcherItemData
    {
        public TypeHandle Type { get; }

        public TypeSearcherItemData(TypeHandle type)
        {
            Type = type;
        }
    }

    /// <summary>
    /// Data for a Searcher Item linked to a node.
    /// </summary>
    public readonly struct NodeSearcherItemData : ISearcherItemData
    {
        public Type Type { get; }

        /// <summary>
        /// Initializes a new instance of the NodeSearcherItemData class.
        /// </summary>
        /// <param name="type">Type of the node represented by the item.</param>
        public NodeSearcherItemData(Type type)
        {
            Type = type;
        }
    }
}
