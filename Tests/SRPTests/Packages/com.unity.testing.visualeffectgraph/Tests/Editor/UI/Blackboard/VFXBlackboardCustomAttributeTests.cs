using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.UI.Blackboard
{
    [TestFixture]
    public class VFXBlackboardCustomAttributeTests
    {
        private const string testFolder = "TmpTests";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            AssetDatabase.DeleteAsset("Assets/" + testFolder);
            AssetDatabase.CreateFolder("Assets", testFolder);
            VFXTestCommon.CloseAllVFXWindow();
        }

        [OneTimeTearDown]
        public void OneTimeCleanup()
        {
            VFXTestCommon.CloseAllVFXWindow();
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [UnityTest]
        public IEnumerator Add_Custom_Attribute()
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

            // Act
            AddCustomAttribute(blackboard, VFXValueType.Boolean);
            yield return null;

            // Assert
            var enumerator = VFXBlackboardTestHelper.IsOpenForEdit(blackboard, "textField", "CustomAttribute");
            while (enumerator.MoveNext())
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Remove_CustomAttribute()
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

            AddCustomAttribute(blackboard, VFXValueType.Boolean);
            yield return null;

            var attributeField = blackboard.Query<VFXBlackboardAttributeField>().Where(x => x.attribute.title == "CustomAttribute").First();
            Assert.NotNull(attributeField);
            Assert.AreEqual(1, graph.customAttributes.Count());

            // Act
            window.graphView.AddToSelection(attributeField);
            window.graphView.Delete();
            yield return null;

            // Assert
            attributeField = blackboard.Query<VFXBlackboardAttributeField>().Where(x => x.attribute.title == "CustomAttribute").First();
            Assert.Null(attributeField);
            Assert.IsEmpty(graph.customAttributes);
        }

        [UnityTest]
        public IEnumerator DoubleClick_To_Rename_Attribute()
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

            AddCustomAttribute(blackboard, VFXValueType.Boolean);
            yield return null;

            // Remove focus from newly created attribute so that it's not open for edit
            blackboard.Focus();
            yield return null;

            // Act
            var blackboardAttributeField = blackboard.Q<VFXBlackboardAttributeField>();
            VFXGUITestHelper.SendDoubleClick(blackboardAttributeField, 2);

            // Assert
            var enumerator = VFXBlackboardTestHelper.IsOpenForEdit(blackboard, "textField", "CustomAttribute");
            while (enumerator.MoveNext())
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Subgraph_Custom_Attribute_Rename()
        {
            // Create a subgraph with a custom attribute
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
            var subgraphPath = $"Assets/{testFolder}/vfx_{GUID.Generate()}.vfxoperator";
            var subgraph = VisualEffectAssetEditorUtility.CreateNew<VisualEffectSubgraphOperator>(subgraphPath);
            var window = VFXTestCommon.GetWindow(subgraph.GetResource(), true);
            window.LoadResource(subgraph.GetResource());
            yield return null;

            var blackboard = window.graphView.blackboard;
            AddCustomAttribute(blackboard, VFXValueType.Boolean);
            var customAttributeRow = (VFXBlackboardAttributeRow)null;
            var frames = 32;
            while (frames-- > 0 && customAttributeRow == null)
            {
                yield return null;
                customAttributeRow = blackboard.Query<VFXBlackboardAttributeRow>().First();
            }
            window.graphView.OnSave();
            yield return null;


            // Open a second window with a basic graph
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var window2 = VFXTestCommon.GetWindow(graph, true);
            window2.LoadResource(graph.visualEffectResource);
            var blackboard2 = window.graphView.blackboard;
            yield return null;

            var modelDescriptor = VFXLibrary.GetOperators().Single(x => x.modelType == typeof(VFXSubgraphOperator));
            var subgraphOperator = window2.graphView.controller.AddVFXOperator(Vector2.zero, modelDescriptor.variant);
            subgraphOperator.SetSettingValue("m_Subgraph", subgraph);
            window2.graphView.controller.ApplyChanges();
            window2.graphView.OnSave();
            yield return null;
            window2.graphView.FrameAll();

            // Act
            var newCustomAttributeName = "Toto";
            window.graphView.controller.graph.TryRenameCustomAttribute(customAttributeRow.attribute.title, newCustomAttributeName);
            window.graphView.OnSave();
            yield return null;

            // Assert
            var customAttribute = window2.graphView.controller.graph.customAttributes.Single();
            Assert.AreEqual(customAttribute.attributeName, newCustomAttributeName);
        }

        [UnityTest]
        public IEnumerator Rename_Custom_Attribute_Update_Nodes_Using_It()
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
            AddCustomAttribute(blackboard, VFXValueType.Boolean);
            yield return null;
            var updateContext = (VFXBasicUpdate)VFXLibrary.GetContexts().Single(x => x.model is VFXBasicUpdate).CreateInstance();
            graph.AddChild(updateContext);
            var setAttributeBlock = (SetAttribute)VFXLibrary.GetBlocks().First(x => x.model is SetAttribute).CreateInstance();
            updateContext.AddChild(setAttributeBlock);
            var customAttribute = graph.attributesManager.GetCustomAttributes().First();
            setAttributeBlock.SetSettingValue("attribute", customAttribute.name);
            window.Focus();
            yield return null;

            // Act
            var blackboardAttribute = blackboard.Query<VFXBlackboardAttributeField>().First();
            VFXGUITestHelper.SendDoubleClick(blackboardAttribute, 2);
            yield return null;
            var newCustomAttributeName = "Toto";
            var enumerator = VFXGUITestHelper.SendKeyDown(blackboardAttribute, newCustomAttributeName);
            while (enumerator.MoveNext())
            {
                yield return null;
            }
            VFXGUITestHelper.SendKeyDown(blackboardAttribute, KeyCode.Return);

            // Assert
            Assert.AreEqual(newCustomAttributeName, setAttributeBlock.attribute);
        }

        [UnityTest]
        public IEnumerator Rename_Custom_Attribute_With_Different_Case_Only()
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
            AddCustomAttribute(blackboard, VFXValueType.Boolean);
            yield return null;

            var customAttributeDescriptor = graph.customAttributes.Single();
            var initialName = customAttributeDescriptor.attributeName;
            var newName = initialName.ToUpperInvariant();
            Assert.AreNotEqual(initialName, newName);

            // Act (this is what the inspector's name field does)
            var hasChanged = customAttributeDescriptor.Changed(initialName, newName, customAttributeDescriptor.type, customAttributeDescriptor.description);
            yield return null;

            // Assert
            Assert.IsTrue(hasChanged);
            Assert.AreEqual(newName, customAttributeDescriptor.attributeName);
            Assert.IsTrue(graph.attributesManager.TryFind(newName, out var attribute));
            Assert.AreEqual(newName, attribute.name);
        }

        [UnityTest]
        public IEnumerator Add_And_Remove_SubgraphBlock_With_CustomAttribute()
        {
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var window = VFXTestCommon.GetWindow(graph, true);
            window.LoadResource(graph.visualEffectResource);
            VFXTestCommon.CreateSystems(window.graphView, window.graphView.controller, 1, 0);
            window.graphView.OnSave();

            // Create a subgraph with a custom attribute (used in a set attribute block)
            var subgraph = VFXTestCommon.MakeTemporarySubGraphBlock();
            var subgraphGraph = subgraph.GetResource().GetGraph();
            subgraphGraph.TryAddCustomAttribute("myattribute", VFXValueType.Boolean, string.Empty, false, out var attribute);
            var subgraphBlockContext = (VFXBlockSubgraphContext)subgraphGraph.children.Single(x => x is VFXBlockSubgraphContext);
            var setAttribute = (SetAttribute)VFXLibrary.GetBlocks().First(x => x.modelType == typeof(SetAttribute)).CreateInstance();
            setAttribute.SetSettingValue("attribute", attribute.name);
            subgraphBlockContext.AddChild(setAttribute);
            var subgraphBlock = (VFXSubgraphBlock)VFXLibrary.GetBlocks().Single(x => x.model is VFXSubgraphBlock).CreateInstance();
            subgraphBlock.SetSettingValue("m_Subgraph", subgraph);

            // Add this subgraph in the initialize context
            var initializeContext = (VFXContext)graph.children.Single(x => x is VFXBasicInitialize);
            initializeContext.AddChild(subgraphBlock);
            AssetDatabase.SaveAssetIfDirty(subgraph);
            yield return null;

            // Check that the subgraph block did import the used custom attribute
            var blackboard = window.graphView.blackboard;
            var frame = 16;
            var expectedAttribute = GetItemOfTypeAndName<AttributeItem>(blackboard, attribute.name);
            while (frame-- > 0 && expectedAttribute == null)
            {
                yield return null;
                expectedAttribute = GetItemOfTypeAndName<AttributeItem>(blackboard, attribute.name);
            }

            Assert.NotNull(expectedAttribute);
            Assert.IsFalse(expectedAttribute.isEditable);
            Assert.IsFalse(expectedAttribute.isBuiltIn);
            Assert.AreEqual(attribute.type, CustomAttributeUtility.GetValueType(expectedAttribute.type));
            yield return null;

            var nodeUI = window.graphView.Query<VFXBlockUI>().Where(x => x.controller.model == subgraphBlock).First();
            window.graphView.AddToSelection(nodeUI);
            window.graphView.Delete();
            yield return null;

            frame = 16;
            expectedAttribute = GetItemOfTypeAndName<AttributeItem>(blackboard, attribute.name);
            while (frame-- > 0 && expectedAttribute != null)
            {
                yield return null;
                expectedAttribute = GetItemOfTypeAndName<AttributeItem>(blackboard, attribute.name);
            }

            Assert.IsNull(expectedAttribute);
            Assert.GreaterOrEqual(frame, 0);
        }

        [UnityTest]
        public IEnumerator Add_And_Remove_SubgraphOperator_With_CustomAttribute()
        {
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var window = VFXTestCommon.GetWindow(graph, true);
            window.LoadResource(graph.visualEffectResource);
            VFXTestCommon.CreateSystems(window.graphView, window.graphView.controller, 1, 0);

            // Create a subgraph with a custom attribute (used in a Get attribute operator)
            var subgraph = VFXTestCommon.MakeTemporarySubGraphOperator();
            var subgraphGraph = subgraph.GetResource().GetGraph();
            subgraphGraph.TryAddCustomAttribute("myattribute", VFXValueType.Boolean, string.Empty, false, out var attribute);
            var getAttribute = (VFXAttributeParameter)VFXLibrary.GetOperators().First(x => x.modelType == typeof(VFXAttributeParameter)).CreateInstance();
            getAttribute.SetSettingValue("attribute", attribute.name);
            subgraphGraph.AddChild(getAttribute);
            var subgraphOperator = (VFXSubgraphOperator)VFXLibrary.GetOperators().Single(x => x.model is VFXSubgraphOperator).CreateInstance();
            subgraphOperator.SetSettingValue("m_Subgraph", subgraph);

            // Add this subgraph to the main graph
            graph.AddChild(subgraphOperator);
            AssetDatabase.SaveAssets();
            yield return null;

            // Check that the subgraph operator did import the used custom attribute
            var blackboard = window.graphView.blackboard;
            var frame = 16;
            var expectedAttribute = GetItemOfTypeAndName<AttributeItem>(blackboard, attribute.name);
            while (frame-- > 0 && expectedAttribute == null)
            {
                yield return null;
                expectedAttribute = GetItemOfTypeAndName<AttributeItem>(blackboard, attribute.name);
            }

            Assert.NotNull(expectedAttribute);
            Assert.IsFalse(expectedAttribute.isEditable);
            Assert.IsFalse(expectedAttribute.isBuiltIn);
            Assert.AreEqual(attribute.type, CustomAttributeUtility.GetValueType(expectedAttribute.type));
            yield return null;

            var nodeUI = window.graphView.Query<VFXOperatorUI>().Where(x => x.controller.model == subgraphOperator).First();
            window.graphView.AddToSelection(nodeUI);
            window.graphView.Delete();
            yield return null;

            frame = 16;
            expectedAttribute = GetItemOfTypeAndName<AttributeItem>(blackboard, attribute.name);
            while (frame-- > 0 && expectedAttribute != null)
            {
                yield return null;
                expectedAttribute = GetItemOfTypeAndName<AttributeItem>(blackboard, attribute.name);
            }

            Assert.IsNull(expectedAttribute);
            Assert.GreaterOrEqual(frame, 0);
        }

        [Test]
        public void Rename_Custom_Attribute_To_Built_In_Name_Is_Rejected()
        {
            // Arrange
            var graph = VFXTestCommon.MakeTemporaryGraph();
            Assert.IsTrue(graph.TryAddCustomAttribute("myattribute", VFXValueType.Float, string.Empty, false, out _));

            // Act
            LogAssert.Expect(LogType.Warning, "Cannot rename custom attribute: The name is already used by a built-in attribute");
            var renamed = graph.TryRenameCustomAttribute("myattribute", "position");

            // Assert
            Assert.IsFalse(renamed);
            Assert.AreEqual("myattribute", graph.customAttributes.Single().attributeName);
        }

        private void AddCustomAttribute(VFXBlackboard blackboard, VFXValueType type)
        {
            var onAddCustomAttributeMethod = blackboard.GetType().GetMethod("OnAddCustomAttribute", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(onAddCustomAttributeMethod);
            onAddCustomAttributeMethod.Invoke(blackboard, new object[] { type });
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
    }
}
