using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIToolkitTests
{
    public class CollapsibleSectionHeaderTests : GtfTestFixture
    {
        [Test]
        public void ChangeValueTriggersChangeCallback()
        {
            var collapsibleSectionHeader = new CollapsibleSectionHeader();
            bool called = false;
            collapsibleSectionHeader.RegisterCallback<ChangeEvent<bool>>(_ => called = true);
            Window.rootVisualElement.Add(collapsibleSectionHeader);
            Assert.IsFalse(collapsibleSectionHeader.value);
            collapsibleSectionHeader.value = true;

            Assert.IsTrue(called, "CollapsibleSectionHeader did not called our callback.");
        }

        [Test]
        public void SetValueWithoutNotifyDoesNotTriggerChangeCallback()
        {
            var collapsibleSectionHeader = new CollapsibleSectionHeader();
            bool called = false;
            collapsibleSectionHeader.RegisterCallback<ChangeEvent<bool>>(_ => called = true);
            Window.rootVisualElement.Add(collapsibleSectionHeader);
            Assert.IsFalse(collapsibleSectionHeader.value);
            collapsibleSectionHeader.SetValueWithoutNotify(true);

            Assert.IsTrue(collapsibleSectionHeader.value, "CollapsibleSectionHeader value did not change.");
            Assert.IsFalse(called, "CollapsibleSectionHeader called our callback.");
        }

        [UnityTest]
        public IEnumerator ClickingButtonIconChangesItsValue()
        {
            var collapsibleSectionHeader = new CollapsibleSectionHeader();
            Window.rootVisualElement.Add(collapsibleSectionHeader);
            // Do layout
            yield return null;

            Assert.IsFalse(collapsibleSectionHeader.value);
            Vector2 center = collapsibleSectionHeader.parent.LocalToWorld(collapsibleSectionHeader.layout.center);
            Helpers.Click(center);
            yield return null;

            Assert.IsTrue(collapsibleSectionHeader.value);
        }
    }
}
