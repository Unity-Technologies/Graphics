using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    [SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
    class GraphModelDuplicationTests : BaseFixture<NoUIGraphViewTestGraphTool>
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_CloneGraph_MiniGraph([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            GraphModel.CreateEdge(node0.Input0, node1.Output0);

            var newGraphAsset = GraphAssetCreationHelpers<ClassGraphAssetModel>.CreateInMemoryGraphAsset(CreatedGraphType, "Test_Copy");

            Assert.That(GetNodeCount(), Is.EqualTo(2));
            Assert.That(GetEdgeCount(), Is.EqualTo(1));
            var n0 = GetNode(0) as Type0FakeNodeModel;
            var n1 = GetNode(1) as Type0FakeNodeModel;
            Assert.NotNull(n0);
            Assert.NotNull(n1);
            Assert.That(n0.Input0, Is.ConnectedTo(n1.Output0));
            Assert.That(newGraphAsset.GraphModel.NodeModels.Count, Is.EqualTo(0));
            Assert.That(newGraphAsset.GraphModel.EdgeModels.Cast<EdgeModel>().Count(), Is.EqualTo(0));

            newGraphAsset.GraphModel.CloneGraph(GraphModel);

            Assert.That(newGraphAsset.GraphModel.NodeModels.Count, Is.EqualTo(2));
            Assert.That(newGraphAsset.GraphModel.EdgeModels.Cast<EdgeModel>().Count(), Is.EqualTo(1));
            var nn0 = newGraphAsset.GraphModel.NodeModels.Cast<NodeModel>().ElementAt(0) as Type0FakeNodeModel;
            var nn1 = newGraphAsset.GraphModel.NodeModels.Cast<NodeModel>().ElementAt(1) as Type0FakeNodeModel;
            Assert.NotNull(nn0);
            Assert.NotNull(nn1);
            Assert.AreNotEqual(nn0, n0);
            Assert.AreNotEqual(nn1, n1);
            Assert.That(nn0.Input0, Is.ConnectedTo(nn1.Output0));
        }
    }
}
