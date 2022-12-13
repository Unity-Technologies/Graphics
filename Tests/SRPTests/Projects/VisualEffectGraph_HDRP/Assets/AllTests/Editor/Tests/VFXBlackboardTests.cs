#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using UnityEditor.VFX.UI;
using UnityEngine.TestTools;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXBlackboardTests
    {

        [SetUp]
        public void Init()
        {
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
        }

        [TearDown]
        public void Cleanup()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [UnityTest]
        public IEnumerator Add_Category_With_Only_Spaces_In_Name()
        {
            // Prepare
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
            var categoryName = "new category";
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var window = VFXViewWindow.GetWindow(graph, true);
            window.LoadResource(graph.visualEffectResource);
            VFXBlackboard blackboard = window.graphView.blackboard;
            blackboard.AddCategory(categoryName);
            yield return null;
            window.graphView.OnSave();
            yield return null;
            var cat = GetCategory(blackboard, categoryName);

            // Act
            blackboard.SetCategoryName(cat, " ");

            // Assert
            Assert.AreEqual(categoryName, cat.title);
        }

        [UnityTest]
        public IEnumerator Duplicate_Category_And_Rename()
        {
            // Prepare
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
            var categoryName = "new category";
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var window = VFXViewWindow.GetWindow(graph, true);
            window.LoadResource(graph.visualEffectResource);
            VFXBlackboard blackboard = window.graphView.blackboard;
            blackboard.AddCategory(categoryName);
            yield return null;
            window.graphView.OnSave();
            yield return null;
            var cat = GetCategory(blackboard, categoryName);
            cat.Select(blackboard, false);

            // Act
            // Duplicate category
            DuplicateSelectedCategory(window.graphView);
            blackboard.SetCategoryName(cat, "new name");
            window.graphView.OnSave();
            yield return null;

            // Assert
            Assert.NotNull(GetCategory(blackboard, categoryName + " 1"));
        }

        private VFXBlackboardCategory GetCategory(VFXBlackboard blackboard, string name)
        {
            var fieldInfo = typeof(VFXBlackboard).GetField("m_Categories", BindingFlags.Instance | BindingFlags.NonPublic);
            var categories = (Dictionary<string, VFXBlackboardCategory>)fieldInfo.GetValue(blackboard);

            return categories.TryGetValue(name, out var cat)
                ? cat
                : null;
        }

        private void DuplicateSelectedCategory(VFXView view)
        {
            var methodInfo = typeof(VFXView).GetMethod("DuplicateBlackBoardCategorySelection", BindingFlags.Instance | BindingFlags.NonPublic);
            methodInfo.Invoke(view, null);
        }
    }
}
#endif
