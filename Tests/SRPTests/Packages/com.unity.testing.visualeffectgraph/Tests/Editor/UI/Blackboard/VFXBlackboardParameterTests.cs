using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.UI.Blackboard
{
    [TestFixture]
    public class VFXBlackboardParameterTests
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
        public IEnumerator Add_Parameter()
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

            var descriptorParameter = VFXLibrary.GetParameters().First(d => d.modelType == typeof(int));

            // Act
            m_BlackboardTestHelper.AddParameter(blackboard, descriptorParameter);

            // Assert
            Assert.IsNotNull(GetItemOfTypeAndName<PropertyItem>(blackboard, $"New {descriptorParameter.name}"), "The row must exist as soon as the parameter is added");
            var enumerator = VFXBlackboardTestHelper.IsOpenForEdit(blackboard, "textField", $"New {descriptorParameter.name}");
            while (enumerator.MoveNext())
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Add_OutputParameter()
        {
            // Arrange
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
            var subgraph = VisualEffectAssetEditorUtility.CreateNew<VisualEffectSubgraphOperator>($"Assets/{testFolder}/vfx_{GUID.Generate()}.vfxoperator");
            var window = VFXTestCommon.GetWindow(subgraph.GetResource(), true);
            window.LoadResource(subgraph.GetResource());
            yield return null;

            var blackboard = window.graphView.blackboard;
            var descriptorParameter = VFXLibrary.GetParameters().First(d => d.modelType == typeof(int));
            var outputCategory = blackboard.Query<VFXBlackboardCategory>().Where(x => x.title == "Output").First();
            blackboard.Q<TreeView>().selectedIndex = outputCategory.category.index;

            // Act
            m_BlackboardTestHelper.AddParameter(blackboard, descriptorParameter);

            // Assert
            Assert.IsNotNull(GetItemOfTypeAndName<PropertyItem>(blackboard, $"New {descriptorParameter.name}"), "The row must exist as soon as the parameter is added");
            var enumerator = VFXBlackboardTestHelper.IsOpenForEdit(blackboard, "textField", $"New {descriptorParameter.name}");
            while (enumerator.MoveNext())
            {
                yield return null;
            }
            var outputParameterField = blackboard.Q<VFXBlackboardField>();
            Assert.IsTrue(outputParameterField.controller.isOutput);
        }

        [UnityTest]
        public IEnumerator Move_Range_Parameter_To_Output_Category_Keeps_Node_Port_Visible()
        {
            // Arrange
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
            var subgraph = VisualEffectAssetEditorUtility.CreateNew<VisualEffectSubgraphOperator>($"Assets/{testFolder}/vfx_{GUID.Generate()}.vfxoperator");
            var window = VFXTestCommon.GetWindow(subgraph.GetResource(), true);
            window.LoadResource(subgraph.GetResource());
            yield return null;

            var blackboard = window.graphView.blackboard;
            blackboard.Update();
            yield return null;

            var viewController = window.graphView.controller;
            m_BlackboardTestHelper.AddParameter(blackboard, VFXLibrary.GetParameters().First(x => x.modelType == typeof(float)));
            var enumerator = m_BlackboardTestHelper.WaitForPendingAddParameters(() => viewController.parameterControllers.Any());
            while (enumerator.MoveNext())
            {
                yield return null;
            }

            var parameterController = viewController.parameterControllers.Single();
            // The blackboard row must be expanded for the Mode dropdown to be reachable, as it is in the repro
            parameterController.expanded = true;
            parameterController.valueFilter = VFXValueFilter.Range;
            parameterController.maxValue = 100f;
            viewController.LightApplyChanges();
            var maxFrames = 8;
            while (maxFrames-- > 0)
            {
                yield return null;
            }

            // Act
            // Same calls as VFXView when the property is dropped into the workspace...
            viewController.AddVFXParameter(new Vector2(200, 200), parameterController, null);
            yield return null;
            // ...and as VFXBlackboard when it is then dropped onto the Output category
            parameterController.model.category = string.Empty;
            parameterController.isOutput = true;
            parameterController.exposed = false;

            maxFrames = 16;
            while (maxFrames-- > 0)
            {
                yield return null;
            }

            // Assert
            var parameterUI = window.graphView.Query<VFXParameterUI>().ToList().SingleOrDefault(x => x.controller?.parentController == parameterController);
            Assert.NotNull(parameterUI, "The output parameter node was not created in the workspace.");
            Assert.AreEqual(1, parameterUI.controller.inputPorts.Count, "The output parameter node has no input port.");
            Assert.IsTrue(parameterUI.expanded, "The output parameter node is collapsed and cannot be expanded.");
            var port = parameterUI.GetPorts(true, false).FirstOrDefault();
            Assert.NotNull(port, "The output parameter node has no port element.");
            Assert.IsTrue(port.visible, "The output parameter node port is invisible.");

            // Laying the blackboard row out must not throw, otherwise the whole panel stops being
            // updated, which is what makes the node unexpandable and its port invisible
            var propertyView = blackboard.Query<VFXBlackboardPropertyView>().ToList().SingleOrDefault(x => x.controller == parameterController);
            Assert.NotNull(propertyView, "The blackboard row of the output parameter was not found.");
            var relayout = typeof(VFXBlackboardPropertyView).GetMethod("Relayout", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(relayout);
            Assert.DoesNotThrow(() => relayout.Invoke(propertyView, null));
        }

        [Test]
        public void Move_Parameter_To_Output_Category_And_Back_Keeps_Enum_Values()
        {
            // Arrange
            var subgraph = VisualEffectAssetEditorUtility.CreateNew<VisualEffectSubgraphOperator>($"Assets/{testFolder}/vfx_{GUID.Generate()}.vfxoperator");
            var viewController = VFXViewController.GetController(subgraph.GetResource(), true);
            viewController.useCount++;
            try
            {
                var parameter = viewController.AddVFXParameter(Vector2.zero, VFXLibrary.GetParameters().First(x => x.modelType == typeof(uint)).variant);
                viewController.LightApplyChanges();

                var parameterController = viewController.GetParameterController(parameter);
                parameterController.valueFilter = VFXValueFilter.Enum;
                parameterController.model.enumValues = new List<string> { "Idle", "Walk", "Run" };
                parameterController.value = 2u;
                viewController.LightApplyChanges();

                // Act
                MoveToOutputCategoryAndBack(viewController, parameterController);

                // Assert
                Assert.AreEqual(VFXValueFilter.Enum, parameterController.valueFilter, "The enum mode was lost when moving the property to the Output category and back.");
                CollectionAssert.AreEqual(new[] { "Idle", "Walk", "Run" }, parameterController.model.enumValues, "The enum labels were lost when moving the property to the Output category and back.");
            }
            finally
            {
                viewController.useCount--;
            }
        }

        [Test]
        public void Move_Parameter_To_Output_Category_And_Back_Keeps_Range()
        {
            // Arrange
            var subgraph = VisualEffectAssetEditorUtility.CreateNew<VisualEffectSubgraphOperator>($"Assets/{testFolder}/vfx_{GUID.Generate()}.vfxoperator");
            var viewController = VFXViewController.GetController(subgraph.GetResource(), true);
            viewController.useCount++;
            try
            {
                var parameter = viewController.AddVFXParameter(Vector2.zero, VFXLibrary.GetParameters().First(x => x.modelType == typeof(float)).variant);
                viewController.LightApplyChanges();

                var parameterController = viewController.GetParameterController(parameter);
                parameterController.valueFilter = VFXValueFilter.Range;
                parameterController.maxValue = 100f;
                viewController.LightApplyChanges();

                // Act
                MoveToOutputCategoryAndBack(viewController, parameterController);

                // Assert
                Assert.AreEqual(VFXValueFilter.Range, parameterController.valueFilter, "The range mode was lost when moving the property to the Output category and back.");
                Assert.AreEqual(0f, parameterController.minValue, "The range minimum was lost when moving the property to the Output category and back.");
                Assert.AreEqual(100f, parameterController.maxValue, "The range maximum was lost when moving the property to the Output category and back.");
            }
            finally
            {
                viewController.useCount--;
            }
        }

        private T GetItemOfTypeAndName<T>(VFXBlackboard blackboard, string name)
            where T: class, IParameterItem
        {
            var fieldInfo = blackboard.GetType().GetField("m_ParametersController", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(fieldInfo);
            var parametersController = (List<TreeViewItemData<IParameterItem>>)fieldInfo.GetValue(blackboard);

            var methodInfo = blackboard.GetType().GetMethod("GetDataRecursive", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(methodInfo);


            return parametersController
                .SelectMany(x => (IEnumerable<TreeViewItemData<IParameterItem>>)methodInfo.Invoke(blackboard, new object[] { x }))
                .SingleOrDefault(x => x.data.title == name)
                .data as T;
        }

        private static void MoveToOutputCategoryAndBack(VFXViewController viewController, VFXParameterController parameterController)
        {
            parameterController.isOutput = true;
            viewController.LightApplyChanges();
            parameterController.isOutput = false;
            viewController.LightApplyChanges();
        }

        [UnityTest]
        public IEnumerator Remove_Parameter()
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

            var descriptorParameter = VFXLibrary.GetParameters().First(d => d.modelType == typeof(int));
            m_BlackboardTestHelper.AddParameter(blackboard, descriptorParameter);
            var enumerator = m_BlackboardTestHelper.WaitForPendingAddParameters(() => blackboard.Q<VFXBlackboardField>() != null);
            while (enumerator.MoveNext())
            {
                yield return null;
            }
            blackboard.Focus();
            yield return null;

            // Act
            var parameterField = blackboard.Q<VFXBlackboardField>();
            Assert.NotNull(parameterField);
            window.graphView.AddToSelection(parameterField);
            window.graphView.Delete();
            yield return null;

            // Assert
            parameterField = blackboard.Q<VFXBlackboardField>();
            Assert.Null(parameterField);
            Assert.IsEmpty(graph.m_ParameterInfo);
        }

        [UnityTest]
        public IEnumerator DoubleClick_To_Rename_Parameter()
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

            var descriptorParameter = VFXLibrary.GetParameters().First(d => d.modelType == typeof(int));
            m_BlackboardTestHelper.AddParameter(blackboard, descriptorParameter);
            var enumerator = m_BlackboardTestHelper.WaitForPendingAddParameters(() => blackboard.Q<VFXBlackboardField>() != null);
            while (enumerator.MoveNext())
            {
                yield return null;
            }

            // Remove focus from newly created attribute so that it's not open for edit
            blackboard.Focus();
            yield return null;

            // Act
            var blackboardField = blackboard.Q<VFXBlackboardField>();
            Assert.NotNull(blackboardField);
            VFXGUITestHelper.SendDoubleClick(blackboardField, 2);

            // Assert
            enumerator = VFXBlackboardTestHelper.IsOpenForEdit(blackboard, "textField", $"New {descriptorParameter.name}");
            while (enumerator.MoveNext())
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Rename_Parameter_With_Different_Case_Only([Values("", " ")] string padding)
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

            var descriptorParameter = VFXLibrary.GetParameters().First(d => d.modelType == typeof(int));
            m_BlackboardTestHelper.AddParameter(blackboard, descriptorParameter);
            var enumerator = m_BlackboardTestHelper.WaitForPendingAddParameters(() => blackboard.Q<VFXBlackboardField>() != null);
            while (enumerator.MoveNext())
            {
                yield return null;
            }

            var blackboardField = blackboard.Q<VFXBlackboardField>();
            Assert.NotNull(blackboardField);
            var initialName = blackboardField.controller.exposedName;

            // Act
            var newName = initialName.ToUpperInvariant();
            Assert.AreNotEqual(initialName, newName);
            // Surrounding spaces are trimmed, so the name still only differs by its case
            blackboardField.controller.exposedName = $"{padding}{newName}{padding}";
            yield return null;

            // Assert
            Assert.AreEqual(newName, blackboardField.controller.exposedName);
        }

        [UnityTest]
        public IEnumerator Rename_Parameter_With_Existing_Name_Gets_Unique_Name()
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

            m_BlackboardTestHelper.AddParameter(blackboard, VFXLibrary.GetParameters().First(d => d.modelType == typeof(int)));
            yield return null;
            m_BlackboardTestHelper.AddParameter(blackboard, VFXLibrary.GetParameters().First(d => d.modelType == typeof(float)));
            var enumerator = m_BlackboardTestHelper.WaitForPendingAddParameters(() => blackboard.Query<VFXBlackboardField>().ToList().Count == 2);
            while (enumerator.MoveNext())
            {
                yield return null;
            }

            var parameterFields = blackboard.Query<VFXBlackboardField>().ToList();
            Assert.AreEqual(2, parameterFields.Count);

            // Act
            var existingName = parameterFields[0].controller.exposedName;
            parameterFields[1].controller.exposedName = existingName;
            yield return null;

            // Assert
            Assert.AreEqual($"{existingName} (1)", parameterFields[1].controller.exposedName);

            // Renaming again to the same existing name must give the same suffix, not alternate between 1 and 2
            for (var i = 0; i < 3; i++)
            {
                parameterFields[1].controller.exposedName = existingName;
                yield return null;
                Assert.AreEqual($"{existingName} (1)", parameterFields[1].controller.exposedName);
            }
        }
    }
}
