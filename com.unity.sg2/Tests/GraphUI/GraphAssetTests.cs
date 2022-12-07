using System;
using NUnit.Framework;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    [TestFixture]
    class AssetTestFixture
    {
        static readonly Rect k_WindowRect = new Rect(Vector2.zero, new Vector2(/*SelectionDragger.panAreaWidth*/ 100 * 8, /*SelectionDragger.panAreaWidth*/ 100 * 6));

        [OneTimeSetUp]
        public void Setup()
        {
        }

        [Test]
        public void CreateGraphAssetTest()
        {
            var newGraphAction = ScriptableObject.CreateInstance<AssetUtils.CreateAssetGraphAction>();
            var assetPath = $"Assets\\{ShaderGraphStencil.DefaultGraphAssetName}.{ShaderGraphStencil.GraphExtension}";
            newGraphAction.Action(0, assetPath, "");
            var newAsset = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(assetPath);
            Assert.IsNotNull(newAsset);
        }

        [Test]
        public void GraphSubAssetAssociationTest()
        {
            var newGraphAction = ScriptableObject.CreateInstance<AssetUtils.CreateAssetGraphAction>();
            var assetPath = $"Assets\\{ShaderGraphStencil.DefaultGraphAssetName}.{ShaderGraphStencil.GraphExtension}";
            newGraphAction.Action(0, assetPath, "");
            var materialAsset = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            Assert.IsNotNull(materialAsset);
            var shaderAsset = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
            Assert.IsNotNull(shaderAsset);
            var assetModel = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(assetPath);
            Assert.IsNotNull(assetModel);
            var shaderGraphAsset = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(assetPath);
            Assert.IsNotNull(shaderGraphAsset);
        }

        [TearDown]
        public void TestCleanup()
        {
        }
    }
}
