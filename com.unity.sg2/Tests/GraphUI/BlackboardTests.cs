using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.GraphToolsFoundation;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class BlackboardTests : BaseGraphWindowTest
    {
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

        static readonly (string, Type)[] k_ExpectedFieldTypes =
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
            ("Create Texture2D", typeof(ObjectField)),
            ("Create Texture2DArray", typeof(ObjectField)),
            ("Create Texture3D", typeof(ObjectField)),
            ("Create Cubemap", typeof(ObjectField)),
            ("Create SamplerStateData", null),
        };

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
        public IEnumerator TestPropertyLoadsWithCorrectFieldType(
            [ValueSource(nameof(k_ExpectedFieldTypes))] (string, Type) testCase
        )
        {
            var (createItemName, fieldType) = testCase;

            void ValidateCreatedField()
            {
                var view = GetFirstBlackboardElementOfType<SGBlackboardVariablePropertyView>();
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
                m_BlackboardView = FindBlackboardView(m_MainWindow);
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
            yield return TestVariableNodeCanBeAdded();

            var variableNodeModel = m_MainWindow.GetNodeModelFromGraphByName("Vector 4") as SGVariableNodeModel;
            Assert.IsNotNull(variableNodeModel);

            yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");

            m_TestInteractionHelper.ConnectNodes("Vector 4", "Add", "Output", "A");

            m_GraphView.Dispatch(new UpdateConstantValueCommand(
                variableNodeModel.VariableDeclarationModel.InitializationModel,
                new Vector4(1, 0, 0, 0),
                variableNodeModel.VariableDeclarationModel));

            yield return null;

            var addNodeModel = m_MainWindow.GetNodeModelFromGraphByName("Add") as SGNodeModel;
            Assert.IsNotNull(addNodeModel);

            var status = m_MainWindow.previewUpdateDispatcher.PreviewService.RequestNodePreviewTexture(addNodeModel.graphDataName, out var texture, out _);
            var maxRetry = 60;
            while (maxRetry > 0 && status != PreviewService.PreviewOutputState.Complete)
            {
                maxRetry--;
                yield return null;
            }

            Assert.AreEqual(PreviewService.PreviewOutputState.Complete, status, "Could not get preview quickly enough.");

            yield return null;

            Texture2D output = new(1, 1, TextureFormat.ARGB32, false);
            var color = Color.black;
            var rt = texture as RenderTexture;
            while (maxRetry > 0 && color != Color.red)
            {
                var prevActive = RenderTexture.active;
                RenderTexture.active = rt;
                output.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
                RenderTexture.active = prevActive;
                color = output.GetPixel(0, 0);
                maxRetry--;
                yield return null;
            }

            Assert.AreEqual(Color.red, color);
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
                var s = new SamplerStateData {
                    filter = SamplerStateType.Filter.Linear,
                    wrap = SamplerStateType.Wrap.Repeat,
                    depthCompare = true,
                    aniso = SamplerStateType.Aniso.Aniso2 };

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
            for(var index = 0; index < originalItems.Count; index++)
            {
                var declarationModel = originalItems[index];

                SetVariableValue(declarationModel);
                var originalValue = declarationModel.InitializationModel.ObjectValue;

                m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, declarationModel));
                yield return null;

                m_BlackboardView.Focus();
                m_TestEventHelper.SendDuplicateCommand();
                yield return null;

                // Check to make sure the duplication worked and there are now two of the type of the copied item
                Assert.IsTrue(GraphModel.VariableDeclarations.Count(model => model.DataType == declarationModel.DataType) == 2);

                // Check that values are preserved.
                var copied = GraphModel.VariableDeclarations.First(model => model.Guid != declarationModel.Guid && model.DataType == declarationModel.DataType);
                Assert.AreEqual(originalValue, copied.InitializationModel.ObjectValue);
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
            for(var index = 0; index < originalItems.Count; index++)
            {
                var originalVariable = originalItems[index];

                SetVariableValue(originalVariable);
                var originalValue = originalVariable.InitializationModel.ObjectValue;

                m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, originalVariable));
                yield return null;

                m_BlackboardView.Focus();
                m_TestEventHelper.SendCutCommand();
                yield return null;

                // Check to make sure the cut worked and the original item was deleted
                Assert.IsTrue(GraphModel.VariableDeclarations.FirstOrDefault(model => Equals(model, originalVariable)) == null,
                    $"Variable of type {originalVariable.DataType} was not cut.");

                m_BlackboardView.Focus();
                m_TestEventHelper.SendPasteCommand();
                yield return null;

                // Check to make sure the paste worked and there is only one of the copied item
                var count = GraphModel.VariableDeclarations.Count(model => model.DataType == originalVariable.DataType);
                Assert.IsFalse(count == 0, $"Variable of type {originalVariable.DataType} was not pasted.");
                Assert.IsTrue(count == 1, $"Variable of type {originalVariable.DataType} is present multiple times.");

                // Check that values are preserved.
                var copied = GraphModel.VariableDeclarations.First(model => model.DataType == originalVariable.DataType);
                Assert.AreEqual(originalValue, copied.InitializationModel.ObjectValue);
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

                var declarationModel = originalItems[index];

                SetVariableValue(declarationModel);
                var originalValue = declarationModel.InitializationModel.ObjectValue;

                m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, declarationModel));
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
                Assert.IsTrue(secondGraphAsset.GraphModel.VariableDeclarations.Count(model => model.DataType == declarationModel.DataType) == 1);

                // Check that values are preserved.
                var copied = secondGraphAsset.GraphModel.VariableDeclarations.First(model => model.DataType == declarationModel.DataType);
                Assert.AreEqual(originalValue, copied.InitializationModel.ObjectValue);
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
            for(var index = 0; index < originalItems.Count; index++)
            {
                // Switch back to first graph
                m_MainWindow.Show();
                m_MainWindow.Focus();
                yield return null;

                var declarationModel = originalItems[index];

                SetVariableValue(declarationModel);
                var originalValue = declarationModel.InitializationModel.ObjectValue;

                m_BlackboardView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, declarationModel));
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
                Assert.IsTrue(secondGraphAsset.GraphModel.VariableDeclarations.Count(model => model.DataType == declarationModel.DataType) == 1);

                // Check that values are preserved.
                var copied = secondGraphAsset.GraphModel.VariableDeclarations.First(model => model.DataType == declarationModel.DataType);
                Assert.AreEqual(originalValue, copied.InitializationModel.ObjectValue);
            }
        }
    }
}
