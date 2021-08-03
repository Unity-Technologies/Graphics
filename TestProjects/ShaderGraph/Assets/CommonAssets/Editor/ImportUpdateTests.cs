using NUnit.Framework;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class ImportUpdateTests
    {
        public class SingleImportCases : IEnumerable
        {
            private const string kGraphsLocation = "PreviousGraphVersions/";
            public IEnumerator GetEnumerator()
            {
                var versionDirs =  Directory.GetDirectories(Application.dataPath + "/../" + kGraphsLocation, "*", SearchOption.TopDirectoryOnly);
                foreach (var dir in versionDirs)
                    foreach (var assetPath in Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly))
                        yield return assetPath;
            }
        }

        public class MultiImportDirectories : IEnumerable
        {
            private const string kGraphsLocation = "PreviousGraphVersions/";
            public IEnumerator GetEnumerator()
            {
                var versionDirs = Directory.GetDirectories(Application.dataPath + "/../" + kGraphsLocation, "*", SearchOption.TopDirectoryOnly);
                foreach (var dir in versionDirs)
                    foreach (var multiDir in Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly))
                        yield return multiDir;
            }
        }

        [OneTimeSetUp]
        public void Setup()
        {
            if (!Directory.Exists(Application.dataPath + "Testing/ImportTests"))
                Directory.CreateDirectory(Application.dataPath + "Testing/ImportTests");
            AssetDatabase.Refresh();

            if (!AssetDatabase.IsValidFolder("Assets/Testing/ImportTests"))
            {
                AssetDatabase.CreateFolder("Assets/Testing", "ImportTests");
            }
        }

        [TestCaseSource(typeof(SingleImportCases))]
        public void ImportSingle(string assetPath)
        {
            string fileName = Path.GetFileName(assetPath);
            string fileContents = File.ReadAllText(assetPath);

            string targetPath = Application.dataPath + "/Testing/ImportTests/" + fileName;
            File.WriteAllText(targetPath, fileContents);

            string unityLocalPath = "Assets/Testing/ImportTests/" + fileName;
            TestImportAsset(unityLocalPath, targetPath);
        }

        [TestCaseSource(typeof(MultiImportDirectories))]
        public void ImportMulti(string directory)
        {
            string sourceDir = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar);
            string dirName = Path.GetFileName(sourceDir);

            string targetDir = Application.dataPath + "/Testing/ImportTests/" + dirName;
            try
            {
                // pause asset database, until everything is copied
                AssetDatabase.StartAssetEditing();
                DirectoryCopy(sourceDir, targetDir, true, true);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();

            foreach (var assetFullPath in Directory.GetFiles(targetDir, "*.shader*", SearchOption.TopDirectoryOnly))
            {
                if (!assetFullPath.EndsWith(".meta"))
                {
                    string relativeFilePath = assetFullPath.Substring(targetDir.Length);
                    string unityLocalPath = "Assets/Testing/ImportTests/" + dirName + relativeFilePath;
                    TestImportAsset(unityLocalPath, assetFullPath);
                }
            }
        }

        public void TestImportAsset(string unityLocalPath, string fullPath)
        {
            unityLocalPath = unityLocalPath.Replace("\\", "/");
            Debug.Log("Testing file: " + unityLocalPath);

            // invoke an import
            AssetDatabase.ImportAsset(unityLocalPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);

            // double check we can load it up and validate it
            string fileContents = File.ReadAllText(fullPath);
            Assert.Greater(fileContents.Length, 0);

            var graphGuid = AssetDatabase.AssetPathToGUID(unityLocalPath);
            var messageManager = new MessageManager();
            GraphData graphData = new GraphData() { assetGuid = graphGuid, messageManager = messageManager };
            MultiJson.Deserialize(graphData, fileContents);
            graphData.OnEnable();
            graphData.ValidateGraph();

            string fileExtension = Path.GetExtension(fullPath).ToLower();
            bool isSubgraph = (fileExtension == "shadersubgraph");
            if (isSubgraph)
            {
                // check that the SubGraphAsset is the same after versioning twice
                // this is important to ensure we're not importing subgraphs non-deterministically when they are out-of-date on disk
                AssetDatabase.ImportAsset(unityLocalPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
                var subGraph = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(unityLocalPath);
                var serialized = EditorJsonUtility.ToJson(subGraph);

                AssetDatabase.ImportAsset(unityLocalPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);
                var subGraph2 = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(unityLocalPath);
                var serialized2 = EditorJsonUtility.ToJson(subGraph2);

                Assert.AreEqual(serialized, serialized2, $"Importing the subgraph {unityLocalPath} twice resulted in different subgraph assets.");
            }
            else
            {
                // check that the generated shader is the same after versioning twice
                // this is important to ensure we're not importing shaders non-deterministically when they are out-of-date on disk
                string fileNameNoExtension = Path.GetFileNameWithoutExtension(fullPath);
                var generator = new Generator(graphData, graphData.outputNode, GenerationMode.ForReals, fileNameNoExtension, null);
                string shader = generator.generatedShader;

                // version again
                GraphData graphData2 = new GraphData() { assetGuid = graphGuid, messageManager = messageManager };
                MultiJson.Deserialize(graphData2, fileContents);
                graphData2.OnEnable();
                graphData2.ValidateGraph();
                var generator2 = new Generator(graphData2, graphData2.outputNode, GenerationMode.ForReals, fileNameNoExtension, null);
                string shader2 = generator2.generatedShader;

                Assert.AreEqual(shader, shader2, $"Importing the graph {unityLocalPath} twice resulted in different generated shaders.");
            }
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            AssetDatabase.Refresh();
            foreach (string assetGuid in AssetDatabase.FindAssets("*", new string[] { "Assets/Testing/ImportTests" }))
            {
                Debug.Log(AssetDatabase.GUIDToAssetPath(assetGuid));
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(assetGuid));
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, bool overwriteFiles)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, overwriteFiles);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs, overwriteFiles);
                }
            }
        }
    }
}
