using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using UnityEditor.Experimental.GraphView;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.UIElements.TestFramework;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    class GraphViewTemplateWindowNonRegression
    {
        [SetUp]
        public void SetUp()
        {
            EventHelpers.TestSetUp();
            Cleanup();
        }

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            GraphViewTemplateWindowHelpers.SetLastUsedTemplatePref();
            GraphViewTemplateWindowHelpers.StopSearchIndexingTasks();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            EventHelpers.TestTearDown();
            Cleanup();
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
            while (!ReferenceEquals(VFXViewWindow.GetAllWindows().First().displayedResource, null) && --maxFrame > 0)
            {
                VFXViewWindow.GetAllWindows().First().Focus();
                Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count());
                yield return null;
            }
            Assert.Greater(maxFrame, 0);

            // Find the "Create new Visual Effect Graph" button and click it
            var window = VFXViewWindow.GetAllWindows().First();

            GraphViewTemplateWindowHelpers.StopSearchIndexingTasks();
            yield return OpenTemplateWindowNoAssetWindow(window);
            var templateWindow = EditorWindow.GetWindowDontShow<GraphViewTemplateWindow>();
            var treeView = GraphViewTemplateWindowHelpers.GetTreeView(templateWindow);
            treeView.selectedIndex = 2;

            var newPath = $"{VFXTestCommon.tempBasePath}vfx_from_template_{Guid.NewGuid()}.vfx";
            var mockSaveFileDialogHelper = new MockSaveFileDialogHelper(newPath);

            var remainingAttempts = 10;
            while (!TrySetSaveFileDialogHelper(templateWindow, mockSaveFileDialogHelper) && remainingAttempts > 0)
            {
                remainingAttempts--;
                yield return null;
            }

            yield return ClickButton(templateWindow.rootVisualElement, "CreateButton");
            Assert.AreEqual(1u, mockSaveFileDialogHelper.CallCount);
            yield return null;

            Assert.IsTrue(File.Exists(newPath));
            Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count());

            Assert.IsTrue(window.displayedResource != null);
            var openedAsset = AssetDatabase.GetAssetPath(window.displayedResource.asset);
            Assert.AreEqual(newPath, openedAsset);
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
            yield return null;

            //Delete ObjectA
            AssetDatabase.DeleteAsset(pathA);
            yield return null;
            windowA = GetWindowFromPath(pathA);
            windowB = GetWindowFromPath(pathB);
            Assert.IsFalse(windowA.displayedResource != null);
            Assert.IsTrue(windowB.displayedResource != null);

            //Insure the displayedResource has been correctly updated between "null" and null (not really needed but ease later debug)
            int maxFrame = 16;
            while (!ReferenceEquals(windowA.displayedResource, null) && --maxFrame > 0)
            {
                windowA.Focus();
                yield return null;
            }
            Assert.Greater(maxFrame, 0);

            //windowsA simulate creation of new asset with same path than windowB
            yield return OpenTemplateWindowNoAssetWindow(windowA);
            var templateWindow = EditorWindow.GetWindowDontShow<GraphViewTemplateWindow>();
            var treeView = GraphViewTemplateWindowHelpers.GetTreeView(templateWindow);
            treeView.selectedIndex = 2;

            var mockSaveFileDialogHelper = new MockSaveFileDialogHelper(pathB);
            Assert.True(TrySetSaveFileDialogHelper(templateWindow, mockSaveFileDialogHelper));
            Assert.IsTrue(windowB.displayedResource != null);
            yield return ClickButton(templateWindow.rootVisualElement, "CreateButton");
            Assert.AreEqual(1, mockSaveFileDialogHelper.CallCount);

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
        }

        static bool TrySetSaveFileDialogHelper(GraphViewTemplateWindow window, GraphViewTemplateWindow.ISaveFileDialogHelper saveFileDialogHelper)
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

        void Cleanup()
        {
            if (EditorWindow.HasOpenInstances<GraphViewTemplateWindow>())
            {
                EditorWindow.GetWindow<GraphViewTemplateWindow>()?.Close();
            }
            VFXTestCommon.DeleteAllTemporaryGraph();
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
        }

        static IEnumerator ClickButton(VisualElement buttonParent, string buttonName)
        {
            foreach (var _ in Enumerable.Range(0, 10)) yield return null;

            var button = buttonParent.Q<Button>(buttonName);
            Assert.NotNull(button);
            Assert.AreEqual(DisplayStyle.Flex, button.resolvedStyle.display, $"Cannot click on the button {buttonName} if not displayed");
            Assert.IsTrue(button.enabledInHierarchy, $"Cannot click on the button {buttonName} if not enabled");
            yield return button.SimulateClick();
        }

        static IEnumerator OpenTemplateWindowNoAssetWindow(VFXViewWindow window)
        {
            yield return ClickButton(window.rootVisualElement.Q<VisualElement>("no-asset"), null);
            Assert.IsTrue(EditorWindow.HasOpenInstances<GraphViewTemplateWindow>());
            var templateWindow = EditorWindow.GetWindow<GraphViewTemplateWindow>();
            yield return GraphViewTemplateWindowHelpers.WaitUntilTemplatesAreCollected(templateWindow);
        }

        static VFXViewWindow GetWindowFromPath(string path)
        {
            var vfx = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(path);
            return VFXViewWindow.GetWindow(vfx);
        }

        static void CreateAndOpenVFX(int i, out string path)
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
    }
}
