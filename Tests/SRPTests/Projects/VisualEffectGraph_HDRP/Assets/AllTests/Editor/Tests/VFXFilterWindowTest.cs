using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using NUnit.Framework;

using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXFilterWindowTest
    {
        private readonly Regex removeSpecialCharacters = new Regex("[#@]", RegexOptions.Compiled);

        private bool useExperimental;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            useExperimental = VFXViewPreference.displayExperimentalOperator;
            EditorPrefs.SetBool(VFXViewPreference.experimentalOperatorKey, true);
            SetSubvariantSearch(false);
            VFXViewPreference.SetDirty();
        }

        [OneTimeTearDown]
        public void OneTimeCleanup()
        {
            EditorPrefs.SetBool(VFXViewPreference.experimentalOperatorKey, useExperimental);
            VFXViewPreference.SetDirty();
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        public static object[] CheckBlockContextList =
        {
            new object[] { VFXContextType.Init, new[] { "Favorites", "Attribute", "Attribute from Curve", "Attribute from Map", "Collision", "GPUEvent", "HLSL", "Position Shape" }},
            new object[] { VFXContextType.Update, new[] { "Favorites", "Attribute", "Attribute from Curve", "Attribute from Map", "Collision", "FlipBook", "Force", "GPUEvent", "HLSL", "Implicit", "Position Shape" }},
            new object[] { VFXContextType.Output, new[] { "Favorites", "Attribute", "Attribute from Curve", "Attribute from Map", "Collision", "HLSL", "Orientation", "Output", "Position Shape", "Size" }},
        };

        [TestCaseSource(nameof(CheckBlockContextList))]
        public void CheckBlockList(int contextType, string[] expectedRootItems)
        {
            // Arrange
            var graph = VFXTestCommon.CreateGraph_And_System();
            var window = VFXViewWindow.GetWindow(graph, true, true);
            window.LoadResource(graph.visualEffectResource);
            var controller = window.graphView.controller;
            var contextController = controller.contexts.Single(x => x.model.contextType == (VFXContextType)contextType);
            var blockProvider = new VFXBlockProvider(contextController, (v, pos) => { });
            VFXFilterWindow.Show(Vector2.zero, Vector2.zero, blockProvider);
            var vfxFilterWindow = EditorWindow.GetWindow<VFXFilterWindow>();

            // Act
            var treeviewData = GetTreeData(vfxFilterWindow);

            // Assert
            CollectionAssert.AreEqual(expectedRootItems, treeviewData.Take(expectedRootItems.Length).Select(x => x.data.name));
        }

        [Test]
        public void CheckOperatorList()
        {
            // Arrange
            var graph = VFXTestCommon.CreateGraph_And_System();
            var window = VFXViewWindow.GetWindow(graph, true, true);
            window.LoadResource(graph.visualEffectResource);
            var nodeProvider = new VFXNodeProvider(window.graphView.controller, (v, pos) => { }, null, null);
            VFXFilterWindow.Show(Vector2.zero, Vector2.zero, nodeProvider);
            var vfxFilterWindow = EditorWindow.GetWindow<VFXFilterWindow>();
            var expectedRootItems = new[] { "Favorites", "Context", "Operator" };

            // Act
            var treeviewData = GetTreeData(vfxFilterWindow);

            // Assert
            CollectionAssert.AreEqual(expectedRootItems, treeviewData.Take(expectedRootItems.Length).Select(x => x.data.name));
        }

        public static IEnumerable<object> CheckBlockCategoryContextList()
        {
            yield return new object[] { "Attribute", new[] { "Basic Simulation", "|Set|_Age", "|Set|_Alive", "|Set|_Lifetime", "|Set|_Position", "|Set|_Velocity", "Advanced Simulation", "|Set|_Angle", "|Set|_Angular Velocity", "|Set|_Direction", "|Set|_Mass", "|Set|_Old Position", "|Set|_Target Position", "Rendering", "|Set|_Alpha", "|Set|_Axis X", "|Set|_Axis Y", "|Set|_Axis Z", "|Set|_Color", "|Set|_Mesh Index", "|Set|_Pivot", "|Set|_Scale", "|Set|_Size", "|Set|_Tex Index", "Derived" } };
            yield return new object[] { "Attribute from Curve", new[] { "Basic Simulation", "|Set|_Age|By Speed", "|Set|_Lifetime|Over Life", "|Set|_Position|Over Life", "|Set|_Velocity|Over Life", "Advanced Simulation", "|Set|_Angle|Over Life", "|Set|_Angular Velocity|Over Life", "|Set|_Direction|Over Life", "|Set|_Mass|Over Life", "|Set|_Old Position|Over Life", "|Set|_Target Position|Over Life", "Rendering", "|Set|_Alpha|Over Life", "|Set|_Axis X|Over Life", "|Set|_Axis Y|Over Life", "|Set|_Axis Z|Over Life", "|Set|_Color|Over Life", "|Set|_Mesh Index|Over Life", "|Set|_Pivot|Over Life", "|Set|_Scale|Over Life", "|Set|_Size|Over Life", "|Set|_Tex Index|Over Life" } };
        }

        [TestCaseSource(nameof(CheckBlockCategoryContextList))]
        public void CheckBlockCategoryList(string categoryPath, string[] expectedItems)
        {
            // Arrange
            Assert.IsTrue(EditorPrefs.GetBool(VFXViewPreference.experimentalOperatorKey));

            var graph = VFXTestCommon.CreateGraph_And_System();
            var window = VFXViewWindow.GetWindow(graph, true, true);
            window.LoadResource(graph.visualEffectResource);
            var controller = window.graphView.controller;
            var contextController = controller.contexts.Single(x => x.model.contextType == VFXContextType.Update);
            var blockProvider = new VFXBlockProvider(contextController, (v, pos) => { });
            VFXFilterWindow.Show(Vector2.zero, Vector2.zero, blockProvider);
            var vfxFilterWindow = EditorWindow.GetWindow<VFXFilterWindow>();

            // Act
            var treeviewData = GetTreeData(vfxFilterWindow);

            // Assert
            var path = categoryPath.Split('/');
            var currentTreeviewData = treeviewData;
            foreach (var p in path)
            {
                var list = treeviewData.FirstOrDefault(x => x.data.name == p);
                Assert.IsNotNull(list);
                currentTreeviewData = list.children as List<TreeViewItemData<VFXFilterWindow.Descriptor>>;
                Assert.IsNotNull(currentTreeviewData);
            }

            CollectionAssert.AreEqual(expectedItems, currentTreeviewData.Select(x => x.data.name));
        }

        public static object[] CheckBlockSearchResultCases =
        {
            new object[] { "set pos", "|Set|_Position", new[] { "Favorites", "Attribute", "|Set|_Position", "|Set|_Old Position", "|Set|_Target Position", "Attribute from Curve", "|Set|_Position|Over Life", "|Set|_Old Position|Over Life", "|Set|_Target Position|Over Life", "Attribute from Map", "|Set|_Position from Map|2D", "|Set|_Old Position from Map|2D", "|Set|_Target Position from Map|2D", "Position Shape", "|Set|_Position Mesh|Mesh", "|Set|_Position Shape|Sphere", "|Set|_Position Mesh|Skinned Mesh", "|Set|_Position Shape|Signed Distance Field", "Sequential", "|Set|_Position Sequential|Line", "|Set|_Position Sequential|Circle", "|Set|_Position Sequential|Three Dimensional" }},
            new object[] { "set q", "|Set|_Position Sequential|Line", new[] { "Favorites", "Position Shape", "Sequential", "|Set|_Position Sequential|Line", "|Set|_Position Sequential|Circle", "|Set|_Position Sequential|Three Dimensional" }},
            new object[] { "+size *", "|Multiply|_Size (*)", new[] { "Favorites", "Attribute", "|Multiply|_Size (*)", "|Multiply|_Size|Random Uniform (*)", "Attribute from Curve", "|Multiply|_Size|By Speed (*)", "|Multiply|_Size|Over Life (*)", "|Multiply|_Size|Random from Curve (*)", "Attribute from Map", "|Multiply|_Size from Map|3D (*)", "|Multiply|_Size from Map|Index (*)", "|Multiply|_Size from Map|Random (*)", "|Multiply|_Size from Map|Sequential (*)" }},
            new object[] { "sprit", "Flipbook Player (Sprite)", new[] { "Favorites", "FlipBook", "Flipbook Player (Sprite)" }},
            new object[] { "spfm", "|Set|_Position from Map|2D", new[] { "Favorites", "Attribute from Map", "|Set|_Position from Map|2D", "|Set|_Old Position from Map|2D", "|Set|_Target Position from Map|2D", "|Set|_Pivot from Map|2D" }},
        };

        [TestCaseSource(nameof(CheckBlockSearchResultCases))]
        public void CheckBlockSearchResult(string pattern, string expectedSelection, string[] expectedItems)
        {
            // Arrange
            var graph = VFXTestCommon.CreateGraph_And_System();
            var window = VFXViewWindow.GetWindow(graph, true, true);
            window.LoadResource(graph.visualEffectResource);
            var controller = window.graphView.controller;
            var contextController = controller.contexts.Single(x => x.model.contextType == VFXContextType.Update);
            var blockProvider = new VFXBlockProvider(contextController, (v, pos) => { });
            VFXFilterWindow.Show(Vector2.zero, Vector2.zero, blockProvider);
            var vfxFilterWindow = EditorWindow.GetWindow<VFXFilterWindow>();

            // Act
            SetSearchPattern(vfxFilterWindow, pattern);
            var treeviewData = GetTreeData(vfxFilterWindow);

            // Assert
            var searchResults = FlattenTreeviewData(treeviewData);
            CollectionAssert.AreEqual(expectedItems, searchResults.Select(GetReadableDisplayName));
            Assert.AreEqual(expectedSelection, GetReadableDisplayName(GetSelection(vfxFilterWindow)));
        }

        // We cleanup the string because some special characters like # and @ are inserted to handle highlighting in the UI
        private string GetReadableDisplayName(VFXFilterWindow.Descriptor descriptor)
        {
            return removeSpecialCharacters.Replace(descriptor.GetDisplayNameAndSynonym(), string.Empty);
        }

        private IEnumerable<VFXFilterWindow.Descriptor> FlattenTreeviewData(List<TreeViewItemData<VFXFilterWindow.Descriptor>> treeViewItemData)
        {
            foreach (var itemData in treeViewItemData)
            {
                yield return itemData.data;
                if (!itemData.hasChildren)
                    continue;
                foreach (var child in FlattenTreeviewData((List<TreeViewItemData<VFXFilterWindow.Descriptor>>)itemData.children))
                {
                    yield return child;
                }
            }
        }

        private List<TreeViewItemData<VFXFilterWindow.Descriptor>> GetTreeData(VFXFilterWindow filterWindow)
        {
            var treeviewDataFieldInfo = typeof(VFXFilterWindow).GetField("m_TreeviewData", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(treeviewDataFieldInfo, "The private field 'm_TreeviewData' must have been renamed or removed from VFXFilterWindow class");

            var  treeviewData = treeviewDataFieldInfo.GetValue(filterWindow) as List<TreeViewItemData<VFXFilterWindow.Descriptor>>;
            Assert.IsNotNull(treeviewData);

            return treeviewData;
        }

        private VFXFilterWindow.Descriptor GetSelection(VFXFilterWindow filterWindow)
        {
            var treeviewFieldInfo = typeof(VFXFilterWindow).GetField("m_Treeview", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(treeviewFieldInfo, "The private field 'm_Treeview' must have been renamed or removed from VFXFilterWindow class");

            var  treeview = treeviewFieldInfo.GetValue(filterWindow) as TreeView;
            Assert.IsNotNull(treeview);

            var selection = treeview.selectedItem as VFXFilterWindow.Descriptor;
            Assert.IsNotNull(selection);

            return selection;
        }

        private void SetSearchPattern(VFXFilterWindow vfxFilterWindow, string pattern)
        {
            var onSearchChangedMethodInfo = typeof(VFXFilterWindow).GetMethod("OnSearchChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(onSearchChangedMethodInfo, "The private method 'OnSearchChanged' must have been renamed or removed from VFXFilterWindow class");
            onSearchChangedMethodInfo.Invoke(vfxFilterWindow, new object[] { ChangeEvent<string>.GetPooled(null, pattern) });
        }

        private void SetSubvariantSearch(bool enable)
        {
            var settingsAsJson = EditorPrefs.GetString($"{nameof(VFXFilterWindow)}.settings", null);
            var settings = !string.IsNullOrEmpty(settingsAsJson) ? JsonUtility.FromJson<VFXFilterWindow.Settings>(settingsAsJson) : default;
            settings.showSubVariantsInSearchResults = enable;
            var json = JsonUtility.ToJson(settings);
            EditorPrefs.SetString($"{nameof(VFXFilterWindow)}.settings", json);
        }
    }
}
