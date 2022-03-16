using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public class ClassSearcherDatabaseProvider : DefaultSearcherDatabaseProvider
    {
        SearcherDatabase m_StaticTypesSearcherDatabase;
        int m_AssetModificationVersion = AssetModificationWatcher.Version;

        static IReadOnlyList<Type> s_DefaultTypes = new List<Type> { typeof(float), typeof(bool) };

        protected override IReadOnlyList<Type> SupportedTypes => s_DefaultTypes;

        public ClassSearcherDatabaseProvider(Stencil stencil)
            :base(stencil)
        {
        }

        public override IReadOnlyList<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            if (AssetModificationWatcher.Version != m_AssetModificationVersion)
            {
                m_AssetModificationVersion = AssetModificationWatcher.Version;
                ResetGraphElementsSearcherDatabases();
            }

            return base.GetGraphElementsSearcherDatabases(graphModel);
        }
    }
}
