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
            m_RegistryInstance.Register<Registry.Types.GraphType>();
            m_RegistryInstance.Register<Registry.Types.AddNode>();
            m_RegistryInstance.Register<Registry.Types.GraphTypeAssignment>();
        }

        [TearDown]
        public void TestCleanup()
        {
            // Consider flushing the cached state of the preview manager between tests, depending on the test in question
            // And/or having a separate test fixture for the contiguous/standalone tests
        }

        bool DoesMaterialMatchColor(Material testMaterial, Color expectedColor)
        {
            // TODO: Verify with Esme the intention of her material-image testing framework
            // Could just use Graphics.Blit() to compare
            // Note: use pass=-1 at start
            return false;
        }

        bool DoesMaterialMatchImage(Material testMaterial, Texture expectedImage)
        {
            // TODO: Verify with Esme the intention of her material-image testing framework
            // Could just use Graphics.Blit() to compare
            // Note: use pass=-1 at start
            return false;
        }

        [Test]
        public void MasterPreview_SingleColor()
        {
            var assetPath = "Assets/CommonAssets/Graphs/Preview/SingleColor";
            var graphHandler = GraphUtil.OpenGraph(assetPath) as IGraphHandler;
            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // TODO: Consider async compile/rendering, need to wait until thread is done and results are returned
            // https://forum.unity.com/threads/support-for-async-await-in-tests.787853/#post-6838493
            var masterPreviewMaterial = m_PreviewManager.RequestMasterPreviewMaterial();
            Assert.IsTrue(DoesMaterialMatchColor(masterPreviewMaterial, Color.red));
        }

        public void MasterPreview_AddTwoColors()
        {
            var assetPath = "Assets/CommonAssets/Graphs/Preview/AddTwoColors";
            var graphHandler = GraphUtil.OpenGraph(assetPath) as IGraphHandler;
            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // TODO: Consider async compile/rendering, need to wait until thread is done and results are returned
            // https://forum.unity.com/threads/support-for-async-await-in-tests.787853/#post-6838493
            var masterPreviewMaterial = m_PreviewManager.RequestMasterPreviewMaterial();
            Assert.IsTrue(DoesMaterialMatchColor(masterPreviewMaterial, Color.green));
        }

        public void MasterPreview_SubtractTwoColors()
        {
            // Graph with two Color nodes subtracting Red from Red
            var assetPath = "Assets/CommonAssets/Graphs/Preview/SubtractTwoColors";
            var graphHandler = GraphUtil.OpenGraph(assetPath) as IGraphHandler;
            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            // TODO: Consider async compile/rendering, need to wait until thread is done and results are returned
            // https://forum.unity.com/threads/support-for-async-await-in-tests.787853/#post-6838493
            var masterPreviewMaterial = m_PreviewManager.RequestMasterPreviewMaterial();
            Assert.IsTrue(DoesMaterialMatchColor(masterPreviewMaterial, Color.black));
        }

        public void MasterPreview_MasterPreviewShaderTest()
        {
            // Graph with node network setup as expected
            var assetPath = "Assets/CommonAssets/Graphs/Preview/MasterPreviewCode";
            var graphHandler = GraphUtil.OpenGraph(assetPath) as IGraphHandler;
            m_PreviewManager.SetActiveGraph(graphHandler);
            m_PreviewManager.SetActiveRegistry(m_RegistryInstance);

            var previewShader = m_PreviewManager.RequestMasterPreviewShaderCode();
            // Q) Possible test ideas:
            //      1) Check if the shader code provided by the preview manager matches up to the shader code provided by Interpreter directly?
            //      2) Testing the caching behavior, i.e. if changes are made or not made, the shader code object returned should match expected state
            //      3) Testing if the shader code output is valid
            //      4) Testing if shader code output compiles
            //      5) Testing if the material being generated and its shader code matches the cached shader code
            //      6) Testing if the material render output (graph & node) matches the desired state after making some property changes
            //      7) Testing if a given property exists in the material property block?
            //      8) Testing if a property is at a given value in the MPB after setting the property
            //
        }
    }
}
