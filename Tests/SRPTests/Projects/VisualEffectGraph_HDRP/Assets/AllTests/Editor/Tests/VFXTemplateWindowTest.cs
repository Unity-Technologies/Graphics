using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXTemplateWindowTest
    {
        private class MockSaveFileDialogHelper : VFXTemplateWindow.ISaveFileDialogHelper
        {
            private readonly string m_ReturnPath;

            public MockSaveFileDialogHelper(string returnPath)
            {
                m_ReturnPath = returnPath;
            }

            public int callCount { get; private set; }

            public string OpenSaveFileDialog(string title, string defaultName, string extension, string message)
            {
                callCount++;
                Assert.AreEqual(string.Empty, title);
                Assert.AreEqual("New VFX", defaultName);
                Assert.AreEqual("vfx", extension);
                return m_ReturnPath;
            }
        }

        [SetUp]
        public void Setup()
        {
            Cleanup();
        }

        [TearDown]
        public void TearDown()
        {
            Cleanup();
        }

        [UnityTest]
        public IEnumerator Create_VFX_TemplateList()
        {
            VisualEffectAssetEditorUtility.CreateVisualEffectAsset();
            yield return null;

            var templateWindow = EditorWindow.GetWindowDontShow<VFXTemplateWindow>();
            Assert.NotNull(templateWindow);
            Assert.AreEqual("Create new VFX Asset", templateWindow.titleContent.text);

            var templateTree = GetTemplateTree(templateWindow);

            // Only the built-in category
            Assert.AreEqual(1, templateTree.Count);
            var builtInCategory = templateTree.First();
            Assert.AreEqual("Default VFX Graph Templates", builtInCategory.data.header);

            // 7 built-in templates
            Assert.AreEqual(7, builtInCategory.children.Count());
            var headers = builtInCategory.children.Select(x => x.data.header).ToArray();
            CollectionAssert.AreEqual(new string[] { "Minimal System", "Simple Loop", "Simple Burst", "Simple Trail", "Head & Trail", "Firework", "Empty VFX"}, headers);
        }

        [UnityTest]
        public IEnumerator Create_VFX_From_Project_Browser()
        {
            VisualEffectAssetEditorUtility.CreateVisualEffectAsset();
            yield return null;

            var enumerator = CheckNewVFXIsCreated();
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        [UnityTest]
        public IEnumerator Create_VFX_From_Inspector()
        {
            VisualEffectAssetEditorUtility.CreateVisualEffectGameObject(new MenuCommand(null));
            yield return null;

            var go = new GameObject("Visual Effect");
            var vfxComp = go.AddComponent<VisualEffect>();
            var editor = (AdvancedVisualEffectEditor)Editor.CreateEditor(vfxComp);
            editor.serializedObject.Update();

            // Simulate click on "new" button
            var createNewVFXMethod = editor.GetType().GetMethod("CreateNewVFX", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(createNewVFXMethod);
            createNewVFXMethod.Invoke(editor, null);
            yield return null;

            Assert.True(EditorWindow.HasOpenInstances<VFXTemplateWindow>());
            var enumerator = CheckNewVFXIsCreated();
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        [UnityTest]
        public IEnumerator Create_VFX_From_VFXGraph_Editor()
        {
            var controller = VFXTestCommon.StartEditTestAsset();
            yield return null;

            // Get template dropdown from the VFX graph toolbar
            var vfxViewWindow = VFXViewWindow.GetWindow(controller.graph, false, true);
            var templateDropDown = vfxViewWindow.rootVisualElement.Q<CreateFromTemplateDropDownButton>();
            Assert.NotNull(templateDropDown);

            var onCreateNewMethod = templateDropDown.GetType().GetMethod("OnCreateNew", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(onCreateNewMethod);
            onCreateNewMethod.Invoke(templateDropDown, null);

            var enumerator = CheckNewVFXIsCreated();
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        [UnityTest]
        public IEnumerator Create_VFX_From_VFXGraph_Editor_NoAsset()
        {
            VFXViewWindow.ShowWindow();
            yield return null;

            Assert.True(EditorWindow.HasOpenInstances<VFXViewWindow>());
            var vfxViewWindow = EditorWindow.GetWindowDontShow<VFXViewWindow>();

            var onCreateAssetMethod = vfxViewWindow.graphView.GetType().GetMethod("OnCreateAsset", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(onCreateAssetMethod);
            onCreateAssetMethod.Invoke(vfxViewWindow.graphView, null);
            yield return null;

            var enumerator = CheckNewVFXIsCreated();
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }

        [UnityTest]
        public IEnumerator Create_VFX_From_VFXGraph_Editor_Cancel()
        {
            var controller = VFXTestCommon.StartEditTestAsset();
            yield return null;

            // Get template dropdown from the VFX graph toolbar
            var vfxViewWindow = VFXViewWindow.GetWindow(controller.graph, false, true);
            var templateDropDown = vfxViewWindow.rootVisualElement.Q<CreateFromTemplateDropDownButton>();
            Assert.NotNull(templateDropDown);

            var onCreateMethod = templateDropDown.GetType().GetMethod("OnCreateNew", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(onCreateMethod);
            onCreateMethod.Invoke(templateDropDown, null);
            yield return null;

            var templateWindow = EditorWindow.GetWindowDontShow<VFXTemplateWindow>();
            Assert.NotNull(templateWindow);

            // This is to avoid the save file dialog user interaction
            var mockSaveFileDialogHelper = new MockSaveFileDialogHelper(string.Empty);
            SetSaveFileDialogHelper(templateWindow, mockSaveFileDialogHelper);

            // Select Simple Loop item
            var treeView = GetTreeView(templateWindow);
            treeView.selectedIndex = 3;

            // Simulate click on create button
            CallMethod(templateWindow, "OnCancel");
            yield return null;

            Assert.AreEqual(0, mockSaveFileDialogHelper.callCount);
            Assert.False(EditorWindow.HasOpenInstances<VFXTemplateWindow>());
        }

        [UnityTest]
        public IEnumerator Insert_VFX_Template()
        {
            var controller = VFXTestCommon.StartEditTestAsset();
            yield return null;

            Assert.AreEqual(0, controller.contexts.Count());

            // Get template dropdown from the VFX graph toolbar
            var vfxViewWindow = VFXViewWindow.GetWindow(controller.graph, false, true);
            var templateDropDown = vfxViewWindow.rootVisualElement.Q<CreateFromTemplateDropDownButton>();
            Assert.NotNull(templateDropDown);

            var onInsertMethod = templateDropDown.GetType().GetMethod("OnInsert", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(onInsertMethod);
            onInsertMethod.Invoke(templateDropDown, null);
            yield return null;

            var templateWindow = EditorWindow.GetWindowDontShow<VFXTemplateWindow>();
            Assert.NotNull(templateWindow);

            // Select Simple Loop item
            var treeView = GetTreeView(templateWindow);
            treeView.selectedIndex = 3;

            // Simulate click on create button
            CallMethod(templateWindow, "OnCreate");
            yield return null;

            Assert.False(EditorWindow.HasOpenInstances<VFXTemplateWindow>());
            Assert.AreEqual(4, controller.contexts.Count());
        }

        [UnityTest]
        public IEnumerator Insert_VFX_Template_Cancel()
        {
            var controller = VFXTestCommon.StartEditTestAsset();
            yield return null;

            Assert.AreEqual(0, controller.contexts.Count());

            // Get template dropdown from the VFX graph toolbar
            var vfxViewWindow = VFXViewWindow.GetWindow(controller.graph, false, true);
            var templateDropDown = vfxViewWindow.rootVisualElement.Q<CreateFromTemplateDropDownButton>();
            Assert.NotNull(templateDropDown);

            var onInsertMethod = templateDropDown.GetType().GetMethod("OnInsert", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(onInsertMethod);
            onInsertMethod.Invoke(templateDropDown, null);
            yield return null;

            var templateWindow = EditorWindow.GetWindowDontShow<VFXTemplateWindow>();
            Assert.NotNull(templateWindow);

            // Select Simple Loop item
            var treeView = GetTreeView(templateWindow);
            treeView.selectedIndex = 3;

            // Simulate click on cancel button
            CallMethod(templateWindow, "OnCancel");
            yield return null;

            Assert.False(EditorWindow.HasOpenInstances<VFXTemplateWindow>());
            Assert.AreEqual(0, controller.contexts.Count());
        }


        private IEnumerator CheckNewVFXIsCreated()
        {
            // Make sure the project browser is opened
            var projectBrowser = EditorWindow.GetWindow<ProjectBrowser>();

            var templateWindow = EditorWindow.GetWindowDontShow<VFXTemplateWindow>();
            Assert.NotNull(templateWindow);

            // This is to avoid the save file dialog user interaction
            var destinationPath = "Assets/New VFX.vfx";
            SetSaveFileDialogHelper(templateWindow, new MockSaveFileDialogHelper(destinationPath));

            var treeView = GetTreeView(templateWindow);

            // Select Simple Loop item
            treeView.selectedIndex = 3;

            // Simulate click on create button
            CallMethod(templateWindow, "OnCreate");
            yield return null;
            Assert.False(EditorWindow.HasOpenInstances<VFXTemplateWindow>());

            // Move focus to end new file name edition
            var sceneHierarchyWindow = EditorWindow.GetWindow<SceneHierarchyWindow>();
            sceneHierarchyWindow.Focus();
            yield return null;

            Assert.True(projectBrowser.ListArea.GetCurrentVisibleNames().Contains("New VFX"), "Could not find 'New VFX' file in the project browser");
            yield break;
        }

        private void SetSaveFileDialogHelper(VFXTemplateWindow window, VFXTemplateWindow.ISaveFileDialogHelper saveFileDialogHelper)
        {
            var saveFileDialogHelperField = window.GetType().GetField("m_SaveFileDialogHelper", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(saveFileDialogHelperField);
            saveFileDialogHelperField.SetValue(window, saveFileDialogHelper);
        }

        private List<TreeViewItemData<IVFXTemplateDescriptor>> GetTemplateTree(VFXTemplateWindow window)
        {
            var templateTreeField = window.GetType().GetField("m_TemplatesTree", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(templateTreeField);
            List<TreeViewItemData<IVFXTemplateDescriptor>> templateTree = templateTreeField.GetValue(window) as List<TreeViewItemData<IVFXTemplateDescriptor>>;
            Assert.NotNull(templateTree);

            return templateTree;
        }

        private TreeView GetTreeView(VFXTemplateWindow window)
        {
            var treeViewField = window.GetType().GetField("m_ListOfTemplates", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(treeViewField);
            TreeView treeView = treeViewField.GetValue(window) as TreeView;
            Assert.NotNull(treeView);

            return treeView;
        }

        private void CallMethod(VFXTemplateWindow window, string methodName)
        {
            var method = window.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method.Invoke(window, null);
        }

        private void Cleanup()
        {
            var defaultFilePath = "Assets/New VFX.vfx";
            if (File.Exists(defaultFilePath))
            {
                AssetDatabase.DeleteAsset(defaultFilePath);
            }
            AssetDatabase.Refresh();
            if (EditorWindow.HasOpenInstances<VFXTemplateWindow>())
            {
                EditorWindow.GetWindow<VFXTemplateWindow>()?.Close();
            }
            VFXTestCommon.DeleteAllTemporaryGraph();
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
        }
    }
}
