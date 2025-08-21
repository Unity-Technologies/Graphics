using System;
using System.IO;
using NUnit.Framework;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    [Category("Utilities")]
    partial class CoreUtilsTests
    {
        [Test]
        [TestCase("Assets/", TestName = "Root Assets folder with trailing slash")]
        [TestCase("Assets/TestFolder/Folder/", TestName = "Simple folder path with trailing slash")]
        [TestCase("Assets/TestFolder\\Folder\\", TestName = "Simple folder path with backslash")]
        [TestCase("Assets/TestFolder/123/Folder/", TestName = "Numeric folder name inside path")]
        [TestCase("Assets/TestFolder\\123\\Folder\\", TestName = "Numeric folder name with backslashes")]
        [TestCase("Assets/TestFolder/something.mat", TestName = "Valid file with extension")]
        [TestCase("Assets/TestFolder\\something.mat", TestName = "Valid file with extension and backslashes")]
        [TestCase("Assets/TestFolder/Folder-With-Dashes/", TestName = "Folder name containing dashes")]
        [TestCase("Assets/TestFolder/Folder With Spaces/", TestName = "Folder name containing spaces")]
        [TestCase("Assets/TestFolder/Folder_123/SubFolder/", TestName = "Folder name with underscore and numbers")]
        [TestCase("Assets/TestFolder/Folder.Special/", TestName = "Folder name containing a dot")]
        [TestCase("Assets/TestFolder/Folder#Test/SubFolder/", TestName = "Folder name containing a hash character")]
        [TestCase("Assets/TestFolder/Folder@Test/SubFolder/File.txt", TestName = "File name containing @ character")]
        [TestCase("Assets/TestFolder/Folder\\SubFolder/File", TestName = "File without extension")]

        public void EnsureFolderTreeInAssetFilePath(string path)
        {
            string folderPath = Path.GetDirectoryName(path);
            CoreUtils.EnsureFolderTreeInAssetFilePath(path);
            Assert.True(AssetDatabase.IsValidFolder(folderPath), $"Folder '{folderPath}' should exist.");
        }

        [Test]
        [TestCase("Assets", TestName = "Just Assets and not Assets/")]
        [TestCase("NotAssetsFolder/TestFolder/", TestName = "FilePath does not start with Assets/")]
        public void EnsureFolderTreeInAssetFilePathThrows(string folderPath)
        {
            Assert.Throws<ArgumentException>(() => CoreUtils.EnsureFolderTreeInAssetFilePath(folderPath));
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/TestFolder");
        }
    }
}
