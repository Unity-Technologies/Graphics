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
            string fileContents = File.ReadAllText(assetPath);
            string localFilePath = "Assets/Testing/ImportTests/" + fileName;
            File.WriteAllText(Application.dataPath + "/Testing/ImportTests/" + fileName, fileContents);
            AssetDatabase.ImportAsset(localFilePath);
            var graphGuid = AssetDatabase.AssetPathToGUID(localFilePath);
            var messageManager = new MessageManager();
            GraphData graphData = new GraphData() { assetGuid = graphGuid, messageManager = messageManager };
            MultiJson.Deserialize(graphData, fileContents);
            graphData.OnEnable();
            graphData.ValidateGraph();
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
