using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class Type2FakeNodeModel : NodeModel
    {
        public static void AddToSearcherDatabase(GraphElementSearcherDatabase db)
        {
            db.Items.Add(new GraphNodeModelSearcherItem(nameof(Type2FakeNodeModel),
                new NodeSearcherItemData(typeof(Type2FakeNodeModel)),
                data => data.CreateNode<Type2FakeNodeModel>())
            {
                CategoryPath = "Fake"
            });
        }

        public IPortModel StringInput { get; private set; }
        public IPortModel FloatInput { get; private set; }
        public IPortModel FloatOutput { get; private set; }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            StringInput = this.AddDataInputPort<string>("input1");
            FloatInput = this.AddDataInputPort<float>("input2");
            FloatOutput = this.AddDataOutputPort<float>("output0");
        }
    }
}
