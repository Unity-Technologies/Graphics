using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphElementCopyPasteTests : GraphViewTester
    {
        const int k_DefaultNodeCount = 4;

        int m_SelectedNodeCount;

        int GetElementCount()
        {
            var allUIs = new List<ModelView>();
            GraphModel.GraphElementModels.GetAllViewsInList(GraphView, null, allUIs);
            return allUIs.Count;
        }

        void SelectThreeElements()
        {
            var list = GraphModel.NodeModels.ToList();
            m_SelectedNodeCount = 3;
            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, list[0], list[1], list[2]));
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            for (int i = 0; i < k_DefaultNodeCount; ++i)
            {
                CreateNode("Deletable element " + i);
            }

            GraphView.ViewSelection = new TestViewSelection(GraphView, GraphView.GraphViewModel.GraphModelState, GraphView.GraphViewModel.SelectionState, this);
            m_SelectedNodeCount = 0;
        }

        [UnityTest]
        public IEnumerator CopyWithoutSelectedElementsLeavesCopyBufferUntouched()
        {
            MarkGraphViewStateDirty();
            yield return null;

            GraphView.ViewSelection.Clipboard = "Unknown data";
            GraphView.Dispatch(new ClearSelectionCommand());
            GraphView.Focus();
            yield return null;

            bool used = Helpers.ValidateCommand("Copy");
            Assert.IsFalse(used);
            yield return null;

            Helpers.ExecuteCommand("Copy");
            yield return null;

            Assert.AreEqual("Unknown data", GraphView.ViewSelection.Clipboard);
        }

        //[Ignore("sometimes the graphView.clipboard is still Unknown data after the Copy execute")]
        [UnityTest]
        public IEnumerator SelectedElementsCanBeCopyPasted()
        {
            GraphView.RebuildUI();
            yield return null;

            GraphView.ViewSelection.Clipboard = "Unknown data";
            SelectThreeElements();
            MouseCaptureController.ReleaseMouse();
            GraphView.Focus();
            yield return null;

            bool used = Helpers.ValidateCommand("Copy");
            Assert.IsTrue(used);
            yield return null;

            Helpers.ExecuteCommand("Copy");
            yield return null;

            Assert.AreNotEqual("Unknown data", GraphView.ViewSelection.Clipboard);

            used = Helpers.ValidateCommand("Paste");
            Assert.IsTrue(used);
            yield return null;

            Helpers.ExecuteCommand("Paste");
            GraphView.RebuildUI();
            yield return null;

            Assert.AreEqual(k_DefaultNodeCount + m_SelectedNodeCount, GetElementCount());
        }

        [UnityTest]
        public IEnumerator SelectedElementsCanBeCut()
        {
            MarkGraphViewStateDirty();
            yield return null;

            GraphView.ViewSelection.Clipboard = "Unknown data";
            SelectThreeElements();
            MouseCaptureController.ReleaseMouse();
            GraphView.Focus();
            yield return null;

            bool used = Helpers.ValidateCommand("Cut");
            Assert.IsTrue(used);
            yield return null;

            Helpers.ExecuteCommand("Cut");
            yield return null;

            Assert.AreNotEqual("Unknown data", GraphView.ViewSelection.Clipboard);
            Assert.AreEqual(k_DefaultNodeCount - m_SelectedNodeCount, GetElementCount());
        }

        [UnityTest]
        public IEnumerator SelectedElementsCanBeDuplicated()
        {
            GraphView.RebuildUI();
            yield return null;

            GraphView.ViewSelection.Clipboard = "Unknown data";
            SelectThreeElements();
            MouseCaptureController.ReleaseMouse();
            GraphView.Focus();
            yield return null;

            bool used = Helpers.ValidateCommand("Duplicate");
            Assert.IsTrue(used);
            yield return null;

            Helpers.ExecuteCommand("Duplicate");
            GraphView.RebuildUI();
            yield return null;

            // Duplicate does not change the copy buffer.
            Assert.AreEqual("Unknown data", GraphView.ViewSelection.Clipboard);
            Assert.AreEqual(k_DefaultNodeCount + m_SelectedNodeCount, GetElementCount());
        }

        [UnityTest]
        public IEnumerator SelectedElementsCanBeDeleted()
        {
            MarkGraphViewStateDirty();
            yield return null;

            SelectThreeElements();
            MouseCaptureController.ReleaseMouse();
            GraphView.Focus();

            bool used = Helpers.ValidateCommand("Delete");
            Assert.IsTrue(used);
            yield return null;

            Helpers.ExecuteCommand("Delete");
            yield return null;

            Assert.AreEqual(k_DefaultNodeCount - m_SelectedNodeCount, GetElementCount());
        }
    }
}
