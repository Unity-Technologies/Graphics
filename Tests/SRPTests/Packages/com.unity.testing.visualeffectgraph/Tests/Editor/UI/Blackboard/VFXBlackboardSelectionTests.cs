using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.UI.Blackboard
{
    [TestFixture]
    public class VFXBlackboardSelectionTests
    {
        private const string testFolder = "TmpTests";
        private readonly VFXBlackboardTestHelper m_BlackboardTestHelper = new VFXBlackboardTestHelper();

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            AssetDatabase.DeleteAsset("Assets/" + testFolder);
            AssetDatabase.CreateFolder("Assets", testFolder);
            VFXTestCommon.CloseAllVFXWindow();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            // A pending OnAddParameter delay call would otherwise run during the next test
            var enumerator = m_BlackboardTestHelper.WaitForPendingAddParameters();
            while (enumerator.MoveNext())
            {
                yield return null;
            }
        }

        [OneTimeTearDown]
        public void OneTimeCleanup()
        {
            VFXTestCommon.CloseAllVFXWindow();
            VFXTestCommon.DeleteAllTemporaryGraph();
        }
        [UnityTest]
        public IEnumerator Select_Parameter_RecordsToHistory_AndCanNavigate()
        {
            // Arrange
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var window = VFXViewWindow.GetWindow(graph, true);
            window.LoadResource(graph.visualEffectResource);
            VFXBlackboard blackboard = window.graphView.blackboard;
            yield return null;
            blackboard.Update();
            yield return null;

            var paramDescArray = VFXLibrary.GetParameters().ToArray();
            for (int i = 0; i < paramDescArray.Length; i++)
            {
                m_BlackboardTestHelper.AddParameter(blackboard, paramDescArray[i]);
            }

            var enumerator = m_BlackboardTestHelper.WaitForPendingAddParameters(() => blackboard.Q<VFXBlackboardField>() != null);
            while (enumerator.MoveNext())
            {
                yield return null;
            }
            blackboard.Focus();
            yield return null;

            // Act
            var parameterFieldFirst = blackboard.Query<VFXBlackboardField>().First();
            var parameterFieldLast = blackboard.Query<VFXBlackboardField>().Last();
            Assert.NotNull(parameterFieldFirst);
            Assert.NotNull(parameterFieldLast);

            SelectionHistory.instance.ClearHistory();
            window.graphView.AddToSelection(parameterFieldFirst);
            Undo.IncrementCurrentGroup();
            yield return null;

            // Assert
            Assert.IsTrue(SelectionHistory.instance.GetHistory().Count == 1);

            window.graphView.ClearSelection();
            yield return null;

            window.graphView.AddToSelection(parameterFieldLast);
            Undo.IncrementCurrentGroup();
            yield return null;

            Assert.IsTrue(SelectionHistory.instance.GetHistory().Count == 2);
            var currentSelection = SelectionHistory.instance.GetHistory()[SelectionHistory.instance.GetIndex()];
            Assert.IsTrue(currentSelection.m_CustomData.Contains(parameterFieldLast.PropertyItem.title));

            SelectionHistory.instance.GoBack();
            yield return null;

            currentSelection = SelectionHistory.instance.GetHistory()[SelectionHistory.instance.GetIndex()];
            Assert.IsTrue(currentSelection.m_CustomData.Contains(parameterFieldFirst.PropertyItem.title));
        }

        [UnityTest, Ignore("Click event do not reach the treeview, I keep it to fix it later")]
        public IEnumerator Shift_Click_To_Multi_Select_Attributes()
        {
            // Arrange
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var window = VFXTestCommon.GetWindow(graph, true);
            window.LoadResource(graph.visualEffectResource);
            VFXBlackboard blackboard = window.graphView.blackboard;
            yield return null;
            blackboard.Update();
            yield return null;
            var blackboardAttributes = blackboard.Query<VFXBlackboardAttributeField>().ToList().Take(4).ToArray();

            // Act
            blackboardAttributes[0].GetFirstAncestorOfType<VFXBlackboardAttributeRow>().SendEvent(ClickEvent.GetPooled());
            yield return null;
            blackboardAttributes[3].SendEvent(ClickEvent.GetPooled(new Touch(), EventModifiers.Shift));

            // Assert
            Assert.AreEqual(4, blackboard.selection.Count);
        }
    }
}
