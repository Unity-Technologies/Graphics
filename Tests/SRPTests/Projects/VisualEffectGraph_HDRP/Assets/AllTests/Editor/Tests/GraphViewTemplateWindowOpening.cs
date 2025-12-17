using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements.TestFramework;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    class GraphViewTemplateWindowOpening : EditorWindowUITestFixture<VFXViewWindow>
    {
        VFXViewController m_Controller;

        public GraphViewTemplateWindowOpening()
        {
            //debugMode = true;
            createWindowFunction = () =>
            {
                var vfxWindow = VFXViewWindow.GetWindow((VisualEffectAsset)null, true);
                return vfxWindow;
            };
        }

        [SetUp]
        public void SetUp()
        {
            GraphViewTemplateWindowHelpers.StopSearchIndexingTasks();
            while (EditorWindow.HasOpenInstances<GraphViewTemplateWindow>())
            {
                EditorWindow.GetWindow<GraphViewTemplateWindow>()?.Close();
            }
            Directory.CreateDirectory(VFXTestCommon.tempBasePath);
            AssetDatabase.Refresh();
        }

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            GraphViewTemplateWindowHelpers.StopSearchIndexingTasks();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            //window.disableInputEvents = false;
            Cleanup();
        }

        [UnityTest]
        public IEnumerator Create_VFX_From_VFXGraph_Editor()
        {
            LoadTempGraph();
            yield return ClickButton(window.rootVisualElement, "create-button", EventModifiers.Control);
            yield return CheckNewVFXIsCreated();
        }

        [UnityTest]
        public IEnumerator Create_VFX_From_VFXGraph_Editor_NoAsset()
        {
            yield return OpenTemplateWindowNoAssetWindow();
            yield return CheckNewVFXIsCreated();
        }

        [UnityTest]
        public IEnumerator Create_VFX_From_VFXGraph_Editor_Cancel()
        {
            LoadTempGraph();
            yield return ClickButton(window.rootVisualElement, "create-button", EventModifiers.Control);

            var templateWindow = EditorWindow.GetWindowDontShow<GraphViewTemplateWindow>();
            Assert.NotNull(templateWindow);
            yield return null;

            // This is to avoid the save file dialog user interaction
            var mockSaveFileDialogHelper = new MockSaveFileDialogHelper(string.Empty);
            Assert.True(GraphViewTemplateWindowHelpers.TrySetSaveFileDialogHelper(templateWindow, mockSaveFileDialogHelper));

            // Select Simple Loop item
            var treeView = GraphViewTemplateWindowHelpers.GetTreeView(templateWindow);
            treeView.selectedIndex = 3;

            // Simulate click on cancel button
            yield return ClickButton(templateWindow.rootVisualElement, "CancelButton");

            Assert.AreEqual(0, mockSaveFileDialogHelper.CallCount);
            Assert.False(EditorWindow.HasOpenInstances<GraphViewTemplateWindow>());
        }


        [UnityTest]
        public IEnumerator Create_VFX_From_Project_Browser()
        {
            yield return CreateWindowFromProjectBrowser();

            yield return CheckNewVFXIsCreated();
        }

        [UnityTest]
        public IEnumerator Create_VFX_From_Inspector()
        {
            yield return CreateWindowFromInspector();

            yield return CheckNewVFXIsCreated();
        }

        [UnityTest]
        public IEnumerator Insert_VFX_Template()
        {
            LoadTempGraph();

            GraphViewTemplateWindowHelpers.SetLastUsedTemplatePref("a8d8823499ff50847aa460cb119c445d");

            Assert.AreEqual(0, m_Controller.contexts.Count());

            // Get template dropdown from the VFX graph toolbar
            yield return ClickButton(window.rootVisualElement, "create-button");

            var templateWindow = EditorWindow.GetWindowDontShow<GraphViewTemplateWindow>();
            Assert.NotNull(templateWindow);

            // This is to avoid the save file dialog user interaction
            var mockSaveFileDialogHelper = new MockSaveFileDialogHelper(string.Empty);
            GraphViewTemplateWindowHelpers.TrySetSaveFileDialogHelper(templateWindow, mockSaveFileDialogHelper);

            // Simulate click on create button
            yield return ClickButton(templateWindow.rootVisualElement, "CreateButton");

            Assert.False(EditorWindow.HasOpenInstances<GraphViewTemplateWindow>());
            Assert.AreEqual(0, mockSaveFileDialogHelper.CallCount);
            Assert.AreEqual(4, m_Controller.contexts.Count());
        }

        [UnityTest]
        public IEnumerator Insert_VFX_Template_Cancel()
        {
            LoadTempGraph();

            // Force selection of the simple loop template
            var templateWindowPrefs = new GraphViewTemplateWindowPrefs { LastUsedTemplateGuid = "a8d8823499ff50847aa460cb119c445d" };
            templateWindowPrefs.SavePrefs(VFXTemplateHelperInternal.VFXGraphToolKey);

            Assert.AreEqual(0, m_Controller.contexts.Count());

            // Get template dropdown from the VFX graph toolbar
            yield return ClickButton(window.rootVisualElement, "create-button");

            var templateWindow = EditorWindow.GetWindowDontShow<GraphViewTemplateWindow>();
            Assert.NotNull(templateWindow);

            // This is to avoid the save file dialog user interaction
            var mockSaveFileDialogHelper = new MockSaveFileDialogHelper(string.Empty);
            GraphViewTemplateWindowHelpers.TrySetSaveFileDialogHelper(templateWindow, mockSaveFileDialogHelper);

            // Simulate click on cancel button
            yield return ClickButton(templateWindow.rootVisualElement, "CancelButton");

            Assert.False(EditorWindow.HasOpenInstances<GraphViewTemplateWindow>());
            Assert.AreEqual(0, m_Controller.contexts.Count());
            Assert.AreEqual(0, mockSaveFileDialogHelper.CallCount);
        }

        void LoadTempGraph()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            m_Controller = VFXViewController.GetController(graph.GetResource(), true);
            window.graphView.controller = m_Controller;
        }

        internal static IEnumerator CheckNewVFXIsCreated()
        {
            // Make sure the project browser is opened
            var projectBrowser = EditorWindow.GetWindow<ProjectBrowser>();

            Assert.True(EditorWindow.HasOpenInstances<GraphViewTemplateWindow>());
            var templateWindow = EditorWindow.GetWindow<GraphViewTemplateWindow>();
            Assert.NotNull(templateWindow);

            // Select the temporary folder in the project browser
            if (!Directory.Exists($"{Application.dataPath}/{VFXTestCommon.tempBasePath}"))
            {
                Directory.CreateDirectory(VFXTestCommon.tempBasePath);
                AssetDatabase.Refresh();
            }

            var temporaryFolderPath = VFXTestCommon.tempBasePath.TrimEnd('/');
            yield return SelectFolderPathInProjectWindow(projectBrowser, temporaryFolderPath);
            var destinationPath = $"{temporaryFolderPath}/New VFX.vfx";
            Debug.Log($"Destination path: {destinationPath}");

            // This is to avoid the save file dialog user interaction
            GraphViewTemplateWindowHelpers.TrySetSaveFileDialogHelper(templateWindow, new MockSaveFileDialogHelper(destinationPath));

            // Simulate click on create button
            yield return ClickButton(templateWindow.rootVisualElement, "CreateButton");
            Assert.False(EditorWindow.HasOpenInstances<GraphViewTemplateWindow>());

            // Move focus to end new file name edition
            var sceneHierarchyWindow = EditorWindow.GetWindow<SceneHierarchyWindow>();
            sceneHierarchyWindow.Focus();
            yield return null;

            Assert.True(projectBrowser.ListArea.GetCurrentVisibleNames().Contains("New VFX"), "Could not find 'New VFX' file in the project browser");
        }

        public static IEnumerator SelectFolderPathInProjectWindow(ProjectBrowser projectBrowser, string folder)
        {
            // Select temporary folder in the project browser
            projectBrowser.Focus();
            yield return null;

            // Find the asset for the temporary folder
            Assert.True(AssetDatabase.IsValidFolder(folder), $"Folder '{folder}' does not exist.");
            var folderAsset = AssetDatabase.LoadMainAssetAtPath(folder);
            Assert.NotNull(folderAsset);

            var showFolderContents = projectBrowser.GetType().GetMethod("ShowFolderContents", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(showFolderContents);
            showFolderContents.Invoke(projectBrowser, new object[] { (EntityId)folderAsset.GetInstanceID(), true });
            yield return null;
        }

        static List<TreeViewItemData<ITemplateDescriptor>> GetTemplateTree(GraphViewTemplateWindow window)
        {
            var templateTreeField = window.GetType().GetField("m_TemplatesTree", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(templateTreeField);
            List<TreeViewItemData<ITemplateDescriptor>> templateTree = templateTreeField.GetValue(window) as List<TreeViewItemData<ITemplateDescriptor>>;
            Assert.NotNull(templateTree);

            return templateTree;
        }

        /*static TreeView GetTreeView(GraphViewTemplateWindow window)
        {
            var treeViewField = window.GetType().GetField("m_ListOfTemplates", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(treeViewField);
            TreeView treeView = treeViewField.GetValue(window) as TreeView;
            Assert.NotNull(treeView);

            return treeView;
        }*/

        static IEnumerator ClickButton(VisualElement buttonParent, string buttonName, EventModifiers modifier = EventModifiers.None)
        {
            foreach (var _ in Enumerable.Range(0, 10)) yield return null;

            var button = buttonParent.Q<Button>(buttonName);
            Assert.NotNull(button);
            button.style.display = DisplayStyle.Flex;
            yield return button.SimulateClick(MouseButton.LeftMouse, modifier);
        }

        IEnumerator CreateWindowFromProjectBrowser()
        {
            VisualEffectAssetEditorUtility.CreateVisualEffectAsset();
            Assert.True(EditorWindow.HasOpenInstances<GraphViewTemplateWindow>());
            var templateWindow = EditorWindow.GetWindowDontShow<GraphViewTemplateWindow>();
            yield return GraphViewTemplateWindowHelpers.WaitUntilTemplatesAreCollected(templateWindow);
        }

        IEnumerator CreateWindowFromInspector()
        {
            var go = new GameObject("Visual Effect");
            var vfxComp = go.AddComponent<VisualEffect>();
            var editor = (AdvancedVisualEffectEditor)Editor.CreateEditor(vfxComp);
            editor.serializedObject.Update();

            // Force selection of the simple loop template
            var templateWindowPrefs = new GraphViewTemplateWindowPrefs { LastUsedTemplateGuid = "a8d8823499ff50847aa460cb119c445d" };
            templateWindowPrefs.SavePrefs(VFXTemplateHelperInternal.VFXGraphToolKey);

            // Simulate click on "new" button
            var createNewVFXMethod = editor.GetType().GetMethod("CreateNewVFX", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(createNewVFXMethod);
            createNewVFXMethod.Invoke(editor, null);

            Assert.True(EditorWindow.HasOpenInstances<GraphViewTemplateWindow>());
            var templateWindow = EditorWindow.GetWindowDontShow<GraphViewTemplateWindow>();
            yield return GraphViewTemplateWindowHelpers.WaitUntilTemplatesAreCollected(templateWindow);
        }

        private IEnumerator OpenTemplateWindowNoAssetWindow()
        {
            var noAssetContainer = window.rootVisualElement.Q<VisualElement>("no-asset");
            var createButton = noAssetContainer?.Q<Button>();
            if (createButton == null)
            {
                Debug.Log(m_Controller.graph.visualEffectResource.asset.name);
                AssetDatabase.DeleteAsset(m_Controller.graph.visualEffectResource.asset.name);
                AssetDatabase.Refresh();
                yield return null;
                createButton = window.rootVisualElement.Q<VisualElement>("no-asset").Q<Button>();
            }

            Assert.NotNull(createButton);
            simulate.FrameUpdate();
            simulate.Click(createButton);
        }

        private void Cleanup()
        {
            AssetDatabase.Refresh();
            VFXTestCommon.DeleteAllTemporaryGraph();
        }
    }
}
