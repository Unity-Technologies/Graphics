using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class Type0FakeNodeModel : NodeModel, IFakeNode
    {
        public static void AddToSearcherDatabase(IGraphModel graphModel, GraphElementSearcherDatabase db)
        {
            SearcherItem parent = db.Items.GetItemFromPath("Fake");

            parent.AddChild(new GraphNodeModelSearcherItem(graphModel,
                new NodeSearcherItemData(typeof(Type0FakeNodeModel)),
                data => data.CreateNode<Type0FakeNodeModel>(),
                nameof(Type0FakeNodeModel)
            ));
        }

        public IPortModel ExeInput0 { get; private set; }
        public IPortModel ExeOutput0 { get; private set; }
        public IPortModel Input0 { get; private set; }
        public IPortModel Input1 { get; private set; }
        public IPortModel Input2 { get; private set; }
        public IPortModel Output0 { get; private set; }
        public IPortModel Output1 { get; private set; }
        public IPortModel Output2 { get; private set; }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            ExeInput0 = this.AddExecutionInputPort("exe0");
            ExeOutput0 = this.AddExecutionOutputPort("exe0");
            Input0 = this.AddDataInputPort<int>("input0");
            Input1 = this.AddDataInputPort<int>("input1");
            Input2 = this.AddDataInputPort<int>("input2");
            Output0 = this.AddDataOutputPort<int>("output0");
            Output1 = this.AddDataOutputPort<int>("output1");
            Output2 = this.AddDataOutputPort<int>("output2");
        }
    }

    interface IFakeNode : INodeModel {}
}
