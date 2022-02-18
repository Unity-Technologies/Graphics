using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.UnitTests.Controllers;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    internal class CategoryTests
    {
        static string kGraphName = "Assets/CommonAssets/Graphs/Categories.shadergraph";
        GraphData m_Graph;

        PropertyCollector m_Collector;

        GraphEditorView m_GraphEditorView;
        MaterialGraphEditWindow m_Window;

        Dictionary<string, PreviewNode> m_TestNodes = new Dictionary<string, PreviewNode>();

        BlackboardTestController m_BlackboardTestController;
        static void CreateBlackboardCategory(BlackboardTestController blackboardTestController)
        {
            var menuItems = blackboardTestController.addBlackboardItemsMenu.GetPrivateProperty<IList>("menuItems");
            Assert.IsNotNull(menuItems, "Could not retrieve reference to the menu items of the Blackboard Add Items menu");

            // Create category
            // We know that category is at the top of the menu items, so we can invoke it that way,
            // though ideally we would not be reliant on the in-menu order in such an explicit way as that can easily break or change
            var categoryMenuObject = menuItems[0];
            if (categoryMenuObject != null)
            {
                var menuFunction = categoryMenuObject.GetNonPrivateField<GenericMenu.MenuFunction>("func");
                menuFunction?.Invoke();
            }
        }

        [OneTimeSetUp]
        public void LoadGraph()
        {
            List<PropertyCollector.TextureInfo> lti;
            var assetCollection = new AssetCollection();
            ShaderGraphImporterLegacy.GetShaderText(kGraphName, out lti, assetCollection, out m_Graph);
            Assert.NotNull(m_Graph, $"Invalid graph data found for {kGraphName}");

            m_Graph.ValidateGraph();

            m_Collector = new PropertyCollector();
            m_Graph.CollectShaderProperties(m_Collector, GenerationMode.ForReals);

            // Open up the window
            if (!ShaderGraphImporterLegacyEditor.ShowGraphEditWindow(kGraphName))
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
            // Don't spawn ask-to-save dialog
            m_Window.graphObject = null;
            m_Window.Close();
        }

        [UnityTest]
        public IEnumerator AddCategoryTests()
        {
            Assert.IsNotNull(m_BlackboardTestController.addBlackboardItemsMenu, "Blackboard Add Items menu reference owned by BlackboardTestController is null.");

            CreateBlackboardCategory(m_BlackboardTestController);
            yield return null;

            var newCategory = m_BlackboardTestController.blackboard.Q<SGBlackboardCategory>();
            if(newCategory == null)
                Assert.Fail("Failed to create Category during AddCategoryTests");

            m_BlackboardTestController.ResetBlackboardState();
        }

        [UnityTest]
        public IEnumerator DeleteCategoryTests()
        {
            Assert.IsNotNull(m_BlackboardTestController.addBlackboardItemsMenu, "Blackboard Add Items menu reference owned by BlackboardTestController is null.");

            CreateBlackboardCategory(m_BlackboardTestController);
            yield return null;

            var categories = m_BlackboardTestController.blackboard.Query<SGBlackboardCategory>().ToList();
            if (categories != null)
            {
                var newCategory = categories[1];
                if(newCategory == null)
                    Assert.Fail("Failed to create Category during DuplicateCategoryTests");

                // Select the new category
                ShaderGraphUITestHelpers.SendMouseEvent(m_Window, newCategory, EventType.MouseDown, positionOffset: new Vector2(40, 10));
                // Wait a frame
                yield return null;
                ShaderGraphUITestHelpers.SendMouseEvent(m_Window, newCategory, EventType.MouseUp, positionOffset:new Vector2(40, 10));
                yield return null;
                // Then send delete command
                ShaderGraphUITestHelpers.SendDeleteCommand(m_Window, newCategory);
                yield return null;

                Assert.IsTrue(m_BlackboardTestController.blackboard.Query<SGBlackboardCategory>().ToEnumerable().Count() == 1, "Failed to delete blackboard category view");
            }

            m_BlackboardTestController.ResetBlackboardState();
        }

        [UnityTest]
        public IEnumerator DuplicateCategoryTests()
        {
            Assert.IsNotNull(m_BlackboardTestController.addBlackboardItemsMenu, "Blackboard Add Items menu reference owned by BlackboardTestController is null.");

            CreateBlackboardCategory(m_BlackboardTestController);
            yield return null;

            var categories = m_BlackboardTestController.blackboard.Query<SGBlackboardCategory>().ToList();
            if (categories != null)
            {
                var newCategory = categories[1];
                if(newCategory == null)
                    Assert.Fail("Failed to create Category during DuplicateCategoryTests");

                var categoryClickOffset = new Vector2(40, 10);
                // Select the new category
                ShaderGraphUITestHelpers.SendMouseEvent(m_Window, newCategory, EventType.MouseDown, positionOffset: categoryClickOffset);
                yield return null;
                ShaderGraphUITestHelpers.SendMouseEvent(m_Window, newCategory, EventType.MouseUp, positionOffset: categoryClickOffset);
                yield return null;
                // Then send duplicate command
                ShaderGraphUITestHelpers.SendDuplicateCommand(m_Window);
                yield return null;
                var categoryViewsList = m_BlackboardTestController.blackboard.Query<SGBlackboardCategory>().ToList();
                Assert.IsTrue(categoryViewsList.Count() == 3, "Failed to duplicate blackboard category view");
                Assert.IsTrue(categoryViewsList[1].title == categoryViewsList[2].title, "Failed to duplicate blackboard category");
            }

            m_BlackboardTestController.ResetBlackboardState();
        }

        [UnityTest]
        public IEnumerator RenameCategoryTests()
        {
            Assert.IsNotNull(m_BlackboardTestController.addBlackboardItemsMenu, "Blackboard Add Items menu reference owned by BlackboardTestController is null.");

            CreateBlackboardCategory(m_BlackboardTestController);
            yield return null;

            var categories = m_BlackboardTestController.blackboard.Query<SGBlackboardCategory>().ToList();
            if (categories != null)
            {
                var newCategory = categories[1];
                if(newCategory == null)
                    Assert.Fail("Failed to create Category during RenameCategoryTests");

                var categoryClickOffset = new Vector2(40, 10);
                // Trigger category rename
                ShaderGraphUITestHelpers.SendMouseEvent(m_Window, newCategory, EventType.MouseDown, positionOffset: categoryClickOffset, clickCount: 2);
                yield return null;
                ShaderGraphUITestHelpers.SendMouseEvent(m_Window, newCategory, EventType.MouseUp, positionOffset: categoryClickOffset);

                // Send the test rename input
                ShaderGraphUITestHelpers.SendKeyEvent(m_Window, newCategory.textField, keyboardCharacter: 'T', keyCode: KeyCode.T);
                ShaderGraphUITestHelpers.SendKeyEvent(m_Window, newCategory.textField, keyboardCharacter: 'e', keyCode: KeyCode.E);
                ShaderGraphUITestHelpers.SendKeyEvent(m_Window, newCategory.textField, keyboardCharacter: 's', keyCode: KeyCode.S);
                ShaderGraphUITestHelpers.SendKeyEvent(m_Window, newCategory.textField, keyboardCharacter: 't', keyCode: KeyCode.T);

                // Confirm the change to the text field
                ShaderGraphUITestHelpers.SendKeyEvent(m_Window, newCategory.textField, keyboardCharacter: '\n', keyCode: KeyCode.Return);
                yield return null;

                Assert.IsTrue(newCategory.title == "Test", "Failed to rename blackboard category");
            }

            m_BlackboardTestController.ResetBlackboardState();
        }

        [UnityTest]
        public IEnumerator ExpandCollapseCategoryTests()
        {
            Assert.IsNotNull(m_BlackboardTestController.addBlackboardItemsMenu, "Blackboard Add Items menu reference owned by BlackboardTestController is null.");

            CreateBlackboardCategory(m_BlackboardTestController);
            yield return null;

            var categories = m_BlackboardTestController.blackboard.Query<SGBlackboardCategory>().ToList();
            if (categories != null)
            {
                var newCategory = categories[1];
                if(newCategory == null)
                    Assert.Fail("Failed to create Category during RenameCategoryTests");

                var categoryClickOffset = new Vector2(10, 10);
                // Trigger category collapse
                ShaderGraphUITestHelpers.SendMouseEvent(m_Window, newCategory, EventType.MouseDown, positionOffset: categoryClickOffset);
                yield return null;
                ShaderGraphUITestHelpers.SendMouseEvent(m_Window, newCategory, EventType.MouseUp, positionOffset: categoryClickOffset);
                Assert.IsTrue(newCategory.viewModel.isExpanded == false, "Failed to collapse blackboard category");

                // Trigger category expand
                ShaderGraphUITestHelpers.SendMouseEvent(m_Window, newCategory, EventType.MouseDown, positionOffset: categoryClickOffset);
                yield return null;
                ShaderGraphUITestHelpers.SendMouseEvent(m_Window, newCategory, EventType.MouseUp, positionOffset: categoryClickOffset);
                Assert.IsTrue(newCategory.viewModel.isExpanded, "Failed to collapse blackboard category");
            }

            m_BlackboardTestController.ResetBlackboardState();
        }
    }
}
