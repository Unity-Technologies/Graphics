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
        Type m_ContextType;

        /// <summary>
        /// Creates a ContextSearcherDatabase.
        /// </summary>
        /// <param name="stencil">The stencil to use.</param>
        /// <param name="graphModel">The GraphModel to use.</param>
        /// <param name="contextType">The Type of context to use.</param>
        [Obsolete("Graphmodel isn't needed anymore (2021-09-21).")]
        // ReSharper disable once UnusedParameter.Local
        public ContextSearcherDatabase(Stencil stencil, IGraphModel graphModel, Type contextType)
        :this(stencil, contextType)
        {
        }

        /// <summary>
        /// Creates a ContextSearcherDatabase.
        /// </summary>
        /// <param name="stencil">The stencil to use.</param>
        /// <param name="contextType">The Type of context to use.</param>
        public ContextSearcherDatabase(Stencil stencil, Type contextType)
        {
            m_Stencil = stencil;
            m_Items = new List<SearcherItem>();
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

                if (blockInstance == null || !blockInstance.IsCompatibleWith(containerInstance))
                    continue;

                foreach (var attribute in attributes)
                {
                    if (!attribute.StencilType.IsInstanceOfType(m_Stencil))
                        continue;

                    if (attribute.Context == SearcherContext.Graph)
                    {
                        var node = new GraphNodeModelSearcherItem(new NodeSearcherItemData(type),
                            data => data.CreateBlock(type, contextTypeToCreate: m_ContextType))
                        {
                            FullName = attribute.Path,
                            StyleName = attribute.StyleName
                        };
                        m_Items.Add(node);
                    }

                    break;
                }
            }

            SearcherDatabase database = new SearcherDatabase(m_Items);

            return database;
        }
    }
}
