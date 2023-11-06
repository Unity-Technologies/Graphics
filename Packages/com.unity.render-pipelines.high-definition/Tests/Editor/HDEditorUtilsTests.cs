using NUnit.Framework;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.Tests
{
    class HDEditorUtilsTest
    {
        private static readonly string k_AssetPath =
            $"Assets/{nameof(HDEditorUtilsTest)}/{nameof(HDRenderPipelineAsset)}.asset";

        private HDRenderPipelineAsset m_Asset;

        [SetUp]
        public void Setup()
        {
            if (GraphicsSettings.currentRenderPipeline is not HDRenderPipelineAsset)
                Assert.Ignore("This is an HDRP Tests, and the current pipeline is not HDRP.");

            m_Asset = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
            m_Asset.name = nameof(HDEditorUtilsTest);

            CoreUtils.EnsureFolderTreeInAssetFilePath(k_AssetPath);

            AssetDatabase.CreateAsset(m_Asset, k_AssetPath);
            AssetDatabase.SaveAssets();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Asset != null)
                AssetDatabase.DeleteAsset(k_AssetPath);
        }

        [Test]
        [TestCase(HDUtils.k_HdrpAssetBuildLabel, ExpectedResult = true)]
        [TestCase("", ExpectedResult = false)]
        public bool CheckAssetContainsHDRPLabel(string label)
        {
            if (!string.IsNullOrEmpty(label))
            {
                AssetDatabase.SetLabels(m_Asset, new[] { label });
                AssetDatabase.SaveAssets();
            }

            return HDEditorUtils.NeedsToBeIncludedInBuild(m_Asset);
        }
    }
}
