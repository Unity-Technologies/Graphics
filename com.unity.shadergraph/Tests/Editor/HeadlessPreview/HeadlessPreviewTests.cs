using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ContextLayeredDataStorage;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine;

namespace UnityEditor.ShaderGraph.HeadlessPreview.UnitTests
{
    [TestFixture]
    class HeadlessPreviewTestFixture
    {
        HeadlessPreviewManager m_PreviewManager = new HeadlessPreviewManager();

        [OneTimeSetUp]
        public void Setup()
        {

        }

        bool DoesMaterialMatchColor(Material testMaterial, Color expectedColor)
        {

        }

        bool DoesMaterialMatchImage(Material testMaterial, Image expectedImage)
        {

        }

        [Test]
        public void MasterPreview_SingleColor()
        {
            var assetPath = "Assets/CommonAssets/Graphs/Preview/SingleColor";
            GraphDelta graphHandler = GraphUtil.OpenGraph(assetPath) as GraphDelta;
            m_PreviewManager.SetActiveGraph(graphHandler);
            // TODO: Consider async compile/rendering, need to wait until thread is done and results are returned
            // https://forum.unity.com/threads/support-for-async-await-in-tests.787853/#post-6838493
            var masterPreviewMaterial = m_PreviewManager.RequestMasterPreviewMaterial();
            Assert.IsTrue(DoesMaterialMatchColor(masterPreviewMaterial, Color.Red));
        }

        public void MasterPreview_AddTwoColors()
        {
            var assetPath = "Assets/CommonAssets/Graphs/Preview/AddTwoColors";
            GraphDelta graphHandler = GraphUtil.OpenGraph(assetPath) as GraphDelta;
            m_PreviewManager.SetActiveGraph(graphHandler);
            // TODO: Consider async compile/rendering, need to wait until thread is done and results are returned
            // https://forum.unity.com/threads/support-for-async-await-in-tests.787853/#post-6838493
            var masterPreviewMaterial = m_PreviewManager.RequestMasterPreviewMaterial();
            Assert.IsTrue(DoesMaterialMatchColor(masterPreviewMaterial, Color.Green));
        }

        public void MasterPreview_SubtractTwoColors()
        {
            // Graph with two Color nodes subtracting Red from Red
            var assetPath = "Assets/CommonAssets/Graphs/Preview/SubtractTwoColors";
            GraphDelta graphHandler = GraphUtil.OpenGraph(assetPath) as GraphDelta;
            m_PreviewManager.SetActiveGraph(graphHandler);
            // TODO: Consider async compile/rendering, need to wait until thread is done and results are returned
            // https://forum.unity.com/threads/support-for-async-await-in-tests.787853/#post-6838493
            var masterPreviewMaterial = m_PreviewManager.RequestMasterPreviewMaterial();
            Assert.IsTrue(DoesMaterialMatchColor(masterPreviewMaterial, Color.Black));
        }
    }
}
