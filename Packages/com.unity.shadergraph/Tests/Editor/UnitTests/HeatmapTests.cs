using System;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.ShaderGraph.UnitTests
{
    class HeatmapTests
    {
        class TestNode : AbstractMaterialNode { }

        class TemporarySubGraph : IDisposable
        {
            SubGraphAsset m_SubGraph;

            public SubGraphNode Node { get; }

            public TemporarySubGraph()
            {
                m_SubGraph = ScriptableObject.CreateInstance<SubGraphAsset>();
                AssetDatabase.CreateAsset(m_SubGraph, AssetDatabase.GenerateUniqueAssetPath("Assets/HeatmapTests_TemporarySubGraph.asset"));
                Node = new SubGraphNode {asset = m_SubGraph};
                Assert.IsFalse(string.IsNullOrEmpty(Node.subGraphGuid), "Temporary subgraph was not created correctly.");
            }

            public void Dispose()
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(m_SubGraph));
            }
        }

        [Test]
        public void TestHeatmapEntries_TryGetCategory_PresentEntry_ReturnsValue()
        {
            var he = new HeatmapEntries();
            he.Entries.Add(new HeatmapEntry("Test", 123));
            Assert.IsTrue(he.TryGetCategory("Test", out var category));
            Assert.AreEqual(123, category);
        }

        [Test]
        public void TestHeatmapEntries_TryGetCategory_MissingEntry_ReturnsZero()
        {
            var he = new HeatmapEntries();
            Assert.IsFalse(he.TryGetCategory("Test", out var category));
            Assert.AreEqual(0, category);
        }

        [Test]
        public void TestHeatmapEntries_TryGetCategory_MissingEntryNonEmpty_ReturnsZero()
        {
            var he = new HeatmapEntries();
            he.Entries.Add(new HeatmapEntry("Test", 123));
            Assert.IsFalse(he.TryGetCategory("Test2", out var category));
            Assert.AreEqual(0, category);
        }

        [Test]
        public void TestShaderGraphHeatmapValues_GetHeatmapKey_BuiltInNode_IsTypeName()
        {
            var key = ShaderGraphHeatmapValues.GetHeatmapKey(new TestNode());
            Assert.AreEqual("TestNode", key);
        }

        [Test]
        public void TestShaderGraphHeatmapValues_GetHeatmapKey_SubGraph_IsSubGraphGuid()
        {
            using var sub = new TemporarySubGraph();
            var key = ShaderGraphHeatmapValues.GetHeatmapKey(sub.Node);
            Assert.AreEqual(sub.Node.subGraphGuid, key);
        }

        [Test]
        public void TestShaderGraphHeatmapValues_TryGetCategory_PresentBuiltInNode_ReturnsValue()
        {
            var heatmapValues = ScriptableObject.CreateInstance<ShaderGraphHeatmapValues>();

            heatmapValues.m_Colors = new[] {Color.white};
            heatmapValues.Nodes.Entries.Add(new HeatmapEntry("TestNode", 0));

            Assert.IsTrue(heatmapValues.TryGetCategoryColor(new TestNode(), out var category));
            Assert.AreEqual(Color.white, category);
        }

        [Test]
        public void TestShaderGraphHeatmapValues_TryGetCategory_PresentSubGraph_ReturnsValue()
        {
            using var sub = new TemporarySubGraph();
            var heatmapValues = ScriptableObject.CreateInstance<ShaderGraphHeatmapValues>();

            heatmapValues.m_Colors = new[] {Color.white};
            heatmapValues.Subgraphs.Entries.Add(new HeatmapEntry(sub.Node.subGraphGuid, 0));

            Assert.IsTrue(heatmapValues.TryGetCategoryColor(sub.Node, out var category));
            Assert.AreEqual(Color.white, category);
        }

        [Test]
        public void TestShaderGraphHeatmapValues_TryGetCategory_MissingNode_ReturnsDefault()
        {
            var heatmapValues = ScriptableObject.CreateInstance<ShaderGraphHeatmapValues>();
            heatmapValues.m_Colors = new[] {Color.white};

            Assert.IsFalse(heatmapValues.TryGetCategoryColor(new TestNode(), out var category));
            Assert.AreEqual(default(Color), category);
        }

        [Test]
        public void TestShaderGraphHeatmapValues_TryGetCategory_MissingSubGraph_ReturnsDefault()
        {
            var heatmapValues = ScriptableObject.CreateInstance<ShaderGraphHeatmapValues>();
            heatmapValues.m_Colors = new[] {Color.white};

            using var sub = new TemporarySubGraph();

            Assert.IsFalse(heatmapValues.TryGetCategoryColor(sub.Node, out var category));
            Assert.AreEqual(default(Color), category);
        }

        [Test]
        public void TestShaderGraphHeatmapValues_TryGetCategory_ValueIsClampedToColorCount()
        {
            var heatmapValues = ScriptableObject.CreateInstance<ShaderGraphHeatmapValues>();
            heatmapValues.m_Colors = new[] { Color.red, Color.green, Color.blue };

            using var s1 = new TemporarySubGraph();
            using var s2 = new TemporarySubGraph();
            using var s3 = new TemporarySubGraph();

            heatmapValues.Subgraphs.Entries.Add(new HeatmapEntry(s1.Node.subGraphGuid, -100));
            heatmapValues.Subgraphs.Entries.Add(new HeatmapEntry(s2.Node.subGraphGuid, 1));
            heatmapValues.Subgraphs.Entries.Add(new HeatmapEntry(s3.Node.subGraphGuid, 100));

            Assert.IsTrue(heatmapValues.TryGetCategoryColor(s1.Node, out var category));
            Assert.AreEqual(Color.red, category);

            Assert.IsTrue(heatmapValues.TryGetCategoryColor(s2.Node, out category));
            Assert.AreEqual(Color.green, category);

            Assert.IsTrue(heatmapValues.TryGetCategoryColor(s3.Node, out category));
            Assert.AreEqual(Color.blue, category);
        }
    }
}
