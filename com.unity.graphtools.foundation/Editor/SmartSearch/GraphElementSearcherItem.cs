using System;
using System.Collections.Generic;
using System.Linq;
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
        public override string Name => m_GetName != null ? m_GetName.Invoke() : m_Name;

        /// <summary>
        /// Function to create a <see cref="IGraphElementModel"/> in the graph from the Searcher Item.
        /// </summary>
        public Func<IGraphNodeCreationData, IGraphElementModel[]> CreateElements { get; }

        /// <summary>
        /// Custom Data for the Searcher Item.
        /// </summary>
        public ISearcherItemData Data { get; }

        readonly Func<string> m_GetName;
        readonly string m_Name;
        readonly IGraphModel m_GraphModel;

        /// <summary>
        /// Instantiates a <see cref="GraphNodeModelSearcherItem"/>.
        /// </summary>
        /// <param name="data">Custom data for the searcher item.</param>
        /// <param name="createElement">Function to create the element in the graph.</param>
        /// <param name="getName">Function providing the item name to show in the searcher.</param>
        /// <param name="children">Other Searcher Items nested under this one.</param>
        /// <param name="help">Help text for the searcher item.</param>
        [Obsolete("Use a constructor that provides a GraphModel to GraphNodeModelSearcherItem to allow building index. Added in 0.10.1+ (2021-06-03).")]
        public GraphNodeModelSearcherItem(
            ISearcherItemData data,
            Func<IGraphNodeCreationData, IGraphElementModel> createElement,
            Func<string> getName,
            List<SearcherItem> children = null,
            string help = null
        ) : this(null, data, createElement, getName(), children, getName, help)
        {
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
        public GraphNodeModelSearcherItem(
            IGraphModel graphModel,
            ISearcherItemData data,
            Func<IGraphNodeCreationData, IGraphElementModel> createElement,
            Func<string> getName,
            List<SearcherItem> children = null,
            string help = null
        ) : this(graphModel, data, createElement, getName(), children, getName, help)
        {
        }

        /// <summary>
        /// Instantiates a <see cref="GraphNodeModelSearcherItem"/>.
        /// </summary>
        /// <param name="data">Custom SearcherItem data.</param>
        /// <param name="createElement">Function to create the element in the graph.</param>
        /// <param name="name">Name of the item to show in the searcher.</param>
        /// <param name="children">Other Searcher Items nested under this one.</param>
        /// <param name="getName">Function providing the item name to show in the searcher.</param>
        /// <param name="help">Help text for the searcher item.</param>
        [Obsolete("Use a constructor that provides a GraphModel to GraphNodeModelSearcherItem to allow building index. Added in 0.10.1+ (2021-06-03).")]
        public GraphNodeModelSearcherItem(
            ISearcherItemData data,
            Func<IGraphNodeCreationData, IGraphElementModel> createElement,
            string name,
            List<SearcherItem> children = null,
            Func<string> getName = null,
            string help = null
        ) : this(null, data, createElement, name, children, getName, help)
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
        public GraphNodeModelSearcherItem(
            IGraphModel graphModel,
            ISearcherItemData data,
            Func<IGraphNodeCreationData, IGraphElementModel> createElement,
            string name,
            List<SearcherItem> children = null,
            Func<string> getName = null,
            string help = null
        ) : base(name, children: children, help: help)
        {
            m_GraphModel = graphModel;
            m_Name = name;
            m_GetName = getName;
            Data = data;
            CreateElements = d => new[] { createElement.Invoke(d) };
        }

        /// <inheritdoc />
        public override void Build()
        {
            base.Build();
            if (m_GraphModel != null)
            {
                var model = Enumerable.FirstOrDefault(CreateElements(
                    new GraphNodeCreationData(m_GraphModel, Vector2.zero, SpawnFlags.Orphan)));
                BuildItemFromNode(model);
            }
        }

        /// <summary>
        /// Extract data from an <see cref="IGraphElementModel"/> to the Searcher Item.
        /// </summary>
        /// <param name="model"><see cref="IGraphElementModel"/> to extract data from.</param>
        protected virtual void BuildItemFromNode(IGraphElementModel model)
        {
        }
    }
}
