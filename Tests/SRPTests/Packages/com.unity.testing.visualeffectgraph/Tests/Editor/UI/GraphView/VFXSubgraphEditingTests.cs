using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VFX.UI;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using UnityEditor.VFX.Block;
using UnityEngine.UIElements.TestFramework;

namespace UnityEditor.VFX.Test.UI.GraphView
{
    [TestFixture]
    public class VFXSubgraphEditingTests
    {
        [OneTimeTearDown]
        public void DestroyTestAssets()
        {
            VFXTestCommon.CloseAllVFXWindow();
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [SetUp]
        public void SetUp()
        {
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
            VFXTestCommon.CloseAllUnecessaryWindows();
            EventHelpers.TestSetUp();
        }

        [TearDown]
        public void TearDown()
        {
            EventHelpers.TestTearDown();
            if (EditorWindow.HasOpenInstances<GraphViewTemplateWindow>())
            {
                EditorWindow.GetWindow<GraphViewTemplateWindow>().Close();
            }
        }

        const string TempDirectoryName = VFXGraphViewTestGraphFactory.TempDirectoryName;

        [UnityTest]
        [Description("When a subgraph is entered in-place but that subgraph is already opened, then current tab is left unchanged and the tab with that subgraph is focused")]
        public IEnumerator Open_Subgraph_In_Same_Tab_When_Its_Already_Opened()
        {
            // Prepare
            VFXTestCommon.CloseAllVFXWindow();
            var window = VFXGraphViewTestGraphFactory.CreateSimpleVFXGraph();
            var graphTitle = window.titleContent.text;

            yield return null;

            // Get Set Lifetime node and convert it to subgraph block
            var controllers = GetBlocks(window, "Set".Label(false).AppendLiteral("Lifetime")).Take(1);
            var subgraphFileName = TempDirectoryName + $"/subgraph_{GUID.Generate()}.vfxblock";
            VFXConvertSubgraph.ConvertToSubgraphBlock(window.graphView, controllers, Rect.zero, subgraphFileName);

            // Open created subgraph
            AssetDatabase.ImportAsset(subgraphFileName);
            var vfx = AssetDatabase.LoadAssetAtPath<VisualEffectSubgraph>(subgraphFileName);
            var subgraphWindow = VFXTestCommon.GetWindow(vfx.GetResource(), true);

            yield return null;

            // Get the subgraph block model and enter inside, it should not open new tab, but focus on existing one
            var subgraphController = GetSubgraphBlocks(window).Single();
            EnterSubgraphByReflection(window.graphView, subgraphController.model, false);

            yield return null;

            Assert.AreEqual(2, VFXViewWindow.GetAllWindows().Count);
            Assert.AreEqual(graphTitle, window.titleContent.text);
            Assert.AreEqual(Path.GetFileNameWithoutExtension(subgraphFileName), subgraphWindow.titleContent.text);
        }

        [UnityTest]
        [Description("When a subgraph is entered in-place no other tab should be created")]
        public IEnumerator Open_Subgraph_In_Same_Tab()
        {
            // Prepare
            VFXTestCommon.CloseAllVFXWindow();
            var window = VFXGraphViewTestGraphFactory.CreateSimpleVFXGraph();

            yield return null;

            // Get Set Lifetime node and convert it to subgraph block
            var controllers = GetBlocks(window, "Set".Label(false).AppendLiteral("Lifetime")).Take(1);
            var subgraphFileName = TempDirectoryName + $"/subgraph_{GUID.Generate()}.vfxblock";
            VFXConvertSubgraph.ConvertToSubgraphBlock(window.graphView, controllers, Rect.zero, subgraphFileName);

            yield return null;

            // Get the subgraph block model and enter inside, it should not open new tab, but focus on existing one
            var subgraphController = GetSubgraphBlocks(window).Single();
            EnterSubgraphByReflection(window.graphView, subgraphController.model, false);

            yield return null;

            Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count);
            Assert.AreEqual(Path.GetFileNameWithoutExtension(subgraphFileName), window.titleContent.text);
        }

        bool Find_In_Update_Context_Source(VisualEffectAsset asset, string pattern)
        {
            var resource = asset.GetResource();
            var shaderSourcesCount = resource.GetShaderSourceCount();
            for (int shaderIndex = 0; shaderIndex < shaderSourcesCount; ++shaderIndex)
            {
                var shaderName = resource.GetShaderSourceName(shaderIndex);
                if (shaderName.Contains("Update"))
                {
                    var shaderSource = resource.GetShaderSource(shaderIndex);
                    return shaderSource.Contains(pattern);
                }
            }
            return false;
        }

        public static readonly bool[] kCreate_Graph_Modify_And_Save_Subgraph_Applied_To_MainCase =
        {
            true,
            //false //Coverage from UUM-116297
        };
        [UnityTest]
        [Description("Expected main graph being updated when a subgraph is modified on disk")]
        public IEnumerator Create_Graph_Modify_And_Save_Subgraph_Applied_To_Main([ValueSource(nameof(kCreate_Graph_Modify_And_Save_Subgraph_Applied_To_MainCase))] bool saveMainGraphOnDisk)
        {
            VFXTestCommon.CloseAllVFXWindow();
            Assert.AreEqual(0, VFXViewWindow.GetAllWindows().Count());

            //Prepare asset with subgraph open for edition
            var rootGraph = VFXTestCommon.CreateGraph_And_System();
            var rootAsset = rootGraph.GetResource().asset;
            var blockCollision = ScriptableObject.CreateInstance<Block.CollisionShape>();
            blockCollision.SetSettingValue("shape", CollisionShapeBase.Type.Sphere);
            var updateContext = rootGraph.children.OfType<VFXBasicUpdate>().Single();
            Assert.AreEqual(0, updateContext.children.Count());
            updateContext.AddChild(blockCollision);
            VFXTestCommon.ReimportVFXGraph(rootGraph);

            Assert.AreEqual(VFXCompilationMode.Runtime, VisualEffectAssetUtility.GetCompilationMode(rootGraph.GetResource().asset));
            var rootWindow = VFXTestCommon.GetWindow(rootAsset, true);
            rootWindow.LoadResource(rootAsset.GetResource());
            Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count());

            for (int i = 0; i < 8; ++i)
            {
                if (VisualEffectAssetUtility.GetCompilationMode(rootGraph.GetResource().asset) == VFXCompilationMode.Edition)
                    break;
                yield return null;
            }

            Assert.AreEqual(VFXCompilationMode.Edition, VisualEffectAssetUtility.GetCompilationMode(rootGraph.GetResource().asset));
            Assert.IsTrue(Find_In_Update_Context_Source(rootAsset, "shape:Sphere"));
            Assert.IsFalse(Find_In_Update_Context_Source(rootAsset, "shape:Cone"));

            var controllers = rootWindow.graphView.Query<VFXContextUI>()
                .ToList()
                .Single(x => x.controller.model is VFXBasicUpdate)
                .controller.allChildren
                .OfType<VFXBlockController>()
                .ToList();
            Assert.AreEqual(1, controllers.Count);

            var subgraphFilename = TempDirectoryName + $"/subgraph_{GUID.Generate()}.vfxblock";
            VFXConvertSubgraph.ConvertToSubgraphBlock(rootWindow.graphView, controllers, Rect.zero, subgraphFilename);
            yield return null;

            if (saveMainGraphOnDisk)
            {
                rootWindow.SaveChanges();
                yield return null;
            }

            var subgraphVfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectSubgraphBlock>(subgraphFilename);
            var subgraphResource = subgraphVfxAsset.GetResource();
            VFXViewWindow subgraphWindow = VFXTestCommon.GetWindow(subgraphResource, true);
            subgraphWindow.LoadResource(subgraphResource);
            yield return null;

            //Modify Subgraph
            Assert.AreEqual(2, VFXViewWindow.GetAllWindows().Count());
            var subgraphVfx = subgraphWindow.graphView.controller.graph;
            var subgraphContext = subgraphVfx.children.OfType<VFXBlockSubgraphContext>().Single();
            var blockCollisionInSubgraph = subgraphContext.children.Single();
            blockCollisionInSubgraph.SetSettingValue("shape", CollisionShapeBase.Type.Cone);
            subgraphWindow.graphView.controller.ApplyChanges();
            yield return null;

            Assert.IsTrue(Find_In_Update_Context_Source(rootAsset, "shape:Sphere"));
            Assert.IsFalse(Find_In_Update_Context_Source(rootAsset, "shape:Cone"));

            Assert.IsTrue(EditorUtility.IsDirty(subgraphVfx));
            subgraphWindow.SaveChanges();
            yield return null;

            Assert.IsFalse(Find_In_Update_Context_Source(rootAsset, "shape:Sphere"));
            Assert.IsTrue(Find_In_Update_Context_Source(rootAsset, "shape:Cone"));
            VFXTestCommon.CloseAllVFXWindow();
            yield return null;
        }


        private static bool[] kOpen_Subgraph_In_Same_Tab_And_Go_Back_Case = new[] {true, false};
        [UnityTest]
        [Description("When a subgraph is entered in-place a back button is available and allow to reload original graph in that same tab")]
        public IEnumerator Open_Subgraph_In_Same_Tab_And_Go_Back([ValueSource(nameof(kOpen_Subgraph_In_Same_Tab_And_Go_Back_Case))] bool saveMainGraph)
        {
            // Prepare
            VFXTestCommon.CloseAllVFXWindow();
            var window = VFXGraphViewTestGraphFactory.CreateSimpleVFXGraph();
            var originalTitle = window.titleContent.text;

            yield return null;

            // Get Set Lifetime node and convert it to subgraph block
            var controllers = GetBlocks(window, "Set".Label(false).AppendLiteral("Lifetime")).Take(1);
            var subgraphFileName = TempDirectoryName + $"/subgraph_{GUID.Generate()}.vfxblock";
            VFXConvertSubgraph.ConvertToSubgraphBlock(window.graphView, controllers, Rect.zero, subgraphFileName);
            Assert.IsTrue(EditorUtility.IsDirty(window.graphView.controller.graph));
            yield return null;
            Assert.IsTrue(window.hasUnsavedChanges);

            if (saveMainGraph)
                window.graphView.OnSave();

            yield return null;

            // Get the subgraph block model and enter inside, it should not open new tab, but focus on existing one
            var subgraphController = GetSubgraphBlocks(window).Single();
            EnterSubgraphByReflection(window.graphView, subgraphController.model, false);

            yield return null;

            Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count);
            Assert.AreEqual(Path.GetFileNameWithoutExtension(subgraphFileName), window.titleContent.text);
            Assert.AreEqual(true, window.CanPopResource());

            // Go back to original graph
            window.PopResource();

            yield return null;

            Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count);
            Assert.AreEqual(originalTitle, window.titleContent.text);
            //Since we are relying on hasUnsavedChanges status, the titleContent doesn't change
            //The '*' is now handled internally in EditorWindow
            Assert.AreEqual(!saveMainGraph, EditorUtility.IsDirty(window.graphView.controller.graph));
            Assert.AreEqual(!saveMainGraph, window.hasUnsavedChanges);
        }

        [UnityTest]
        [Description("If we go back to original graph but that graph is already opened, then the current tab is left unchanged and the focus is given to the opened graph window")]
        public IEnumerator Open_Subgraph_In_Same_Tab_And_Go_Back_And_Original_Graph_Is_Opened([ValueSource(nameof(kOpen_Subgraph_In_Same_Tab_And_Go_Back_Case))] bool saveMainGraph)
        {
            // Prepare
            VFXTestCommon.CloseAllVFXWindow();
            var window = VFXGraphViewTestGraphFactory.CreateSimpleVFXGraph();
            var originalTitle = window.titleContent.text;
            var originalResource = window.displayedResource;

            yield return null;

            // Get Set Lifetime node and convert it to subgraph block
            var controllers = GetBlocks(window, "Set".Label(false).AppendLiteral("Lifetime")).Take(1);
            var subgraphFileName = TempDirectoryName + $"/subgraph_{GUID.Generate()}.vfxblock";
            VFXConvertSubgraph.ConvertToSubgraphBlock(window.graphView, controllers, Rect.zero, subgraphFileName);
            yield return null;
            Assert.IsTrue(EditorUtility.IsDirty(window.graphView.controller.graph));
            Assert.IsTrue(window.hasUnsavedChanges);

            if (saveMainGraph)
                window.graphView.OnSave();

            yield return null;

            // Get the subgraph block model and enter inside, it should not open new tab, but focus on existing one
            var subgraphController = GetSubgraphBlocks(window).Single();
            EnterSubgraphByReflection(window.graphView, subgraphController.model, false);

            yield return null;

            Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count);
            Assert.AreEqual(Path.GetFileNameWithoutExtension(subgraphFileName), window.titleContent.text);

            // Open the original resource
            var originalWindow = VFXTestCommon.GetWindow(originalResource, true);
            originalWindow.LoadResource(originalResource);
            Assert.AreEqual(2, VFXViewWindow.GetAllWindows().Count);

            // Go back to original graph
            window.PopResource();

            yield return null;

            Assert.AreEqual(2, VFXViewWindow.GetAllWindows().Count);
            Assert.AreEqual(originalTitle, originalWindow.titleContent.text);
            //Since we are relying on hasUnsavedChanges status, the titleContent doesn't change
            //The '*' is now handled internally in EditorWindow
            Assert.AreEqual(!saveMainGraph, EditorUtility.IsDirty(originalWindow.graphView.controller.graph));
            Assert.AreEqual(!saveMainGraph, originalWindow.hasUnsavedChanges);

            Assert.AreEqual(Path.GetFileNameWithoutExtension(subgraphFileName), window.titleContent.text);
        }

        [UnityTest]
        [Description("When a subgraph has been entered, and the original graph has been deleted, then the current tab cannot go back anymore")]
        public IEnumerator Open_Subgraph_In_Same_Tab_And_Go_Back_And_Original_Graph_Is_Deleted()
        {
            // Prepare
            VFXTestCommon.CloseAllVFXWindow();
            var window = VFXGraphViewTestGraphFactory.CreateSimpleVFXGraph();
            var originalGraphPath = AssetDatabase.GetAssetPath(window.displayedResource);

            yield return null;

            // Get Set Lifetime node and convert it to subgraph block
            var controllers = GetBlocks(window, "Set".Label(false).AppendLiteral("Lifetime")).Take(1);
            var subgraphFileName = TempDirectoryName + $"/subgraph_{GUID.Generate()}.vfxblock";
            VFXConvertSubgraph.ConvertToSubgraphBlock(window.graphView, controllers, Rect.zero, subgraphFileName);

            yield return null;

            // Get the subgraph block model and enter inside, it should not open new tab, but focus on existing one
            var subgraphController = GetSubgraphBlocks(window).Single();
            EnterSubgraphByReflection(window.graphView, subgraphController.model, false);

            yield return null;

            Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count);
            Assert.AreEqual(Path.GetFileNameWithoutExtension(subgraphFileName), window.titleContent.text);

            // Delete the original resource
            AssetDatabase.DeleteAsset(originalGraphPath);

            // Go back to original graph
            yield return null;

            Assert.AreEqual(false, window.CanPopResource());
        }

        private void EnterSubgraphByReflection(VFXView vfxView, VFXBlock model, bool newTab)
        {
            var enterSubgraphMethod = vfxView.GetType().GetMethod("EnterSubgraph", BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.NotNull(enterSubgraphMethod, "Trying to access `EnterSubgraph` method by reflection, but failed");
            enterSubgraphMethod.Invoke(vfxView, new object[] { model, newTab });
        }

        private IEnumerable<VFXBlockController> GetBlocks(VFXViewWindow window, string namePattern)
        {
            return window.graphView.Query<VFXContextUI>()
                .ToList()
                .Single(x => x.controller.model is VFXBasicInitialize)
                .controller.allChildren
                .OfType<VFXBlockController>()
                .Where(x => x.model.name.Contains(namePattern));
        }

        private IEnumerable<VFXBlockController> GetSubgraphBlocks(VFXViewWindow window)
        {
            return window.graphView.Query<VFXContextUI>()
                .ToList()
                .Single(x => x.controller.model is VFXBasicInitialize)
                .controller.allChildren
                .OfType<VFXBlockController>()
                .Where(x => x.model is VFXSubgraphBlock);
        }

        [UnityTest]
        public IEnumerator Drag_And_Drop_VFX_With_Circular_Dependency()
        {
            // Create main graph
            var window1 = VFXGraphViewTestGraphFactory.CreateSimpleVFXGraph();
            var mainGraph = window1.graphView.controller.graph.GetResource();
            window1.graphView.OnSave();
            window1.Close();
            yield return null;

            // Create another graph that only contains a Subgraph context referencing the graph above
            var controller = VFXTestCommon.StartEditTestAsset();
            var subgraphContext = ScriptableObject.CreateInstance<VFXSubgraphContext>();;
            subgraphContext.SetSettingValue("m_Subgraph", mainGraph.asset);
            controller.graph.AddChild(subgraphContext);
            controller.ApplyChanges();
            var subgraphResource = controller.graph.GetResource();
            subgraphResource.WriteAsset();
            yield return null;

            window1 = VFXTestCommon.GetWindow((VisualEffectAsset)null, true);//.CreateWindow<VFXViewWindow>();
            window1.LoadResource(mainGraph);
            yield return null;
            Assert.AreEqual(DragAndDropVisualMode.Rejected, window1.graphView.GetDragAndDropModeForVisualEffectObject(subgraphResource.asset), "Should not be able to create a circular dependency");

            window1.Close();
            yield return null;
        }
    }
}
