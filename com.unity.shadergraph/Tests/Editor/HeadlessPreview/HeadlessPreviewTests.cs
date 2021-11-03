using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ContextLayeredDataStorage;
using UnityEditor.ShaderGraph.Registry;

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

        [Test]
        public void BasicPreviewOutputTest()
        {
            // Create
        }
    }
}
