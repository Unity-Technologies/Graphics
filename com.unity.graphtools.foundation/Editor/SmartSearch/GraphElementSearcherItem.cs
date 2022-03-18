using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Graph node creation data used by the searcher.
    /// </summary>
    public readonly struct GraphNodeCreationData : IGraphNodeCreationData
    {
        /// <summary>
        /// The interface to the graph where we want the node to be created in.
        /// </summary>
        public IGraphModel GraphModel { get; }

        /// <summary>
        /// The position at which the node should be created.
        /// </summary>
        public Vector2 Position { get; }

        /// <summary>
        /// The flags specifying how the node is to be spawned.
        /// </summary>
        public SpawnFlags SpawnFlags { get; }

        /// <summary>
        /// The SerializableGUID to assign to the newly created item.
        /// </summary>
        public SerializableGUID Guid { get; }

        /// <summary>
        /// Initializes a new GraphNodeCreationData.
        /// </summary>
        /// <param name="graphModel">The interface to the graph where we want the node to be created in.</param>
        /// <param name="position">The position at which the node should be created.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        public GraphNodeCreationData(IGraphModel graphModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, SerializableGUID guid = default)
        {
            GraphModel = graphModel;
            Position = position;
            SpawnFlags = spawnFlags;
            Guid = guid;
        }
    }

    /// <summary>
    /// Searcher Item allowing to create a <see cref="IGraphElementModel"/> in a Graph.
    /// </summary>
    public class GraphNodeModelSearcherItem : SearcherItem, ISearcherItemDataProvider
    {
        /// <inheritdoc />
        public override string Name => GetName != null ? GetName.Invoke() : base.Name;

        /// <summary>
        /// Function to create a <see cref="IGraphElementModel"/> in the graph from the Searcher Item.
        /// </summary>
        public Func<IGraphNodeCreationData, IGraphElementModel> CreateElement { get; }

        /// <summary>
        /// Custom Data for the Searcher Item.
        /// </summary>
        public ISearcherItemData Data { get; }

        /// <summary>
        /// Function providing the item name to show in the searcher dynamically.
        /// </summary>
        public Func<string> GetName { get; set; }

        /// <summary>
        /// Instantiates a <see cref="GraphNodeModelSearcherItem"/>.
        /// </summary>
        /// <param name="name">Name used to find the item in the searcher.</param>
        /// <param name="data">Custom data for the searcher item.</param>
        /// <param name="createElement">Function to create the element in the graph.</param>
        public GraphNodeModelSearcherItem(
            string name,
            ISearcherItemData data,
            Func<IGraphNodeCreationData, IGraphElementModel> createElement
        ) : base(name)
        {
            Data = data;
            CreateElement = createElement;
        }

        /// <summary>
        /// Instantiates a <see cref="GraphNodeModelSearcherItem"/>.
        /// </summary>
        /// <param name="data">Custom data for the searcher item.</param>
        /// <param name="createElement">Function to create the element in the graph.</param>
        public GraphNodeModelSearcherItem(
            ISearcherItemData data,
            Func<IGraphNodeCreationData, IGraphElementModel> createElement
        )
        {
            Data = data;
            CreateElement = createElement;
        }

        /// <summary>
        /// Instantiates a <see cref="GraphNodeModelSearcherItem"/>.
        /// </summary>
        /// <param name="graphModel"><see cref="IGraphModel"/> where graph element should be created.</param>
        /// <param name="data">Custom SearcherItem data.</param>
        /// <param name="createElement">Function to create the element in the graph.</param>
        /// <param name="getName">Function providing the item name to show in the searcher.</param>
        /// <param name="children">Other Searcher Items nested under this one.</param>
        /// <param name="help">Help text for the searcher item.</param>
        [Obsolete("Graphmodel isn't needed anymore - SearcherItems can't have children anymore, see SearcherItem ctor (2021-09-21).")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        public GraphNodeModelSearcherItem(
            IGraphModel graphModel,
            ISearcherItemData data,
            Func<IGraphNodeCreationData, IGraphElementModel> createElement,
            Func<string> getName,
            List<SearcherItem> children = null,
            string help = null
        ) : this(data, createElement)
        {
        }

        /// <summary>
        /// Instantiates a <see cref="GraphNodeModelSearcherItem"/>.
        /// </summary>
        /// <param name="graphModel"><see cref="IGraphModel"/> where graph element should be created.</param>
        /// <param name="data">Custom SearcherItem data.</param>
        /// <param name="createElement">Function to create the element in the graph.</param>
        /// <param name="name">Name of the item to show in the searcher.</param>
        /// <param name="children">Other Searcher Items nested under this one.</param>
        /// <param name="getName">Function providing the item name to show in the searcher.</param>
        /// <param name="help">Help text for the searcher item.</param>
        [Obsolete("Graphmodel isn't needed anymore - SearcherItems can't have children anymore, see SearcherItem ctor (2021-09-21).")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        public GraphNodeModelSearcherItem(
            IGraphModel graphModel,
            ISearcherItemData data,
            Func<IGraphNodeCreationData, IGraphElementModel> createElement,
            string name,
            List<SearcherItem> children = null,
            Func<string> getName = null,
            string help = null
        )  : this(data, createElement)
        {
        }
    }
}
