using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.GraphToolsFoundation;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class BlackboardTests : BlackboardTestsBase
    {
        protected override GraphInstantiation GraphToInstantiate => GraphInstantiation.Memory;

        [Test]
        public void ExpectedFieldTypesIsUpToDate()
        {
            var stencil = (ShaderGraphStencil)GraphModel.Stencil;
            var createMenu = new List<Stencil.MenuItem>();
            stencil.PopulateBlackboardCreateMenu("Properties", createMenu, m_BlackboardView);

            var expected = new HashSet<string>(k_ExpectedFieldTypes.Select(e => e.Item1));
            var menu = new HashSet<string>(createMenu.Select(e => e.name));
            expected.SymmetricExceptWith(menu);

            foreach (var s in expected)
            {
                Debug.Log($"{s} is only in one set.");
            }

            Assert.AreEqual(0, expected.Count);
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
            var graphViewCenterPosition = TestEventHelpers.GetScreenPosition(m_MainWindow, m_GraphView, true);
            var command = new CreateNodeCommand();
            var variable = GraphModel.VariableDeclarations.FirstOrDefault();
            command.WithNodeOnGraph(variable, graphViewCenterPosition);
            m_GraphView.Dispatch(command);

            yield return null;

            Assert.IsNotNull(m_MainWindow.GetNodeModelFromGraphByName("Vector 4"));
        }

        [UnityTest]
        public IEnumerator TestVariableNodeCanBeDeleted()
        {
            yield return TestVariableNodeCanBeAdded();

            var variableNodeModel = m_MainWindow.GetNodeModelFromGraphByName("Vector 4");

            m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, variableNodeModel));
            yield return null;

            m_GraphView.Focus();
            Assert.IsTrue(m_TestEventHelper.SendDeleteCommand());
            yield return null;

            var vector4Node = m_MainWindow.GetNodeModelFromGraphByName("Vector 4");
            Assert.IsNull(vector4Node, "Node should be null after delete operation");

            var graphDataVariableNodeModel = variableNodeModel as SGVariableNodeModel;
            var variableNodeHandler = GraphModel.GraphHandler.GetNode(graphDataVariableNodeModel.graphDataName);
            Assert.IsNull(variableNodeHandler, "Node should also be removed from CLDS after delete operation");
        }

        [UnityTest]
        public IEnumerator TestVariableNodeCanBeDuplicated()
        {
            yield return TestVariableNodeCanBeAdded();

            var variableNodeModel = m_MainWindow.GetNodeModelFromGraphByName("Vector 4");
            Assert.IsNotNull(variableNodeModel);

            m_GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, variableNodeModel));
            yield return null;

            m_GraphView.Focus();
            Assert.IsTrue(m_TestEventHelper.SendDuplicateCommand());
            yield return null;

            Assert.IsTrue(m_MainWindow.GetNodeModelsFromGraphByName("Vector 4").Count == 2, "Should be two variable nodes after copy");
        }

        [UnityTest, Explicit("Time dependent test, which often means this will be an unstable test.")]
        public IEnumerator TestVariableValueChangeAffectsPreview()
        {
            var graphViewCenterPosition = TestEventHelpers.GetScreenPosition(m_MainWindow, m_GraphView, true);

            var group = GraphModel.GetSectionModel("Properties");
            var vdm = GraphModel.CreateGraphVariableDeclaration(typeof(SGVariableDeclarationModel), TypeHandle.Vector4, "Vector 4", ModifierFlags.None, true, group);
            var variableNodeModel = GraphModel.CreateVariableNode(vdm, Vector2.zero);
            Assert.IsNotNull(variableNodeModel);

            var addNodeModel = SGGraphTestUtils.CreateNodeByName(GraphModel, "Add", graphViewCenterPosition - new Vector2(100, 100));
            Assert.IsNotNull(addNodeModel);
            yield return null;

            var nodeGraphElement = m_GraphView.GetGraphElement(addNodeModel);
            Assert.IsNotNull(nodeGraphElement);

            m_TestInteractionHelper.ConnectNodes("Vector 4", "Add", "Output", "A");

            m_GraphView.Dispatch(new UpdateConstantValueCommand(
                variableNodeModel.VariableDeclarationModel.InitializationModel,
                new Vector4(1, 0, 0, 0),
                variableNodeModel.VariableDeclarationModel));

            yield return null;

            Texture2D output = new(1, 1, TextureFormat.ARGB32, false);
            var maxRetry = 600;
            var color = Color.black;
            var status = PreviewService.PreviewOutputState.Updating;
            while (maxRetry > 0 && color != Color.red)
            {
                status = m_MainWindow.previewUpdateDispatcher.PreviewService.RequestNodePreviewTexture(addNodeModel.graphDataName, out var texture, out _);

                var rt = texture as RenderTexture;
                var prevActive = RenderTexture.active;
                RenderTexture.active = rt;
                output.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
                RenderTexture.active = prevActive;
                color = output.GetPixel(0, 0);

                maxRetry--;
                yield return null;
            }

            Assert.AreEqual(Color.red, color);
            Assert.AreEqual(PreviewService.PreviewOutputState.Complete, status);
        }

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


        static void SetVariableValue(VariableDeclarationModel originalVariable)
        {
            if (originalVariable.DataType == TypeHandle.Int)
            {
                originalVariable.InitializationModel.ObjectValue = 42;
            }
            else if (originalVariable.DataType == TypeHandle.Float)
            {
                originalVariable.InitializationModel.ObjectValue = 42.0f;
            }
            else if (originalVariable.DataType == TypeHandle.Bool)
            {
                originalVariable.InitializationModel.ObjectValue = true;
            }
            else if (originalVariable.DataType == TypeHandle.Vector2)
            {
                originalVariable.InitializationModel.ObjectValue = new Vector2(42f, 42f);
            }
            else if (originalVariable.DataType == TypeHandle.Vector3)
            {
                originalVariable.InitializationModel.ObjectValue = new Vector3(42f, 42f, 42f);
            }
            else if (originalVariable.DataType == TypeHandle.Vector4)
            {
                originalVariable.InitializationModel.ObjectValue = new Vector4(42f, 42f, 42f, 42f);
            }
            else if (originalVariable.DataType == ShaderGraphExampleTypes.Color)
            {
                originalVariable.InitializationModel.ObjectValue = new Vector4(42f, 42f, 42f, 42f);
            }
            else if (originalVariable.DataType == ShaderGraphExampleTypes.Matrix2 ||
                     originalVariable.DataType == ShaderGraphExampleTypes.Matrix3 ||
                     originalVariable.DataType == ShaderGraphExampleTypes.Matrix4)
            {
                var m = new Matrix4x4 { [0] = 42f };
                originalVariable.InitializationModel.ObjectValue = m;
            }
            else if (originalVariable.DataType == ShaderGraphExampleTypes.Texture2DTypeHandle)
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/BlackboardTests/2dtexture.png");
                Assert.IsNotNull(t);
                originalVariable.InitializationModel.ObjectValue = t;
            }
            else if (originalVariable.DataType == ShaderGraphExampleTypes.Texture3DTypeHandle)
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture3D>("Assets/BlackboardTests/3dtexture.png");
                Assert.IsNotNull(t);
                originalVariable.InitializationModel.ObjectValue = t;
            }
            else if (originalVariable.DataType == ShaderGraphExampleTypes.Texture2DArrayTypeHandle)
            {
                var t = AssetDatabase.LoadAssetAtPath<Texture2DArray>("Assets/BlackboardTests/2darray.png");
                Assert.IsNotNull(t);
                originalVariable.InitializationModel.ObjectValue = t;
            }
            else if (originalVariable.DataType == ShaderGraphExampleTypes.CubemapTypeHandle)
            {
                var t = AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/BlackboardTests/cubemap.tga");
                Assert.IsNotNull(t);
                originalVariable.InitializationModel.ObjectValue = t;
            }
            else if (originalVariable.DataType == ShaderGraphExampleTypes.SamplerStateTypeHandle)
            {
                var s = new SamplerStateData { filter = SamplerStateType.Filter.Linear, wrap = SamplerStateType.Wrap.Repeat, depthCompare = true, aniso = SamplerStateType.Aniso.Aniso2 };

                originalVariable.InitializationModel.ObjectValue = s;
            }
            else
            {
                Assert.IsTrue(false, $"Unexpected variable type: {originalVariable.DataType}.");
            }
        }

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
            for (var index = 0; index < originalItems.Count; index++)
            {
                var originalVariable = originalItems[index] as SGVariableDeclarationModel;

                var originalGuid = originalVariable.Guid;
                var originalDataType = originalVariable.DataType;

                var originalIsExposed = originalVariable.IsExposable && index % 2 == 0;
                originalVariable.IsExposed = originalIsExposed;
                Assert.AreEqual(originalIsExposed, originalVariable.IsExposed, $"Failed to set IsExposed field for {originalDataType}.");

                SetVariableValue(originalVariable);
                var originalValue = originalVariable.InitializationModel.ObjectValue;

                m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, originalVariable));
                yield return null;

                m_BlackboardView.Focus();
                m_TestEventHelper.SendDuplicateCommand();
                yield return null;

                // Check to make sure the duplication worked and there are now two of the type of the copied item
                Assert.IsTrue(GraphModel.VariableDeclarations.Count(model => model.DataType == originalDataType) == 2);

                // Check that values are preserved.
                var copied = GraphModel.VariableDeclarations.First(model => model.Guid != originalGuid && model.DataType == originalDataType);
                Assert.AreEqual(originalValue, copied.InitializationModel.ObjectValue);

                // Check that IsExposed is preserved.
                Assert.AreEqual(originalIsExposed, copied.IsExposed, "Failed to copy IsExposed field.");
            }
        }

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
            }

            // We want a copy of the original blackboard items
            var originalItems = GraphModel.VariableDeclarations.ToList();
            for (var index = 0; index < originalItems.Count; index++)
            {
                var originalVariable = originalItems[index] as SGVariableDeclarationModel;

                var originalDataType = originalVariable.DataType;

                var originalIsExposed = originalVariable.IsExposable && index % 2 == 0;
                originalVariable.IsExposed = originalIsExposed;
                Assert.AreEqual(originalIsExposed, originalVariable.IsExposed, $"Failed to set IsExposed field for {originalDataType}.");

                SetVariableValue(originalVariable);
                var originalValue = originalVariable.InitializationModel.ObjectValue;

                m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, originalVariable));
                yield return null;

                m_BlackboardView.Focus();
                m_TestEventHelper.SendCutCommand();
                yield return null;

                // Check to make sure the cut worked and the original item was deleted
                Assert.IsTrue(GraphModel.VariableDeclarations.FirstOrDefault(model => Equals(model, originalVariable)) == null,
                    $"Variable of type {originalDataType} was not cut.");

                m_BlackboardView.Focus();
                m_TestEventHelper.SendPasteCommand();
                yield return null;

                // Check to make sure the paste worked and there is only one of the copied item
                var count = GraphModel.VariableDeclarations.Count(model => model.DataType == originalDataType);
                Assert.IsFalse(count == 0, $"Variable of type {originalDataType} was not pasted.");
                Assert.IsTrue(count == 1, $"Variable of type {originalDataType} is present multiple times.");

                // Check that values are preserved.
                var copied = GraphModel.VariableDeclarations.First(model => model.DataType == originalDataType);
                Assert.AreEqual(originalValue, copied.InitializationModel.ObjectValue);

                // Check that IsExposed is preserved.
                Assert.AreEqual(originalIsExposed, copied.IsExposed, "Failed to copy IsExposed field.");
            }
        }

        [UnityTest]
        public IEnumerator TestAllPropertyTypesCanBeCopiedBetweenGraphs()
        {
            // Wait till first graph is loaded
            while (GraphModel == null)
                yield return null;

            CreateGraphInNewWindow(out var secondGraphAsset, out var secondEditorWindow, out var secondWindowTestHelper);

            // Wait till second graph is loaded as well
            while (secondGraphAsset.GraphModel == null)
                yield return null;

            // Go back to first graph
            m_MainWindow.Show();
            m_MainWindow.Focus();

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
            for (var index = 0; index < originalItems.Count; index++)
            {
                // Switch back to first graph
                m_MainWindow.Show();
                m_MainWindow.Focus();
                yield return null;

                var originalVariable = originalItems[index] as SGVariableDeclarationModel;

                var originalDataType = originalVariable.DataType;

                var originalIsExposed = originalVariable.IsExposable && index % 2 == 0;
                originalVariable.IsExposed = originalIsExposed;
                Assert.AreEqual(originalIsExposed, originalVariable.IsExposed, $"Failed to set IsExposed field for {originalDataType}.");

                SetVariableValue(originalVariable);
                var originalValue = originalVariable.InitializationModel.ObjectValue;

                m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, originalVariable));
                yield return null;

                // Copy Item from first graph
                m_BlackboardView.Focus();
                Assert.IsTrue(m_TestEventHelper.SendCopyCommand());
                yield return null;

                // Switch to second graph
                secondEditorWindow.Show();
                secondEditorWindow.Focus();
                yield return null;

                var secondBlackboardView = FindBlackboardView(secondEditorWindow);

                // Paste in second graph
                secondBlackboardView.Focus();
                Assert.IsTrue(secondWindowTestHelper.SendPasteCommand());
                yield return null;

                // Check to make sure the copy worked and there is one of the copied type in the target graph
                Assert.IsTrue(secondGraphAsset.GraphModel.VariableDeclarations.Count(model => model.DataType == originalDataType) == 1);

                // Check that values are preserved.
                var copied = secondGraphAsset.GraphModel.VariableDeclarations.First(model => model.DataType == originalDataType);
                Assert.AreEqual(originalValue, copied.InitializationModel.ObjectValue);

                // Check that IsExposed is preserved.
                Assert.AreEqual(originalIsExposed, copied.IsExposed, "Failed to copy IsExposed field.");
            }
        }

        // TODO: (Sai) Make these generalized to keywords also when those come in
        [UnityTest]
        public IEnumerator TestAllPropertyTypesCanBeCutBetweenGraphs()
        {
            // Wait till first graph is loaded
            while (GraphModel == null)
                yield return null;

            CreateGraphInNewWindow(out var secondGraphAsset, out var secondEditorWindow, out var secondWindowTestHelper);
            var secondBlackboardView = FindBlackboardView(secondEditorWindow);

            // Wait till second graph is loaded
            while (secondGraphAsset.GraphModel == null)
                yield return null;

            // Go back to first graph
            m_MainWindow.Show();
            m_MainWindow.Focus();

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
            for (var index = 0; index < originalItems.Count; index++)
            {
                // Switch back to first graph
                m_MainWindow.Show();
                m_MainWindow.Focus();
                yield return null;

                var originalVariable = originalItems[index] as SGVariableDeclarationModel;

                var originalDataType = originalVariable.DataType;

                var originalIsExposed = originalVariable.IsExposable && index % 2 == 0;
                originalVariable.IsExposed = originalIsExposed;
                Assert.AreEqual(originalIsExposed, originalVariable.IsExposed, $"Failed to set IsExposed field for {originalDataType}.");

                SetVariableValue(originalVariable);
                var originalValue = originalVariable.InitializationModel.ObjectValue;

                m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, originalVariable));
                yield return null;

                // Cut Item from first graph
                m_BlackboardView.Focus();
                Assert.IsTrue(m_TestEventHelper.SendCutCommand());
                yield return null;

                // Switch to second graph
                secondEditorWindow.Show();
                secondEditorWindow.Focus();
                yield return null;

                // Paste in second graph
                secondBlackboardView.Focus();
                Assert.IsTrue(secondWindowTestHelper.SendPasteCommand());
                yield return null;

                // Check to make sure the copy worked and there is one of the copied type in the target graph
                Assert.IsTrue(secondGraphAsset.GraphModel.VariableDeclarations.Count(model => model.DataType == originalDataType) == 1);

                // Check that values are preserved.
                var copied = secondGraphAsset.GraphModel.VariableDeclarations.First(model => model.DataType == originalDataType);
                Assert.AreEqual(originalValue, copied.InitializationModel.ObjectValue);

                // Check that IsExposed is preserved.
                Assert.AreEqual(originalIsExposed, copied.IsExposed, "Failed to copy IsExposed field.");
            }
        }
    }
}
