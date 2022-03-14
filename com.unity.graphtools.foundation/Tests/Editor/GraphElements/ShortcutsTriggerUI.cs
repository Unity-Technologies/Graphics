using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class ShortcutsTriggerUI : GraphViewTester
    {
        [UnityTest, Ignore("VladN 06-2021: Test is too unstable.")]
        public IEnumerator ShortcutRenameWorks()
        {
            GraphModel.CreateConstantNode(TypeHandle.Float, "Blah", Vector2.zero);

            MarkGraphViewStateDirty();
            yield return null;

            var node = GraphModel.NodeModels[0].GetUI<GraphElement>(graphView);
            var editableLabel = node.SafeQ<EditableLabel>();
            var textField = editableLabel.SafeQ<TextField>();
            Assert.IsTrue(textField.style.display == DisplayStyle.None);

            helpers.Click(node.layout.center);

#if UNITY_STANDALONE_OSX
            helpers.KeyPressed(KeyCode.Return, EventModifiers.None);
#else
            helpers.KeyPressed(KeyCode.F2, EventModifiers.None);
#endif

            yield return null;

            node = GraphModel.NodeModels[0].GetUI<GraphElement>(graphView);
            editableLabel = node.SafeQ<EditableLabel>();
            textField = editableLabel.SafeQ<TextField>();

            Assert.IsTrue(textField.style.display != DisplayStyle.None);
        }

        [UnityTest]
        public IEnumerator ShortcutDisplaySmartSearchWorks()
        {
            MarkGraphViewStateDirty();
            yield return null;

            graphView.DisplaySmartSearchCalled = false;
            helpers.KeyPressed(ShortcutDisplaySmartSearchEvent.keyCode, ShortcutEventTests.ConvertModifiers(ShortcutDisplaySmartSearchEvent.modifiers));
            yield return null;

            Assert.IsTrue(graphView.DisplaySmartSearchCalled);
        }
    }
}
