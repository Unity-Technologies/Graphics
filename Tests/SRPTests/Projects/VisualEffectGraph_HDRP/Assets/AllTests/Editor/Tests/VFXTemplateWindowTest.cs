using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Moq;
using NUnit.Framework;

using UnityEditor.PackageManager.UI;
using UnityEditor.VFX.UI;
using UnityEngine;
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

        private static void CreateAndOpenVFX(int i, out string path)
        {
            var graph = VFXTestCommon.CreateGraph_And_System();
            var inlineOperator = ScriptableObject.CreateInstance<VFXInlineOperator>();
            inlineOperator.SetSettingValue("m_Type", (SerializableType)typeof(int));
            inlineOperator.inputSlots[0].value = i;
            graph.AddChild(inlineOperator);
            path = AssetDatabase.GetAssetPath(graph);
            AssetDatabase.ImportAsset(path);

            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
            Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetInstanceID(), 0));
            var window = VFXViewWindow.GetWindow(asset, true);
            window.LoadAsset(asset, null);
            window.Show();
        }

        private static VFXViewWindow GetWindowFromPath(string path)
        {
            var vfx = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
            return VFXViewWindow.GetWindow(vfx);
        }

        [UnityTest, Description("Relative to UUM-41334, cover basic asset replacement.")]
        public IEnumerator VFXView_Open_Three_View_Replace_Two_Content()
        {
            CreateAndOpenVFX(666, out var pathA);
            CreateAndOpenVFX(777, out var pathB);
            CreateAndOpenVFX(888, out var pathC);
            yield return null;

            var content = File.ReadAllBytes(pathA);
            File.WriteAllBytes(pathB, content);
            File.WriteAllBytes(pathC, content);
            AssetDatabase.ImportAsset(pathB);
            AssetDatabase.ImportAsset(pathC);
            yield return null;

            var resources = VFXViewWindow.GetAllWindows()
                .Select(o => o.displayedResource.ToString())
                .ToArray();

            Assert.AreEqual(3, resources.Length);
            Assert.AreEqual(resources.Length, resources.Distinct().Count());

            var pathCResource = GetWindowFromPath(pathC).displayedResource;
            var getWindow = VFXViewWindow.GetWindow(pathCResource);
            Assert.IsNotNull(getWindow);

            //Close now all windows to track potential failure
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());

            yield return null;
        }

        [UnityTest, Description("Relative to UUM-41334, cover create new from an empty window.")]
        public IEnumerator VFXView_Create_From_No_Asset_Reuse_Same_View()
        {
            CreateAndOpenVFX(159, out var path);

            Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count());
            AssetDatabase.DeleteAsset(path);

            //Insure the displayedResource has been correctly updated between "null" and null
            //not really needed but ease debug and more realistic regarding user real usage
            int maxFrame = 16;
            while (!object.ReferenceEquals(VFXViewWindow.GetAllWindows().First().displayedResource, null) && --maxFrame > 0)
            {
                VFXViewWindow.GetAllWindows().First().Focus();
                Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count());
                yield return null;
            }
            Assert.Greater(maxFrame, 0);

            var templateWindow = EditorWindow.GetWindow<VFXTemplateWindow>(true, "Create_From_No_Asset_Reuse_Same_View", false);
            Assert.NotNull(templateWindow);
            var treeView = GetTreeView(templateWindow);
            treeView.selectedIndex = 2;

            var oldWindow = VFXViewWindow.GetAllWindows().First();
            var newPath = $"{VFXTestCommon.tempBasePath}vfx_from_template_{System.Guid.NewGuid()}.vfx";
            var mockSaveFileDialogHelper = new MockSaveFileDialogHelper(newPath);
            SetSaveFileDialogHelper(templateWindow, mockSaveFileDialogHelper);
            CallMethod(templateWindow, "Setup", oldWindow.graphView, /*VFXTemplateWindow.CreateMode.CreateNew*/0, (Action<string>)null);
            CallMethod(templateWindow, "OnCreate");
            Assert.AreEqual(1u, mockSaveFileDialogHelper.callCount);
            yield return null;

            Assert.IsTrue(File.Exists(newPath));
            Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count());

            var newWindow = VFXViewWindow.GetAllWindows().First();
            Assert.AreEqual(oldWindow, newWindow);
            Assert.IsTrue(newWindow.displayedResource != null);
            var openedAsset = AssetDatabase.GetAssetPath(newWindow.displayedResource.asset);
            Assert.AreEqual(newPath, openedAsset);
            yield return null;
        }

        [UnityTest, Description("Repro UUM-41334")]
        public IEnumerator VFXView_Open_Two_View_Delete_Replace_Other_Content()
        {
            Assert.AreEqual(0, VFXViewWindow.GetAllWindows().Count());

            CreateAndOpenVFX(123, out var pathA);
            CreateAndOpenVFX(456, out var pathB);

            Assert.AreNotEqual(pathA, pathB);
            var windowA = GetWindowFromPath(pathA);
            var windowB = GetWindowFromPath(pathB);
            Assert.AreNotEqual(windowA, windowB);
            Assert.IsTrue(windowA.displayedResource != null);
            Assert.IsTrue(windowB.displayedResource != null);

            //Delete ObjectA
            AssetDatabase.DeleteAsset(pathA);
            yield return null;
            windowA = GetWindowFromPath(pathA);
            windowB = GetWindowFromPath(pathB);
            Assert.IsFalse(windowA.displayedResource != null);
            Assert.IsTrue(windowB.displayedResource != null);

            //Insure the displayedResource has been correctly updated between "null" and null (not really needed but ease later debug)
            int maxFrame = 16;
            while (!object.ReferenceEquals(windowA.displayedResource, null) && --maxFrame > 0)
            {
                windowA.Focus();
                yield return null;
            }
            Assert.Greater(maxFrame, 0);

            //windowsA simulate creation of new asset with same path than windowB
            var templateWindow = EditorWindow.GetWindow<VFXTemplateWindow>(true, "Open_Two_View_Delete_Replace_Other_Content", false);
            Assert.NotNull(templateWindow);
            var treeView = GetTreeView(templateWindow);
            treeView.selectedIndex = 2;

            var mockSaveFileDialogHelper = new MockSaveFileDialogHelper(pathB);
            SetSaveFileDialogHelper(templateWindow, mockSaveFileDialogHelper);
            CallMethod(templateWindow, "Setup", windowA.graphView, /*VFXTemplateWindow.CreateMode.CreateNew*/0, (Action<string>)null);
            Assert.IsTrue(windowB.displayedResource != null);
            CallMethod(templateWindow, "OnCreate");
            Assert.AreNotEqual(0, mockSaveFileDialogHelper.callCount);
            yield return null;

            windowA = GetWindowFromPath(pathA);
            windowB = GetWindowFromPath(pathB);

            //We are supposed to switch back to the other single windows
            Assert.IsTrue(File.Exists(pathB));
            Assert.IsTrue(windowB.displayedResource != null);
            Assert.IsFalse(windowA.displayedResource != null);

            //Focus is expected to be have been requested
            maxFrame = 16;
            while (!windowB.hasFocus && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.Greater(maxFrame, 0);

            //Check if expected path still opened
            var displayedPath = AssetDatabase.GetAssetPath(windowB.displayedResource.asset);
            Assert.AreEqual(pathB, displayedPath);

            yield return null;
        }

        [UnityTest, Description("Covers UUM-95871")]
        public IEnumerator Check_Install_Learning_Sample_Button()
        {
            var controller = VFXTestCommon.StartEditTestAsset();
            yield return null;

            // Create a mock package extension that throws to simulate a failing package
            var throwingExtensionMock = new Mock<IPackageManagerExtension>();
            throwingExtensionMock.Setup(x => x.OnPackageSelectionChange(It.IsAny<PackageManager.PackageInfo>())).Throws<NullReferenceException>();

            try
            {
                PackageManagerExtensions.RegisterExtension(throwingExtensionMock.Object);

                // Get template dropdown from the VFX graph toolbar
                var vfxViewWindow = VFXViewWindow.GetWindow(controller.graph, false, true);
                var templateDropDown = vfxViewWindow.rootVisualElement.Q<CreateFromTemplateDropDownButton>();
                Assert.NotNull(templateDropDown);

                // Open the template window
                var onCreateNewMethod = templateDropDown.GetType().GetMethod("OnCreateNew", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(onCreateNewMethod);
                onCreateNewMethod.Invoke(templateDropDown, null);

                Assert.True(EditorWindow.HasOpenInstances<VFXTemplateWindow>());
                var templateWindow = EditorWindow.GetWindow<VFXTemplateWindow>();
                var installButton = templateWindow.rootVisualElement.Q<Button>("InstallButton");
                Assert.NotNull(installButton);
                Assert.True(installButton.enabledSelf);

                // Check the failing extension package has been called otherwise the test does nothing useful
                throwingExtensionMock.Verify(x => x.OnPackageSelectionChange(It.IsAny<PackageManager.PackageInfo>()), Times.Once);
            }
            finally
            {
                PackageManagerExtensions.Extensions.Remove(throwingExtensionMock.Object);
            }
        }

        internal static IEnumerator CheckNewVFXIsCreated(int templateIndex = 3)
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
            treeView.selectedIndex = templateIndex;

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

        private static void SetSaveFileDialogHelper(VFXTemplateWindow window, VFXTemplateWindow.ISaveFileDialogHelper saveFileDialogHelper)
        {
            var saveFileDialogHelperField = window.GetType().GetField("m_SaveFileDialogHelper", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(saveFileDialogHelperField);
            saveFileDialogHelperField.SetValue(window, saveFileDialogHelper);
        }

        internal static List<TreeViewItemData<IVFXTemplateDescriptor>> GetTemplateTree(VFXTemplateWindow window)
        {
            var templateTreeField = window.GetType().GetField("m_TemplatesTree", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(templateTreeField);
            List<TreeViewItemData<IVFXTemplateDescriptor>> templateTree = templateTreeField.GetValue(window) as List<TreeViewItemData<IVFXTemplateDescriptor>>;
            Assert.NotNull(templateTree);

            return templateTree;
        }

        private static TreeView GetTreeView(VFXTemplateWindow window)
        {
            var treeViewField = window.GetType().GetField("m_ListOfTemplates", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(treeViewField);
            TreeView treeView = treeViewField.GetValue(window) as TreeView;
            Assert.NotNull(treeView);

            return treeView;
        }

        private static void CallMethod(VFXTemplateWindow window, string methodName, params object[] paramMethod)
        {
            var method = window.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method.Invoke(window, paramMethod);
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
