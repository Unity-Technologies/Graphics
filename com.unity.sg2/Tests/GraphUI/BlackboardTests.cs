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
            m_BlackboardView = FindBlackboardView(m_Window);
        }

        // TODO: This will probably be useful elsewhere, consider abstracting it out
        string CreateSecondGraph(
            out ShaderGraphAsset secondGraphAsset,
            out TestEditorWindow secondEditorWindow,
            out TestEventHelpers secondWindowTestHelper)
        {
            // Create second graph
            var secondGraphPath = testAssetPath.Replace(ShaderGraphStencil.DefaultGraphAssetName, "NewShaderGraph1");
            var newGraphAction = ScriptableObject.CreateInstance<GraphAssetUtils.CreateGraphAssetAction>();
            newGraphAction.Action(0, secondGraphPath, "");
            secondGraphAsset = AssetDatabase.LoadAssetAtPath<ShaderGraphAsset>(secondGraphPath);

            // Create second window
            secondEditorWindow = EditorWindow.CreateWindow<TestEditorWindow>(typeof(TestEditorWindow), typeof(TestEditorWindow));
            secondEditorWindow.shouldCloseWindowNoPrompt = true;

            // Load second graph
            secondEditorWindow.SetCurrentSelection(secondGraphAsset, GraphViewEditorWindow.OpenMode.Open);
            secondWindowTestHelper = new TestEventHelpers(secondEditorWindow);
            return secondGraphPath;
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

        static readonly (string, Type)[] ExpectedFieldTypes =
        {
            // name of item in blackboard create menu, type of initialization field (or null if it should not exist)
            ("Create Integer", typeof(IntegerField)),
            ("Create Float", typeof(FloatField)),
            ("Create Boolean", typeof(Toggle)),
            ("Create Vector 2", typeof(Vector2Field)),
            ("Create Vector 3", typeof(Vector3Field)),
            ("Create Vector 4", typeof(Vector4Field)),
            ("Create Color", typeof(ColorField)),
            ("Create Matrix 2", typeof(MatrixField)),
            ("Create Matrix 3", typeof(MatrixField)),
            ("Create Matrix 4", typeof(MatrixField)),
            ("Create Gradient", typeof(GradientField)),
            ("Create Texture2D", typeof(ObjectField)),
            ("Create Texture2DArray", typeof(ObjectField)),
            ("Create Texture3D", typeof(ObjectField)),
            ("Create Cubemap", typeof(ObjectField)),
            ("Create SamplerStateData", null),
        };

        [UnityTest]
        public IEnumerator TestPropertyLoadsWithCorrectFieldType(
            [ValueSource(nameof(ExpectedFieldTypes))] (string, Type) testCase
        )
        {
            var (createItemName, fieldType) = testCase;

            void ValidateCreatedField()
            {
                var view = GetFirstBlackboardElementOfType<GraphDataBlackboardVariablePropertyView>();
                Assert.IsNotNull(view, "View for created property was not found");

                var field = view.Q<BaseModelPropertyField>(className: "ge-inline-value-editor");
                if (fieldType is null)
                {
                    Assert.IsNull(field, "Created blackboard item should not have an Initialization field");
                }
                else
                {
                    Assert.IsNotNull(field, "Created blackboard item should have an Initialization field");
                    var firstChild = field.Children().First();
                    Assert.IsTrue(firstChild.GetType().IsAssignableFrom(fieldType), $"Property created with \"{createItemName}\" should have field of type {fieldType.Name}");
                }
            }

            {
                var stencil = (ShaderGraphStencil)GraphModel.Stencil;

                var createMenu = new List<Stencil.MenuItem>();
                stencil.PopulateBlackboardCreateMenu("Properties", createMenu, m_BlackboardView);

                var floatItem = createMenu.FirstOrDefault(i => i.name == createItemName);
                Assert.IsNotNull(floatItem, $"\"{createItemName}\" item from test case was not found in Blackboard create menu. Are the test cases up-to-date?");

                floatItem.action.Invoke();
                yield return null;

                ValidateCreatedField();
            }

            yield return SaveAndReopenGraph();

            {
                m_BlackboardView = FindBlackboardView(m_Window);
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

            // Mimic drag-and-drop interaction by the user from blackboard item to center of the graph
            var graphViewCenterPosition = TestEventHelpers.GetScreenPosition(m_Window, m_GraphView, true);
            var command = new CreateNodeCommand();
            var variable = GraphModel.VariableDeclarations.FirstOrDefault();
            command.WithNodeOnGraph(variable, graphViewCenterPosition);
            m_GraphView.Dispatch(command);

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

        // Disabled for now, need to refactor preview tests to be async-ified
        /*[UnityTest]
        public IEnumerator TestItemValueChangeAffectsPreview()
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

            var nodePreviewMaterial = m_Window.previewUpdateDispatcher.(graphDataVariable.graphDataName);
            Assert.IsNotNull(nodePreviewMaterial);
            Assert.AreEqual(Color.red, SampleMaterialColor(nodePreviewMaterial));
        }*/

        // TODO: (Sai) Make these generalized to keywords also when those come in
        [UnityTest]
        public IEnumerator TestAllPropertyTypesCanBeCreated()
        {
            var stencil = (ShaderGraphStencil)m_GraphView.GraphModel.Stencil;

            var createMenu = new List<Stencil.MenuItem>();
            stencil.PopulateBlackboardCreateMenu("Properties", createMenu, m_BlackboardView);

            foreach (var menuItem in createMenu)
            {
                menuItem.action.Invoke();
                yield return null;
            }
        }

        // TODO: (Sai) Make these generalized to keywords also when those come in
        [UnityTest]
        public IEnumerator TestAllPropertyTypesCanBeDuplicated()
        {
            var stencil = (ShaderGraphStencil)m_GraphView.GraphModel.Stencil;

            var createMenu = new List<Stencil.MenuItem>();
            stencil.PopulateBlackboardCreateMenu("Properties", createMenu, m_BlackboardView);

            foreach (var menuItem in createMenu)
            {
                menuItem.action.Invoke();
                yield return null;
            }

            // We want a copy of the original blackboard items
            var originalItems = GraphModel.VariableDeclarations.ToList();
            for(var index = 0; index < originalItems.Count; index++)
            {
                var declarationModel = originalItems[index];
                m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, declarationModel));
                yield return null;

                Assert.IsTrue(m_TestEventHelper.SendDuplicateCommand());
                yield return null;

                // Check to make sure the duplication worked and there are now two of the type of the copied item
                Assert.IsTrue(GraphModel.VariableDeclarations.Count(model => model.DataType == declarationModel.DataType) == 2);
            }
        }

        // TODO: (Sai) Make these generalized to keywords also when those come in
        [UnityTest]
        public IEnumerator TestAllPropertyTypesCanBeCutPasted()
        {
            var stencil = (ShaderGraphStencil)m_GraphView.GraphModel.Stencil;

            var createMenu = new List<Stencil.MenuItem>();
            stencil.PopulateBlackboardCreateMenu("Properties", createMenu, m_BlackboardView);

            foreach (var menuItem in createMenu)
            {
                menuItem.action.Invoke();
                yield return null;
                break;
            }

            // We want a copy of the original blackboard items
            var originalItems = GraphModel.VariableDeclarations.ToList();
            for(var index = 0; index < originalItems.Count; index++)
            {
                var originalVariable = originalItems[index];

                m_TestEventHelper.SendMouseDownEvent(m_BlackboardView);
                m_TestEventHelper.SendMouseUpEvent(m_BlackboardView);
                yield return null;

                m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, originalVariable));
                yield return null;

                Assert.IsTrue(m_TestEventHelper.SendCutCommand());
                yield return null;
                yield return null;
                yield return null;
                yield return null;

                // TODO: The ShaderGraphViewSelection is responding to cut command currently for some reason, need to get SGBlackboardSelection to do that instead
                // Check to make sure the cut worked and the original item was deleted
                Assert.IsTrue(GraphModel.VariableDeclarations.FirstOrDefault(model => Equals(model, originalVariable)) == null);

                Assert.IsTrue(m_TestEventHelper.SendPasteCommand());
                yield return null;
                yield return null;
                yield return null;
                yield return null;

                // Check to make sure the paste worked and there is only one of the copied item
                Assert.IsTrue(GraphModel.VariableDeclarations.Count(model => model.DataType == originalVariable.DataType) == 1);

            }
        }

        // TODO: (Sai) Make these generalized to keywords also when those come in
        [UnityTest]
        public IEnumerator TestAllPropertyTypesCanBeCopiedBetweenGraphs()
        {
            // Wait till first graph is loaded
            while (GraphModel == null)
                yield return null;

            var secondGraphPath = CreateSecondGraph(out ShaderGraphAsset secondGraphAsset, out var secondEditorWindow, out var secondWindowTestHelper);

            // Wait till second graph is loaded as well
            while (secondGraphAsset.GraphModel == null)
                yield return null;

            // Go back to first graph
            m_Window.Show();
            m_Window.Focus();

            var stencil = (ShaderGraphStencil)m_GraphView.GraphModel.Stencil;

            var createMenu = new List<Stencil.MenuItem>();
            stencil.PopulateBlackboardCreateMenu("Properties", createMenu, m_BlackboardView);

            foreach (var menuItem in createMenu)
            {
                menuItem.action.Invoke();
                yield return null;
            }

            // We want a copy of the original blackboard items
            var originalItems = GraphModel.VariableDeclarations.ToList();
            for(var index = 0; index < originalItems.Count; index++)
            {
                // Switch back to first graph
                m_Window.Show();
                m_Window.Focus();
                yield return null;

                // Select item
                m_TestEventHelper.SendMouseDownEvent(m_BlackboardView);
                m_TestEventHelper.SendMouseUpEvent(m_BlackboardView);

                var declarationModel = originalItems[index];
                m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, declarationModel));
                yield return null;

                // Copy Item from first graph
                Assert.IsTrue(m_TestEventHelper.SendCopyCommand());
                yield return null;

                // Switch to second graph
                secondEditorWindow.Show();
                secondEditorWindow.Focus();
                yield return null;

                var secondBlackboardView = FindBlackboardView(secondEditorWindow);
                secondWindowTestHelper.SendMouseDownEvent(secondBlackboardView);
                secondWindowTestHelper.SendMouseUpEvent(secondBlackboardView);

                // Paste in second graph
                Assert.IsTrue(secondWindowTestHelper.SendPasteCommand());
                yield return null;

                // Check to make sure the copy worked and there is one of the copied type in the target graph
                Assert.IsTrue(secondGraphAsset.GraphModel.VariableDeclarations.Count(model => model.DataType == declarationModel.DataType) == 1);
            }

            // Close second window
            secondEditorWindow.Close();
            // Remove second graph asset
            AssetDatabase.DeleteAsset(secondGraphPath);
        }

        // TODO: (Sai) Make these generalized to keywords also when those come in
        /*[UnityTest]
        public IEnumerator TestAllPropertyTypesCanBeCutBetweenGraphs()
        {
            // Wait till first graph is loaded
            while (GraphModel == null)
                yield return null;

            var secondGraphPath = CreateSecondGraph(out ShaderGraphAsset secondGraphAsset, out var secondEditorWindow, out var secondWindowTestHelper);

            // Wait till second graph is loaded
            while (secondGraphAsset.GraphModel == null)
                yield return null;

            // Go back to first graph
            m_Window.Show();
            m_Window.Focus();

            var stencil = (ShaderGraphStencil)m_GraphView.GraphModel.Stencil;

            var createMenu = new List<Stencil.MenuItem>();
            stencil.PopulateBlackboardCreateMenu("Properties", createMenu, m_BlackboardView);

            foreach (var menuItem in createMenu)
            {
                menuItem.action.Invoke();
                yield return null;
            }

            // We want a copy of the original blackboard items
            var originalItems = GraphModel.VariableDeclarations.ToList();
            for(var index = 0; index < originalItems.Count; index++)
            {
                // Switch back to first graph
                m_Window.Show();
                m_Window.Focus();
                yield return null;

                // Select item
                m_TestEventHelper.SendMouseDownEvent(m_BlackboardView);
                m_TestEventHelper.SendMouseUpEvent(m_BlackboardView);

                var declarationModel = originalItems[index];
                m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, declarationModel));
                yield return null;

                // Cut Item from first graph
                Assert.IsTrue(m_TestEventHelper.SendCutCommand());
                yield return null;

                // Switch to second graph
                secondEditorWindow.Show();
                secondEditorWindow.Focus();
                yield return null;

                secondWindowTestHelper.SendMouseDownEvent(secondEditorWindow.blackboardView);
                secondWindowTestHelper.SendMouseUpEvent(secondEditorWindow.blackboardView);

                // Paste in second graph
                Assert.IsTrue(secondWindowTestHelper.SendPasteCommand());
                yield return null;

                // Check to make sure the copy worked and there is one of the copied type in the target graph
                Assert.IsTrue(secondGraphAsset.GraphModel.VariableDeclarations.Count(model => model.DataType == declarationModel.DataType) == 1);
            }

            // Close second window
            secondEditorWindow.Close();
            // Remove second graph asset
            AssetDatabase.DeleteAsset(secondGraphPath);
        }*/
    }
}
