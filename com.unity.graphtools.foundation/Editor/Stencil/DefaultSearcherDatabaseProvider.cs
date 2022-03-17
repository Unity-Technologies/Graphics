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
        protected static readonly IReadOnlyList<SearcherDatabaseBase> k_NoDatabase = new List<SearcherDatabaseBase>();
        protected static readonly IReadOnlyList<Type> k_NoTypeList = new List<Type>();

        /// <summary>
        /// List of types supported for variables and constants.
        /// <remarks>Will populate the default implementation of <see cref="GetVariableTypesSearcherDatabases"/>.</remarks>
        /// </summary>
        protected virtual IReadOnlyList<Type> SupportedTypes => k_NoTypeList;

        List<SearcherDatabaseBase> m_GraphElementsSearcherDatabases;
        List<SearcherDatabaseBase> m_GraphVariablesSearcherDatabases;
        List<SearcherDatabaseBase> m_TypeSearcherDatabases;


        protected Dictionary<Type, List<SearcherDatabaseBase>> m_GraphElementContainersSearcherDatabases;

        /// <summary>
        /// The graph stencil.
        /// </summary>
        public Stencil Stencil { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultSearcherDatabaseProvider"/> class.
        /// </summary>
        /// <param name="stencil">The stencil.</param>
        public DefaultSearcherDatabaseProvider(Stencil stencil)
        {
            Stencil = stencil;
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            return m_GraphElementsSearcherDatabases ??= new List<SearcherDatabaseBase>
            {
                InitialGraphElementDatabase(graphModel).Build()
            };
        }

        /// <summary>
        /// Creates the initial database used for graph elements.
        /// </summary>
        /// <param name="graphModel">The graph in which to search for elements.</param>
        /// <returns>A database containing the searcher items for graph elements.</returns>
        public virtual GraphElementSearcherDatabase InitialGraphElementDatabase(IGraphModel graphModel)
        {
            return new GraphElementSearcherDatabase(Stencil, graphModel)
                .AddNodesWithSearcherItemAttribute()
                .AddStickyNote();
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<SearcherDatabaseBase> GetGraphElementContainerSearcherDatabases(
            IGraphModel graphModel, IGraphElementContainer container)
        {
            List<SearcherDatabaseBase> databases;
            if (m_GraphElementContainersSearcherDatabases != null)
            {
                m_GraphElementContainersSearcherDatabases.TryGetValue(container.GetType(), out databases);

                if (databases != null)
                    return databases;
            }

            if (container is IContextNodeModel)
                return m_GraphElementsSearcherDatabases ??= new List<SearcherDatabaseBase>
                {
                    new ContextSearcherDatabase(Stencil, container.GetType())
                        .Build()
                };

            return null;
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<SearcherDatabaseBase> GetVariableTypesSearcherDatabases()
        {
            return m_TypeSearcherDatabases ??= new List<SearcherDatabaseBase>
            {
                SupportedTypes.ToSearcherDatabase()
            };
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<SearcherDatabaseBase> GetGraphVariablesSearcherDatabases(IGraphModel graphModel)
        {
            return m_GraphVariablesSearcherDatabases ??= new List<SearcherDatabaseBase>
            {
                InitialGraphVariablesDatabase(graphModel).Build()
            };
        }

        /// <summary>
        /// Creates the initial database used for graph variables.
        /// </summary>
        /// <param name="graphModel">The graph in which to search for variables.</param>
        /// <returns>A database containing the searcher items for variables.</returns>
        public virtual GraphElementSearcherDatabase InitialGraphVariablesDatabase(IGraphModel graphModel)
        {
            return new GraphElementSearcherDatabase(Stencil, graphModel)
                .AddGraphVariables(graphModel);
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<SearcherDatabaseBase> GetDynamicSearcherDatabases(IPortModel portModel)
        {
            return k_NoDatabase;
        }

        /// <inheritdoc />
        public virtual IReadOnlyList<SearcherDatabaseBase> GetDynamicSearcherDatabases(
            IEnumerable<IPortModel> portModel)
        {
            return k_NoDatabase;
        }

        /// <summary>
        /// Resets Graph Elements Searcher Databases to force invalidating the cached version.
        /// </summary>
        protected void ResetGraphElementsSearcherDatabases()
        {
            m_GraphElementsSearcherDatabases = null;
        }

        /// <summary>
        /// Resets Graph Variable Searcher Databases to force invalidating the cached version.
        /// </summary>
        protected void ResetGraphVariablesSearcherDatabases()
        {
            m_GraphVariablesSearcherDatabases = null;
        }
    }
}
