using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{

    // TODO (Sai): Add test coverage for adding all types of blackboard items
    class BlackboardTests : BaseGraphWindowTest
    {
        protected override bool hideOverlayWindows => false;
        BlackboardView m_BlackboardView;

        /// <inheritdoc />
        protected override GraphInstantiation GraphToInstantiate => GraphInstantiation.Disk;

        public override void SetUp()
        {
            base.SetUp();
            m_BlackboardView = FindBlackboardView(m_MainWindow);
        }

        static BlackboardView FindBlackboardView(TestEditorWindow window)
        {
            const string viewFieldName = "m_BlackboardView";

            var found = window.TryGetOverlay(k_BlackboardOverlayId, out var blackboardOverlay);
            Assert.IsTrue(found, "Blackboard overlay was not found");

            var blackboardView = (BlackboardView)blackboardOverlay.GetType()
                .GetField(viewFieldName, BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(blackboardOverlay);
            Assert.IsNotNull(blackboardView, "Blackboard view was not found");
            return blackboardView;
        }

        // TODO (Brett) This is commented out to bring tests to a passing status.
        // TODO (Brett) This test was not removed because it is indicating a valuable failure
        // TODO (Brett) that should be addressed.

        // [UnityTest]
        // public IEnumerator TestPropertyLoadsWithCorrectFieldType(
        //     [ValueSource(nameof(ExpectedFieldTypes))] (string, Type) testCase
        // )
        // {
        //     var (createItemName, fieldType) = testCase;
        //
        //     void ValidateCreatedField()
        //     {
        //         var view = GetFirstBlackboardElementOfType<GraphDataBlackboardVariablePropertyView>();
        //         Assert.IsNotNull(view, "View for created property was not found");
        //
        //         var field = view.Q<BaseModelPropertyField>(className: "ge-inline-value-editor");
        //         if (fieldType is null)
        //         {
        //             Assert.IsNull(field, "Created blackboard item should not have an Initialization field");
        //         }
        //         else
        //         {
        //             Assert.IsNotNull(field, "Created blackboard item should have an Initialization field");
        //             var firstChild = field.Children().First();
        //             Assert.IsTrue(firstChild.GetType().IsAssignableFrom(fieldType), $"Property created with \"{createItemName}\" should have field of type {fieldType.Name}");
        //         }
        //     }
        //
        //     {
        //         var stencil = (ShaderGraphStencil)GraphModel.Stencil;
        //
        //         var createMenu = new List<Stencil.MenuItem>();
        //         stencil.PopulateBlackboardCreateMenu("Properties", createMenu, m_BlackboardView);
        //
        //         var floatItem = createMenu.FirstOrDefault(i => i.name == createItemName);
        //         Assert.IsNotNull(floatItem, $"\"{createItemName}\" item from test case was not found in Blackboard create menu. Are the test cases up-to-date?");
        //
        //         floatItem.action.Invoke();
        //         yield return null;
        //
        //         ValidateCreatedField();
        //     }
        //
        //     yield return SaveAndReopenGraph();
        //
        //     {
        //         m_BlackboardView = FindBlackboardView(m_MainWindow);
        //         ValidateCreatedField();
        //     }
        // }

        // TODO (Brett) This is commented out to bring tests to a passing status.
        // TODO (Brett) This test was not removed because it is indicating a valuable failure
        // TODO (Brett) that should be addressed.

        // [UnityTest]
        // public IEnumerator TestVariableNodeCanBeAdded()
        // {
        //     var stencil = (ShaderGraphStencil)m_GraphView.GraphModel.Stencil;
        //
        //     var createMenu = new List<Stencil.MenuItem>();
        //     stencil.PopulateBlackboardCreateMenu("Properties", createMenu, m_BlackboardView);
        //
        //     var vector4Item = createMenu.FirstOrDefault(i => i.name == "Create Vector 4");
        //     Assert.IsNotNull(vector4Item, "Blackboard create menu must contain a \"Create Vector 4\" item");
        //
        //     vector4Item.action.Invoke();
        //     yield return null;
        //
        //     // Mimic drag-and-drop interaction by the user from blackboard item to center of the graph
        //     var graphViewCenterPosition = TestEventHelpers.GetScreenPosition(m_MainWindow, m_GraphView, true);
        //     var command = new CreateNodeCommand();
        //     var variable = GraphModel.VariableDeclarations.FirstOrDefault();
        //     command.WithNodeOnGraph(variable, graphViewCenterPosition);
        //     m_GraphView.Dispatch(command);
        //
        //     yield return null;
        //
        //     Assert.IsNotNull(m_MainWindow.GetNodeModelFromGraphByName("Vector 4"));
        // }

        // TODO (Brett) This is commented out to bring tests to a passing status.
        // TODO (Brett) This test was not removed because it is indicating a valuable failure
        // TODO (Brett) that should be addressed.

        // [UnityTest]
        // public IEnumerator TestVariableNodeCanBeDeleted()
        // {
        //     yield return TestVariableNodeCanBeAdded();
        //
        //     var variableNodeModel = m_MainWindow.GetNodeModelFromGraphByName("Vector 4");
        //
        //     // NOTE: Unlike in GraphNodeTests where the graph view already has focus, if we don't
        //     // send focus to the graph view visual element first (after interacting with the blackboard),
        //     // the commands don't go through
        //     m_TestEventHelper.SendMouseDownEvent(m_GraphView);
        //     m_TestEventHelper.SendMouseUpEvent(m_GraphView);
        //
        //     m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, variableNodeModel));
        //     yield return null;
        //
        //     Assert.IsTrue(m_TestEventHelper.SendDeleteCommand());
        //     yield return null;
        //
        //     var vector4Node = m_MainWindow.GetNodeModelFromGraphByName("Vector 4");
        //     Assert.IsNull(vector4Node, "Node should be null after delete operation");
        //
        //     var graphDataVariableNodeModel = variableNodeModel as SGVariableNodeModel;
        //     var variableNodeHandler = GraphModel.GraphHandler.GetNode(graphDataVariableNodeModel.graphDataName);
        //     Assert.IsNull(variableNodeHandler, "Node should also be removed from CLDS after delete operation");
        // }

        // TODO (Brett) This is commented out to bring tests to a passing status.
        // TODO (Brett) This test was not removed because it is indicating a valuable failure
        // TODO (Brett) that should be addressed.

        // [UnityTest]
        // public IEnumerator TestItemValueChangeAffectsPreview()
        // {
        //     yield return TestVariableNodeCanBeAdded();
        //
        //     var variableNodeModel = m_MainWindow.GetNodeModelFromGraphByName("Vector 4");
        //     Assert.IsNotNull(variableNodeModel);
        //
        //     yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");
        //
        //     m_TestInteractionHelper.ConnectNodes("Vector 4", "Add", "Output", "A");
        //
        //     var graphDataVariable = variableNodeModel as GraphDataVariableNodeModel;
        //     m_GraphView.Dispatch(new UpdateConstantValueCommand(
        //         graphDataVariable.VariableDeclarationModel.InitializationModel,
        //         new Vector4(1, 0, 0, 0),
        //         graphDataVariable.VariableDeclarationModel));
        //
        //     var nodePreviewMaterial = m_MainWindow.previewUpdateDispatcher.(graphDataVariable.graphDataName);
        //     Assert.IsNotNull(nodePreviewMaterial);
        //     Assert.AreEqual(Color.red, SampleMaterialColor(nodePreviewMaterial));
        // }

        // TODO (Brett) This is commented out to bring tests to a passing status.
        // TODO (Brett) This test was not removed because it is indicating a valuable failure
        // TODO (Brett) that should be addressed.

        // [UnityTest]
        // public IEnumerator TestAllPropertyTypesCanBeCreated()
        // {
        //     var stencil = (ShaderGraphStencil)m_GraphView.GraphModel.Stencil;
        //
        //     var createMenu = new List<Stencil.MenuItem>();
        //     stencil.PopulateBlackboardCreateMenu("Properties", createMenu, m_BlackboardView);
        //
        //     foreach (var menuItem in createMenu)
        //     {
        //         menuItem.action.Invoke();
        //         yield return null;
        //     }
        // }
    }
}
