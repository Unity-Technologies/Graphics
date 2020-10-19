using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class ImportUpdateTests
    {
        public class ImportCases : IEnumerable
        {
            private const string kGraphsLocation = "PreviousGraphVersions/";
            public IEnumerator GetEnumerator()
            {
                return Directory.GetFiles(Application.dataPath + "/../" + kGraphsLocation, "*", SearchOption.AllDirectories).GetEnumerator();
            }
        }

        [OneTimeSetUp]
        public void Setup()
        {
            if(!AssetDatabase.IsValidFolder("Assets/Testing/ImportTests"))
            {
                AssetDatabase.CreateFolder("Assets/Testing", "ImportTests");
            }
        }

        [TestCaseSource(typeof(ImportCases))]
        public void CopyOverAndImport(string assetPath)
        {
            string fileName = Path.GetFileName(assetPath);
            string fileNameNoExtension = Path.GetFileNameWithoutExtension(assetPath);
            string fileContents = File.ReadAllText(assetPath);
            string fileExtension = Path.GetExtension(assetPath).ToLower();
            bool isSubgraph = (fileExtension == "shadersubgraph");

            string localFilePath = "Assets/Testing/ImportTests/" + fileName;
            string localFilePathNoExtension = "Assets/Testing/ImportTests/" + fileNameNoExtension;

            File.WriteAllText(Application.dataPath + "/Testing/ImportTests/" + fileName, fileContents);
            AssetDatabase.ImportAsset(localFilePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
            var graphGuid = AssetDatabase.AssetPathToGUID(localFilePath);
            var messageManager = new MessageManager();
            GraphData graphData = new GraphData() { assetGuid = graphGuid, messageManager = messageManager };
            MultiJson.Deserialize(graphData, fileContents);
            graphData.OnEnable();
            graphData.ValidateGraph();

            if (isSubgraph)
            {
                // check that the SubGraphAsset is the same after versioning twice
                // this is important to ensure we're not importing subgraphs non-deterministically when they are out-of-date on disk
                AssetDatabase.ImportAsset(localFilePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
                var subGraph = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(localFilePath);
                var serialized = EditorJsonUtility.ToJson(subGraph);

                AssetDatabase.ImportAsset(localFilePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
                var subGraph2 = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(localFilePath);
                var serialized2 = EditorJsonUtility.ToJson(subGraph2);

                Assert.AreEqual(serialized, serialized2, $"Importing the subgraph {localFilePath} twice resulted in different subgraph assets.");
            }
            else
            {
                // check that the generated shader is the same after versioning twice
                // this is important to ensure we're not importing shaders non-deterministically when they are out-of-date on disk
                var generator = new Generator(graphData, graphData.outputNode, GenerationMode.ForReals, fileNameNoExtension, null);
                string shader = generator.generatedShader;

                // version again
                GraphData graphData2 = new GraphData() { assetGuid = graphGuid, messageManager = messageManager };
                MultiJson.Deserialize(graphData2, fileContents);
                graphData2.OnEnable();
                graphData2.ValidateGraph();
                var generator2 = new Generator(graphData2, graphData2.outputNode, GenerationMode.ForReals, fileNameNoExtension, null);
                string shader2 = generator2.generatedShader;

                Assert.AreEqual(shader, shader2, $"Importing the graph {localFilePath} twice resulted in different generated shaders.");
            }
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            foreach (string assetGuid in AssetDatabase.FindAssets("*", new string[] { "Assets/Testing/ImportTests" }))
            {
                Debug.Log(AssetDatabase.GUIDToAssetPath(assetGuid));
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(assetGuid));
            }
        }
    }
}
