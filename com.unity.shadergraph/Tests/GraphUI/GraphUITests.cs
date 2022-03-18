using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    [TestFixture]
    class UITestFixture
    {
        static readonly Rect k_WindowRect = new Rect(Vector2.zero, new Vector2(SelectionDragger.panAreaWidth * 8, SelectionDragger.panAreaWidth * 6));

        //ShaderGraphEditorWindow m_EditorWindow;

        [OneTimeSetUp]
        public void Setup()
        {
            //m_EditorWindow = EditorWindow.GetWindowWithRect<ShaderGraphEditorWindow>(k_WindowRect);
        }

        [Test]
        public void CreateGraphAssetTest()
        {
            var newGraphAction = ScriptableObject.CreateInstance<GraphAssetUtils.CreateAssetAction>();
            var assetPath = $"Assets\\{ShaderGraphStencil.DefaultAssetName}.{ShaderGraphStencil.Extension}";
            newGraphAction.Action(0, assetPath, "");
            var newAsset = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(assetPath);
            Assert.IsNotNull(newAsset);
        }

        [Test]
        public void GraphSubAssetAssociationTest()
        {
            var newGraphAction = ScriptableObject.CreateInstance<GraphAssetUtils.CreateAssetAction>();
            var assetPath = $"Assets\\{ShaderGraphStencil.DefaultAssetName}.{ShaderGraphStencil.Extension}";
            newGraphAction.Action(0, assetPath, "");
            var materialAsset = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            Assert.IsNotNull(materialAsset);
            var shaderAsset = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
            Assert.IsNotNull(shaderAsset);
            var assetModel = AssetDatabase.LoadAssetAtPath<ShaderGraphAssetModel>(assetPath);
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
