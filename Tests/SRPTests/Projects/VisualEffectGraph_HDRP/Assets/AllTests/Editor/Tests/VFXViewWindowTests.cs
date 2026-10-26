using System.Collections;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEditor.UIElements.TestFramework;
using UnityEditor.VFX.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.Test
{
    class VFXViewWindowTests : EditorWindowUITestFixture<VFXViewWindow>
    {
        public VFXViewWindowTests()
        {
            createWindowFunction = () => VFXViewWindow.GetWindow((VFXGraph)null, true);
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
        }

        [OneTimeTearDown]
        public void DestroyTestAssets()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [TearDown]
        public void TearDown()
        {
            if (EditorWindow.HasOpenInstances<GraphViewTemplateWindow>())
            {
                EditorWindow.GetWindow<GraphViewTemplateWindow>().Close();
            }
        }

        [UnityTest]
        public IEnumerator Create_New_ShaderGraph_Output_Context()
        {
            const int kTemplateWindowTimeoutInMs = 5000;

            var graph = VFXTestCommon.MakeTemporaryGraph();
            var viewController = VFXViewController.GetController(graph.GetResource(), true);
            window.graphView.controller = viewController;
            var outputContextDesc = VFXLibrary.GetContexts().First(x => x.modelType == typeof(VFXComposedParticleOutput));
            viewController.AddVFXContext(new Vector2(300, 2000), outputContextDesc.variant);
            viewController.ApplyChanges();
            simulate.FrameUpdate();
            window.graphView.FrameAll();
            simulate.FrameUpdate();

            Debug.Log($"view size: {window.graphView.contentRect.size}");
            var contextUI = window.graphView.Query<VFXContextUI>().First();
            var newButton = contextUI.Q<Button>("NewButton");
            Assert.NotNull(newButton);
            simulate.Click(newButton);

            var isTemplateWindowOpen = EditorWindow.HasOpenInstances<GraphViewTemplateWindow>();
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            while (!isTemplateWindowOpen && sw.ElapsedMilliseconds < kTemplateWindowTimeoutInMs)
            {
                yield return null;
                isTemplateWindowOpen = EditorWindow.HasOpenInstances<GraphViewTemplateWindow>();
            }
            sw.Stop();
            Assert.IsTrue(isTemplateWindowOpen, $"Template window did not open within {sw.ElapsedMilliseconds} ms after clicking the New Button.");

            var templateWindow = EditorWindow.GetWindow<GraphViewTemplateWindow>();

            // The search index may still be building; re-run until all expected templates are collected.
            const int kExpectedTemplateCount = 3;
            const int kCollectTimeoutInMs = 30000;
            var searchField = templateWindow.rootVisualElement.Q<ToolbarSearchField>();
            var collectSw = System.Diagnostics.Stopwatch.StartNew();
            List<TreeViewItemData<ITemplateDescriptor>> templateTree;
            int childCount;
            do
            {
                // Waits for the search to finish without asserting on the count, so we can re-run.
                yield return GraphViewTemplateWindowHelpers.WaitTemplateSearchCompleted(templateWindow);
                templateTree = GraphViewTemplateWindowHelpers.GetTemplateTree(templateWindow);
                childCount = templateTree.Count > 0 ? templateTree[0].children.Count() : 0;
                if (childCount >= kExpectedTemplateCount)
                    break;

                // Re-run the search against the now more-complete index.
                if (searchField != null)
                {
                    searchField.value = " ";
                    simulate.FrameUpdate();
                    searchField.value = string.Empty;
                    simulate.FrameUpdate();
                }
                yield return null;
            }
            while (collectSw.ElapsedMilliseconds < kCollectTimeoutInMs);
            collectSw.Stop();
            simulate.FrameUpdate();

            Assert.AreEqual(1, templateTree.Count);
            Assert.AreEqual(kExpectedTemplateCount, childCount,
                $"Expected {kExpectedTemplateCount} templates but found {childCount} after {collectSw.ElapsedMilliseconds} ms.");
        }
    }
}
