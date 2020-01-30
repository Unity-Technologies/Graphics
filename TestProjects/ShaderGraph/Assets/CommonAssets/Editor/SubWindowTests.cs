using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using UnityEngine;
using System.Reflection;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

/* Changes:
 * Made ShaderGraphImporterEditor.ShowGraphEditWindow public
 * Made MaterialGraphEditWindow.graphEditorView public
 */

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class SubWindowTests
    {
        static string kGraphName = "Assets/CommonAssets/Graphs/SubWindow.shadergraph";

        GraphEditorView m_GraphEditorView;
        MaterialGraphEditWindow m_Window;

        public void OpenGraphWindow()
        {
            if (ShaderGraphImporterEditor.ShowGraphEditWindow("HelloWorld"))
            {
                Assert.Fail("ShaderGraphImporterEditor.ShowGraphEditWindow return true on a Shader Graph that does not exists");
            }

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

            // TODO: Probably don't want to make this public
            m_GraphEditorView = m_Window.graphEditorView;
        }

        private void UpdateWindow()
        {
            m_Window.Repaint(); // TODO: ?

            MethodInfo methodInfo = typeof(MaterialGraphEditWindow).GetMethod("Update",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            methodInfo.Invoke(m_Window, null);

            m_Window.Repaint(); // TODO: ?
        }

        private void UpdateSubWindowsVisibility(GraphEditorView graphEditorView)
        {
            MethodInfo methodInfo = typeof(GraphEditorView).GetMethod("UpdateSubWindowsVisibility",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            methodInfo.Invoke(graphEditorView, null);
        }

        private void ToggleSubWindows(bool showBlackboard, bool showPreview)
        {
            m_GraphEditorView.viewSettings.isBlackboardVisible = showBlackboard;
            m_GraphEditorView.viewSettings.isPreviewVisible = showPreview;

            UpdateSubWindowsVisibility(m_GraphEditorView);
            UpdateWindow();
        }

        [Test]
        public void ToggleSubWindows()
        {
            OpenGraphWindow();

            Blackboard blackboard;
            MasterPreviewView masterPreviewView;

            // TRUE - TRUE
            ToggleSubWindows(true, true);

            blackboard = m_GraphEditorView.Q<Blackboard>();
            masterPreviewView = m_GraphEditorView.Q<MasterPreviewView>();

            if (blackboard == null)
            {
                Assert.Fail("Blackboard is not visible when it should be.");
            }

            if (masterPreviewView == null)
            {
                Assert.Fail("MasterPreviewView is not visible when it should be.");
            }

            // FALSE - FALSE
            ToggleSubWindows(false, false);

            blackboard = m_GraphEditorView.Q<Blackboard>();
            masterPreviewView = m_GraphEditorView.Q<MasterPreviewView>();

            if (blackboard != null)
            {
                Assert.Fail("Blackboard remained visible when it should not be.");
            }

            if (masterPreviewView != null)
            {
                Assert.Fail("MasterPreviewView remained visible when it should not be.");
            }

            // TRUE - FALSE
            ToggleSubWindows(true, false);

            blackboard = m_GraphEditorView.Q<Blackboard>();
            masterPreviewView = m_GraphEditorView.Q<MasterPreviewView>();

            if (blackboard == null)
            {
                Assert.Fail("Blackboard remained visible when it should not be.");
            }

            if (masterPreviewView != null)
            {
                Assert.Fail("MasterPreviewView remained visible when it should not be.");
            }

            // FALSE - TRUE
            ToggleSubWindows(false, true);

            blackboard = m_GraphEditorView.Q<Blackboard>();
            masterPreviewView = m_GraphEditorView.Q<MasterPreviewView>();

            if (blackboard != null)
            {
                Assert.Fail("Blackboard remained visible when it should not be.");
            }

            if (masterPreviewView == null)
            {
                Assert.Fail("MasterPreviewView remained visible when it should not be.");
            }

            m_Window.Close();
        }

        [Test]
        public void TogglingBlackbordAndMasterPreviewWhileClosingAndReopeningTheSameGraphCrashCheck0()
        {
            OpenGraphWindow();

            // Repro 1
            ToggleSubWindows(true, true);
            m_Window.Close();
            OpenGraphWindow();

            UpdateWindow();
            UpdateWindow();

            ToggleSubWindows(true, true);
            m_Window.Close();
            OpenGraphWindow();

            UpdateWindow();
            UpdateWindow();

            ToggleSubWindows(true, false);
            m_Window.Close();
            OpenGraphWindow();

            UpdateWindow();
            UpdateWindow();

            ToggleSubWindows(true, false);
            m_Window.Close();
            OpenGraphWindow();

            UpdateWindow();
            UpdateWindow();

            ToggleSubWindows(true, false);
            m_Window.Close();
            OpenGraphWindow();

            UpdateWindow();
            UpdateWindow();

            m_Window.Close();
        }

        [UnityTest]
        public IEnumerator TogglingBlackbordAndMasterPreviewWhileClosingAndReopeningTheSameGraphCrashCheck1()
        {
            OpenGraphWindow();

            // Repro 2
            ToggleSubWindows(true, true);
            m_Window.Close();
            OpenGraphWindow();

            UpdateWindow();
            UpdateWindow();

            ToggleSubWindows(true, true);
            m_Window.Close();
            OpenGraphWindow();

            UpdateWindow();
            UpdateWindow();

            ToggleSubWindows(true, true);
            m_Window.Close();
            OpenGraphWindow();

            UpdateWindow();
            UpdateWindow();

            ToggleSubWindows(false, false);
            m_Window.Close();
            OpenGraphWindow();

            UpdateWindow();
            UpdateWindow();

            ToggleSubWindows(false, false);
            m_Window.Close();
            OpenGraphWindow();

            UpdateWindow();
            UpdateWindow();

            ToggleSubWindows(false, false);
            m_Window.Close();
            OpenGraphWindow();

            UpdateWindow();
            UpdateWindow();

            ToggleSubWindows(true, true);
            m_Window.Close();
            OpenGraphWindow();

            UpdateWindow();
            UpdateWindow();

            ToggleSubWindows(true, true);
            m_Window.Close();
            OpenGraphWindow();

            UpdateWindow();
            UpdateWindow();

            yield return null;

            ToggleSubWindows(true, true);
            m_Window.Close();
            OpenGraphWindow();

            UpdateWindow();
            UpdateWindow();

            m_Window.Close();
        }

//        [UnityTest]
//        public IEnumerator Paddle1StaysInUpperCameraBounds()
//        {
//            OpenGraphWindow();
//
//            // Increase the timeScale so the test executes quickly
//            Time.timeScale = 20.0f;
//
//            float time = 0;
//            while (time < 5)
//            {
//                time += Time.fixedDeltaTime;
//                yield return null;
//            }
//
//            // Reset timeScale
//            Time.timeScale = 1.0f;
//
//            // Edge of paddle should not leave edge of screen
//            // (Camera.main.orthographicSize - paddle.transform.localScale.y /2) is where the edge
//            //of the paddle touches the edge of the screen, and 0.15 is the margin of error I gave it
//            //to wait for the next frame
//            Assert.LessOrEqual(0.0f, 0.0f);
//
//            m_Window.Close();
//        }

    }
}
