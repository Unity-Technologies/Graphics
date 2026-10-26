using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.UnitTests
{
    class MissingSubGraphTests
    {
        const ImportAssetOptions k_ImportOptions =
            ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer;

        string m_SubGraphPath;
        string m_MainGraphPath;

        [TearDown]
        public void DeleteGraphs()
        {
            AssetDatabase.DeleteAsset(m_SubGraphPath);
            AssetDatabase.DeleteAsset(m_MainGraphPath);
        }

        void CreateMainGraphUsingSubGraph()
        {
            m_SubGraphPath = AssetDatabase.GenerateUniqueAssetPath("Assets/MissingSubGraphTests_Sub.shadersubgraph");
            m_MainGraphPath = AssetDatabase.GenerateUniqueAssetPath("Assets/MissingSubGraphTests_Main.shadergraph");

            var subGraph = new GraphData { isSubGraph = true };
            var outputNode = new SubGraphOutputNode();
            subGraph.AddNode(outputNode);
            subGraph.outputNode = outputNode;
            outputNode.AddSlot(ConcreteSlotValueType.Vector4);
            subGraph.path = "Sub Graphs";
            FileUtilities.WriteShaderGraphToDisk(m_SubGraphPath, subGraph);
            AssetDatabase.ImportAsset(m_SubGraphPath, k_ImportOptions);

            var mainGraph = new GraphData();
            mainGraph.AddContexts();
            mainGraph.AddNode(new SubGraphNode { asset = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(m_SubGraphPath) });
            FileUtilities.WriteShaderGraphToDisk(m_MainGraphPath, mainGraph);
            AssetDatabase.ImportAsset(m_MainGraphPath, k_ImportOptions);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Shader>(m_MainGraphPath));
        }

        [Test]
        public void ImportGraph_UsedSubGraphDeleted_StillGeneratesShader()
        {
            CreateMainGraphUsingSubGraph();

            AssetDatabase.DeleteAsset(m_SubGraphPath);
            AssetDatabase.ImportAsset(m_MainGraphPath, k_ImportOptions);

            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Shader>(m_MainGraphPath));
        }

        [Test]
        public void SubGraphNode_SubGraphDeleted_DocumentationUrlDoesNotThrow()
        {
            CreateMainGraphUsingSubGraph();
            AssetDatabase.DeleteAsset(m_SubGraphPath);

            var graph = new GraphData();
            MultiJson.Deserialize(graph, File.ReadAllText(m_MainGraphPath));
            graph.OnEnable();
            var node = graph.GetNodes<SubGraphNode>().Single();

            Assert.IsNull(node.asset);
            Assert.DoesNotThrow(() => { var url = node.documentationURL; });
        }
    }
}
