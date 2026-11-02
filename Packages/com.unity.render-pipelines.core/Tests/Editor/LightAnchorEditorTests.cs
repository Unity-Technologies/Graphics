using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Rendering.Tests;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Tests
{
    class LightAnchorEditorTests
    {
        GameObject m_GameObject;
        Editor m_Editor;
        int m_OriginalGameViewCount;
        UnityEditor.Tool m_OriginalTool;

        static IEnumerable<EditorWindow> FindGameViews()
        {
            return Resources.FindObjectsOfTypeAll<EditorWindow>()
                .Where(w => w.GetType().FullName == "UnityEditor.GameView");
        }

        static void OpenGameView()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
        }

        static void CloseAllGameViews()
        {
            foreach (var gameView in FindGameViews().ToArray())
                gameView.Close();
        }

        [SetUp]
        public void SetUp()
        {
            m_OriginalTool = UnityEditor.Tools.current;
            m_OriginalGameViewCount = FindGameViews().Count();
            CloseAllGameViews();

            m_GameObject = new GameObject("LightAnchorEditorTestsRoot");
            m_GameObject.AddComponent<LightAnchor>();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEditor.Tools.current = m_OriginalTool;
            Selection.objects = new UnityEngine.Object[0];

            if (m_Editor != null)
                UnityEngine.Object.DestroyImmediate(m_Editor);
            if (m_GameObject != null)
                UnityEngine.Object.DestroyImmediate(m_GameObject);

            // Restore the Game View count the test found, so it does not leak into later tests
            CloseAllGameViews();
            for (int i = 0; i < m_OriginalGameViewCount; ++i)
                OpenGameView();
        }

        [Test]
        [NUnit.Framework.Property("Jira", "UUM-153999")]
        public void OnEnable_WithNoGameViewOpen_DoesNotThrowAndInitializesSerializedProperties()
        {
            Assume.That(FindGameViews().Any(), Is.False, "No Game View must be open for this test to be meaningful");

            m_Editor = Editor.CreateEditor(m_GameObject.GetComponent<LightAnchor>());

            LogAssert.NoUnexpectedReceived();

            Assert.NotNull(m_Editor.GetField("m_DistanceProperty"));
            Assert.NotNull(m_Editor.GetField("m_FrameSpaceProperty"));
            Assert.NotNull(m_Editor.GetField("m_AnchorPositionOverrideProperty"));
            Assert.NotNull(m_Editor.GetField("m_AnchorPositionOffsetProperty"));
        }

        [UnityTest]
        [NUnit.Framework.Property("Jira", "UUM-153999")]
        public IEnumerator ClickCatcher_AttachesToGameView_OpenedAfterToolWasActivated()
        {
            Assume.That(FindGameViews().Any(), Is.False, "No Game View must be open for this test to be meaningful");

            m_Editor = Editor.CreateEditor(m_GameObject.GetComponent<LightAnchor>());

            // The tool manager only picks up the new selection on the next editor update
            Selection.activeGameObject = m_GameObject;
            yield return null;

            ToolManager.SetActiveTool<LightAnchorEditorTool>();

            var clickCatcher = (VisualElement)m_Editor.GetField("m_ClickCatcher");
            Assume.That(clickCatcher.parent, Is.Null, "The click catcher cannot attach while no Game View exists");

            OpenGameView();

            for (int i = 0; i < 10 && clickCatcher.parent == null; ++i)
                yield return null;

            Assert.NotNull(clickCatcher.parent, "The click catcher should attach to a Game View opened while the tool is active");
        }
    }
}
