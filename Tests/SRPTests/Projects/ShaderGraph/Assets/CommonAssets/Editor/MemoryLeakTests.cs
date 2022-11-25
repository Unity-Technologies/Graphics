using System.Collections;
using NUnit.Framework;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class MemoryLeakTests
    {
        static string kGraphName = "Assets/CommonAssets/Graphs/MemoryLeak.shadergraph";

        GraphEditorView m_GraphEditorView;
        MaterialGraphEditWindow m_Window;

        [SetUp]
        public void OpenGraphWindow()
        {
            // Open up the window
            if (!ShaderGraphImporterEditor.ShowGraphEditWindow(kGraphName))
            {
                Assert.Fail("ShaderGraphImporterEditor.ShowGraphEditWindow could not open " + kGraphName);
            }

            m_Window = EditorWindow.GetWindow<MaterialGraphEditWindow>();

            if (m_Window == null)
            {
                Assert.Fail("Could not open window");
            }

            // EditorWindow.GetWindow will return a new window if one is not found. A new window will have graphObject == null.
            if (m_Window.graphObject == null)
            {
                Assert.Fail("Existing Shader Graph window of " + kGraphName + " not found.");
            }

            m_GraphEditorView = m_Window.graphEditorView;
        }

        [TearDown]
        public void OnTearDown()
        {

        }

        [UnityTest]
        public IEnumerator WindowDisposeTest()
        {
            // Close the graph window, and don't spawn ask-to-save dialog
            m_Window.graphObject = null;
            m_Window.Close();

            while(m_Window.WereWindowResourcesDisposed != true)
                yield return null;

            Assert.IsTrue(m_Window.WereWindowResourcesDisposed);
        }
    }
}
