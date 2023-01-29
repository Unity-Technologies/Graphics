using System;
using System.IO;
using NUnit.Framework;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    partial class CoreUtilsTests
    {
        [Test]
        [TestCase("Assets/TestFolder/")]
        [TestCase("Assets/TestFolder\\")]
        [TestCase("Assets/TestFolder/123/Folder/")]
        [TestCase("Assets/TestFolder\\123\\Folder\\")]
        [TestCase("Assets/TestFolder/something.mat")]
        [TestCase("Assets/TestFolder\\something.mat")]
        public void EnsureFolderTreeInAssetFilePath(string path)
        {
            string folderPath = Path.GetDirectoryName(path);
            CoreUtils.EnsureFolderTreeInAssetFilePath(path);
            Assert.True(AssetDatabase.IsValidFolder(folderPath));
        }

        [Test]
        [TestCase("NotAssetsFolder/TestFolder/", TestName = "EnsureFolderTreeInAssetFilePath throws when filePath does not start with Assets/")]
        public void EnsureFolderTreeInAssetFilePathThrows(string folderPath)
        {
            Assert.False(AssetDatabase.IsValidFolder(folderPath));
            Assert.Throws<ArgumentException>(() => CoreUtils.EnsureFolderTreeInAssetFilePath(folderPath));
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/TestFolder");
        }
    }
}
