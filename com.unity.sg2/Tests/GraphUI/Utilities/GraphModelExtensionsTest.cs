using NUnit.Framework;
using Unity.GraphToolsFoundation;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests.Utilities
{
    class GraphModelExtensionsTest : BaseGraphAssetTest
    {
        [Test]
        public void TestCreateGraphDataNode_WithDefaultFlags_AddsNodeToGraph()
        {
            var guid = SerializableGUID.Generate();
            GraphModel.CreateGraphDataNode(new RegistryKey {Name = "Add", Version = 1}, "Test", Vector2.zero, guid, SpawnFlags.Default);

            Assert.IsNotNull(GraphModel.GraphHandler.GetNode(guid.ToString()));
        }

        [Test]
        public void TestCreateGraphDataNode_WithOrphanFlag_DoesNotAddNode()
        {
            var guid = SerializableGUID.Generate();
            GraphModel.CreateGraphDataNode(new RegistryKey {Name = "Add", Version = 1}, "Test", Vector2.zero, guid, SpawnFlags.Orphan);

            Assert.IsNull(GraphModel.GraphHandler.GetNode(guid.ToString()));
        }
    }
}
