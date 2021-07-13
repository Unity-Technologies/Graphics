using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphSearcherDatabaseProvider : DefaultSearcherDatabaseProvider
    {
        public ShaderGraphSearcherDatabaseProvider(Stencil stencil, List<Preset> presets)
            : base(stencil)
        {
            m_Presets = presets;
        }

        public class Preset
        {
            public readonly string Name;
            public Dictionary<string, TypeHandle> inputs = new();
            public Dictionary<string, TypeHandle> outputs = new();

            public Preset(string name)
            {
                Name = name;
            }
        }

        readonly List<Preset> m_Presets;

        public override List<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            var root = new SearcherItem("Customizable Node Presets");

            foreach (var preset in m_Presets)
            {
                root.AddChild(new GraphNodeModelSearcherItem(graphModel, null,
                    data =>
                    {
                        var m = data.CreateNode<CustomizableNodeModel>();

                        foreach (var input in preset.inputs)
                        {
                            m.AddCustomDataInputPort(input.Key, input.Value);
                        }

                        foreach (var output in preset.outputs)
                        {
                            m.AddCustomDataOutputPort(output.Key, output.Value);
                        }

                        m.Title = preset.Name;
                        return m;
                    }, preset.Name));
            }

            var db = new SearcherDatabase(new List<SearcherItem> {root});
            var baseDbs = base.GetGraphElementsSearcherDatabases(graphModel);
            baseDbs.Add(db);

            return baseDbs;
        }
    }
}
