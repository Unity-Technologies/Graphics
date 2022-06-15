using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Assert = UnityEngine.Assertions.Assert;
using Object = System.Object;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{

    // TODO (Sai): Add test coverage for adding all types of blackboard items
    public class BlackboardTests : BaseGraphWindowTest
    {
        protected override bool hideOverlayWindows => false;
        BlackboardView m_BlackboardView;

        public override void SetUp()
        {
            base.SetUp();
            FindBlackboardView();
        }

        private void FindBlackboardView()
        {
            const string viewFieldName = "m_BlackboardView";

            var found = m_Window.TryGetOverlay(k_BlackboardOverlayId, out var blackboardOverlay);
            Assert.IsTrue(found, "Blackboard overlay was not found");

            m_BlackboardView = (BlackboardView)blackboardOverlay.GetType()
                .GetField(viewFieldName, BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(blackboardOverlay);
            Assert.IsNotNull(m_BlackboardView, "Blackboard view was not found");
        }

        T GetFirstBlackboardElementOfType<T>() where T : VisualElement
        {
            var decl = GraphModel.VariableDeclarations.FirstOrDefault();
            Assert.IsNotNull(decl, "Menu item should have created underlying variable declaration");

            var views = new List<ModelView>();
            decl.GetAllViews(m_BlackboardView, v => v is T, views);

            if (views.FirstOrDefault() is T view)
                return view;

            return null;
        }

        [UnityTest]
        public IEnumerator TestBlackboardLoadsWithCorrectFieldType()
        {
            void ValidateCreatedField()
            {
                var view = GetFirstBlackboardElementOfType<GraphDataBlackboardVariablePropertyView>();
                if (view == null)
                {
                    Assert.IsFalse(true);
                    throw new Exception("Unreachable");
                }

                var field = view.Q<ConstantField>(className: "ge-inline-value-editor");
                Assert.IsNotNull(field, "Created blackboard item should contain an Initialization field");

                var firstChild = field.Children().First();
                Assert.IsTrue(firstChild is FloatField, "Float property should have a float field");
            }

            {
                var stencil = (ShaderGraphStencil)GraphModel.Stencil;

                var createMenu = new List<Stencil.MenuItem>();
                stencil.PopulateBlackboardCreateMenu("Properties", createMenu, m_BlackboardView);

                var floatItem = createMenu.FirstOrDefault(i => i.name == "Create Float");
                Assert.IsNotNull(floatItem, "Blackboard create menu must contain a \"Create Float\" item");

                floatItem.action.Invoke();
                yield return null;

                ValidateCreatedField();
            }

            yield return SaveAndReopenGraph();

            {
                FindBlackboardView();
                ValidateCreatedField();
            }
        }

        [UnityTest]
        public IEnumerator TestVariableNodeCanBeAdded()
        {
            var stencil = (ShaderGraphStencil)m_GraphView.GraphModel.Stencil;

            var createMenu = new List<Stencil.MenuItem>();
            stencil.PopulateBlackboardCreateMenu("Properties", createMenu, m_BlackboardView);

            var vector4Item = createMenu.FirstOrDefault(i => i.name == "Create Vector 4");
            Assert.IsNotNull(vector4Item, "Blackboard create menu must contain a \"Create Vector 4\" item");

            vector4Item.action.Invoke();
            yield return null;

            // Drag and drop to create variable node
            var blackboardField = GetFirstBlackboardElementOfType<BlackboardField>();
            m_TestEventHelper.SendMouseDownEvent(blackboardField);
            m_TestEventHelper.SendMouseDragEvent(blackboardField, m_GraphView);
            m_TestEventHelper.SendDragEnterEvent(m_GraphView);
            yield return null;

            var graphViewCenterPosition = TestEventHelpers.GetScreenPosition(m_Window, m_GraphView, true);
            m_TestEventHelper.SendDragPerformEvent(graphViewCenterPosition);

            yield return null;

            Assert.IsNotNull(m_Window.GetNodeModelFromGraphByName("Vector 4"));
        }

        [UnityTest]
        public IEnumerator TestVariableNodeCanBeDeleted()
        {
            yield return TestVariableNodeCanBeAdded();

            var variableNodeModel = m_Window.GetNodeModelFromGraphByName("Vector 4");

            // NOTE: Unlike in GraphNodeTests where the graph view already has focus, if we don't
            // send focus to the graph view visual element first (after interacting with the blackboard),
            // the commands don't go through
            m_TestEventHelper.SendMouseDownEvent(m_GraphView);
            m_TestEventHelper.SendMouseUpEvent(m_GraphView);

            m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, variableNodeModel));
            yield return null;

            Assert.IsTrue(m_TestEventHelper.SendDeleteCommand());
            yield return null;

            var vector4Node = m_Window.GetNodeModelFromGraphByName("Vector 4");
            Assert.IsNull(vector4Node, "Node should be null after delete operation");

            var graphDataVariableNodeModel = variableNodeModel as GraphDataVariableNodeModel;
            var variableNodeHandler = GraphModel.GraphHandler.GetNode(graphDataVariableNodeModel.graphDataName);
            Assert.IsNull(variableNodeHandler, "Node should also be removed from CLDS after delete operation");
        }


        // TODO (Sai) : Find out why we can't use TestEventHelpers.SelectAndCopyNodes() here
        [UnityTest]
        public IEnumerator TestVariableNodeCanBeCopied()
        {
            yield return TestVariableNodeCanBeAdded();

            var variableNodeModel = m_Window.GetNodeModelFromGraphByName("Vector 4");
            Assert.IsNotNull(variableNodeModel);

            // NOTE: Unlike in GraphNodeTests where the graph view already has focus, if we don't
            // send focus to the graph view visual element first (after interacting with the blackboard),
            // the commands don't go through
            m_TestEventHelper.SendMouseDownEvent(m_GraphView);
            m_TestEventHelper.SendMouseUpEvent(m_GraphView);

            m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, variableNodeModel));
            yield return null;

            Assert.IsTrue(m_TestEventHelper.SendDuplicateCommand());
            yield return null;

            Assert.IsTrue(m_Window.GetNodeModelsFromGraphByName("Vector 4").Count == 2, "Should be two variable nodes after copy");
        }

        [UnityTest]
        public IEnumerator TestBlackboardItemValueChangeAffectsPreview()
        {
            yield return TestVariableNodeCanBeAdded();

            var variableNodeModel = m_Window.GetNodeModelFromGraphByName("Vector 4");
            Assert.IsNotNull(variableNodeModel);

            yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");

            m_TestInteractionHelper.ConnectNodes("Vector 4", "Add", "Output", "A");

            var graphDataVariable = variableNodeModel as GraphDataVariableNodeModel;
            m_GraphView.Dispatch(new UpdateConstantValueCommand(
                graphDataVariable.VariableDeclarationModel.InitializationModel,
                new Vector4(1, 0, 0, 0),
                graphDataVariable.VariableDeclarationModel));

            var nodePreviewMaterial = m_Window.previewManager.GetPreviewMaterialForNode(graphDataVariable.graphDataName);
            Assert.IsNotNull(nodePreviewMaterial);
            Assert.AreEqual(Color.red, SampleMaterialColor(nodePreviewMaterial));
        }
    }
}
