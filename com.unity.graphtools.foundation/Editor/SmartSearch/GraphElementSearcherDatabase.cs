using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// SearcherDatabase for Graph Elements.
    /// </summary>
    [PublicAPI]
    public class GraphElementSearcherDatabase
    {
        const string k_Constant = "Constant";
        const string k_Sticky = "Sticky Note";
        const string k_GraphVariables = "Graph Variables";
        const string k_Subgraphs = "Subgraphs";

        // TODO: our builder methods ("AddStack",...) all use this field. Users should be able to create similar methods. making it public until we find a better solution
        public readonly List<SearcherItem> Items;
        public readonly Stencil Stencil;
        private IGraphModel m_GraphModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphElementSearcherDatabase"/> class.
        /// </summary>
        /// <param name="stencil">Stencil of the graph elements.</param>
        /// <param name="graphModel">GraphModel of the graph elements.</param>
        public GraphElementSearcherDatabase(Stencil stencil, IGraphModel graphModel)
        {
            Stencil = stencil;
            Items = new List<SearcherItem>();
            m_GraphModel = graphModel;
        }

        /// <summary>
        /// Adds a <see cref="SearcherItem"/> for each node marked with <see cref="SearcherItemAttribute"/> to the database.
        /// <remarks>Nodes marked with <see cref="SeacherHelpAttribute"/> will also display a description in the details panel.</remarks>
        /// </summary>
        /// <returns>The database with the elements.</returns>
        public GraphElementSearcherDatabase AddNodesWithSearcherItemAttribute()
        {
            var types = TypeCache.GetTypesWithAttribute<SearcherItemAttribute>();
            foreach (var type in types)
            {
                var attributes = type.GetCustomAttributes<SearcherItemAttribute>().ToList();
                if (!attributes.Any())
                    continue;

                //Blocks and Nodes share SearcherItemAttribute but blocks shouldn't be added to nodes lists.
                if (typeof(IBlockNodeModel).IsAssignableFrom(type))
                    continue;

                string nodeHelp = null;
                var nodeHelpAttribute = type.GetCustomAttribute<SeacherHelpAttribute>();
                if (nodeHelpAttribute != null)
                    nodeHelp = nodeHelpAttribute.HelpText;

                foreach (var attribute in attributes)
                {
                    if (!attribute.StencilType.IsInstanceOfType(Stencil))
                        continue;

                    var name = attribute.Path.Split('/').Last();
                    var path = attribute.Path.Remove(attribute.Path.LastIndexOf('/') + 1);

                    switch (attribute.Context)
                    {
                        case SearcherContext.Graph:
                        {
                            var node = new GraphNodeModelSearcherItem(
                                Stencil.GraphModel,
                                new NodeSearcherItemData(type),
                                data => data.CreateNode(type, name),
                                name,
                                help: nodeHelp
                            );

                            Items.AddAtPath(node, path);
                            break;
                        }

                        default:
                            Debug.LogWarning($"The node {type} is not a " +
                                $"{SearcherContext.Graph} node, so it cannot be add in the Searcher");
                            break;
                    }

                    break;
                }
            }

            return this;
        }

        /// <summary>
        /// Adds a searcher item for Sticky Note to the database.
        /// </summary>
        /// <returns>The database with the elements.</returns>
        public GraphElementSearcherDatabase AddStickyNote()
        {
            var node = new GraphNodeModelSearcherItem(
                Stencil.GraphModel,
                new TagSearcherItemData(CommonSearcherTags.StickyNote),
                data =>
                {
                    var rect = new Rect(data.Position, StickyNote.defaultSize);
                    var graphModel = data.GraphModel;
                    return graphModel.CreateStickyNote(rect, data.SpawnFlags);
                },
                k_Sticky
            );
            Items.AddAtPath(node);

            return this;
        }

        /// <summary>
        /// Adds searcher items for constants to the database.
        /// </summary>
        /// <param name="types">The types of constants to add.</param>
        /// <returns>The database with the elements.</returns>
        public GraphElementSearcherDatabase AddConstants(IEnumerable<Type> types)
        {
            foreach (Type type in types)
            {
                AddConstant(type);
            }

            return this;
        }

        /// <summary>
        /// Adds a searcher item for a constant of a certain type to the database.
        /// </summary>
        /// <param name="type">The type of constant to add.</param>
        /// <returns>The database with the elements.</returns>
        public GraphElementSearcherDatabase AddConstant(Type type)
        {
            TypeHandle handle = type.GenerateTypeHandle();

            SearcherItem parent = Items.GetItemFromPath(k_Constant);
            parent.AddChild(new GraphNodeModelSearcherItem(
                Stencil.GraphModel,
                new TypeSearcherItemData(handle),
                data => data.CreateConstantNode("", handle),
                $"{type.FriendlyName().Nicify()} {k_Constant}"
            ));

            return this;
        }

        /// <summary>
        /// Adds searcher items for every graph variable to the database.
        /// </summary>
        /// <param name="graphModel">The GraphModel containing the variables.</param>
        /// <returns>The database with the elements.</returns>
        public GraphElementSearcherDatabase AddGraphVariables(IGraphModel graphModel)
        {
            SearcherItem parent = null;

            foreach (var declarationModel in graphModel.VariableDeclarations)
            {
                if (parent == null)
                {
                    parent = Items.GetItemFromPath(k_GraphVariables);
                }

                parent.AddChild(new GraphNodeModelSearcherItem(
                    Stencil.GraphModel,
                    new TypeSearcherItemData(declarationModel.DataType),
                    data => data.CreateVariableNode(declarationModel),
                    declarationModel.DisplayTitle
                ));
            }

            return this;
        }

        /// <summary>
        /// Adds a searcher item for a Asset Graph Subgraph to the database.
        /// </summary>
        /// <param name="graphModel">The GraphModel containing the variables.</param>
        /// <returns>The database with the elements.</returns>
        public GraphElementSearcherDatabase AddAssetGraphSubgraphs(IGraphModel graphModel)
        {
            SearcherItem parent = null;
            var assetPaths = AssetDatabase.FindAssets($"t:{typeof(GraphAssetModel)}").Select(AssetDatabase.GUIDToAssetPath).ToList();
            var assetGraphModels = assetPaths.Select(p => AssetDatabase.LoadAssetAtPath(p, typeof(object)) as GraphAssetModel)
                .Where(g => g != null && g.GraphAssetType == GraphAssetType.AssetGraph);

            var handle = Stencil.GetSubgraphNodeTypeHandle();

            foreach (var assetGraphModel in assetGraphModels.Where(asset => asset.GraphModel.VariableDeclarations.Any(variable => variable.IsInputOrOutput())))
            {
                if (parent == null)
                {
                    parent = Items.GetItemFromPath(k_Subgraphs);
                }

                parent.AddChild(new GraphNodeModelSearcherItem(
                    graphModel,
                    new TypeSearcherItemData(handle),
                    data => data.CreateSubgraphNode(assetGraphModel),
                    assetGraphModel?.Name
                ));
            }

            return this;
        }

        /// <summary>
        /// Get a version of the database compatible with the Searcher.
        /// </summary>
        /// <returns>A version of the database compatible with the Searcher.</returns>
        public SearcherDatabase Build()
        {
            return new SearcherDatabase(Items);
        }
    }
}
