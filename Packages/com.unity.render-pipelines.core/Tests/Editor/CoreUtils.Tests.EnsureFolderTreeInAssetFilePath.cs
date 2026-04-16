using System;
using System.IO;
using System.Text;
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
        [TestCase("Assets/TestFolder//DoubleSlash/File.txt", TestName = "Path with double slashes")]
        [TestCase("Assets/TestFolder///TripleSlash/File.txt", TestName = "Path with triple slashes")]
        [TestCase("Assets/TestFolder/Mixed\\Separators/File.txt", TestName = "Mixed forward and backslashes")]
        [TestCase("Assets/TestFolder/A/B/C/D/E/F/G/H/I/J/Deep.txt", TestName = "Deep nested path (10 levels)")]
        [TestCase("Assets/TestFolder/Unicode文件夹/File.txt", TestName = "Unicode characters in folder name")]
        [TestCase("Assets/TestFolder/Émojis😀/File.txt", TestName = "Emoji in folder name")]
        [TestCase("Assets/TestFolder/VeryLongFolderNameThatExceedsNormalLength123456789012345678901234567890/File.txt", TestName = "Very long folder name")]
        public void EnsureFolderTreeInAssetFilePath(string path)
        {
            string folderPath = Path.GetDirectoryName(path);
            CoreUtils.EnsureFolderTreeInAssetFilePath(path);
            Assert.True(AssetDatabase.IsValidFolder(folderPath), $"Folder '{folderPath}' should exist.");
        }

        [Test]
        [TestCase("Assets", TestName = "Just Assets and not Assets/")]
        [TestCase("NotAssetsFolder/TestFolder/", TestName = "FilePath does not start with Assets/")]
        [TestCase("assets/TestFolder/", TestName = "Lowercase assets (case sensitivity)")]
        [TestCase("ASSETS/TestFolder/", TestName = "Uppercase ASSETS")]
        [TestCase("FileName.txt", TestName = "FileName.txt")]
        [TestCase("C:\\Filename.txt", TestName = "C:\\Filename.txt")]
        public void EnsureFolderTreeInAssetFilePathThrows(string folderPath)
        {
            Assert.Throws<ArgumentException>(() => CoreUtils.EnsureFolderTreeInAssetFilePath(folderPath));
        }

        [Test]
        public void EnsureFolderTreeInAssetFilePath_NullOrEmpty_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => CoreUtils.EnsureFolderTreeInAssetFilePath(null));
            Assert.DoesNotThrow(() => CoreUtils.EnsureFolderTreeInAssetFilePath(""));
            Assert.DoesNotThrow(() => CoreUtils.EnsureFolderTreeInAssetFilePath(string.Empty));
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder("Assets/TestFolder"))
            {
                AssetDatabase.DeleteAsset("Assets/TestFolder");
            }
            AssetDatabase.Refresh();
        }
    }
}
