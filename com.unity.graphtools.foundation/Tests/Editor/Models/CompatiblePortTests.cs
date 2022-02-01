using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    class CompatiblePortTests : BaseFixture
    {
        [Serializable]
        class NodeType1 : NodeModel
        {
            public IPortModel DataOut0 { get; private set; }

            protected override void OnDefineNode()
            {
                base.OnDefineNode();

                DataOut0 = this.AddDataOutputPort<int>("dataOut0");
            }
        }

        [Serializable]
        class NodeType2 : NodeModel
        {
            public IPortModel DataIn0 { get; private set; }

            protected override void OnDefineNode()
            {
                base.OnDefineNode();

                DataIn0 = this.AddDataInputPort<string>("dataIn0");
            }
        }

        [Serializable]
        class NodeType3 : NodeModel
        {
            public IPortModel ExecOut0 { get; private set; }

            protected override void OnDefineNode()
            {
                base.OnDefineNode();

                ExecOut0 = this.AddExecutionOutputPort("execOut0");
            }
        }

        [Serializable]
        class NodeType4 : NodeModel
        {
            public IPortModel ExecIn0 { get; private set; }

            protected override void OnDefineNode()
            {
                base.OnDefineNode();

                ExecIn0 = this.AddExecutionInputPort("execIn0");
            }
        }

        protected override bool CreateGraphOnStartup => true;

        [Test]
        public void GetPortFitToConnectToReturnNullForIncompatiblePorts()
        {
            var node1 = GraphModel.CreateNode<NodeType1>("test1", Vector2.zero);
            var node2 = GraphModel.CreateNode<NodeType2>("test2", 100 * Vector2.right);

            Assert.IsNull(node2.GetPortFitToConnectTo(node1.DataOut0));
        }

        [Test]
        public void ExecutionPortDoesNotConnectToDataPort()
        {
            var node1 = GraphModel.CreateNode<NodeType3>("test1", Vector2.zero);
            var node2 = GraphModel.CreateNode<NodeType2>("test2", 100 * Vector2.right);

            Assert.IsNull(node2.GetPortFitToConnectTo(node1.ExecOut0));
        }

        [Test]
        public void DataPortDoesNotConnectToExecutionPort()
        {
            var node1 = GraphModel.CreateNode<NodeType1>("test1", Vector2.zero);
            var node2 = GraphModel.CreateNode<NodeType4>("test2", 100 * Vector2.right);

            Assert.IsNull(node2.GetPortFitToConnectTo(node1.DataOut0));
        }
    }
}
