using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX.UI;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Experimental.VFX.Utility;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.Operator;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.Test.UI.CopyPaste
{
    [TestFixture]
    public class VFXGraphCopyPasteTests
    {
        VFXViewController m_ViewController;

        [SetUp]
        public void CreateTestAsset()
        {
            m_ViewController = VFXCopyPasteTestHelper.CreateTestAsset();
        }

        [TearDown]
        public void DestroyTestAsset()
        {
            VFXCopyPasteTestHelper.DestroyTestAsset();
            m_ViewController = null;
        }

        [UnityTest]
        public IEnumerator CopyPasteEdges()
        {
            // Load a pre-made vfx asset into a VFX Graph window
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>("Packages/com.unity.testing.visualeffectgraph/Tests/TestData/CopyPaste/Edges/CopyPasteTest.vfx");
            var resource = asset.GetResource();
            var window = VFXTestCommon.GetWindow(resource, true, true);
            window.LoadAsset(asset, null);
            yield return null;

            var view = window.graphView;
            view.ClearSelection();

            // Select all nodes in the graph
            var originalElements = view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>().ToArray();
            Assert.AreNotEqual(0, originalElements.Length);
            foreach (var element in originalElements)
            {
                view.AddToSelection(element);
            }

            // Copy / Paste selection
            string copyData = view.SerializeElements(view.selection.OfType<GraphElement>());

            var emptyVFX = VFXTestCommon.MakeTemporaryGraph();
            var otherWindow = VFXTestCommon.GetWindow(emptyVFX.GetResource(), true, true);
            otherWindow.LoadAsset(emptyVFX.GetResource().asset, null);
            view = otherWindow.graphView; //Switch to other view from here

            view.UnserializeAndPasteElements("paste", copyData);

            var parameters = view.Query().OfType<VFXParameterUI>().ToList();
            Assert.AreEqual(2, parameters.Count);
            if (parameters[0].title == "Vector3")
            {
                (parameters[0], parameters[1]) = (parameters[1], parameters[0]);
            }

            var operators = view.Query().OfType<VFXOperatorUI>().ToList();
            Assert.AreEqual(2, operators.Count);

            var contexts = view.Query().OfType<VFXContextUI>().ToList();
            Assert.AreEqual(2, contexts.Count);

            if (contexts[0].controller.model is VFXBasicUpdate)
            {
                (contexts[0], contexts[1]) = (contexts[1], contexts[0]);
            }


            var dataEdges = view.Query().OfType<VFXDataEdge>().ToList();

            Assert.AreEqual(4, dataEdges.Count);

            Assert.IsNotNull(dataEdges.SingleOrDefault(t => t.output.GetFirstAncestorOfType<VFXNodeUI>() == parameters[1] &&
                                                           operators.Contains(t.input.GetFirstAncestorOfType<VFXOperatorUI>())));

            Assert.IsNotNull(dataEdges.SingleOrDefault(t => operators.Contains(t.input.GetFirstAncestorOfType<VFXOperatorUI>()) &&
                                                           operators.Contains(t.output.GetFirstAncestorOfType<VFXOperatorUI>()) &&
                                                           t.output.GetFirstAncestorOfType<VFXNodeUI>() != t.input.GetFirstAncestorOfType<VFXNodeUI>()));

            Assert.IsNotNull(dataEdges.SingleOrDefault(t => t.output.GetFirstAncestorOfType<VFXNodeUI>() == parameters[0] &&
                                                           t.input.GetFirstAncestorOfType<VFXNodeUI>() == contexts[0]));

            Assert.IsNotNull(dataEdges.FirstOrDefault(t => operators.Contains<VFXNodeUI>(t.output.GetFirstAncestorOfType<VFXNodeUI>()) &&
                                                           t.input.GetFirstAncestorOfType<VFXNodeUI>() == contexts[0].GetAllBlocks().First()));


            VFXFlowEdge flowEdge = view.Query().OfType<VFXFlowEdge>();

            Assert.IsNotNull(flowEdge);

            Assert.AreEqual(flowEdge.output.GetFirstAncestorOfType<VFXContextUI>(), contexts[1]);
            Assert.AreEqual(flowEdge.input.GetFirstAncestorOfType<VFXContextUI>(), contexts[0]);

            window.Close();
            otherWindow.Close();
            yield return null;
        }

        [Test]
        public void CopyPast_Context_With_Objects_In_Settings()
        {
            var outputContextDesc = VFXLibrary.GetContexts().First(t => t.modelType == typeof(VFXComposedParticleOutput));
            m_ViewController.AddVFXContext(new Vector2(100, 100), outputContextDesc.variant);
            m_ViewController.ApplyChanges();

            var window = VFXTestCommon.GetViewWindow();
            var view = window.graphView;
            view.controller = m_ViewController;
            view.ClearSelection();
            foreach (var element in view.Query().OfType<VFXContextUI>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }

            var copyData = view.SerializeElements(view.selection.OfType<GraphElement>());
            view.UnserializeAndPasteElements("Paste", copyData);

            // Retrieve the block from the copied context
            var copiedContexts = view.Query()
                .OfType<VFXContextUI>()
                .ToList()
                .Select(x => x.controller.model).ToArray();

            Assert.AreEqual(2, copiedContexts.Length);

            Assert.IsInstanceOf<VFXComposedParticleOutput>(copiedContexts[0]);
            Assert.IsInstanceOf<VFXComposedParticleOutput>(copiedContexts[1]);

            var originalTopology = copiedContexts[0].GetSetting("m_Topology");
            var originalShading = copiedContexts[0].GetSetting("m_Shading");
            var copyTopology = copiedContexts[1].GetSetting("m_Topology");
            var copyShading = copiedContexts[1].GetSetting("m_Shading");

            Assert.IsTrue(originalTopology.valid);
            Assert.IsTrue(originalShading.valid);
            Assert.IsTrue(copyTopology.valid);
            Assert.IsTrue(copyShading.valid);

            Assert.AreEqual(originalTopology.GetType(), copyTopology.GetType());
            Assert.AreEqual(originalShading.GetType(), copyShading.GetType());
            Assert.IsFalse(ReferenceEquals(originalTopology.value, copyTopology.value));
            Assert.IsFalse(ReferenceEquals(originalShading.value, copyShading.value));
        }

        [Test]
        public void PasteSystems()
        {
            VFXViewWindow window = VFXTestCommon.GetViewWindow();

            VFXView view = window.graphView;
            view.controller = m_ViewController;

            // Create a bunch of systems
            const int spawnerCount = 4, GPUSystemsCount = 4;
            var spawners = VFXTestCommon.CreateSpawners(view, m_ViewController, spawnerCount);
            VFXTestCommon.CreateSystems(view, m_ViewController, GPUSystemsCount, 0);

            // Copy paste them
            view.ClearSelection();
            foreach (var element in view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }
            view.CopySelectionCallback();
            view.PasteCallback();
            m_ViewController.ApplyChanges();

            // Query unique names
            var systemNames = view.controller.graph.systemNames;

            var uniqueNames = m_ViewController.graph.children.OfType<VFXBasicSpawner>()
                .Select(x => systemNames.GetUniqueSystemName(x.GetData()))
                .Union(m_ViewController.graph.children.OfType<VFXBasicInitialize>().Select(x => systemNames.GetUniqueSystemName(x.GetData())))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            var models = new HashSet<ScriptableObject>();
            m_ViewController.graph.CollectDependencies(models);
            var vfxDatas = models.OfType<VFXData>().ToArray();

            const int dataExpectedCount = 2 * (spawnerCount + GPUSystemsCount);
            Assert.AreEqual(dataExpectedCount, vfxDatas.Length, "There should be one distinct VFXData per system (8 spawners and 8 initialize");
            // Assert all names are unique, and the expected number of elements was obtained
            Assert.AreEqual(dataExpectedCount, uniqueNames.Count, "Some systems have the same name or are null or empty.");
        }

        [UnityTest, Description("UUM-46548")]
        public IEnumerator PasteMissingPointCacheAsset()
        {
            VFXViewWindow window = VFXTestCommon.GetViewWindow();

            VFXView view = window.graphView;
            view.controller = m_ViewController;

            // Create one system
            const int spawnerCount = 1, GPUSystemsCount = 1;
            var spawner = VFXTestCommon.CreateSpawners(view, m_ViewController, spawnerCount).Single();
            VFXTestCommon.CreateSystems(view, m_ViewController, GPUSystemsCount, 0);

            // Create a point cache operator
            var pCacheAssetPath = "Packages/com.unity.testing.visualeffectgraph/Tests/TestData/CopyPaste/PointCache/UnityLogoPrimeCount.pcache";
            var copypCacheAssetPath = Path.Combine(VFXTestCommon.tempBasePath, "pointCache.pcache");
            File.Copy(FileUtil.PathToAbsolutePath(pCacheAssetPath), copypCacheAssetPath, true);
            AssetDatabase.ImportAsset(copypCacheAssetPath);
            var pointCacheAsset = AssetDatabase.LoadAssetAtPath(copypCacheAssetPath, typeof(PointCacheAsset));
            var pointCacheOperator = VFXLibrary.GetOperators().Single(x => x.modelType == typeof(VFXOperatorPointCache)).CreateInstance() as VFXOperatorPointCache;
            pointCacheOperator.SetSettingValue("Asset", pointCacheAsset);
            m_ViewController.AddVFXModel(Vector2.zero, pointCacheOperator);
            yield return null;

            // Create a set position from map
            var setPositionBlock = VFXLibrary.GetBlocks().Single(x => x.name == "Set".Label(false).AppendLiteral("Position from Map").AppendLabel("2D")).CreateInstance() as AttributeFromMap;
            var initializeContext = m_ViewController.contexts.Single(x => x.model is VFXBasicInitialize);
            initializeContext.model.LinkFrom(spawner, 0, 0);
            initializeContext.AddBlock(0, setPositionBlock);
            setPositionBlock.GetInputSlot(0).Link(pointCacheOperator.GetOutputSlot(1));
            m_ViewController.ApplyChanges();
            yield return null;

            // Copy paste them
            view.ClearSelection();
            foreach (var element in view.Query().OfType<GraphElement>().ToList().OfType<ISelectable>())
            {
                view.AddToSelection(element);
            }
            view.CopySelectionCallback();
            // We delete the point cache asset to check that it does not break the past operation
            AssetDatabase.DeleteAsset(copypCacheAssetPath);
            view.PasteCallback();
            m_ViewController.ApplyChanges();
            yield return null;

            Assert.AreEqual(1, spawner.outputFlowSlot.Length);
            Assert.AreEqual(1, initializeContext.model.inputFlowSlot.Length);
            Assert.AreEqual(1, initializeContext.model.outputFlowSlot.Length);
        }
        [UnityTest, Description("UUM-75893")]
        public IEnumerator CopyPasteMultipleParametersWithEdges()
        {
            VFXViewWindow window = VFXTestCommon.GetViewWindow();
            VFXView view = window.graphView;
            view.controller = m_ViewController;

            // Create a parameter and add a two nodes to the graph
            var parameter = m_ViewController.AddVFXParameter(Vector2.zero, VFXLibrary.GetParameters().First(x => x.modelType == typeof(float)).variant);
            m_ViewController.LightApplyChanges();

            var parameterController = m_ViewController.GetParameterController(parameter);
            parameterController.model.AddNode(new Vector2(123, 456));
            parameterController.model.AddNode(new Vector2(123, 556));
            m_ViewController.LightApplyChanges();
            var parameterNode1 = parameterController.nodes.First();
            var parameterNode2 = parameterController.nodes.Last();
            Assert.AreNotEqual(parameterNode1, parameterNode2);
            yield return null;

            // Create a Add operator
            var addOperator = VFXLibrary.GetOperators().Single(x => x.modelType == typeof(Add));
            m_ViewController.AddNode(new Vector2(300, 500), addOperator.variant, null);
            m_ViewController.LightApplyChanges();
            var addOperatorController = m_ViewController.nodes.OfType<VFXOperatorController>().Last();
            yield return null;

            // Create links
            m_ViewController.CreateLink(addOperatorController.inputPorts.First(), parameterNode1.outputPorts.Single());
            m_ViewController.CreateLink(addOperatorController.inputPorts.Skip(1).First(), parameterNode2.outputPorts.Single());
            m_ViewController.LightApplyChanges();
            yield return null;

            Assert.AreEqual(2, m_ViewController.dataEdges.Count);

            // Select all and copy/paste
            window.graphView.ExecuteCommand(ExecuteCommandEvent.GetPooled("SelectAll"));
            window.graphView.CopySelectionCallback();
            window.graphView.GetType().GetProperty(nameof(VFXView.pasteCenter), BindingFlags.Instance|BindingFlags.NonPublic)?.SetValue(window.graphView, window.graphView.contentViewContainer.LocalToWorld(new Vector2(123, 650)));
            window.graphView.PasteCallback();
            yield return null;

            Assert.AreEqual(4, m_ViewController.dataEdges.Count);
        }

        [UnityTest, Description("UUM-122054")]
        [UnityPlatform(exclude = new RuntimePlatform[] {
            RuntimePlatform.WindowsEditor // Disabled for Instability https://unitycorp.atlassian.net/browse/UUM-161997
        })]
        public IEnumerator CopyPasteParameter_Check_Order()
        {
            var window = VFXTestCommon.GetViewWindow();
            var view = window.graphView;
            view.controller = m_ViewController;

            var parameter = m_ViewController.AddVFXParameter(Vector2.zero, VFXLibrary.GetParameters().First(x => x.modelType == typeof(float)).variant);

            //TODOPAUL: Check with @julien-amsellem, we got a null category here
            //Later, in MoveParameterAfter, we are doing ".Where(t => t.Value.isOutput == controller.isOutput && t.Value.model.category == controller.model.category)"
            //It fails due to confusion between null and string.empty, it seems specific to test
            //This test has been added with PR #82090
            parameter.category = string.Empty;

            m_ViewController.LightApplyChanges();
            view.blackboard.Update(true);
            yield return null;

            var parameterOrder = m_ViewController.GetParameterController(parameter).order;
            var treeView = view.blackboard.Q<TreeView>();
            treeView.selectedIndex = 1;
            yield return null;

            view.DuplicateSelectionCallback();
            yield return null;

            var copySelectedPropertyItem = treeView.selectedItem as PropertyItem;
            Assert.NotNull(copySelectedPropertyItem);
            Assert.AreEqual(parameterOrder + 1, copySelectedPropertyItem.controller.order);
        }
    }
}
