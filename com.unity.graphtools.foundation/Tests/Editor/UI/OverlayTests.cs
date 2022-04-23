using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class OverlayTests : BaseUIFixture
    {
        /// <inheritdoc />
        protected override bool CreateGraphOnStartup => true;

        /// <inheritdoc />
        protected override bool WithOverlays => true;

        VisualElement GetToggleToolbar()
        {
            var windowRoot = Window.rootVisualElement.panel.visualTree;
            var overlay = windowRoot.Q<VisualElement>("PanelToggles", "unity-overlay");
            Assert.IsNotNull(overlay);
            var toolbar = overlay.Q<UnityEditor.Overlays.OverlayToolbar>();
            return toolbar;
        }

        [Test]
        public void OverlayToolbarIsDisplayedByDefault()
        {
            var toolbar = GetToggleToolbar();
            Assert.IsNotNull(toolbar);
        }

        static IEnumerable OverlayToggleWorksTestData()
        {
            yield return new TestCaseData("Blackboard", "Blackboard").Returns(null);
            yield return new TestCaseData("Inspector", "Inspector").Returns(null);
            yield return new TestCaseData("MiniMap", "MiniMap").Returns(null);
        }

        [UnityTest, TestCaseSource(nameof(OverlayToggleWorksTestData))]
        public IEnumerator OverlayToggleWorks(string toggleName, string overlayName)
        {
            yield return null;

            var toolbar = GetToggleToolbar();
            var toggle = toolbar.Q(toggleName, "unity-toolbar-toggle");
            Assert.IsNotNull(toggle);

            var overlay = Window.rootVisualElement.panel.visualTree.Q<VisualElement>(overlayName, "unity-overlay");
            Assert.IsNotNull(overlay);
            var displayInitial = overlay.resolvedStyle.display;

            var buttonCenter = toggle.parent.LocalToWorld(toggle.layout.center);
            Helpers.Click(buttonCenter);
            var displayFinal = overlay.resolvedStyle.display;
            Assert.AreNotEqual(displayInitial, displayFinal);
        }

        [UnityTest]
        public IEnumerator BlackboardOverlayIsDisplayedAndHasContent()
        {
            yield return null;

            var overlay = Window.rootVisualElement.panel.visualTree.Q<VisualElement>("Blackboard", "unity-overlay");
            Assert.IsNotNull(overlay);
            Assert.AreEqual(DisplayStyle.Flex, overlay.resolvedStyle.display);

            var content = overlay.Q<Blackboard>();
            Assert.IsNotNull(content);
        }

        [UnityTest]
        public IEnumerator InspectorOverlayIsDisplayedAndHasContent()
        {
            yield return null;

            var overlay = Window.rootVisualElement.panel.visualTree.Q<VisualElement>("Inspector", "unity-overlay");
            Assert.IsNotNull(overlay);
            Assert.AreEqual(DisplayStyle.Flex, overlay.resolvedStyle.display);

            var content = overlay.Q<Label>(null, "model-inspector-view__title");
            Assert.IsNotNull(content);
            Assert.IsNotNull(content.text);
            Assert.AreNotEqual("", content.text);
        }
    }
}
