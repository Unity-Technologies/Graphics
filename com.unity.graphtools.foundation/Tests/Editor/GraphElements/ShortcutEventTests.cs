using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    [ToolShortcutEvent(ShortcutTestGraphViewWindow.toolName, id, keyCode, modifiers)]
    class TestShortcutEvent : ShortcutEventBase<TestShortcutEvent>
    {
        public const string id = "Test Event Shortcut";
        public const KeyCode keyCode = KeyCode.F6;
        public const ShortcutModifiers modifiers = ShortcutModifiers.Alt | ShortcutModifiers.Action | ShortcutModifiers.Shift;
    }

    [ToolShortcutEvent(ShortcutTestGraphViewWindow.toolName, id, keyCode, modifiers)]
    class TestShortcutEventFilteredOut : ShortcutEventBase<TestShortcutEventFilteredOut>
    {
        public const string id = "Filtered Out Event Shortcut";
        public const KeyCode keyCode = KeyCode.F7;
        public const ShortcutModifiers modifiers = ShortcutModifiers.Alt | ShortcutModifiers.Action | ShortcutModifiers.Shift;
    }

    [ToolShortcutEvent(otherToolName, id, keyCode, modifiers)]
    class TestShortcutEventUnused : ShortcutEventBase<TestShortcutEventUnused>
    {
        public const string otherToolName = "_ _ _ Random Tool Name _ _ _";
        public const string id = "Filtered Out Event Shortcut";
        public const KeyCode keyCode = KeyCode.F7;
        public const ShortcutModifiers modifiers = ShortcutModifiers.Alt | ShortcutModifiers.Action | ShortcutModifiers.Shift;
    }

    class ShortcutTestGraphViewWindow : GraphViewEditorWindow
    {
        public const string toolName = "Shortcut Tests";

        static bool ShortcutFilter(string shortcutId)
        {
            if (shortcutId == TestShortcutEventFilteredOut.id)
                return false;

            return true;
        }

        [InitializeOnLoadMethod]
        static void RegisterTool()
        {
            ShortcutHelper.RegisterDefaultShortcuts<ShortcutTestGraphViewWindow>(toolName, ShortcutFilter);
        }

        public ShortcutTestGraphViewWindow()
        {
            this.SetDisableInputEvents(true);
            WithSidePanel = false;
        }

        protected override BaseGraphTool CreateGraphTool()
        {
            return CsoTool.Create<GraphViewTestGraphTool>();
        }

        protected override GraphView CreateGraphView()
        {
            return new TestGraphView(this, GraphTool);
        }

        protected override bool CanHandleAssetType(IGraphAssetModel asset)
        {
            return true;
        }
    }

    class ShortcutEventTests
    {
        TestEventHelpers m_Helper;
        ShortcutTestGraphViewWindow m_Window;

        Type m_EventTypeReceived;

        [SetUp]
        public void SetUp()
        {
            m_Window = EditorWindow.GetWindow<ShortcutTestGraphViewWindow>();
            m_Helper = new TestEventHelpers(m_Window);

            var graphAsset = GraphAssetCreationHelpers<TestGraphAssetModel>.CreateInMemoryGraphAsset(typeof(TestStencil), "Test");
            m_Window.GraphTool.Dispatch(new LoadGraphAssetCommand(graphAsset));
            m_Window.GraphTool.Update();
        }

        [TearDown]
        public virtual void TearDown()
        {
            // See case: https://fogbugz.unity3d.com/f/cases/998343/
            // Clearing the capture needs to happen before closing the window
            MouseCaptureController.ReleaseMouse();
            if (m_Window != null)
            {
                m_Window.Close();
            }
        }

        void OnShortcutEvent(TestShortcutEvent e)
        {
            m_EventTypeReceived = e.GetType();
        }

        // Check if keyCombination is used by another shortcut than shortcutId.
        // This is important that it is not the case otherwise test will hang or fail
        // because of the shortcut manager dialog asking to select which shortcut to execute.
        static string FindShortcutConflict(string shortcutId, KeyCombination keyCombination)
        {
            var availableShortcutIds = ShortcutManager.instance.GetAvailableShortcutIds();
            foreach (var id in availableShortcutIds)
            {
                if (id.EndsWith("/" + shortcutId))
                    continue;

                var binding = ShortcutManager.instance.GetShortcutBinding(id);
                if (binding.keyCombinationSequence.Any(kc => kc.Equals(keyCombination)))
                {
                    return id;
                }
            }

            return null;
        }

        [Test]
        public void ShortcutIsRegistered()
        {
            Assert.IsTrue(ShortcutManager.instance.GetAvailableShortcutIds().Contains(ShortcutTestGraphViewWindow.toolName + "/" + TestShortcutEvent.id));
        }

        [Test]
        public void FilteredOutShortcutIsNotRegistered()
        {
            Assert.IsFalse(ShortcutManager.instance.GetAvailableShortcutIds().Contains(ShortcutTestGraphViewWindow.toolName + "/" + TestShortcutEventFilteredOut.id));
        }

        [Test]
        public void OtherToolShortcutIsNotRegistered()
        {
            Assert.IsFalse(ShortcutManager.instance.GetAvailableShortcutIds().Contains(TestShortcutEventUnused.otherToolName + "/" + TestShortcutEventUnused.id));
        }

        public static EventModifiers ConvertModifiers(ShortcutModifiers modifiers)
        {
            var m = EventModifiers.None;

            if (modifiers.HasFlag(ShortcutModifiers.Alt))
                m |= EventModifiers.Alt;

            if (modifiers.HasFlag(ShortcutModifiers.Action))
            {
                if (Application.platform == RuntimePlatform.OSXEditor)
                    m |= EventModifiers.Command;
                else
                    m |= EventModifiers.Control;
            }

            if (modifiers.HasFlag(ShortcutModifiers.Shift))
                m |= EventModifiers.Shift;

            return m;
        }

        [UnityTest]
        public IEnumerator ShortcutSendsEvent()
        {
            var conflict = FindShortcutConflict(TestShortcutEvent.id, new KeyCombination(TestShortcutEvent.keyCode, TestShortcutEvent.modifiers));
            Assert.IsNull(conflict);

            m_EventTypeReceived = null;
            m_Window.rootVisualElement.RegisterCallback<TestShortcutEvent>(OnShortcutEvent);
            m_Window.Focus();
            m_Helper.KeyPressed(TestShortcutEvent.keyCode, ConvertModifiers(TestShortcutEvent.modifiers));
            yield return null;

            Assert.AreEqual(typeof(TestShortcutEvent), m_EventTypeReceived);
        }
    }
}
