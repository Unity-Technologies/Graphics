using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Moq;
using NUnit.Framework;

using UnityEditor.Experimental.GraphView;
using UnityEditor.PackageManager.UI;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEditor.UIElements.TestFramework;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;

namespace UnityEditor.VFX.Test
{
    class MockSaveFileDialogHelper : GraphViewTemplateWindow.ISaveFileDialogHelper
    {
        readonly string m_ReturnPath;

        public MockSaveFileDialogHelper(string returnPath)
        {
            m_ReturnPath = returnPath;
        }

        public int CallCount { get; private set; }

        public string OpenSaveFileDialog()
        {
            CallCount++;
            return m_ReturnPath;
        }
    }

    static class GraphViewTemplateWindowHelpers
    {
        const float kTimeout = 30_000f; // 30 seconds

        public static void StopSearchIndexingTasks()
        {
            for (var index = 0; index < Progress.GetCount(); index++)
            {
                var id = Progress.GetId(index);
                var taskName = Progress.GetName(id);
                if (taskName.Contains("search", StringComparison.InvariantCultureIgnoreCase))
                {
                    Progress.Cancel(id);
                }
            }
        }

        public static IEnumerator WaitTemplateSearchCompleted(GraphViewTemplateWindow templateWindow, bool stopIndexing = true)
        {
            var searchProviderField = templateWindow.GetType().GetField("m_SearchProvider", BindingFlags.Instance | BindingFlags.NonPublic);
            var searchProvider = (TemplateSearchProvider)searchProviderField?.GetValue(templateWindow);
            Assert.NotNull(searchProvider, "Search provider should not be null.");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (searchProvider.IsSearching && sw.ElapsedMilliseconds < kTimeout)
            {
                yield return null;
            }
            sw.Stop();
            Assert.IsFalse(searchProvider.IsSearching, $"Search indexing did not end after {sw.ElapsedMilliseconds} ms.");
            Debug.Log($"Waited {sw.ElapsedMilliseconds} ms for search to complete.");
        }

        public static IEnumerator WaitUntilTemplatesAreCollected(GraphViewTemplateWindow templateWindow)
        {
            yield return WaitTemplateSearchCompleted(templateWindow);

            var templateTree = GetTemplateTree(templateWindow);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (templateTree.Count == 0 && sw.ElapsedMilliseconds < kTimeout)
            {
                yield return null;
            }
            sw.Stop();
            Assert.Greater(templateTree.Count, 0, $"No templates found in the template window after {sw.ElapsedMilliseconds} ms.");
            Debug.Log($"Waited {sw.ElapsedMilliseconds} ms for templates to be collected.");
        }

        public static bool TrySetSaveFileDialogHelper(GraphViewTemplateWindow window, GraphViewTemplateWindow.ISaveFileDialogHelper saveFileDialogHelper)
        {
            var templateHelperField = window.GetType().GetField("m_TemplateHelper", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(templateHelperField);
            var templateHelper = (ITemplateHelper)templateHelperField.GetValue(window);
            if (templateHelper != null)
            {
                templateHelper.saveFileDialogHelper = saveFileDialogHelper;
                return true;
            }

            return false;
        }

        public static void SetLastUsedTemplatePref(string guid = "a8d8823499ff50847aa460cb119c445d")
        {
            // Force selection of the simple loop template
            var templateWindowPrefs = new GraphViewTemplateWindowPrefs { LastUsedTemplateGuid = guid };
            templateWindowPrefs.SavePrefs(VFXTemplateHelperInternal.VFXGraphToolKey);
        }

        public static void SortBy(GraphViewTemplateWindow window, string sortBy)
        {
            var sortDropdown = window.rootVisualElement.Q<DropdownField>();
            Assert.NotNull(sortDropdown);
            sortDropdown.value = sortBy;
        }

        public static TreeView GetTreeView(GraphViewTemplateWindow window)
        {
            var treeViewField = window.GetType().GetField("m_ListOfTemplates", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(treeViewField);
            TreeView treeView = treeViewField.GetValue(window) as TreeView;
            Assert.NotNull(treeView);

            return treeView;
        }

        public static void ClearFavorite(List<TreeViewItemData<ITemplateDescriptor>> items)
        {
            items.ForEach(x =>
            {
                var id = GraphViewTemplateWindow.GetGlobalId((GraphViewTemplateDescriptor)x.data);
                SearchSettings.RemoveItemFavorite(new SearchItem(id));
            });
            SearchSettings.Save();
        }

        public static List<TreeViewItemData<ITemplateDescriptor>> GetTemplateTree(GraphViewTemplateWindow window)
        {
            var templateTreeField = window.GetType().GetField("m_TemplatesTree", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(templateTreeField);
            List<TreeViewItemData<ITemplateDescriptor>> templateTree = templateTreeField.GetValue(window) as List<TreeViewItemData<ITemplateDescriptor>>;
            Assert.NotNull(templateTree);

            return templateTree;
        }
    }

    [TestFixture]
    class GraphViewTemplateWindowTest : EditorWindowUITestFixture<GraphViewTemplateWindow>
    {
        static readonly string k_SampleExpectedPath = "Assets/Samples";
        static readonly string k_AltSamplePackageName = "Visual Effect Graph Additions";

        readonly VFXTemplateHelperInternal m_vfxTemplateHelper = new();
        readonly Mock<ITemplateHelper> m_templateHelperMock;

        List<TreeViewItemData<ITemplateDescriptor>> m_TemplateTree;
        ToolbarSearchField m_SearchField;

        private delegate bool TryGetTemplateDelegate(string guid, out GraphViewTemplateDescriptor descriptor);

        public GraphViewTemplateWindowTest()
        {
            m_templateHelperMock = new Mock<ITemplateHelper>();
            m_templateHelperMock.SetupGet(x => x.assetType).Returns(m_vfxTemplateHelper.assetType);
            m_templateHelperMock.SetupGet(x => x.toolKey).Returns(m_vfxTemplateHelper.toolKey);
            m_templateHelperMock.SetupGet(x => x.packageInfoName).Returns(m_vfxTemplateHelper.packageInfoName);
            m_templateHelperMock.SetupGet(x => x.emptyTemplateGuid).Returns(m_vfxTemplateHelper.emptyTemplateGuid);
            m_templateHelperMock.SetupGet(x => x.builtInCategory).Returns(m_vfxTemplateHelper.builtInCategory);
            m_templateHelperMock.SetupGet(x => x.builtInTemplatePath).Returns(m_vfxTemplateHelper.builtInTemplatePath);
            m_templateHelperMock.SetupGet(x => x.createNewAssetTitle).Returns(m_vfxTemplateHelper.createNewAssetTitle);
            m_templateHelperMock.SetupGet(x => x.learningSampleName).Returns(k_AltSamplePackageName);
            m_templateHelperMock
                .Setup(x => x.RaiseImportSampleDependencies(It.IsAny<PackageManager.PackageInfo>(), It.IsAny<Sample>()))
                .Callback((PackageManager.PackageInfo pkg, Sample sample) => m_vfxTemplateHelper.RaiseImportSampleDependencies(pkg, sample));
            m_templateHelperMock
                .Setup(x => x.TryGetTemplate(It.IsAny<string>(), out It.Ref<GraphViewTemplateDescriptor>.IsAny))
                .Returns(new TryGetTemplateDelegate((string guid, out GraphViewTemplateDescriptor descriptor) => m_vfxTemplateHelper.TryGetTemplate(guid, out descriptor)));

            createWindowFunction = () =>
            {
                GraphViewTemplateWindow.ShowCreateFromTemplateAdbOnly(m_templateHelperMock.Object, null, false);
                return EditorWindow.GetWindowDontShow<GraphViewTemplateWindow>();
            };
        }

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            AssetDatabase.DeleteAsset(k_SampleExpectedPath);
            GraphViewTemplateWindowHelpers.StopSearchIndexingTasks();
            m_TemplateTree = GraphViewTemplateWindowHelpers.GetTemplateTree(window);

            m_SearchField = window.rootVisualElement.Q<ToolbarSearchField>();
            Assert.NotNull(m_SearchField);
            m_SearchField.value = string.Empty;
            yield return null;
            GraphViewTemplateWindowHelpers.SortBy(window, "Name");
            yield return null;
            yield return GraphViewTemplateWindowHelpers.WaitUntilTemplatesAreCollected(window);
        }

        [Test]
        public void Favorite_A_VFX_Template()
        {
            var allItems = m_TemplateTree.SelectMany(x => x.children).ToList();
            var lastItem = allItems.Last();
            Debug.Log($"Last item: {lastItem.data.header}");

            GraphViewTemplateWindowHelpers.ClearFavorite(allItems);

            // Click in the favorites button
            var treeview = GraphViewTemplateWindowHelpers.GetTreeView(window);
            var lastItemRoot = treeview.GetRootElementForId(lastItem.id);
            var id = GraphViewTemplateWindow.GetGlobalId((GraphViewTemplateDescriptor)lastItem.data);
            Assert.False(GraphViewTemplateWindow.IsFavorite(id));

            var button = lastItemRoot.Q<Button>("Favorite");
            simulate.FrameUpdate();
            simulate.MouseMove(button.worldBound.center, button.worldBound.center + Vector2.up);
            simulate.FrameUpdate();
            simulate.Click(button);

            Assert.True(GraphViewTemplateWindow.IsFavorite(id));
        }

        [UnityTest]
        public IEnumerator Check_Default_Templates()
        {
            Assert.AreEqual("Create new VFX Asset", window.titleContent.text);
            yield return null;

            // Only the built-in category
            Assert.AreEqual(1, m_TemplateTree.Count);
            var builtInCategory = m_TemplateTree.First();
            Assert.AreEqual("Default VFX Graph Templates", builtInCategory.data.header);

            // 7 built-in templates
            Assert.AreEqual(7, builtInCategory.children.Count());
            var headers = builtInCategory.children.Select(x => x.data.header).ToArray();
            CollectionAssert.AreEqual(new[] { "Empty VFX", "Firework", "Head & Trail", "Minimal System", "Simple Burst", "Simple Loop", "Simple Trail" }, headers);
        }

        [UnityTest]
        public IEnumerator Search_VFX_Template()
        {
            m_SearchField.value = "*adbonly* Simple";

            // Wait for the search service to operate
            yield return GraphViewTemplateWindowHelpers.WaitTemplateSearchCompleted(window);
            yield return null;

            var items = m_TemplateTree.SelectMany(x => x.children).Select(x => x.data.header).ToArray();
            CollectionAssert.AreEqual(new[] { "Simple Burst", "Simple Loop", "Simple Trail" }, items);
        }

        [Test]
        public void Sort_Template_By_Favorite()
        {
            var allItems = m_TemplateTree.SelectMany(x => x.children).ToList();
            var lastItem = allItems.Last();
            var lastItemName = lastItem.data.header;
            GraphViewTemplateWindowHelpers.ClearFavorite(allItems);

            // Put the last item in favorites (should use the button, but it's not working right now)
            var id = GraphViewTemplateWindow.GetGlobalId((GraphViewTemplateDescriptor)lastItem.data);
            SearchSettings.AddItemFavorite(new SearchItem(id));
            SearchSettings.Save();

            GraphViewTemplateWindowHelpers.SortBy(window, "Favorite");
            allItems = m_TemplateTree.SelectMany(x => x.children).ToList();
            Assert.AreEqual(lastItemName, allItems[0].data.header);
        }

        [UnityTest]
        public IEnumerator Search_No_Result()
        {
            m_SearchField.value = "xx##yy"; // A string matching no existing template
            simulate.FrameUpdate();

            yield return GraphViewTemplateWindowHelpers.WaitTemplateSearchCompleted(window, false);
            yield return null;

            var emptyResultLabel = window.rootVisualElement.Q<Label>("EmptyResults");
            Assert.AreEqual(DisplayStyle.Flex, emptyResultLabel.resolvedStyle.display);
        }

        [Test, Description("Covers UUM-95871")]
        public void Check_Install_Learning_Sample_Button()
        {
            // Create a mock package extension that throws to simulate a failing package
            var throwingExtensionMock = new Mock<IPackageManagerExtension>();
            throwingExtensionMock.Setup(x => x.OnPackageSelectionChange(It.IsAny<PackageManager.PackageInfo>())).Throws<NullReferenceException>();

            try
            {
                PackageManagerExtensions.RegisterExtension(throwingExtensionMock.Object);

                RecreatePanel();

                var installButton = window.rootVisualElement.Q<Button>("InstallButton");
                Assert.True(installButton.enabledSelf);
                simulate.FrameUpdate();

                //Behavior changed since UUM-121936, we aren't relying on OnPackageSelectionChange anymore
                throwingExtensionMock.Verify(x => x.OnPackageSelectionChange(It.IsAny<PackageManager.PackageInfo>()), Times.Never);
            }
            finally
            {
                PackageManagerExtensions.Extensions.Remove(throwingExtensionMock.Object);
            }
        }

        [UnityTest, Description("Covers UUM-121936")]
        public IEnumerator Check_Install_Dependencies_Learning_Sample_Button()
        {
            var installButton = window.rootVisualElement.Q<Button>("InstallButton");
            Assert.NotNull(installButton);

            m_templateHelperMock.Verify(x => x.RaiseImportSampleDependencies(It.IsAny<PackageManager.PackageInfo>(), It.IsAny<Sample>()), Times.Never);
            Assert.IsFalse(Directory.Exists(k_SampleExpectedPath), $"The directory {k_SampleExpectedPath} should not exist before installing the sample.");
            yield return ClickButton(window.rootVisualElement, "InstallButton");

            Assert.IsTrue(Directory.Exists(k_SampleExpectedPath), $"Fail to find: {k_SampleExpectedPath}");
            yield return null;

            Assert.True(EditorWindow.HasOpenInstances<GraphViewTemplateWindow>()); //Install template doesn't trigger Close
            m_templateHelperMock.Verify(x => x.RaiseImportSampleDependencies(It.IsAny<PackageManager.PackageInfo>(), It.IsAny<Sample>()), Times.Once);

            AssetDatabase.DeleteAsset(k_SampleExpectedPath);
        }

        internal static IEnumerator CheckNewVFXIsCreated(int templateIndex = 3)
        {
            // Make sure the project browser is opened
            var projectBrowser = EditorWindow.GetWindow<ProjectBrowser>();

            Assert.True(EditorWindow.HasOpenInstances<GraphViewTemplateWindow>());
            var templateWindow = EditorWindow.GetWindow<GraphViewTemplateWindow>();
            Assert.NotNull(templateWindow);

            // This is to avoid the save file dialog user interaction
            var destinationPath = "Assets/New VFX.vfx";
            TrySetSaveFileDialogHelper(templateWindow, new MockSaveFileDialogHelper(destinationPath));

            // Select Simple Loop item
            var treeView = GetTreeView(templateWindow);
            treeView.selectedIndex = templateIndex;

            // Simulate click on create button
            yield return ClickButton(templateWindow.rootVisualElement, "CreateButton");
            Assert.False(EditorWindow.HasOpenInstances<GraphViewTemplateWindow>());

            // Move focus to end new file name edition
            var sceneHierarchyWindow = EditorWindow.GetWindow<SceneHierarchyWindow>();
            sceneHierarchyWindow.Focus();
            yield return null;

            Assert.True(projectBrowser.ListArea.GetCurrentVisibleNames().Contains("New VFX"), "Could not find 'New VFX' file in the project browser");
        }

        private static bool TrySetSaveFileDialogHelper(GraphViewTemplateWindow window, GraphViewTemplateWindow.ISaveFileDialogHelper saveFileDialogHelper)
        {
            var templateHelperField = window.GetType().GetField("m_TemplateHelper", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(templateHelperField);
            var templateHelper = (ITemplateHelper)templateHelperField.GetValue(window);
            if (templateHelper != null)
            {
                templateHelper.saveFileDialogHelper = saveFileDialogHelper;
                return true;
            }

            return false;
        }

        static List<TreeViewItemData<ITemplateDescriptor>> GetTemplateTree(GraphViewTemplateWindow window)
        {
            var templateTreeField = window.GetType().GetField("m_TemplatesTree", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(templateTreeField);
            List<TreeViewItemData<ITemplateDescriptor>> templateTree = templateTreeField.GetValue(window) as List<TreeViewItemData<ITemplateDescriptor>>;
            Assert.NotNull(templateTree);

            return templateTree;
        }

        private static TreeView GetTreeView(GraphViewTemplateWindow window)
        {
            var treeViewField = window.GetType().GetField("m_ListOfTemplates", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(treeViewField);
            TreeView treeView = treeViewField.GetValue(window) as TreeView;
            Assert.NotNull(treeView);

            return treeView;
        }

        private static IEnumerator ClickButton(VisualElement buttonParent, string buttonName)
        {
            foreach (var x in Enumerable.Range(0, 10)) yield return null;

            var button = buttonParent.Q<Button>(buttonName);
            Assert.NotNull(button);
            yield return button.SimulateClick();
        }

        private static IEnumerator OpenTemplateWindowFromDropDown(VFXViewWindow window, string methodName)
        {
            foreach (var x in Enumerable.Range(0, 10)) yield return null;

            var templateDropDown = window.rootVisualElement.Q<CreateFromTemplateDropDownButton>();
            Assert.NotNull(templateDropDown);

            var onCreateNewMethod = templateDropDown.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(onCreateNewMethod);
            onCreateNewMethod.Invoke(templateDropDown, null);
        }

        private static IEnumerator OpenTemplateWindowNoAssetWindow(VFXViewWindow window)
        {
            foreach (var x in Enumerable.Range(0, 10)) yield return null;

            var createButton = window.rootVisualElement.Q<VisualElement>("no-asset").Q<Button>();
            Assert.NotNull(createButton);

            yield return createButton.SimulateClick();
        }

        private void Cleanup()
        {
            var defaultFilePath = "Assets/New VFX.vfx";
            if (File.Exists(defaultFilePath))
            {
                AssetDatabase.DeleteAsset(defaultFilePath);
            }
            AssetDatabase.Refresh();
            if (EditorWindow.HasOpenInstances<GraphViewTemplateWindow>())
            {
                EditorWindow.GetWindow<GraphViewTemplateWindow>()?.Close();
            }
            VFXTestCommon.DeleteAllTemporaryGraph();
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());

            if (Directory.Exists(k_SampleExpectedPath))
                AssetDatabase.DeleteAsset(k_SampleExpectedPath);
        }
    }
}
