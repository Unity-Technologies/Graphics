using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class Type3FakeNodeModel : NodeModel
    {
        public static void AddToSearcherDatabase(GraphElementSearcherDatabase db)
        {
            db.Items.Add(new GraphNodeModelSearcherItem(nameof(Type3FakeNodeModel),
                new NodeSearcherItemData(typeof(Type3FakeNodeModel)),
                data => data.CreateNode<Type3FakeNodeModel>())
            {
                CategoryPath = "Fake"
            });
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
