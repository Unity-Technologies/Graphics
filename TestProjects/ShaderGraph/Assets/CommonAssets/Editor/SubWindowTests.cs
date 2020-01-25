using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Inspector;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class SubWindowTests
    {
        static string kGraphName = "Assets/CommonAssets/Graphs/SubWindow.shadergraph";
        GraphData m_Graph;
        GraphEditorView m_GraphEditorView;

        MaterialGraphEditWindow _window;

        [OneTimeSetUp]
        public void LoadGraph()
        {
            List<PropertyCollector.TextureInfo> lti;
            var lsadp = new List<string>();
            ShaderGraphImporter.GetShaderText(kGraphName, out lti, lsadp, out m_Graph);
            Assert.NotNull(m_Graph, $"Invalid graph data found for {kGraphName}");

            m_Graph.ValidateGraph();

            //m_GraphEditorView = m_Graph.owner
        }

        // [SetUp]
        [Test]
        public void TogglePreviews()
        {
            if (ShaderGraphImporterEditor.ShowGraphEditWindow("HelloWorld"))
            {
                Assert.Fail("ShaderGraphImporterEditor.ShowGraphEditWindow return true on a Shader Graph that does not exists");
            }

            if (!ShaderGraphImporterEditor.ShowGraphEditWindow(kGraphName))
            {
                Assert.Fail("ShaderGraphImporterEditor.ShowGraphEditWindow could not open " + kGraphName);
            }

            _window = EditorWindow.GetWindow<MaterialGraphEditWindow>();

            if (_window == null)
            {
                Assert.Fail("Could not open window");
            }

            // EditorWindow.GetWindow will return a new window if one is not found. A new window will have graphObject == null/
            if (_window.graphObject == null)
            {
                Assert.Fail("Existing Shader Graph window of " + kGraphName + " not found.");
            }

            // TODO: I think we don't have to make this public
            m_GraphEditorView = _window.graphEditorView;

            Blackboard blackboard;
            MasterPreviewView masterPreviewView;

            m_GraphEditorView.viewSettings.isBlackboardVisible = true;
            m_GraphEditorView.viewSettings.isPreviewVisible = true;

            m_GraphEditorView.UpdateSubWindowsVisibility(); // TODO: I don't think we should have to make this public

            blackboard = m_GraphEditorView.GetFirstOfType<Blackboard>();
            masterPreviewView = m_GraphEditorView.GetFirstOfType<MasterPreviewView>();

//            if (blackboard == null)
//            {
//                Assert.Fail("Blackboard is not visible when it should be.");
//            }
            if (masterPreviewView == null)
            {
                Assert.Fail("MasterPreviewView is not visible when it should be.");
            }

            m_GraphEditorView.viewSettings.isBlackboardVisible = false;
            m_GraphEditorView.viewSettings.isPreviewVisible = false;

            m_GraphEditorView.UpdateSubWindowsVisibility(); // TODO: I don't think we should have to make this public

            blackboard = null;
            masterPreviewView = null;
            blackboard = m_GraphEditorView.GetFirstOfType<Blackboard>();
            masterPreviewView = m_GraphEditorView.GetFirstOfType<MasterPreviewView>();

//            if (blackboard != null)
//            {
//                Assert.Fail("Blackboard remained visible when it should not be.");
//            }
            if (masterPreviewView != null)
            {
                Assert.Fail("MasterPreviewView remained visible when it should not be.");
            }
        }

    }
}
