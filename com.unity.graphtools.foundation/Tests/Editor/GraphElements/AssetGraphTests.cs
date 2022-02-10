using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.SubgraphTesting
{
    class AssetGraphTests : GraphViewTester
    {
        IGraphAssetModel m_ReferenceAssetGraph;
        IGraphAssetModel m_CurrentGraphAssetModel;

        IEnumerable<string> m_AssetPaths;

        const string k_ReferenceGraphName = "Reference Asset Graph";
        const string k_CurrentGraphName = "Current Graph";

        static IGraphAssetModel CreateGraph(string graphName, GraphAssetType graphAssetType)
        {
            var template = new GraphTemplate<ClassStencil>(graphName);

            return GraphAssetCreationHelpers<TestGraphAssetModel>.CreateGraphAsset(typeof(ClassStencil), graphName, $"Assets/{graphName}.asset", template, graphAssetType);
        }

        static SearcherDatabase GetSearcherDatabaseWithAssetGraphs(IGraphAssetModel graphAssetModel)
        {
            return new GraphElementSearcherDatabase((ClassStencil)graphAssetModel.GraphModel.Stencil, graphAssetModel.GraphModel)
                .AddAssetGraphSubgraphs(graphAssetModel.GraphModel)
                .Build();
        }

        void CreateNodesAndValidateGraphModel(GraphNodeModelSearcherItem item, Action<List<INodeModel>> assertNodesCreation)
        {
            var initialNodes = m_CurrentGraphAssetModel.GraphModel.NodeModels.ToList();
            item.CreateElements.Invoke(new GraphNodeCreationData(m_CurrentGraphAssetModel.GraphModel, Vector2.zero));
            assertNodesCreation.Invoke(initialNodes);
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_ReferenceAssetGraph = CreateGraph(k_ReferenceGraphName, GraphAssetType.AssetGraph);

            m_CurrentGraphAssetModel = CreateGraph(k_CurrentGraphName, GraphAssetType.AssetGraph);

            m_AssetPaths = AssetDatabase.FindAssets($"t:{typeof(TestGraphAssetModel)}").Select(AssetDatabase.GUIDToAssetPath);
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            if (m_AssetPaths == null)
                return;

            foreach (var assetPath in m_AssetPaths)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void ShouldFindAssetGraphWithDataIOInSearcher()
        {
            // Add data I/O variables to the first reference asset graph model
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Input Data", ModifierFlags.ReadOnly, true);
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Output Data", ModifierFlags.WriteOnly, true);

            var searcherDatabase = GetSearcherDatabaseWithAssetGraphs(m_CurrentGraphAssetModel);

            var results = searcherDatabase.Search(k_ReferenceGraphName);

            Assert.AreEqual(1, results.Count);
        }

        [Test]
        public void ShouldFindAssetGraphWithExecutionIOInSearcher()
        {
            // Add execution I/O variables to the first reference asset graph model
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.ExecutionFlow, "Input Trigger", ModifierFlags.ReadOnly, true);
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.ExecutionFlow, "Output Trigger", ModifierFlags.WriteOnly, true);

            var searcherDatabase = GetSearcherDatabaseWithAssetGraphs(m_CurrentGraphAssetModel);

            var results = searcherDatabase.Search(k_ReferenceGraphName);

            Assert.AreEqual(1, results.Count);
        }

        [Test]
        public void ShouldNotFindAssetGraphWithNoIOInSearcher()
        {
            // No I/O variables added in the first reference asset graph model
            var searcherDatabase = GetSearcherDatabaseWithAssetGraphs(m_CurrentGraphAssetModel);

            var results = searcherDatabase.Search(k_ReferenceGraphName);
            Assert.IsEmpty(results);
        }

        [Test]
        public void ShouldFindAssetGraphInItsOwnSearcher()
        {
            // Add I/O variables to the first reference asset graph model
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Input Data", ModifierFlags.ReadOnly, true);
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Output Data", ModifierFlags.WriteOnly, true);

            var searcherDatabase = GetSearcherDatabaseWithAssetGraphs(m_ReferenceAssetGraph);

            var results = searcherDatabase.Search(k_ReferenceGraphName);

            Assert.AreEqual(1, results.Count);
        }

        [Test]
        public void ShouldNotFindContainerGraphInSearcher()
        {
            var containerGraphModel = CreateGraph("Container Graph", GraphAssetType.ContainerGraph);
            m_AssetPaths = AssetDatabase.FindAssets($"t:{typeof(TestGraphAssetModel)}").Select(AssetDatabase.GUIDToAssetPath);

            // Add I/O variables to container graph model
            containerGraphModel.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Input Data", ModifierFlags.ReadOnly, true);
            containerGraphModel.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Output Data", ModifierFlags.WriteOnly, true);

            var searcherDatabase = GetSearcherDatabaseWithAssetGraphs(m_CurrentGraphAssetModel);

            var results = searcherDatabase.Search("Container Graph");

            Assert.IsEmpty(results);
        }

        [Test]
        public void SubgraphNodeHasSameIOPortsAsReferenceAssetGraphIOVariables()
        {
            // Add data I/O variables to the reference asset graph model
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Input Data", ModifierFlags.ReadOnly, true);
            m_ReferenceAssetGraph.GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "Output Data", ModifierFlags.WriteOnly, true);

            var searcherDatabase = GetSearcherDatabaseWithAssetGraphs(m_CurrentGraphAssetModel);

            var results = searcherDatabase.Search(k_ReferenceGraphName);

            var item = (GraphNodeModelSearcherItem)results[0];

            CreateNodesAndValidateGraphModel(item, initialNodes =>
            {
                var node = m_CurrentGraphAssetModel.GraphModel.NodeModels.OfType<SubgraphNodeModel>().FirstOrDefault();
                Assert.IsNotNull(node);
                Assert.AreEqual(initialNodes.Count + 1, m_CurrentGraphAssetModel.GraphModel.NodeModels.Count);
                Assert.AreEqual(k_ReferenceGraphName.Nicify(), node.DisplayTitle);
                Assert.AreEqual(m_ReferenceAssetGraph.GraphModel.VariableDeclarations.Count(v => v.IsInputOrOutput()), node.DataInputPortToVariableDeclarationDictionary.Count + node.DataOutputPortToVariableDeclarationDictionary.Count + node.ExecutionInputPortToVariableDeclarationDictionary.Count + node.ExecutionOutputPortToVariableDeclarationDictionary.Count);

                foreach (var port in node.Ports)
                {
                    Assert.IsTrue(m_ReferenceAssetGraph.GraphModel.VariableDeclarations.Any(v => v.Title == port.UniqueName));
                }
            });
        }
    }
}
