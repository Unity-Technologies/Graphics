#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
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

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXBlackboardTests
    {
        private const string testFolder = "TmpTests";

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            AssetDatabase.DeleteAsset("Assets/" + testFolder);
            AssetDatabase.CreateFolder("Assets", testFolder);
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
        }

        [OneTimeTearDown]
        public void OneTimeCleanup()
        {
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [UnityTest]
        public IEnumerator Add_Category()
        {
            // Arrange
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var window = VFXViewWindow.GetWindow(graph, true);
            window.LoadResource(graph.visualEffectResource);
            VFXBlackboard blackboard = window.graphView.blackboard;
            yield return null;

            // Act
            blackboard.AddCategory("new category");
            yield return null;

            // Assert
            var enumerator = IsOpenForEdit(blackboard, "titleEdit", "new category");
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
            var window = VFXViewWindow.GetWindow(graph, true);
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
            blackboard.SetCategoryName(cat, " ");

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
            var window = VFXViewWindow.GetWindow(graph, true);
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
            yield return null;

            // Assert
            cat = blackboard.Query<VFXBlackboardCategory>().Where(x => x.title == categoryName + " 1").First();
            Assert.NotNull(cat);
        }

        [UnityTest]
        public IEnumerator Add_Custom_Attribute()
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

            // Act
            AddCustomAttribute(blackboard, VFXValueType.Boolean);
            yield return null;

            // Assert
            var enumerator = IsOpenForEdit(blackboard, "textField", "CustomAttribute");
            while (enumerator.MoveNext())
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Add_Parameter()
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

            var descriptorParameter = VFXLibrary.GetParameters().First(d => d.modelType == typeof(int));

            // Act
            AddParameter(blackboard, descriptorParameter);

            // Assert
            var enumerator = IsOpenForEdit(blackboard, "textField", $"New {descriptorParameter.name}");
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
            var window = VFXViewWindow.GetWindow(subgraph.GetResource(), true);
            window.LoadResource(subgraph.GetResource());
            yield return null;

            var blackboard = window.graphView.blackboard;
            var descriptorParameter = VFXLibrary.GetParameters().First(d => d.modelType == typeof(int));
            var outputCategory = blackboard.Query<VFXBlackboardCategory>().Where(x => x.title == "Output").First();
            blackboard.Q<TreeView>().selectedIndex = outputCategory.category.index;

            // Act
            AddParameter(blackboard, descriptorParameter);

            // Assert
            var enumerator = IsOpenForEdit(blackboard, "textField", $"New {descriptorParameter.name}");
            while (enumerator.MoveNext())
            {
                yield return null;
            }
            var outputParameterField = blackboard.Q<VFXBlackboardField>();
            Assert.IsTrue(outputParameterField.controller.isOutput);
        }

        [UnityTest]
        public IEnumerator Remove_Parameter()
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

            var descriptorParameter = VFXLibrary.GetParameters().First(d => d.modelType == typeof(int));
            AddParameter(blackboard, descriptorParameter);
            yield return null;
            blackboard.Focus();
            yield return null;

            // Act
            VFXBlackboardField parameterField = null;
            var maxFrames = 16;
            while (parameterField == null && maxFrames-- > 0)
            {
                yield return null;
                parameterField = blackboard.Q<VFXBlackboardField>();
            }
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
        public IEnumerator Remove_Category()
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
            var window = VFXViewWindow.GetWindow(graph, true);
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
            AddParameter(blackboard, descriptorParameter);
            yield return null;
            AddParameter(blackboard, descriptorParameter);
            var parameterFields = new List<VFXBlackboardField>();

            var frames = 32;
            while (frames-- > 0 && parameterFields.Count != 2)
            {
                yield return null;
                parameterFields = blackboard.Query<VFXBlackboardField>().ToList();
            }

            // Act
            window.graphView.AddToSelection(categoryRow);
            parameterFields.ForEach(window.graphView.AddToSelection);
            window.graphView.askForConfirmationBeforeDelete = false;
            window.graphView.Delete();

            // Assert
            frames = 32;
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
        public IEnumerator Remove_CustomAttribute()
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
            var window = VFXViewWindow.GetWindow(graph, true);
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
            var enumerator = IsOpenForEdit(blackboard, "textField", "CustomAttribute");
            while (enumerator.MoveNext())
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator DoubleClick_To_Rename_Parameter()
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

            var descriptorParameter = VFXLibrary.GetParameters().First(d => d.modelType == typeof(int));
            AddParameter(blackboard, descriptorParameter);
            yield return null;

            // Remove focus from newly created attribute so that it's not open for edit
            blackboard.Focus();
            yield return null;

            // Act
            VFXBlackboardField blackboardField = null;
            var maxFrames = 16;
            while (blackboardField == null && maxFrames-- > 0)
            {
                yield return null;
                blackboardField = blackboard.Q<VFXBlackboardField>();
            }
            Assert.NotNull(blackboardField);
            VFXGUITestHelper.SendDoubleClick(blackboardField, 2);

            // Assert
            var enumerator = IsOpenForEdit(blackboard, "textField", $"New {descriptorParameter.name}");
            while (enumerator.MoveNext())
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator DoubleClick_To_Rename_Category()
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
            var enumerator = IsOpenForEdit(blackboard, "titleEdit", category);
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
            var window = VFXViewWindow.GetWindow(graph, true);
            window.LoadResource(graph.visualEffectResource);
            VFXBlackboard blackboard = window.graphView.blackboard;
            yield return null;
            blackboard.Update();
            yield return null;

            // Act
            var blackboardCategory = blackboard.Query<VFXBlackboardCategory>().Where(x => string.Compare(x.category.title, categoryName, StringComparison.OrdinalIgnoreCase) == 0).First();
            VFXGUITestHelper.SendDoubleClick(blackboardCategory, 2);

            // Assert
            var enumerator = IsOpenForEdit(blackboard, "titleEdit", categoryName, false);
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
            var window = VFXViewWindow.GetWindow(subgraph.GetResource(), true);
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
            var window2 = VFXViewWindow.GetWindow(graph, true);
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
            var window = VFXViewWindow.GetWindow(graph, true);
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

        [UnityTest, Ignore("Click event do not reach the treeview, I keep it to fix it later")]
        public IEnumerator Shift_Click_To_Multi_Select_Attributes()
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
            var blackboardAttributes = blackboard.Query<VFXBlackboardAttributeField>().ToList().Take(4).ToArray();

            // Act
            blackboardAttributes[0].GetFirstAncestorOfType<VFXBlackboardAttributeRow>().SendEvent(ClickEvent.GetPooled());
            yield return null;
            blackboardAttributes[3].SendEvent(ClickEvent.GetPooled(new Touch(), EventModifiers.Shift));

            // Assert
            Assert.AreEqual(4, blackboard.selection.Count);
        }

        [UnityTest]
        public IEnumerator Add_And_Remove_SubgraphBlock_With_CustomAttribute()
        {
            BoardPreferenceHelper.SetVisible(BoardPreferenceHelper.Board.blackboard, true);
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var window = VFXViewWindow.GetWindow(graph, true);
            window.LoadResource(graph.visualEffectResource);
            VFXTestCommon.CreateSystems(window.graphView, window.graphView.controller, 1, 0);
            window.graphView.OnSave();

            // Create a subgraph with a custom attribute (used in a set attribute block)
            var subgraph = VFXTestCommon.MakeTemporarySubGraphBlock();
            var subgraphGraph = subgraph.GetResource().GetOrCreateGraph();
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
            var window = VFXViewWindow.GetWindow(graph, true);
            window.LoadResource(graph.visualEffectResource);
            VFXTestCommon.CreateSystems(window.graphView, window.graphView.controller, 1, 0);

            // Create a subgraph with a custom attribute (used in a Get attribute operator)
            var subgraph = VFXTestCommon.MakeTemporarySubGraphOperator();
            var subgraphGraph = subgraph.GetResource().GetOrCreateGraph();
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

        private void AddCustomAttribute(VFXBlackboard blackboard, VFXValueType type)
        {
            var onAddCustomAttributeMethod = blackboard.GetType().GetMethod("OnAddCustomAttribute", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(onAddCustomAttributeMethod);
            onAddCustomAttributeMethod.Invoke(blackboard, new object[] { type });
        }

        private void AddParameter(VFXBlackboard blackboard, VFXModelDescriptorParameters descriptorParameters)
        {
            var onAddParameterMethod = blackboard.GetType().GetMethod("OnAddParameter", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(onAddParameterMethod);
            onAddParameterMethod.Invoke(blackboard, new object[] { descriptorParameters });
        }

        private IEnumerator IsOpenForEdit(VisualElement parent, string fieldName, string textFieldValue, bool isInEdit = true)
        {
            var maxFrame = 16;
            while (maxFrame-- > 0)
            {
                yield return null;
                var textField = parent.Query<TextField>(fieldName).Where(x => x.resolvedStyle.display == DisplayStyle.Flex).First();
                if (textField != null && isInEdit)
                {
                    Assert.AreEqual(textFieldValue, textField.value);
                    yield break;
                }
            }

            Assert.IsFalse(isInEdit,  $"Could not find field with '{textFieldValue}' value in edit mode.");
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
#endif
