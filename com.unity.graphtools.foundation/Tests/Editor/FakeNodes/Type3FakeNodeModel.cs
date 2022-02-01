using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class Type3FakeNodeModel : NodeModel
    {
        public static void AddToSearcherDatabase(IGraphModel graphModel, GraphElementSearcherDatabase db)
        {
            SearcherItem parent = db.Items.GetItemFromPath("Fake");

            parent.AddChild(new GraphNodeModelSearcherItem(graphModel,
                new NodeSearcherItemData(typeof(Type3FakeNodeModel)),
                data => data.CreateNode<Type3FakeNodeModel>(),
                nameof(Type3FakeNodeModel)
            ));
        }

        public IPortModel Input { get; private set; }
        public IPortModel Output { get; private set; }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            Input = this.AddDataInputPort<float>("input0");
            Output = this.AddDataOutputPort<float>("output0");
        }
    }
}
