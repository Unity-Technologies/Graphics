using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.UnitTests.Controllers;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class PropertyTests
    {
        static string kGraphName = "Assets/CommonAssets/Graphs/Properties.shadergraph";
        GraphData m_Graph;

        PropertyCollector m_Collector;

        GraphEditorView m_GraphEditorView;
        MaterialGraphEditWindow m_Window;

        Dictionary<string, PreviewNode> m_TestNodes = new Dictionary<string, PreviewNode>();

        BlackboardTestController m_BlackboardTestController;

        [OneTimeSetUp]
        public void LoadGraph()
        {
            List<PropertyCollector.TextureInfo> lti;
            var assetCollection = new AssetCollection();
            ShaderGraphImporter.GetShaderText(kGraphName, out lti, assetCollection, out m_Graph);
            Assert.NotNull(m_Graph, $"Invalid graph data found for {kGraphName}");

            m_Graph.ValidateGraph();

            m_Collector = new PropertyCollector();
            m_Graph.CollectShaderProperties(m_Collector, GenerationMode.ForReals);

            // Open up the window
            if (!ShaderGraphImporterEditor.ShowGraphEditWindow(kGraphName, disablePreviewsForTesting: true))
            {
                Assert.Fail("ShaderGraphImporterEditor.ShowGraphEditWindow could not open " + kGraphName);
            }

            m_Window = EditorWindow.GetWindow<MaterialGraphEditWindow>();
            if (m_Window == null)
            {
                Assert.Fail("Could not open window");
            }

            // EditorWindow.GetWindow will return a new window if one is not found. A new window will have graphObject == null.
            if (m_Window.graphObject == null)
            {
                Assert.Fail("Existing Shader Graph window of " + kGraphName + " not found.");
            }

            m_GraphEditorView = m_Window.graphEditorView;

            // Create the blackboard test controller
            var blackboardViewModel = new BlackboardViewModel() { parentView = m_Window.graphEditorView.graphView, model = m_Window.graphObject.graph, title = m_Window.assetName };
            m_BlackboardTestController = new BlackboardTestController(m_Window, m_Graph, blackboardViewModel, m_Window.graphObject.graphDataStore);

            // Remove the normal blackboard
            m_GraphEditorView.blackboardController.blackboard.RemoveFromHierarchy();
            // And override reference to the blackboard controller to point at the test controller
            m_GraphEditorView.blackboardController = m_BlackboardTestController;
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            if (m_Window != null)
            {
                m_Window.DiscardChanges();
                m_Window.Close();
            }
            m_Window = null;
        }

        [Test]
        public void SliderPropertyRangeMinLesserThanMax()
        {
            foreach (AbstractShaderProperty property in m_Collector.properties)
            {
                if (property is Vector1ShaderProperty vector1ShaderProperty && vector1ShaderProperty.floatType == FloatType.Slider)
                {
                    Assert.IsTrue(vector1ShaderProperty.rangeValues.x < vector1ShaderProperty.rangeValues.y,
                        "Slider property cannot have min be greater than max!");
                }
            }
        }

        [UnityTest]
        public IEnumerator ResetPropertyReference()
        {
            var fieldViewElements = m_GraphEditorView.Query("blackboardFieldView");
            foreach (var visualElement in fieldViewElements.ToList())
            {
                var blackboardPropertyView = (SGBlackboardField)visualElement;
                if (blackboardPropertyView == null) continue;

                var shaderInput = (AbstractShaderProperty)blackboardPropertyView.shaderInput;
                var originalReferenceName = shaderInput.referenceName;
                var propertyType = shaderInput.GetPropertyTypeString();
                var modifiedReferenceName = $"{propertyType}_Test";
                shaderInput.SetReferenceNameAndSanitizeForGraph(m_Graph, modifiedReferenceName);

                Assert.IsTrue(shaderInput.referenceName != originalReferenceName);
                m_GraphEditorView.graphView.ClearSelection();
                m_GraphEditorView.graphView.AddToSelection(blackboardPropertyView);
                Assert.AreEqual(1, m_Window.graphEditorView.graphView.selection.Count(), "Could not select property!");

                // Wait a frame for the inspector updates to trigger
                yield return ShaderGraphUITestHelpers.SuppressLogFailures();


                // Cannot actually spawn the right click menu for testing due to ContextMenuManipulators spawning
                // an OS level menu that steals focus from Editor and preventing any future events from being processed
                // Instead, we trigger the action directly from code, for now...
                shaderInput.ResetReferenceName(m_Graph);

                if (shaderInput.referenceName != originalReferenceName)
                    Assert.Fail("Failed to reset reference name to original value.");
            }
        }

        [UnityTest]
        public IEnumerator BlackboardSelectionHistory()
        {
            Assert.IsNotNull(m_BlackboardTestController.addBlackboardItemsMenu, "Blackboard Add Items menu reference owned by BlackboardTestController is null.");

            var menuItems = m_BlackboardTestController.addBlackboardItemsMenu.GetPrivateProperty<IList>("menuItems");
            Assert.IsNotNull(menuItems, "Could not retrieve reference to the menu items of the Blackboard Add Items menu");

            for (int i = 0; i < 6; i++)
            {
                var item = menuItems[i];
                var menuFunction = item.GetNonPrivateField<GenericMenu.MenuFunction>("func");
                menuFunction?.Invoke();
                yield return ShaderGraphUITestHelpers.SuppressLogFailures();
            }

            var fieldViewElements = m_GraphEditorView.Query("SGBlackboardField");
            var elementList = fieldViewElements.ToList();

            var selectionOrder = new List<SGBlackboardField>(elementList.Count);

            for (int i = 0; i < elementList.Count - 1; i++)
            {
                var visualElement = elementList[i];
                if (visualElement is not SGBlackboardField blackboardPropertyView) continue;

                selectionOrder.Add(blackboardPropertyView);

                m_GraphEditorView.graphView.ClearSelection();
                m_GraphEditorView.graphView.AddToSelection(blackboardPropertyView);
                Undo.IncrementCurrentGroup();

                yield return ShaderGraphUITestHelpers.SuppressLogFailures();
            }

            var lastSelection = selectionOrder[^1];
            var currentSelection = m_GraphEditorView.graphView.selection[0];

            Assert.AreEqual(lastSelection, currentSelection);

            EditorApplication.ExecuteMenuItem("Edit/Previous Selection");
            yield return ShaderGraphUITestHelpers.SuppressLogFailures();

            var secondToLastSelection = selectionOrder[^2];
            currentSelection = m_GraphEditorView.graphView.selection[0];

            Assert.AreEqual(secondToLastSelection, currentSelection);

            EditorApplication.ExecuteMenuItem("Edit/Previous Selection");
            LogAssert.ignoreFailingMessages = true;
            try { yield return new WaitForSecondsRealtime(0.1f); }
            finally { LogAssert.ignoreFailingMessages = false; }

            var thirdToLastSelection = selectionOrder[^3];
            currentSelection = m_GraphEditorView.graphView.selection[0];

            Assert.AreEqual(thirdToLastSelection, currentSelection);

            EditorApplication.ExecuteMenuItem("Edit/Next Selection");
            yield return ShaderGraphUITestHelpers.SuppressLogFailures();

            currentSelection = m_GraphEditorView.graphView.selection[0];

            Assert.AreEqual(secondToLastSelection, currentSelection);

            EditorApplication.ExecuteMenuItem("Edit/Next Selection");
            yield return ShaderGraphUITestHelpers.SuppressLogFailures();

            currentSelection = m_GraphEditorView.graphView.selection[0];

            Assert.AreEqual(lastSelection, currentSelection);
        }

        [UnityTest]
        public IEnumerator AddInputTests()
        {
            Assert.IsNotNull(m_BlackboardTestController.addBlackboardItemsMenu, "Blackboard Add Items menu reference owned by BlackboardTestController is null.");

            var menuItems = m_BlackboardTestController.addBlackboardItemsMenu.GetPrivateProperty<IList>("menuItems");
            Assert.IsNotNull(menuItems, "Could not retrieve reference to the menu items of the Blackboard Add Items menu");

            foreach (var item in menuItems)
            {
                var menuFunction = item.GetNonPrivateField<GenericMenu.MenuFunction>("func");
                menuFunction?.Invoke();
                yield return ShaderGraphUITestHelpers.SuppressLogFailures();
            }
        }

        [UnityTest]
        public IEnumerator RemoveInputTests()
        {
            Assert.IsNotNull(m_BlackboardTestController.addBlackboardItemsMenu, "Blackboard Add Items menu reference owned by BlackboardTestController is null.");

            var menuItems = m_BlackboardTestController.addBlackboardItemsMenu.GetPrivateProperty<IList>("menuItems");
            Assert.IsNotNull(menuItems, "Could not retrieve reference to the menu items of the Blackboard Add Items menu");

            // invoke all menu items on the "add Blackboard Items Menu" to add all property types
            foreach (var item in menuItems)
            {
                var menuFunction = item.GetNonPrivateField<GenericMenu.MenuFunction>("func");
                menuFunction?.Invoke();
                yield return ShaderGraphUITestHelpers.SuppressLogFailures();
            }

            var cachedPropertyList = m_Window.graphObject.graph.properties.ToList();
            foreach (var property in cachedPropertyList)
            {
                var blackboardRow = m_BlackboardTestController.GetBlackboardRow(property);
                Assert.IsNotNull(blackboardRow, "No blackboard row found associated with blackboard property.");
                var blackboardPropertyView = blackboardRow.Q<SGBlackboardField>();
                Assert.IsNotNull(blackboardPropertyView, "No blackboard property view found in the blackboard row.");
                m_GraphEditorView.graphView.ClearSelection();
                m_GraphEditorView.graphView.AddToSelection(blackboardPropertyView);
                Assert.AreEqual(1, m_Window.graphEditorView.graphView.selection.Count(), "Could not select property!");
                yield return ShaderGraphUITestHelpers.SuppressLogFailures();

                ShaderGraphUITestHelpers.SendDeleteCommand(m_Window, m_GraphEditorView.graphView);
                yield return ShaderGraphUITestHelpers.SuppressLogFailures();
            }

            var cachedKeywordList = m_Window.graphObject.graph.keywords.ToList();
            foreach (var keyword in cachedKeywordList)
            {
                var blackboardRow = m_BlackboardTestController.GetBlackboardRow(keyword);
                Assert.IsNotNull(blackboardRow, "No blackboard row found associated with blackboard keyword.");
                var blackboardPropertyView = blackboardRow.Q<SGBlackboardField>();
                Assert.IsNotNull(blackboardPropertyView, "No blackboard property view found in the blackboard row.");
                m_GraphEditorView.graphView.ClearSelection();
                m_GraphEditorView.graphView.AddToSelection(blackboardPropertyView);
                Assert.AreEqual(1, m_Window.graphEditorView.graphView.selection.Count(), "Could not select keyword!");
                yield return ShaderGraphUITestHelpers.SuppressLogFailures();

                ShaderGraphUITestHelpers.SendDeleteCommand(m_Window, m_GraphEditorView.graphView);
                yield return ShaderGraphUITestHelpers.SuppressLogFailures();
            }

            // Should be no properties, keywords, dropdowns
            Assert.IsTrue(m_Window.graphObject.graph.properties.Count() == 0 && m_Window.graphObject.graph.dropdowns.Count() == 0);
            yield return ShaderGraphUITestHelpers.SuppressLogFailures();
        }

        [Test]
        public void DefaultNamePropertyTest()
        {
            // Validate that all property types have the correct default display and reference name
            var properties = new List<(ShaderInput shaderInput, string displayName, string referenceName)>
            {
                (new Vector1ShaderProperty(), "Float", "_Float"),
                (new Vector2ShaderProperty(), "Vector2", "_Vector2"),
                (new Vector3ShaderProperty(), "Vector3", "_Vector3"),
                (new Vector4ShaderProperty(), "Vector4", "_Vector4"),
                (new ColorShaderProperty(), "Color", "_Color"),
                (new BooleanShaderProperty(), "Boolean", "_Boolean"),
                (new GradientShaderProperty(), "Gradient", "_Gradient"),
                (new Texture2DShaderProperty(), "Texture2D", "_Texture2D"),
                (new Texture2DArrayShaderProperty(), "Texture2D Array", "_Texture2D_Array"),
                (new Texture3DShaderProperty(), "Texture3D", "_Texture3D"),
                (new CubemapShaderProperty(), "Cubemap", "_Cubemap"),
                (new VirtualTextureShaderProperty(), "VirtualTexture", "_VirtualTexture"),
                (new Matrix2ShaderProperty(), "Matrix2x2", "_Matrix2x2"),
                (new Matrix3ShaderProperty(), "Matrix3x3", "_Matrix3x3"),
                (new Matrix4ShaderProperty(), "Matrix4x4", "_Matrix4x4"),
                (new SamplerStateShaderProperty(), "SamplerState", "_SamplerState"),
                (new ShaderKeyword(KeywordType.Boolean), "Boolean", "_BOOLEAN"),
                (new ShaderKeyword(KeywordType.Enum), "Enum", "_ENUM"),
                (new ShaderKeyword(KeywordType.Enum) {displayName = "Material Quality", isBuiltIn = true }, "Material Quality", "MATERIAL_QUALITY"),
                // A second vector1 property should properly change the display and reference name
                (new Vector1ShaderProperty(), "Float (1)", "_Float_1"),
                // Test manually setting the display name. This should just pre-pend an underscore
                (new Vector4ShaderProperty() { displayName = "A" }, "A", "_A"),
                // Validate duplicate display names are correctly handled too
                (new Vector4ShaderProperty() { displayName = "A" }, "A (1)", "_A_1"),
            };

            foreach (var property in properties)
            {
                m_Graph.AddGraphInput(property.shaderInput);
                // Check that the default display and reference names match what's expected
                Assert.IsTrue(property.shaderInput.displayName == property.displayName, "Expected display name '{0}' but was '{1}'", property.displayName, property.shaderInput.displayName);
                Assert.IsTrue(property.shaderInput.referenceName == property.referenceName, "Expected reference name '{0}' but was '{1}'", property.referenceName, property.shaderInput.referenceName);
            }
        }

        [UnityTest]
        public IEnumerator DuplicateInputTest()
        {
            Assert.IsNotNull(m_BlackboardTestController.addBlackboardItemsMenu, "Blackboard Add Items menu reference owned by BlackboardTestController is null.");

            var menuItems = m_BlackboardTestController.addBlackboardItemsMenu.GetPrivateProperty<IList>("menuItems");
            Assert.IsNotNull(menuItems, "Could not retrieve reference to the menu items of the Blackboard Add Items menu");

            // invoke all menu items on the "add Blackboard Items Menu" to add all property types
            foreach (var item in menuItems)
            {
                var menuFunction = item.GetNonPrivateField<GenericMenu.MenuFunction>("func");
                menuFunction?.Invoke();
                yield return ShaderGraphUITestHelpers.SuppressLogFailures();
            }

            var cachedPropertyList = m_Window.graphObject.graph.properties.ToList();
            foreach (var property in cachedPropertyList)
            {
                var blackboardRow = m_BlackboardTestController.GetBlackboardRow(property);
                Assert.IsNotNull(blackboardRow, "No blackboard row found associated with blackboard property.");
                var blackboardPropertyView = blackboardRow.Q<SGBlackboardField>();
                Assert.IsNotNull(blackboardPropertyView, "No blackboard property view found in the blackboard row.");
                m_GraphEditorView.graphView.ClearSelection();
                m_GraphEditorView.graphView.AddToSelection(blackboardPropertyView);
                Assert.AreEqual(1, m_Window.graphEditorView.graphView.selection.Count(), "Could not select property!");
                yield return ShaderGraphUITestHelpers.SuppressLogFailures();

                ShaderGraphUITestHelpers.SendDuplicateCommand(m_Window);
                yield return ShaderGraphUITestHelpers.SuppressLogFailures();
            }

            var cachedKeywordList = m_Window.graphObject.graph.keywords.ToList();
            foreach (var keyword in cachedKeywordList)
            {
                var blackboardRow = m_BlackboardTestController.GetBlackboardRow(keyword);
                Assert.IsNotNull(blackboardRow, "No blackboard row found associated with blackboard keyword.");
                var blackboardPropertyView = blackboardRow.Q<SGBlackboardField>();
                Assert.IsNotNull(blackboardPropertyView, "No blackboard property view found in the blackboard row.");
                m_GraphEditorView.graphView.ClearSelection();
                m_GraphEditorView.graphView.AddToSelection(blackboardPropertyView);
                Assert.AreEqual(1, m_Window.graphEditorView.graphView.selection.Count(), "Could not select keyword!");
                yield return ShaderGraphUITestHelpers.SuppressLogFailures();

                ShaderGraphUITestHelpers.SendDuplicateCommand(m_Window);
                yield return ShaderGraphUITestHelpers.SuppressLogFailures();

            }
        }

        [UnityTest]
        public IEnumerator RestoreSelectAfterUndoRedoTest()
        {
            //Due to errors occurring from the results of other tests, the graph is reset and then the test is conducted
            Cleanup();
            LoadGraph();

            Assert.IsNotNull(m_BlackboardTestController.addBlackboardItemsMenu, "Blackboard Add Items menu reference owned by BlackboardTestController is null.");

            var menuItems = m_BlackboardTestController.addBlackboardItemsMenu.GetPrivateProperty<IList>("menuItems");
            Assert.IsNotNull(menuItems, "Could not retrieve reference to the menu items of the Blackboard Add Items menu");

            // invoke float menu items on the "add Blackboard Items Menu" to add a property type
            bool addedFloat = false;
            foreach (var item in menuItems)
            {
                if ( item.GetNonPrivateField<GUIContent>("content").text == "Float" )
                {
                    var menuFunction = item.GetNonPrivateField<GenericMenu.MenuFunction>("func");
                    menuFunction?.Invoke();
                    yield return ShaderGraphUITestHelpers.SuppressLogFailures();
                    addedFloat = true;
                    break;
                }
            }

            if (!addedFloat)
                Assert.Fail("Failed to add a Float property.");

            var cachedPropertyList = m_Window.graphObject.graph.properties.ToList();
            var property = cachedPropertyList.Last();   //< last is property added above.
            var vector1Property = property as Vector1ShaderProperty;

            Assert.IsNotNull(vector1Property, "Last property is not Float.");
            var blackboardRow = m_BlackboardTestController.GetBlackboardRow(property);
            Assert.IsNotNull(blackboardRow, "No blackboard row found associated with blackboard property.");
            var blackboardPropertyView = blackboardRow.Q<SGBlackboardField>();
            Assert.IsNotNull(blackboardPropertyView, "No blackboard property view found in the blackboard row.");
            m_GraphEditorView.graphView.ClearSelection();
            m_GraphEditorView.graphView.AddToSelection(blackboardPropertyView);
            Assert.AreEqual(1, m_GraphEditorView.graphView.selection.Count(), "Could not select property!");

            //set value to property
            m_Window.graphObject.RegisterCompleteObjectUndo("Property Change");
            vector1Property.value = 0.245f;

            yield return ShaderGraphUITestHelpers.SuppressLogFailures();
            Assert.AreEqual("Property Change", Undo.GetCurrentGroupName(), "Failed to register undo.");


            Undo.PerformUndo();
            yield return ShaderGraphUITestHelpers.SuppressLogFailures();
            Undo.PerformRedo();
            yield return ShaderGraphUITestHelpers.SuppressLogFailures();

            Assert.AreEqual(1, m_GraphEditorView.graphView.selection.Count(), "Failed to restore select property!");
        }

        [Test]
        public void DuplicatePropertyTest()
        {
            // public void AddGraphInput(ShaderInput input, int index = -1)
            // public void RemoveGraphInput(ShaderInput input)
            // public ShaderInput AddCopyOfShaderInput(ShaderInput source, int insertIndex = -1)

            var B = new Vector4ShaderProperty() { displayName = "B" };

            // add it twice!
            m_Graph.AddGraphInput(B);
            var B2 = m_Graph.AddCopyOfShaderInput(B);
            var B3 = m_Graph.AddCopyOfShaderInput(B);

            // check that both names have been made unique
            Assert.IsTrue(B.displayName != B2.displayName);
            Assert.IsTrue(B.displayName != B3.displayName);
            Assert.IsTrue(B2.displayName != B3.displayName);

            Assert.IsTrue(B.referenceName != B2.referenceName);
            Assert.IsTrue(B.referenceName != B3.referenceName);
            Assert.IsTrue(B2.referenceName != B3.referenceName);

            // set overrides, so that B3 is now called "B"
            B.SetDisplayNameAndSanitizeForGraph(m_Graph, "Q");          // display name "Q"   reference name default "_Q"
            B3.SetDisplayNameAndSanitizeForGraph(m_Graph, "B");         // display name "B"   reference name default "_B"

            // since reference names should still be using default behavior of tracking display names,
            // B3 ref name should now be called "B"
            Assert.IsTrue(B.referenceName == "_Q");
            Assert.IsTrue(B3.referenceName == "_B");

            // now let's try overriding the reference names
            B3.SetReferenceNameAndSanitizeForGraph(m_Graph, "B3");       // display name "B"  reference name "B3"
            B.SetReferenceNameAndSanitizeForGraph(m_Graph, "B");        // display name "Q"   reference name "B"
            Assert.IsTrue(B.referenceName == "B");
            Assert.IsTrue(B3.referenceName == "B3");

            // now let's try resetting the reference name
            B3.ResetReferenceName(m_Graph);                     // display name "B",  reference name default (can't be "B" because of collisions)
            Assert.IsTrue(B3.referenceName != "B");

            // let's check everything is still unique
            Assert.IsTrue(B.displayName != B2.displayName);
            Assert.IsTrue(B.displayName != B3.displayName);
            Assert.IsTrue(B2.displayName != B3.displayName);

            Assert.IsTrue(B.referenceName != B2.referenceName);
            Assert.IsTrue(B.referenceName != B3.referenceName);
            Assert.IsTrue(B2.referenceName != B3.referenceName);
        }

        [Test]
        public void SanitizePropertyTest()
        {
            var C = new Vector4ShaderProperty() { displayName = "C" };

            m_Graph.AddGraphInput(C);

            // numbers not allowed
            C.SetReferenceNameAndSanitizeForGraph(m_Graph, "3");
            Assert.IsTrue(C.referenceName == "_3");

            // shaderlab reserved words not allowed (case insensitive)
            C.SetReferenceNameAndSanitizeForGraph(m_Graph, "ColOrMasK");
            Assert.IsTrue(string.Compare(C.referenceName, "colormask", true) != 0);

            // whitespace not allowed
            C.SetReferenceNameAndSanitizeForGraph(m_Graph, " my float value ");
            Assert.IsFalse(C.referenceName.Contains(" "));
        }
    }
}
