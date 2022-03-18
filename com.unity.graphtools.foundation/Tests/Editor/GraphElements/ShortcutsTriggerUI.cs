using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.ShortcutManagement;
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

            var node = GraphModel.NodeModels[0].GetView<GraphElement>(GraphView);
            var editableLabel = node.SafeQ<EditableLabel>();
            var textField = editableLabel.SafeQ<TextField>();
            Assert.IsTrue(textField.style.display == DisplayStyle.None);

            Helpers.Click(node.layout.center);

#if UNITY_STANDALONE_OSX
            Helpers.KeyPressed(KeyCode.Return);
#else
            Helpers.KeyPressed(KeyCode.F2);
#endif

            yield return null;

            node = GraphModel.NodeModels[0].GetView<GraphElement>(GraphView);
            editableLabel = node.SafeQ<EditableLabel>();
            textField = editableLabel.SafeQ<TextField>();

            Assert.IsTrue(textField.style.display != DisplayStyle.None);
        }

        [UnityTest]
        public IEnumerator ShortcutDisplaySmartSearchWorks()
        {
            MarkGraphViewStateDirty();
            yield return null;

            GraphView.DisplaySmartSearchCalled = false;

            ShortcutDisplaySmartSearchEvent.SendTestEvent(Window, ShortcutStage.Begin);
            yield return null;

            Assert.IsTrue(GraphView.DisplaySmartSearchCalled);
        }
    }
}
