using System.Collections;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using System.IO;
using System.Collections.Generic;

/* Changes:
 * Made ShaderGraphImporterEditor.ShowGraphEditWindow public
 * Made MaterialGraphEditWindow.graphEditorView public
 * Altered MasterPreviewView.OnMouseDragPreviewMesh slightly
 */

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class SaveLoadTests
    {
        GraphEditorView m_GraphEditorView;
        MaterialGraphEditWindow m_Window;

        static string sourceDirectoryPath => Application.dataPath + "/../SaveLoadGraphs";
        static string targetDirectoryPath => Application.dataPath + "/Testing/SaveLoadGraphs";
        static string targetUnityDirectoryPath => "Assets/Testing/SaveLoadGraphs";

        public class ImportTestAssetsEnumerator : IEnumerable
        {
            public IEnumerator GetEnumerator()
            {
                foreach (string assetGuid in AssetDatabase.FindAssets("*", new string[] { targetUnityDirectoryPath }))
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                    yield return assetPath;
                }
            }
        }

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

        public void OpenGraphWindow(string graphName)
        {
            // Open up the window
            if (!ShaderGraphImporterEditor.ShowGraphEditWindow(graphName))
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

        // Tests that loading and saving a fully versioned graph file doesn't change the file on disk.
        [UnityTest]
        public IEnumerator SaveAndLoadTests()
        {
            foreach (string assetGuid in AssetDatabase.FindAssets("*", new string[] { targetUnityDirectoryPath }))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                yield return SaveLoadSaveDoesntChangeFile(assetPath);
            }
        }

        // [TestCaseSource(typeof(ImportTestAssetsEnumerator))]     // TestCaseSource doesn't seem to work with yield return ...
        IEnumerator SaveLoadSaveDoesntChangeFile(string assetPath)
        {
            // Load!
            OpenGraphWindow(assetPath);

            yield return null;

            string originalFile = File.ReadAllText(assetPath);
            var fileInfo = new FileInfo(assetPath);
            var originalModifiedTime = fileInfo.LastWriteTime;

            yield return null;

            Assert.IsFalse(m_Window.FileOnDiskHasChanged(), $"Loading the graph window should not change the file on disk ({assetPath}).");
            bool firstSaveChanged = m_Window.GraphHasChangedSinceLastSerialization();
            bool graphNotEqualFile = m_Window.GraphIsDifferentFromFileOnDisk();
            Assert.AreEqual(firstSaveChanged, graphNotEqualFile, $"GraphHasChangedSinceLastSerialization should match GraphIsDifferentFromFileOnDisk when no external changes have been made to the file ({assetPath}).");

            // Save!
            bool changedOnFirstSave = m_Window.SaveAsset();
            string firstSaveFile = File.ReadAllText(assetPath);
            fileInfo.Refresh();
            var firstSaveModifiedTime = fileInfo.LastWriteTime;

            Assert.That(originalModifiedTime.Equals(firstSaveModifiedTime), Is.EqualTo(!changedOnFirstSave), $"SaveAsset should return true only when the file on disk is modified ({assetPath}).");
            Assert.That(originalFile.Equals(firstSaveFile), Is.EqualTo(!firstSaveChanged), $"GraphHasChangedSinceLastSerialization should only return true when the serialized file would be different ({assetPath}).");

            yield return null;

            CloseGraphWindow();

            yield return null;

            // Load!
            OpenGraphWindow(assetPath);

            yield return null;

            Assert.IsFalse(m_Window.FileOnDiskHasChanged(), $"Loading the graph window should not change the file on disk 2 ({assetPath}).");
            bool secondSaveDirty = m_Window.GraphHasChangedSinceLastSerialization();
            bool secondSaveGraphNotEqualFile = m_Window.GraphIsDifferentFromFileOnDisk();
            Assert.AreEqual(firstSaveChanged, graphNotEqualFile, $"GraphHasChangedSinceLastSerialization should match GraphIsDifferentFromFileOnDisk when no external changes have been made to the file 2 ({assetPath}).");

            bool changedOnSecondSave = m_Window.SaveAsset();
            string secondSaveFile = File.ReadAllText(assetPath);
            fileInfo.Refresh();
            var secondSaveModifiedTime = fileInfo.LastWriteTime;

            Assert.IsFalse(secondSaveDirty, $"Save, Load, Save should not result in any changes to the file on the second save ({assetPath}).");

            Assert.That(firstSaveModifiedTime.Equals(secondSaveModifiedTime), Is.EqualTo(!changedOnSecondSave), $"SaveAsset should return true only when the file on disk is modified 2 ({assetPath}).");
            Assert.That(firstSaveFile.Equals(secondSaveFile), Is.EqualTo(!secondSaveDirty), $"IsDirty should only return true when the serialized files would be different 2 ({assetPath}).");

            Assert.That(firstSaveFile.Equals(secondSaveFile), Is.True, $"Loading and saving a graph file twice should not result in changes to the file on disk ({assetPath}).");

            yield return null;

            CloseGraphWindow();

            yield return null;
        }
    }
}
