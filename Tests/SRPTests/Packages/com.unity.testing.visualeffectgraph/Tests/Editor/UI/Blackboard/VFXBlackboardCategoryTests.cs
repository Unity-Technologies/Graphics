using System;
using System.Collections;
using System.Collections.Generic;
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
    public class VFXBlackboardCategoryTests
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
        public IEnumerator Add_Category()
        {
            // Arrange
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var window = VFXTestCommon.GetWindow(graph, true);
            window.LoadResource(graph.visualEffectResource);
            VFXBlackboard blackboard = window.graphView.blackboard;
            yield return null;

            // Act
            blackboard.AddCategory("new category");
            yield return null;

            // Assert
            var enumerator = VFXBlackboardTestHelper.IsOpenForEdit(blackboard, "titleEdit", "new category");
            while (enumerator.MoveNext())
            {
                yield return null;
            }
            Assert.AreEqual(1, graph.UIInfos.categories.Count);
        }

        [UnityTest]
        public IEnumerator Add_Category_With_Only_Spaces_In_Name()
        {
            // Arrange
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
            var categoryName = "new category";
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var window = VFXTestCommon.GetWindow(graph, true);
            window.LoadResource(graph.visualEffectResource);
            VFXBlackboard blackboard = window.graphView.blackboard;
            yield return null;

            blackboard.Update();
            blackboard.AddCategory(categoryName);
            yield return null;

            window.graphView.OnSave();
            yield return null;
            var cat = blackboard.Query<VFXBlackboardCategory>().Where(x => x.title == categoryName).First();

            // Act
            // The name is trimmed before being validated, so it ends up empty and is rejected
            LogAssert.Expect(LogType.Error, "Cannot rename category: a category name cannot be empty.");
            blackboard.SetCategoryName(cat, " ");

            // Assert
            Assert.AreEqual(categoryName, cat.title);
        }

        [UnityTest]
        public IEnumerator Rename_Category_With_Reserved_Name()
        {
            // Arrange
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
            var categoryName = "new category";
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var window = VFXTestCommon.GetWindow(graph, true);
            window.LoadResource(graph.visualEffectResource);
            VFXBlackboard blackboard = window.graphView.blackboard;
            yield return null;

            blackboard.Update();
            blackboard.AddCategory(categoryName);
            yield return null;

            window.graphView.OnSave();
            yield return null;
            var cat = blackboard.Query<VFXBlackboardCategory>().Where(x => x.title == categoryName).First();

            // Act
            LogAssert.Expect(LogType.Error, "Cannot rename category: 'Attributes' is a reserved category name.");
            blackboard.SetCategoryName(cat, "Attributes");

            // Assert
            Assert.AreEqual(categoryName, cat.title);
        }

        [UnityTest]
        public IEnumerator Duplicate_Category_And_Rename()
        {
            // Arrange
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
            var categoryName = "new category";
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var window = VFXTestCommon.GetWindow(graph, true);
            window.LoadResource(graph.visualEffectResource);
            VFXBlackboard blackboard = window.graphView.blackboard;
            yield return null;

            blackboard.Update();
            blackboard.AddCategory(categoryName);
            yield return null;
            window.graphView.OnSave();
            yield return null;
            var cat = blackboard.Query<VFXBlackboardCategory>().Where(x => x.title == categoryName).First();
            cat.Select(blackboard, false);

            // Act
            // Duplicate category
            window.graphView.selection.Add(cat);
            window.graphView.DuplicateBlackBoardCategorySelection();
            blackboard.SetCategoryName(cat, "new name");
            window.graphView.OnSave();
            var enumerator = VFXBlackboardTestHelper.WaitFor(() => blackboard.Query<VFXBlackboardCategory>().Where(x => x.title == categoryName + " (1)").First() != null);
            while (enumerator.MoveNext())
            {
                yield return null;
            }

            // Assert
            cat = blackboard.Query<VFXBlackboardCategory>().Where(x => x.title == categoryName + " (1)").First();
            Assert.NotNull(cat);
        }

        [UnityTest]
        public IEnumerator Duplicate_Category_With_Long_Parameter_Name_Keeps_Name_Length_Limit()
        {
            // Arrange
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
            string categoryName = "new category";
            VFXGraph graph = VFXTestCommon.MakeTemporaryGraph();
            VFXViewWindow window = VFXTestCommon.GetWindow(graph, true);
            window.LoadResource(graph.visualEffectResource);
            VFXBlackboard blackboard = window.graphView.blackboard;
            yield return null;

            blackboard.Update();
            blackboard.AddCategory(categoryName);
            yield return null;

            VFXViewController viewController = window.graphView.controller;
            VFXParameter parameter = viewController.AddVFXParameter(Vector2.zero, VFXLibrary.GetParameters().First(d => d.modelType == typeof(float)).variant);
            viewController.ApplyChanges();
            var longName = new string('a', VFXParameterController.kMaxExposedNameLength);
            parameter.SetSettingValue("m_ExposedName", longName);
            parameter.category = categoryName;
            yield return null;

            // Act
            blackboard.DuplicateCategory(categoryName);
            yield return null;

            // Assert
            VFXParameterController copy = viewController.parameterControllers.Single(x => x.model != parameter);
            Assert.AreEqual(new string('a', VFXParameterController.kMaxExposedNameLength - 4) + " (1)", copy.exposedName);
        }

        [UnityTest]
        public IEnumerator Remove_Category()
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

            var category = "some category";
            blackboard.AddCategory(category);
            yield return null;

            var categoryRow = blackboard.Query<VFXBlackboardCategory>().Where(x => x.title == category).First();
            Assert.NotNull(categoryRow);
            Assert.AreEqual(1, graph.UIInfos.categories.Count);

            // Act
            window.graphView.AddToSelection(categoryRow);
            window.graphView.Delete();
            yield return null;

            // Assert
            categoryRow = blackboard.Query<VFXBlackboardCategory>().Where(x => x.title == category).First();
            Assert.Null(categoryRow);
            Assert.IsEmpty(graph.UIInfos.categories);
        }

        [UnityTest]
        public IEnumerator Remove_Category_And_Parameters()
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

            var category = "some category";
            blackboard.AddCategory(category);
            yield return null;
            var categoryRow = blackboard.Query<VFXBlackboardCategory>().Where(x => x.title == category).First();
            var descriptorParameter = VFXLibrary.GetParameters().First();
            m_BlackboardTestHelper.AddParameter(blackboard, descriptorParameter);
            yield return null;
            m_BlackboardTestHelper.AddParameter(blackboard, descriptorParameter);
            var enumerator = m_BlackboardTestHelper.WaitForPendingAddParameters(() => blackboard.Query<VFXBlackboardField>().ToList().Count == 2);
            while (enumerator.MoveNext())
            {
                yield return null;
            }
            var parameterFields = blackboard.Query<VFXBlackboardField>().ToList();

            // Act
            window.graphView.AddToSelection(categoryRow);
            parameterFields.ForEach(window.graphView.AddToSelection);
            window.graphView.askForConfirmationBeforeDelete = false;
            window.graphView.Delete();

            // Assert
            var frames = 32;
            while (frames-- > 0 && categoryRow != null)
            {
                yield return null;
                categoryRow = blackboard.Query<VFXBlackboardCategory>().Where(x => x.title == category).First();
            }

            Assert.Null(categoryRow);
            Assert.Null(blackboard.Query<VFXBlackboardField>().First());
            Assert.IsEmpty(graph.UIInfos.categories);
        }

        [UnityTest]
        public IEnumerator DoubleClick_To_Rename_Category()
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

            var category = "some category";
            blackboard.AddCategory(category);
            yield return null;

            // Remove focus from newly created attribute so that it's not open for edit
            blackboard.Focus();
            yield return null;

            // Act
            var blackboardCategory = blackboard.Query<VFXBlackboardCategory>().Where(x => !((IParameterCategory)x.category).isRoot).First();
            VFXGUITestHelper.SendDoubleClick(blackboardCategory, 2);

            // Assert
            var enumerator = VFXBlackboardTestHelper.IsOpenForEdit(blackboard, "titleEdit", category);
            while (enumerator.MoveNext())
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator DoubleClick_To_Rename_Root_Category_Does_Nothing([Values("Properties", "Attributes", "Custom Attributes", "Built-in Attributes")]string categoryName)
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

            var treeView = blackboard.Q<TreeView>();
            Assert.NotNull(treeView);
            treeView.ExpandRootItems();
            var categoryId = treeView.viewController.GetAllItemIds().First(id =>
                string.Equals(treeView.GetItemDataForId<IParameterItem>(id).title, categoryName, StringComparison.OrdinalIgnoreCase));
            treeView.ScrollToItemById(categoryId);

            VFXBlackboardCategory blackboardCategory = null;
            var deadline = EditorApplication.timeSinceStartup + 5.0;
            while (blackboardCategory == null && EditorApplication.timeSinceStartup < deadline)
            {
                yield return null;
                blackboardCategory = blackboard.Query<VFXBlackboardCategory>().Where(x => x.category.id == categoryId).First();
            }
            Assert.NotNull(blackboardCategory, $"Blackboard category '{categoryName}' did not become visible.");
            Assert.NotNull(blackboardCategory.Q<TextField>("titleEdit"));

            // Act
            VFXGUITestHelper.SendDoubleClick(blackboardCategory, 2);

            // Assert
            for (var frame = 0; frame < 16; frame++)
            {
                yield return null;
                Assert.IsNull(blackboard.Query<TextField>("titleEdit").Where(x => x.resolvedStyle.display == DisplayStyle.Flex).First(),
                    $"Blackboard category '{categoryName}' must not enter rename mode.");
            }
        }
    }
}
