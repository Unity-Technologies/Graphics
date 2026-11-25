using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VFX.UI;
using UnityEditor.VFX.Block.Test;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.Operator;
using UnityEngine.UIElements.TestFramework;

namespace UnityEditor.VFX.Test.UI.GraphView
{
    [TestFixture]
    public class VFXGraphInteractionTests
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

        [Test]
        public void ExpandRetractAndSetPropertyValue()
        {
            var viewController = VFXTestCommon.StartEditTestAsset();

            var initContextDesc = VFXLibrary.GetContexts().First(t => typeof(VFXBasicInitialize).IsAssignableFrom(t.modelType));

            var newContext = viewController.AddVFXContext(new Vector2(300, 100), initContextDesc.variant);
            viewController.ApplyChanges();

            var allChildContextControllers = viewController.allChildren.OfType<VFXContextController>().ToArray();
            Assert.AreEqual(1, allChildContextControllers.Length);

            var contextController = allChildContextControllers.First();

            Assert.AreEqual(contextController.model, newContext);

            // Adding every block compatible with an init context

            var blockDesc = new VFXModelDescriptor<VFXBlock>(new Variant(null, null, typeof(AllType), null), null);

            var newBlock = blockDesc.CreateInstance();
            contextController.AddBlock(0, newBlock);

            Assert.IsTrue(newBlock is AllType);
            viewController.ApplyChanges();

            var blockControllerWithModel = contextController.blockControllers.Where(x => x.model == newBlock).ToArray();
            Assert.AreEqual(1, blockControllerWithModel.Length);

            var blockController = blockControllerWithModel.Single();

            Assert.NotNull(blockController);

            var vector3InputControllers = blockController.inputPorts
                .OfType<VFXContextDataInputAnchorController>()
                .Where(x => x.name == "aVector3").ToArray();
            Assert.AreEqual(1, vector3InputControllers.Length);

            VFXSlot slot = blockController.model.inputSlots.First(t => t.name == "aVector3");


            var aVector3Controller = vector3InputControllers.Single();

            Assert.AreEqual(1, blockController.inputPorts.OfType<VFXContextDataInputAnchorController>().Count(x => x.path == "aVector3.x"));
            Assert.AreEqual(1, blockController.inputPorts.OfType<VFXContextDataInputAnchorController>().Count(x => x.path == "aVector3.y"));
            Assert.AreEqual(1, blockController.inputPorts.OfType<VFXContextDataInputAnchorController>().Count(x => x.path == "aVector3.z"));

            aVector3Controller.ExpandPath();
            viewController.ApplyChanges();

            Assert.AreEqual(1, blockController.inputPorts.OfType<VFXContextDataInputAnchorController>().Count(x => x.path == "aVector3.x"));
            Assert.AreEqual(1, blockController.inputPorts.OfType<VFXContextDataInputAnchorController>().Count(x => x.path == "aVector3.y"));
            Assert.AreEqual(1, blockController.inputPorts.OfType<VFXContextDataInputAnchorController>().Count(x => x.path == "aVector3.z"));


            aVector3Controller.RetractPath();
            viewController.ApplyChanges();

            Assert.AreEqual(1, blockController.inputPorts.OfType<VFXContextDataInputAnchorController>().Count(x => x.path == "aVector3.x"));
            Assert.AreEqual(1, blockController.inputPorts.OfType<VFXContextDataInputAnchorController>().Count(x => x.path == "aVector3.y"));
            Assert.AreEqual(1, blockController.inputPorts.OfType<VFXContextDataInputAnchorController>().Count(x => x.path == "aVector3.z"));

            aVector3Controller.value = new Vector3(1.2f, 3.4f, 5.6f);

            Assert.AreEqual(slot.value, new Vector3(1.2f, 3.4f, 5.6f));

            aVector3Controller.ExpandPath();
            viewController.ApplyChanges();

            var vector3yController = blockController.inputPorts.OfType<VFXContextDataInputAnchorController>().First(x => x.path == "aVector3.y");

            vector3yController.value = 7.8f;

            Assert.AreEqual(slot.value, new Vector3(1.2f, 7.8f, 5.6f));
        }

        [UnityTest]
        public IEnumerator CollapseTest()
        {
            var viewController = VFXTestCommon.StartEditTestAsset();

            var builtInItem = VFXLibrary.GetOperators().First(t => typeof(VFXDynamicBuiltInParameter).IsAssignableFrom(t.modelType));

            var builtIn = viewController.AddVFXOperator(Vector2.zero, builtInItem.variant);

            yield return null;

            builtIn.collapsed = true;

            yield return null;

            yield return null;

            builtIn.collapsed = false;

            yield return null;

            yield return null;

            builtIn.superCollapsed = true;

            yield return null;

            yield return null;

            builtIn.superCollapsed = false;

        }

        public static readonly bool[] Create_Simple_Graph_Then_Remove_Edget_Between_Init_And_Update_TestCase = { true, false };
        //Cover issue from 1315593 with system name
        [UnityTest]
        public IEnumerator Create_Simple_Graph_Then_Remove_Edge_Between_Init_And_Update([ValueSource(nameof(Create_Simple_Graph_Then_Remove_Edget_Between_Init_And_Update_TestCase))] bool autoCompile)
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var path = AssetDatabase.GetAssetPath(graph);

            var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();

            var init = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var update = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var output = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();

            graph.AddChild(spawner);
            graph.AddChild(init);
            graph.AddChild(update);
            graph.AddChild(output);

            init.LinkFrom(spawner);
            update.LinkFrom(init);
            output.LinkFrom(update);
            AssetDatabase.ImportAsset(path);
            yield return null;

            //The issue is actually visible in VFXView
            var window = VFXTestCommon.GetViewWindow();
            window.Show();
            var bckpAutoCompile = window.autoCompile;
            window.autoCompile = autoCompile;
            window.LoadAsset(graph.GetResource().asset, null);

            //update.UnlinkFrom(init); //Doesn't reproduce the issue
            var flowEdgeToDelete = window.graphView.controller.allChildren
                .OfType<VFXFlowEdgeController>()
                .Where(o => o.output.context.model.contextType == VFXContextType.Init && o.input.context.model.contextType == VFXContextType.Update)
                .ToArray();
            Assert.AreEqual(1u, flowEdgeToDelete.Length);
            window.graphView.controller.Remove(flowEdgeToDelete);
            window.graphView.controller.NotifyUpdate(); //<= This function will indirectly try to access system name before update (called by VFXView.Update
            yield return null;

            window.autoCompile = bckpAutoCompile;
        }

        [UnityTest]
        public IEnumerator Check_Focus_On_Clear_Selection_When_Node_Is_Selected()
        {
            // Prepare
            var vfxController = VFXTestCommon.StartEditTestAsset();
            var sphereOperatorDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Sphere");

            var window = VFXTestCommon.GetViewWindow();
            var sphereOperator = vfxController.AddVFXOperator(new Vector2(4, 4), sphereOperatorDesc.variant);
            vfxController.ApplyChanges();

            var sphereNode = window.graphView.GetAllNodes().Single(x => x.controller.model == sphereOperator);
            window.graphView.AddToSelection(sphereNode);
            window.graphView.Focus();
            // Wait one frame for selection to apply (in the inspector)
            yield return null;

            // Check the focus is in the graph (so that "space" shortcut will work)
            Assert.True(window.graphView.HasFocus());

            // This is what could mess up with focus
            window.graphView.ClearSelection();

            // VFX graph must keep the focus
            yield return null;
            Assert.True(window.graphView.HasFocus());
        }

        [UnityTest]
        public IEnumerator Check_Focus_On_Clear_Selection_When_No_Selection()
        {
            // Prepare
            var vfxController = VFXTestCommon.StartEditTestAsset();
            var sphereOperatorDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Sphere");

            var window = VFXTestCommon.GetViewWindow();
            var sphereOperator = vfxController.AddVFXOperator(new Vector2(4, 4), sphereOperatorDesc.variant);
            vfxController.ApplyChanges();

            // Check the focus is in the graph (so that "space" shortcut will work)
            window.graphView.Focus();
            Assert.True(window.graphView.HasFocus());

            // This is what could mess up with focus
            window.graphView.ClearSelection();

            // VFX graph must keep the focus
            yield return null;
            Assert.True(window.graphView.HasFocus());
        }

        [UnityTest]
        public IEnumerator Check_Delayed_Field_Correctly_Saved()
        {
            // Prepare
            var vfxController = VFXTestCommon.StartEditTestAsset();
            var initializeContextDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name == "Initialize Particle");

            var window = VFXTestCommon.GetViewWindow();
            var initializeContext = vfxController.AddVFXContext(new Vector2(4, 4), initializeContextDesc.variant) as VFXBasicInitialize;
            vfxController.ApplyChanges();
            yield return null;

            var initializeNode = window.graphView.GetAllContexts().Single(x => x.controller.model is VFXBasicInitialize);
            var capacityField = initializeNode.Q<LongField>();
            Assert.AreEqual(128, capacityField.value);
            Assert.AreEqual(128u, (uint)initializeContext.GetSetting("capacity").value);

            // Act
            capacityField.Focus();
            capacityField.value = 2 * capacityField.value;
            capacityField.Blur();
            window.graphView.OnSave();
            yield return null;

            // Assert
            Assert.AreEqual(256, capacityField.value);
            Assert.AreEqual(256u, (uint)initializeContext.GetSetting("capacity").value);
        }

        [UnityTest, Description("Covers: UUM-61929")]
        public IEnumerator Check_Selection_Is_Emptied_On_Delete()
        {
            // Prepare
            EditorWindow.GetWindow<InspectorWindow>(); // Show the inspector because the issue was triggered by our inspector editor
            EditorWindow.GetWindow<ProjectBrowser>(); // Show the project browser to select asset (so that the selection is not empty)

            var window = VFXGraphViewTestGraphFactory.CreateSimpleVFXGraph();
            var setAgeOperatorDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.name == "|Set|_Age");
            var setAgeBlock = setAgeOperatorDesc.CreateInstance();
            window.graphView.controller.contexts.Single(x => x.model is VFXBasicUpdate).model.AddChild(setAgeBlock);
            window.graphView.controller.ApplyChanges();

            window.graphView.Focus();
            window.graphView.AddRangeToSelection(window.graphView
                .GetAllNodes()
                .Union(window.graphView.nodes.Where(x => x is VFXBlockUI block && block.controller.model is SetAttribute).OfType<ISelectable>())
                .ToList());
            Selection.Add(window.displayedResource.asset);
            Assert.IsTrue(Selection.objects?.Length > 1);

            // Act
            window.graphView.Delete();
            yield return null;

            // Assert
            Assert.IsTrue(Selection.objects?.Length == 1);
        }

        [UnityTest, Description("Covers: UUM-121821")]
        public IEnumerator Check_SaveAs_On_Same_File_Do_Not_Close_Editor()
        {
            // Prepare
            var window = VFXGraphViewTestGraphFactory.CreateSimpleVFXGraph();
            yield return null;
            var assetPath = AssetDatabase.GetAssetPath(window.displayedResource.asset);

            // Act
            window.graphView.SaveAs(assetPath);

            for (var i = 0; i < 10; i++)
                yield return null;

            // Assert
            Assert.IsNotNull(window.displayedResource, "The displayedResource is null, which mean the No Asset window is displayed");
        }
    }
}
