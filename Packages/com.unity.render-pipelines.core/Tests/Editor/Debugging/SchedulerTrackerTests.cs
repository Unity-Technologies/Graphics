#if ENABLE_UIELEMENTS_MODULE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
#define ENABLE_RENDERING_DEBUGGER_UI
#endif

#if ENABLE_RENDERING_DEBUGGER_UI

using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace UnityEditor.Rendering.Tests
{
    class RenderingDebuggerSchedulerTrackerTests
    {
        DebugUI.Panel m_FirstPanel;
        DebugUI.Panel m_SecondPanel;

        DebugWindow m_Window;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            m_FirstPanel = DebugManager.instance.GetPanel("First Panel", createIfNull: true);
            m_SecondPanel = DebugManager.instance.GetPanel("Second Panel", createIfNull: true);

            m_Window = EditorWindow.GetWindow<DebugWindow>();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            m_Window.Close();

            DebugManager.instance.RemovePanel(m_SecondPanel);
            DebugManager.instance.RemovePanel(m_FirstPanel);
        }

        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void TearDown()
        {
            m_FirstPanel.children.Clear();
            m_SecondPanel.children.Clear();
        }

        int CountActiveSchedulersForWidget(DebugUI.Widget widget, int level = 0)
        {
            int numActive = 0;
            var items = DebugManager.instance.schedulerTracker.GetScheduledItemsDictionary(DebugUI.Context.Editor);
            Assert.True(items.TryGetValue(widget, out var schedulers));
            foreach (var s in schedulers)
            {
                if (s.isActive)
                    numActive++;
            }

            if (widget is DebugUI.IContainer container)
            {
                foreach (var child in container.children)
                {
                    numActive += CountActiveSchedulersForWidget(child, level+1);
                }
            }

            string indent = new string(' ', level * 2);
            Debug.Log($"{widget.panel?.displayName, -14} | {indent}Hierarchy under '{widget.displayName}' has {numActive} active schedulers");
            return numActive;
        }


        static Func<DebugUI.Widget> s_BoolField = () => new DebugUI.BoolField { displayName = "bool widget", getter = () => true };
        static Func<DebugUI.Widget> s_IntField = () => new DebugUI.IntField { displayName = "int widget", getter = () => 42 };
        static Func<DebugUI.Foldout> s_OpenFoldout = () => new DebugUI.Foldout { displayName = "open foldout", opened = true, children = { s_BoolField(), s_IntField() } };
        static Func<DebugUI.Foldout> s_ClosedFoldout = () => new DebugUI.Foldout { displayName = "closed foldout", opened = false, children = { s_BoolField(), s_IntField() } };

        static IEnumerable<TestCaseData> s_SchedulersOnInactivePanelsArePaused_Cases()
        {
            yield return new TestCaseData(s_BoolField, 2).SetName("BoolFieldWidget").Returns(null);
            yield return new TestCaseData(s_IntField, 2).SetName("IntFieldWidget").Returns(null);
            yield return new TestCaseData(s_OpenFoldout, 6).SetName("OpenFoldout").Returns(null);
            yield return new TestCaseData(s_ClosedFoldout, 2).SetName("ClosedFoldout").Returns(null);
        }

        [UnityTest, TestCaseSource(nameof(s_SchedulersOnInactivePanelsArePaused_Cases))]
        public IEnumerator SchedulersOnInactivePanelsArePaused(Func<DebugUI.Widget> createWidget, int expectedNumberOfActiveSchedulersOnActivePanel)
        {
            var firstPanelWidget = createWidget();
            var secondPanelWidget = createWidget();
            m_FirstPanel.children.Add(firstPanelWidget);
            m_SecondPanel.children.Add(secondPanelWidget);

            // One frame of delay - adding a new widget causes DebugWindow to recreate the UI in the next Update,
            // at which point the VisualElements and their schedulers are actually created.
            yield return null;

            m_Window.SetSelectedPanel("First Panel");
            Assert.AreEqual(expectedNumberOfActiveSchedulersOnActivePanel, CountActiveSchedulersForWidget(firstPanelWidget));
            Assert.Zero(CountActiveSchedulersForWidget(secondPanelWidget));

            m_Window.SetSelectedPanel("Second Panel");
            Assert.Zero(CountActiveSchedulersForWidget(firstPanelWidget));
            Assert.AreEqual(expectedNumberOfActiveSchedulersOnActivePanel, CountActiveSchedulersForWidget(secondPanelWidget));
        }

        [UnityTest]
        public IEnumerator SchedulersInsideCollapsedFoldoutsArePaused()
        {
            DebugUI.Foldout foldout = s_OpenFoldout();
            DebugUI.Widget widget = foldout.children[0];
            m_FirstPanel.children.Add(foldout);

            // One frame of delay - adding a new widget causes DebugWindow to recreate the UI in the next Update,
            // at which point the VisualElements and their schedulers are actually created.
            yield return null;

            m_Window.SetSelectedPanel("First Panel");

            Assert.Positive(CountActiveSchedulersForWidget(foldout));
            Assert.Positive(CountActiveSchedulersForWidget(widget));

            foldout.opened = false;

            Assert.Positive(CountActiveSchedulersForWidget(foldout)); // foldout scheduler itself stays active
            Assert.Zero(CountActiveSchedulersForWidget(widget)); // but child widgets are paused

            yield return null;
        }
    }
}

#endif
