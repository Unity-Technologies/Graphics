using System.Collections;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/* Changes:
 * Made ShaderGraphImporterEditor.ShowGraphEditWindow public
 * Made MaterialGraphEditWindow.graphEditorView public
 * Altered MasterPreviewView.OnMouseDragPreviewMesh slightly
 */

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class SubWindowTests
    {
        static string kGraphName = "Assets/CommonAssets/Graphs/SubWindow.shadergraph";

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
        public void CloseGraphWindow()
        {
            m_Window.graphObject = null; // Don't spawn ask-to-save dialog
            m_Window.Close();
        }

        private void ToggleSubWindows(bool showBlackboard, bool showPreview)
        {
            m_GraphEditorView.viewSettings.isBlackboardVisible = showBlackboard;
            m_GraphEditorView.viewSettings.isPreviewVisible = showPreview;

            m_GraphEditorView.UserViewSettingsChangeCheck(0);

            // TODO: As a team decide if we are going to make functions internal or use reflection inside of tests.
            // m_GraphEditorView.UpdateSubWindowsVisibility(); // Needs to be non-private
            MethodInfo updateVisibility = typeof(GraphEditorView).GetMethod("UpdateSubWindowsVisibility",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            updateVisibility.Invoke(m_GraphEditorView, null);
        }

        [UnityTest]
        public IEnumerator ToggleSubWindows()
        {
            Blackboard blackboard;
            MasterPreviewView masterPreviewView;

            // Both
            ToggleSubWindows(true, true);

            blackboard = m_GraphEditorView.Q<Blackboard>();
            masterPreviewView = m_GraphEditorView.Q<MasterPreviewView>();

            yield return null;

            Assert.That(blackboard, Is.Not.Null, "Blackboard is not visible when it should be.");
            Assert.That(masterPreviewView, Is.Not.Null, "MasterPreviewView is not visible when it should be.");

            // Neither
            ToggleSubWindows(false, false);

            blackboard = m_GraphEditorView.Q<Blackboard>();
            masterPreviewView = m_GraphEditorView.Q<MasterPreviewView>();

            yield return null;

            Assert.That(blackboard, Is.Null, "Blackboard remained visible when it should not be.");
            Assert.That(masterPreviewView, Is.Null, "MasterPreviewView remained visible when it should not be.");

            // Blackboard Only
            ToggleSubWindows(true, false);

            blackboard = m_GraphEditorView.Q<Blackboard>();
            masterPreviewView = m_GraphEditorView.Q<MasterPreviewView>();

            yield return null;

            Assert.That(blackboard, Is.Not.Null, "Blackboard is not visible when it should be.");
            Assert.That(masterPreviewView, Is.Null, "MasterPreviewView remained visible when it should not be.");

            // Preview Only
            ToggleSubWindows(false, true);

            blackboard = m_GraphEditorView.Q<Blackboard>();
            masterPreviewView = m_GraphEditorView.Q<MasterPreviewView>();

            yield return null;

            Assert.That(masterPreviewView, Is.Not.Null, "MasterPreviewView is not visible when it should be.");
            Assert.That(blackboard, Is.Null, "Blackboard remained visible when it should not be.");
        }

        private IEnumerator ToggleSubWindowsThenCloseThenReopen(bool showBlackboard, bool showPreview)
        {
            ToggleSubWindows(showBlackboard, showPreview);
            yield return null;

            CloseGraphWindow();
            yield return null;

            OpenGraphWindow();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ToggleSubWindowsRememberedAfterAfterCloseAndReopen()
        {
            yield return ToggleSubWindowsThenCloseThenReopen(true, true);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Not.Null, "0: Blackboard is not visible when it should be.");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>(), Is.Not.Null, "0: MasterPreviewView is not visible when it should be.");

            yield return ToggleSubWindowsThenCloseThenReopen(true, true);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Not.Null, "1: Blackboard is not visible when it should be.");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>(), Is.Not.Null, "1: MasterPreviewView is not visible when it should be.");

            yield return ToggleSubWindowsThenCloseThenReopen(true, false);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Not.Null, "2: Blackboard is not visible when it should be.");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>(), Is.Null, "2: MasterPreviewView is visible when it should not be.");

            yield return ToggleSubWindowsThenCloseThenReopen(true, false);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Not.Null, "3: Blackboard is not visible when it should be.");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>(), Is.Null, "3: MasterPreviewView is visible when it should not be.");

            yield return ToggleSubWindowsThenCloseThenReopen(false, true);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Null, "4: Blackboard is visible when it should be.");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>(), Is.Not.Null, "4: MasterPreviewView is not visible when it should not be.");

            yield return ToggleSubWindowsThenCloseThenReopen(false, true);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Null, "5: Blackboard is visible when it should be.");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>(), Is.Not.Null, "5: MasterPreviewView is not visible when it should not be.");

            yield return ToggleSubWindowsThenCloseThenReopen(false, false);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Null, "6: Blackboard is visible when it should not be.");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>(), Is.Null, "6: MasterPreviewView is visible when it should not be.");

            yield return ToggleSubWindowsThenCloseThenReopen(false, false);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Null, "7: Blackboard is visible when it should not be.");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>(), Is.Null, "7: MasterPreviewView is visible when it should not be.");
        }

        [UnityTest]
        public IEnumerator SubWindowLocationRememberedAfterCloseAndReopen()
        {
            yield return TestBlackboardLocation(new Rect(50.0f, 50.0f, 160.0f, 160.0f));
            yield return TestBlackboardLocation(new Rect(80050.0f, 50.0f, 220.0f, 240.0f));
            yield return TestBlackboardLocation(new Rect(50.0f, 90050.0f, 220.0f, 240.0f));
            yield return TestBlackboardLocation(new Rect(80050.0f, 90050.0f, 220.0f, 240.0f));
            yield return TestBlackboardLocation(new Rect(50.0f, -50.0f, 230.0f, 230.0f));
        }

        // Only works for Blackboard... for now. (Plan is to make Internal Inspector, Blackboard 2.0, and MasterPreview use the same SubWindow class someday)
        private IEnumerator TestBlackboardLocation(Rect blackboardRect)
        {
            ToggleSubWindows(true, true);

            Blackboard blackboard = m_GraphEditorView.blackboardProvider.blackboard;
            MasterPreviewView masterPreviewView = m_GraphEditorView.Q<MasterPreviewView>();

            blackboard.SetPosition(blackboardRect);
            yield return null;

            CloseWindow();
            yield return null;
            OpenGraphWindow();
            yield return null;

            blackboard = m_GraphEditorView.blackboardProvider.blackboard;

            // Keep inside the GraphEditor in the same way as expected
            Rect editorViewContainer = m_GraphEditorView.graphView.contentContainer.layout;
            if (blackboardRect.x + blackboardRect.width > editorViewContainer.width)
                blackboardRect.x = editorViewContainer.width - blackboardRect.width;
            if (blackboardRect.y + blackboardRect.height > editorViewContainer.height)
                blackboardRect.y = editorViewContainer.height - blackboardRect.height;
            if (blackboardRect.x < 0)
                blackboardRect.x = 0;
            if (blackboardRect.y < 0)
                blackboardRect.y = 0;

            // Using approximately instead of exact comparisons, which is why we don't use (blackboard.layout == blackboardRect)
            Assert.That(Mathf.Approximately(blackboard.layout.x, blackboardRect.x), "Blackboard did not remember location, x differs: "
                + "m_GraphEditorView.layout=" + m_GraphEditorView.layout + " blackboard.layout=" + blackboard.layout + " blackboardRect=" + blackboardRect);
            Assert.That(Mathf.Approximately(blackboard.layout.y, blackboardRect.y), "Blackboard did not remember location, y differs: "
                + "m_GraphEditorView.layout=" + m_GraphEditorView.layout + " blackboard.layout=" + blackboard.layout + " blackboardRect=" + blackboardRect);
            Assert.That(Mathf.Approximately(blackboard.layout.width, blackboardRect.width), "Blackboard did not remember width: "
                + "m_GraphEditorView.layout=" + m_GraphEditorView.layout + " blackboard.layout=" + blackboard.layout + " blackboardRect=" + blackboardRect);
            Assert.That(Mathf.Approximately(blackboard.layout.height, blackboardRect.height), "Blackboard did not remember height: "
                + "m_GraphEditorView.layout=" + m_GraphEditorView.layout + " blackboard.layout=" + blackboard.layout + " blackboardRect=" + blackboardRect);
        }

    }
}
