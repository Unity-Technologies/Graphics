using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.GraphToolsFoundation.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A searcher database for contexts.
    /// </summary>
    public class ContextSearcherDatabase
    {
        readonly List<SearcherItem> m_Items;
        readonly Stencil m_Stencil;
        IGraphModel m_GraphModel;
        Type m_ContextType;

        /// <summary>
        /// Creates a ContextSearcherDatabase.
        /// </summary>
        /// <param name="stencil">The stencil to use.</param>
        /// <param name="graphModel">The GraphModel to use.</param>
        /// <param name="contextType">The Type of context to use.</param>
        public ContextSearcherDatabase(Stencil stencil, IGraphModel graphModel, Type contextType)
        {
            m_Stencil = stencil;
            m_Items = new List<SearcherItem>();
            m_GraphModel = graphModel;
            m_ContextType = contextType;
        }

        /// <summary>
        /// Builds the <see cref="SearcherDatabase"/>.
        /// </summary>
        /// <returns>The built <see cref="SearcherDatabase"/>.</returns>
        public SearcherDatabase Build()
        {
            var containerInstance = Activator.CreateInstance(m_ContextType) as IContextNodeModel;

            var types = TypeCache.GetTypesWithAttribute<SearcherItemAttribute>();
            foreach (var type in types)
            {
                var attributes = type.GetCustomAttributes<SearcherItemAttribute>().ToList();
                if (!attributes.Any())
                    continue;

                if (!typeof(IBlockNodeModel).IsAssignableFrom(type))
                    continue;

                var blockInstance = Activator.CreateInstance(type) as IBlockNodeModel;

                if (!blockInstance.IsCompatibleWith(containerInstance))
                    continue;

                foreach (var attribute in attributes)
                {
                    if (!attribute.StencilType.IsInstanceOfType(m_Stencil))
                        continue;

                    var name = attribute.Path.Split('/').Last();
                    var path = attribute.Path.Remove(attribute.Path.LastIndexOf('/') + 1);

                    if (attribute.Context == SearcherContext.Graph)
                    {
                        var node = new GraphNodeModelSearcherItem(m_GraphModel,
                            new NodeSearcherItemData(type),
                            data => data.CreateBlock(type, name, contextTypeToCreate: m_ContextType),
                            name
                        );

                        m_Items.AddAtPath(node, path);
                    }

                    break;
                }
            }

            SearcherDatabase database = new SearcherDatabase(m_Items);

            return database;
        }
    }
}
