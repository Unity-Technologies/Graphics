using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class ContextBlockTests
    {
        static string kGraphName = "Assets/CommonAssets/Graphs/ContextBlock.shadergraph";
        GraphData m_Graph;

        GraphEditorView m_GraphEditorView;
        MaterialGraphEditWindow m_Window;

        [OneTimeSetUp]
        public void LoadGraph()
        {
            List<PropertyCollector.TextureInfo> lti;
            var assetCollection = new AssetCollection();
            ShaderGraphImporter.GetShaderText(kGraphName, out lti, assetCollection, out m_Graph);
            Assert.NotNull(m_Graph, $"Invalid graph data found for {kGraphName}");

            m_Graph.ValidateGraph();

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

        [OneTimeTearDown]
        public void Cleanup()
        {
            // Don't spawn ask-to-save dialog
            m_Window.graphObject = null;
            m_Window.Close();
        }

        [Test]
        public void DeleteableTest()
        {
            var propertyInfo = typeof(MaterialGraphView).GetProperty("canDeleteSelection", BindingFlags.NonPublic | BindingFlags.Instance);
            
            m_GraphEditorView.graphView.graphElements.ForEach(e =>
            {
                if ( e is ContextView view)
                {
                    m_GraphEditorView.graphView.AddToSelection(view);
                    Assert.IsFalse((bool)propertyInfo.GetValue(m_GraphEditorView.graphView));
                    m_GraphEditorView.graphView.ClearSelection();
                }
            });
        }
    }
}
