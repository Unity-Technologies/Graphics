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
        }

        // Tests that we the user can toggle the SubWindows
        // The repeating Q<...>ing is done deliberately: to confirm that it's still in the graph view.
        [UnityTest]
        public IEnumerator CanToggleSubWindows()
        {
            // Both
            ToggleSubWindows(true, true);

            yield return null;

            Assert.That(m_GraphEditorView.Q<Blackboard>().enabledInHierarchy, Is.True, "Blackboard is not visible when it should be. (1st pass)");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>().visible, Is.True, "MasterPreviewView is not visible when it should be. (1st pass)");

            yield return null;

            // Neither
            ToggleSubWindows(false, false);

            yield return null;

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Null, "Blackboard remained visible when it should not be. (2nd pass)");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>().visible, Is.False, "MasterPreviewView remained visible when it should not be. (2nd pass)");

            yield return null;

            // Blackboard Only
            ToggleSubWindows(true, false);

            yield return null;

            Assert.That(m_GraphEditorView.Q<Blackboard>().enabledInHierarchy, Is.True, "Blackboard is not visible when it should be. (3rd pass)");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>().visible, Is.False, "MasterPreviewView remained visible when it should not be. (3rd pass)");

            // Preview Only
            ToggleSubWindows(false, true);

            yield return null;

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Null, "Blackboard remained visible when it should not be. (4th pass)");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>().visible, Is.True, "MasterPreviewView is not visible when it should be. (4th pass)");

            yield return null;
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

        // Tests that the Sub Window status is remembered when closing and reopening Shader Graphs.
        // The repeating Q<...>ing is done deliberately: to confirm that it's still in the graph view.
        [UnityTest]
        public IEnumerator SubWindowStatusRememberedAfterCloseAndReopen()
        {
            yield return ToggleSubWindowsThenCloseThenReopen(true, true);

            Assert.That(m_GraphEditorView.Q<Blackboard>().enabledInHierarchy, Is.True, "Blackboard is NOT visible when it should be. (1st pass)");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>().visible, Is.True, "MasterPreviewView is NOT visible when it should be. (1st pass)");

            yield return ToggleSubWindowsThenCloseThenReopen(true, true);

            Assert.That(m_GraphEditorView.Q<Blackboard>().enabledInHierarchy, Is.True, "Blackboard is NOT visible when it should be. (2nd pass)");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>().visible, Is.True, "MasterPreviewView is NOT visible when it should be. (2nd pass)");

            yield return ToggleSubWindowsThenCloseThenReopen(true, false);

            Assert.That(m_GraphEditorView.Q<Blackboard>().enabledInHierarchy, Is.True, "Blackboard is NOT visible when it should be. (3rd pass)");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>().visible, Is.False, "MasterPreviewView IS visible when it should not be. (3rd pass)");

            yield return ToggleSubWindowsThenCloseThenReopen(true, false);

            Assert.That(m_GraphEditorView.Q<Blackboard>().enabledInHierarchy, Is.True, "Blackboard is NOT visible when it should be. (4th pass)");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>().visible, Is.False, "MasterPreviewView IS visible when it should not be. (4th pass)");

            yield return ToggleSubWindowsThenCloseThenReopen(false, true);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Null, "Blackboard IS visible when it should not be. (5th pass)");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>().visible, Is.True, "MasterPreviewView is NOT visible when it should not  (5th pass)be.");

            yield return ToggleSubWindowsThenCloseThenReopen(false, true);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Null, "Blackboard IS visible when it should not be. (6th pass)");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>().visible, Is.True, "MasterPreviewView is NOT visible when it should not  (6th pass)be.");

            yield return ToggleSubWindowsThenCloseThenReopen(false, false);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Null, "Blackboard IS visible when it should not be. (7th pass)");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>().visible, Is.False, "MasterPreviewView IS visible when it should not be. (7th pass)");

            yield return ToggleSubWindowsThenCloseThenReopen(false, false);

            Assert.That(m_GraphEditorView.Q<Blackboard>(), Is.Null, "Blackboard IS visible when it should not be. (8th pass)");
            Assert.That(m_GraphEditorView.Q<MasterPreviewView>().visible, Is.False, "MasterPreviewView IS visible when it should not be. (8th pass)");
        }

        // [UnityTest]
        // public IEnumerator SubWindowLocationRememberedAfterCloseAndReopen()
        // {
        //     yield return TestBlackboardLocation(new Rect(50.0f, 50.0f, 160.0f, 160.0f));
        //     yield return TestBlackboardLocation(new Rect(80050.0f, 50.0f, 220.0f, 240.0f));
        //     yield return TestBlackboardLocation(new Rect(50.0f, 90050.0f, 220.0f, 240.0f));
        //     yield return TestBlackboardLocation(new Rect(80050.0f, 90050.0f, 220.0f, 240.0f));
        //     yield return TestBlackboardLocation(new Rect(50.0f, -50.0f, 230.0f, 230.0f));
        // }
        // Test does not pass when run in batchmode on yamato, needs more investigation to re-enable 

        // Only works for Blackboard... for now. (Plan is to make Internal Inspector, Blackboard 2.0, and MasterPreview use the same SubWindow class someday)
        private IEnumerator TestBlackboardLocation(Rect blackboardRect)
        {
            ToggleSubWindows(true, true);

            Blackboard blackboard = m_GraphEditorView.blackboardProvider.blackboard;
            MasterPreviewView masterPreviewView = m_GraphEditorView.Q<MasterPreviewView>();

            blackboard.SetPosition(blackboardRect);
            yield return null;

            CloseGraphWindow();
            yield return null;
            OpenGraphWindow();
            yield return null;

            blackboard = m_GraphEditorView.blackboardProvider.blackboard;

            // Keep inside the GraphEditor in the same way as expected
            Rect editorViewContainer = m_GraphEditorView.graphView.contentContainer.layout;
            Rect blackboardRectBefore = blackboardRect;
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
                + "m_GraphEditorView.layout=" + m_GraphEditorView.layout + " blackboard.layout=" + blackboard.layout + " blackboardRect=" + blackboardRect + " blackboardRectPreModification=" + blackboardRectBefore);
            Assert.That(Mathf.Approximately(blackboard.layout.y, blackboardRect.y), "Blackboard did not remember location, y differs: "
                + "m_GraphEditorView.layout=" + m_GraphEditorView.layout + " blackboard.layout=" + blackboard.layout + " blackboardRect=" + blackboardRect + " blackboardRectPreModification=" + blackboardRectBefore);
            Assert.That(Mathf.Approximately(blackboard.layout.width, blackboardRect.width), "Blackboard did not remember width: "
                + "m_GraphEditorView.layout=" + m_GraphEditorView.layout + " blackboard.layout=" + blackboard.layout + " blackboardRect=" + blackboardRect + " blackboardRectPreModification=" + blackboardRectBefore);
            Assert.That(Mathf.Approximately(blackboard.layout.height, blackboardRect.height), "Blackboard did not remember height: "
                + "m_GraphEditorView.layout=" + m_GraphEditorView.layout + " blackboard.layout=" + blackboard.layout + " blackboardRect=" + blackboardRect + " blackboardRectPreModification=" + blackboardRectBefore);
        }

    }
}
