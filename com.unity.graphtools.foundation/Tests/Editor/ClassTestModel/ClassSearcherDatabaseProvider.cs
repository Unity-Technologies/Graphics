using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public class ClassSearcherDatabaseProvider : ISearcherDatabaseProvider
    {
        readonly Stencil m_Stencil;
        List<SearcherDatabaseBase> m_GraphElementsSearcherDatabases;
        Dictionary<Type, List<SearcherDatabaseBase>> m_GraphElementContainersSearcherDatabases = new Dictionary<Type, List<SearcherDatabaseBase>>();
        SearcherDatabase m_StaticTypesSearcherDatabase;
        int m_AssetModificationVersion = AssetModificationWatcher.Version;

        public ClassSearcherDatabaseProvider(Stencil stencil)
        {
            m_Stencil = stencil;
        }

        public virtual List<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            if (AssetModificationWatcher.Version != m_AssetModificationVersion)
            {
                m_AssetModificationVersion = AssetModificationWatcher.Version;
                ClearGraphElementsSearcherDatabases();
            }

            return m_GraphElementsSearcherDatabases ??= new List<SearcherDatabaseBase>
            {
                new GraphElementSearcherDatabase(m_Stencil, graphModel)
                    .AddNodesWithSearcherItemAttribute()
                    .AddStickyNote()
                    .Build()
            };
        }

        public virtual List<SearcherDatabaseBase> GetGraphElementContainerSearcherDatabases(IGraphModel graphModel, IGraphElementContainer container)
        {
            List<SearcherDatabaseBase> databases;
            m_GraphElementContainersSearcherDatabases.TryGetValue(container.GetType(), out databases);

            if (databases != null)
                return databases;

            if (container is IContextNodeModel)
                return m_GraphElementsSearcherDatabases ?? (m_GraphElementsSearcherDatabases = new List<SearcherDatabaseBase>
                {
                    new ContextSearcherDatabase(m_Stencil, graphModel, container.GetType())
                        .Build()
                });

            return null;
        }

        public virtual List<SearcherDatabaseBase> GetVariableTypesSearcherDatabases()
        {
            return new List<SearcherDatabaseBase>
            {
                (m_StaticTypesSearcherDatabase ??= new[] {typeof(float), typeof(bool)}.ToSearcherDatabase())
            };
        }

        public virtual List<SearcherDatabaseBase> GetGraphVariablesSearcherDatabases(IGraphModel graphModel)
        {
            return new List<SearcherDatabaseBase>
            {
                new GraphElementSearcherDatabase(m_Stencil, graphModel)
                    .AddGraphVariables(graphModel)
                    .Build()
            };
        }

        public virtual List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IPortModel portModel)
        {
            return new List<SearcherDatabaseBase>();
        }

        public virtual List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IEnumerable<IPortModel> portModel)
        {
            return new List<SearcherDatabaseBase>();
        }

        public virtual void ClearGraphElementsSearcherDatabases()
        {
            m_GraphElementsSearcherDatabases = null;
        }

        public virtual void ClearTypesItemsSearcherDatabases()
        {
            m_StaticTypesSearcherDatabase = null;
        }

        public virtual void ClearTypeMembersSearcherDatabases() {}

        public virtual void ClearGraphVariablesSearcherDatabases() {}
    }
}
