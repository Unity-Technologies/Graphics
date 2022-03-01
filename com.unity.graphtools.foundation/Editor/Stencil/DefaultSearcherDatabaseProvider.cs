using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The default implementation of <see cref="ISearcherDatabaseProvider"/>.
    /// </summary>
    public class DefaultSearcherDatabaseProvider : ISearcherDatabaseProvider
    {
        Stencil m_Stencil;
        List<SearcherDatabaseBase> m_GraphElementsSearcherDatabases;

        Dictionary<Type, List<SearcherDatabaseBase>> m_GraphElementContainersSearcherDatabases;

        static readonly List<SearcherDatabaseBase> EmptyResults = new List<SearcherDatabaseBase>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultSearcherDatabaseProvider"/> class.
        /// </summary>
        /// <param name="stencil">The stencil.</param>
        public DefaultSearcherDatabaseProvider(Stencil stencil)
        {
            m_Stencil = stencil;
        }

        /// <inheritdoc />
        public virtual List<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            return m_GraphElementsSearcherDatabases ??= new List<SearcherDatabaseBase>
            {
                InitialGraphElementDatabase(graphModel).Build()
            };
        }

        public virtual GraphElementSearcherDatabase InitialGraphElementDatabase(IGraphModel graphModel)
        {
            return new GraphElementSearcherDatabase(m_Stencil, graphModel)
                .AddNodesWithSearcherItemAttribute()
                .AddStickyNote();
        }

        /// <inheritdoc />
        public virtual List<SearcherDatabaseBase> GetGraphElementContainerSearcherDatabases(IGraphModel graphModel, IGraphElementContainer container)
        {
            m_GraphElementContainersSearcherDatabases.TryGetValue(container.GetType(), out List<SearcherDatabaseBase> databases);

            if (databases != null)
                return databases;

            if (container is IContextNodeModel)
            {
                databases = new List<SearcherDatabaseBase>
                {
                    new ContextSearcherDatabase(m_Stencil, graphModel, container.GetType())
                        .Build()
                };
                m_GraphElementContainersSearcherDatabases[container.GetType()] = databases;
                return databases;
            }

            return null;
        }

        /// <inheritdoc />
        public virtual List<SearcherDatabaseBase> GetVariableTypesSearcherDatabases()
        {
            return EmptyResults;
        }

        /// <inheritdoc />
        public virtual List<SearcherDatabaseBase> GetGraphVariablesSearcherDatabases(IGraphModel graphModel)
        {
            return new List<SearcherDatabaseBase>
            {
                BuildInitialGraphVariablesDatabase(graphModel)
            };
        }

        public virtual SearcherDatabaseBase BuildInitialGraphVariablesDatabase(IGraphModel graphModel)
        {
            return new GraphElementSearcherDatabase(m_Stencil, graphModel)
                .AddGraphVariables(graphModel)
                .Build();
        }

        /// <inheritdoc />
        public virtual List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IPortModel portModel)
        {
            return EmptyResults;
        }

        /// <inheritdoc />
        public virtual List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IEnumerable<IPortModel> portModel)
        {
            return EmptyResults;
        }
    }
}
