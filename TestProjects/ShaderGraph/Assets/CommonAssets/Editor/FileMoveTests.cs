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
    internal class FileMoveTests
    {
        GraphEditorView m_GraphEditorView;
        MaterialGraphEditWindow m_Window;
        GraphData m_Graph;

        static string sourceDirectoryPath => Application.dataPath + "/../MoveTest";
        static string sourceDirectoryPathSub => Application.dataPath + "/../MoveTest/SubFolder";
        static string targetDirectoryPath => Application.dataPath + "/Testing/MoveTest";
        static string targetDirectoryPath2 => Application.dataPath + "/Testing/MoveTest2";
        static string targetDirectoryPathSub => Application.dataPath + "/Testing/MoveTest/SubFolder";
        static string targetUnityDirectoryPath => "Assets/Testing/MoveTest";
        static string targetUnityDirectoryPath2 => "Assets/Testing/MoveTest2";

        [OneTimeSetUp]
        public void Setup()
        {
            // recursive delete
            if (Directory.Exists(targetDirectoryPath2))
            {
                Directory.Delete(targetDirectoryPath2, true);

                // sync AssetDatabase
                AssetDatabase.DeleteAsset(targetUnityDirectoryPath2);
            }

            Directory.CreateDirectory(targetDirectoryPath);
            Directory.CreateDirectory(targetDirectoryPathSub);

            try
            {
                AssetDatabase.StartAssetEditing();

                // copy all files from source directory to target directory
                string[] sourceFiles = Directory.GetFiles(sourceDirectoryPath, "*", SearchOption.TopDirectoryOnly);
                foreach (var sourceFilePath in sourceFiles)
                {
                    string fileName = Path.GetFileName(sourceFilePath);
                    File.Copy(sourceFilePath, targetDirectoryPath + "/" + fileName);
                }

                // copy all files from source directory to target directory
                sourceFiles = Directory.GetFiles(sourceDirectoryPathSub, "*", SearchOption.TopDirectoryOnly);
                foreach (var sourceFilePath in sourceFiles)
                {
                    string fileName = Path.GetFileName(sourceFilePath);
                    File.Copy(sourceFilePath, targetDirectoryPathSub + "/" + fileName);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // after all files are copied, make sure everything in the directory is imported
            AssetDatabase.ImportAsset(targetUnityDirectoryPath, ImportAssetOptions.ImportRecursive);
        }

        public void OpenGraphWindow(string graphName)
        {
            // Open up the window
            if (!ShaderGraphImporterLegacyEditor.ShowGraphEditWindow(graphName))
            {
                Assert.Fail("ShaderGraphImporterEditor.ShowGraphEditWindow could not open " + graphName);
            }

            m_Window = EditorWindow.GetWindow<MaterialGraphEditWindow>();

            if (m_Window == null)
            {
                Assert.Fail("Could not open MaterialGraphEditWindow");
            }

            // EditorWindow.GetWindow will return a new window if one is not found. A new window will have graphObject == null.
            if (m_Window.graphObject == null)
            {
                Assert.Fail("Existing Shader Graph window of " + graphName + " not found.");
            }

            m_GraphEditorView = m_Window.graphEditorView;
        }

        [TearDown]
        public void CloseGraphWindow()
        {
            if (m_Window != null)
            {
                m_Window.graphObject = null; // Don't spawn ask-to-save dialog
                m_Window.Close();
            }
            m_Window = null;
            m_GraphEditorView = null;
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            try
            {

                AssetDatabase.StartAssetEditing();

                // delete everything from target directory (via assetdatabase to clear anything relevant)
                foreach (string assetGuid in AssetDatabase.FindAssets("*", new string[] { targetUnityDirectoryPath }))
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(assetGuid));
                }

                foreach (string assetGuid in AssetDatabase.FindAssets("*", new string[] { targetUnityDirectoryPath2 }))
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(assetGuid));
                }

                FileUtil.DeleteFileOrDirectory(targetDirectoryPath2);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            if (Directory.Exists(targetDirectoryPath2))
            {
                Directory.Delete(targetDirectoryPath2, true);

                // sync AssetDatabase
                AssetDatabase.DeleteAsset(targetUnityDirectoryPath2);
            }
        }

        // Tests that loading and saving a fully versioned graph file doesn't change the file on disk.
        [UnityTest]
        public IEnumerator MoveDirectoryTests()
        {
            yield return null;

            // rename the directory
            AssetDatabase.MoveAsset(targetUnityDirectoryPath, targetUnityDirectoryPath2);

            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            var graphPath = targetUnityDirectoryPath2 + "/ShaderGraph.shadergraph";
            OpenGraphWindow(graphPath);

            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            // try adding the subgraph node -- see if the asset still works after the files have been moved
            m_Graph = m_Window.graphObject.graph as GraphData;
            var subgraph = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(graphPath);
            var node = new SubGraphNode { asset = subgraph };
            m_Graph.AddNode(node);

            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            yield return null;

            CloseGraphWindow();
        }
    }
}
