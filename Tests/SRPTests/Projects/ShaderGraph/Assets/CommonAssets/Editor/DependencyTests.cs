using System.Collections;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/* Changes:
 * Made ShaderGraphImporterEditor.ShowGraphEditWindow public
 * Made MaterialGraphEditWindow.graphEditorView public
 * Altered MasterPreviewView.OnMouseDragPreviewMesh slightly
 */

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class DependencyTests
    {
        static string sourceDirectoryPath => Application.dataPath + "/../DependencyGraphs";
        static string targetDirectoryPath => Application.dataPath + "/Testing/DependencyGraphs";
        static string targetUnityDirectoryPath => "Assets/Testing/DependencyGraphs";

        [OneTimeSetUp]
        public void Setup()
        {
            Directory.CreateDirectory(targetDirectoryPath);

            try
            {
                AssetDatabase.StartAssetEditing();

                // copy all files from source directory to target directory
                string[] sourceFiles = Directory.GetFiles(sourceDirectoryPath, "*", SearchOption.AllDirectories);
                foreach (var sourceFilePath in sourceFiles)
                {
                    string fileName = Path.GetFileName(sourceFilePath);
                    File.Copy(sourceFilePath, targetDirectoryPath + "/" + fileName);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // after all files are copied, make sure everything in the directory is imported
            AssetDatabase.ImportAsset(targetUnityDirectoryPath, ImportAssetOptions.ImportRecursive);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            // delete everything from target directory (via assetdatabase to clear anything relevant)
            foreach (string assetGuid in AssetDatabase.FindAssets("*", new string[] { targetUnityDirectoryPath }))
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(assetGuid));
            }
        }

        // Tests that loading and saving a fully versioned graph file doesn't change the file on disk.
        [UnityTest]
        public IEnumerator SubGraphDescendentsTests()
        {
            var A = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(targetUnityDirectoryPath + "/A.shadersubgraph");
            var B = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(targetUnityDirectoryPath + "/B.shadersubgraph");
            var C = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(targetUnityDirectoryPath + "/C.shadersubgraph");
            var D = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(targetUnityDirectoryPath + "/D.shadersubgraph");
            var E = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(targetUnityDirectoryPath + "/E.shadersubgraph");
            var F = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(targetUnityDirectoryPath + "/F.shadersubgraph");

            Assert.IsTrue(A.descendents.Contains(B.assetGuid));
            Assert.IsTrue(A.descendents.Contains(C.assetGuid));
            Assert.IsTrue(A.descendents.Contains(D.assetGuid));
            Assert.IsTrue(A.descendents.Contains(E.assetGuid));
            Assert.AreEqual(A.descendents.Count, 4);

            Assert.IsTrue(A.children.Contains(B.assetGuid));
            Assert.IsTrue(A.children.Contains(C.assetGuid));
            Assert.IsTrue(A.children.Contains(E.assetGuid));
            Assert.AreEqual(A.children.Count, 3);

            Assert.IsTrue(B.descendents.Contains(C.assetGuid));
            Assert.IsTrue(B.descendents.Contains(D.assetGuid));
            Assert.AreEqual(B.descendents.Count, 2);

            Assert.IsTrue(B.children.Contains(C.assetGuid));
            Assert.AreEqual(B.children.Count, 1);

            Assert.IsTrue(C.descendents.Contains(D.assetGuid));
            Assert.AreEqual(C.descendents.Count, 1);

            Assert.IsTrue(C.children.Contains(D.assetGuid));
            Assert.AreEqual(C.children.Count, 1);

            Assert.AreEqual(D.descendents.Count, 0);
            Assert.AreEqual(E.descendents.Count, 0);
            Assert.AreEqual(F.descendents.Count, 0);

            Assert.AreEqual(D.children.Count, 0);
            Assert.AreEqual(E.children.Count, 0);
            Assert.AreEqual(F.children.Count, 0);

            yield return null;

            // delete 'C' to test behavior under missing subgraphs
            // this will cause some "missing subgraph" errors, ignore them
            LogAssert.ignoreFailingMessages = true;
            var oldCGuid = C.assetGuid;
            AssetDatabase.DeleteAsset(targetUnityDirectoryPath + "/C.shadersubgraph");

            yield return null;

            LogAssert.ignoreFailingMessages = false;
            A = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(targetUnityDirectoryPath + "/A.shadersubgraph");
            B = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(targetUnityDirectoryPath + "/B.shadersubgraph");
            // var C = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(targetUnityDirectoryPath + "/C.shadersubgraph");
            D = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(targetUnityDirectoryPath + "/D.shadersubgraph");
            E = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(targetUnityDirectoryPath + "/E.shadersubgraph");
            F = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(targetUnityDirectoryPath + "/F.shadersubgraph");

            // NOTE: children currently don't guarantee anything when files are missing... so no tests for children here...

            Assert.IsTrue(A.descendents.Contains(B.assetGuid));
            Assert.IsTrue(A.descendents.Contains(oldCGuid));     // should still declare C as a (missing) descendent -- required for correct behavior
            Assert.IsFalse(A.descendents.Contains(D.assetGuid)); // D is no longer discoverable because C is missing
            Assert.IsTrue(A.descendents.Contains(E.assetGuid));
            Assert.AreEqual(A.descendents.Count, 3);

            Assert.IsTrue(B.descendents.Contains(oldCGuid));     // should still declare C as a (missing) descendent -- required for correct behavior
            Assert.IsFalse(B.descendents.Contains(D.assetGuid)); // D is no longer discoverable because C is missing
            Assert.AreEqual(B.descendents.Count, 1);

            Assert.AreEqual(D.descendents.Count, 0);
            Assert.AreEqual(E.descendents.Count, 0);
            Assert.AreEqual(F.descendents.Count, 0);
        }
    }
}
