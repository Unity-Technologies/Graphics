using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ContextLayeredDataStorage;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.HeadlessPreview.UnitTests
{
    [TestFixture]
    class HeadlessPreviewTestFixture
    {
        HeadlessPreviewManager m_PreviewManager = new ();

        Registry.Registry m_RegistryInstance = new ();

        [OneTimeSetUp]
        public void Setup()
        {
            // TODO: Figure out property
            m_RegistryInstance.Register<Types.GraphType>();
            m_RegistryInstance.Register<Types.AddNode>();
            m_RegistryInstance.Register<Types.ColorNode>();
            m_RegistryInstance.Register<Types.GraphTypeAssignment>();
        }

        bool DoesMaterialMatchColor(Material testMaterial, Color expectedColor)
        {
            return false;
        }

        bool DoesMaterialMatchImage(Material testMaterial, Texture expectedImage)
        {
            return false;
        }

        [Test]
        public void MasterPreview_SingleColor()
        {
            var assetPath = "Assets/CommonAssets/Graphs/Preview/SingleColor";
            var graphHandler = GraphUtil.OpenGraph(assetPath) as IGraphHandler;
            m_PreviewManager.SetActiveGraph(graphHandler);
            // TODO: Consider async compile/rendering, need to wait until thread is done and results are returned
            // https://forum.unity.com/threads/support-for-async-await-in-tests.787853/#post-6838493
            var masterPreviewMaterial = m_PreviewManager.RequestMasterPreviewMaterial();
            //Assert.IsTrue(DoesMaterialMatchColor(masterPreviewMaterial, Color.Red));
        }

        public void MasterPreview_AddTwoColors()
        {
            var assetPath = "Assets/CommonAssets/Graphs/Preview/AddTwoColors";
            var graphHandler = GraphUtil.OpenGraph(assetPath) as IGraphHandler;
            m_PreviewManager.SetActiveGraph(graphHandler);
            // TODO: Consider async compile/rendering, need to wait until thread is done and results are returned
            // https://forum.unity.com/threads/support-for-async-await-in-tests.787853/#post-6838493
            var masterPreviewMaterial = m_PreviewManager.RequestMasterPreviewMaterial();
            //Assert.IsTrue(DoesMaterialMatchColor(masterPreviewMaterial, Color.Green));
        }

        public void MasterPreview_SubtractTwoColors()
        {
            // Graph with two Color nodes subtracting Red from Red
            var assetPath = "Assets/CommonAssets/Graphs/Preview/SubtractTwoColors";
            var graphHandler = GraphUtil.OpenGraph(assetPath) as IGraphHandler;
            m_PreviewManager.SetActiveGraph(graphHandler);
            // TODO: Consider async compile/rendering, need to wait until thread is done and results are returned
            // https://forum.unity.com/threads/support-for-async-await-in-tests.787853/#post-6838493
            var masterPreviewMaterial = m_PreviewManager.RequestMasterPreviewMaterial();
            //Assert.IsTrue(DoesMaterialMatchColor(masterPreviewMaterial, Color.Black));
        }
    }
}
