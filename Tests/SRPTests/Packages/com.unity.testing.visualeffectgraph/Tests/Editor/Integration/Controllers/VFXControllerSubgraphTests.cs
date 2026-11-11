using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using UnityEngine.VFX;

using UnityEditor.VFX.Block;
using UnityEditor.VFX.Block.Test;
using UnityEditor.VFX.UI;
using System.Reflection;

using static UnityEditor.VFX.Test.VFXControllerTestHelper;

namespace UnityEditor.VFX.Test.Integration.Controllers
{
    [TestFixture]
    public class VFXControllerSubgraphTests
    {
        private VFXViewController m_ViewController;
        private int m_StartUndoGroupId;
        private string testAssetRandomFileName;

        [SetUp]
        public void CreateTestAsset()
        {
            m_ViewController = VFXControllerTestHelper.CreateTestAsset();
            m_StartUndoGroupId = Undo.GetCurrentGroup();
        }

        [OneTimeSetUp]
        public void Init()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
        }

        [OneTimeTearDown]
        public void OnTimeCleanup()
        {
            VFXTestCommon.CloseAllVFXWindow();
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [TearDown]
        public void DestroyTestAsset()
        {
            VFXControllerTestHelper.DestroyTestAsset(m_ViewController, m_StartUndoGroupId, testAssetRandomFileName);
        }

        static readonly bool[] kConvertToSubgraphBlock_With_Operators_Case = { true, false };

        [UnityTest, Description("Repro from UUM-153537")]
        public IEnumerator ConvertToSubgraphBlock_With_Operators([ValueSource(nameof(kConvertToSubgraphBlock_With_Operators_Case))] bool withGroupAndStickyNote)
        {
            string vfxPath;
            {
                var vfxGraph = VFXTestCommon.CreateGraph_And_System();
                vfxPath = AssetDatabase.GetAssetPath(vfxGraph);
                var update = vfxGraph.children.OfType<VFXBasicUpdate>().Single();
                var setPosition = ScriptableObject.CreateInstance<SetAttribute>();
                setPosition.SetSettingValue("attribute", "position");
                update.AddChild(setPosition);

                // The operand type differs from the linked inputs, relinking them one by one would change it
                var modulo = ScriptableObject.CreateInstance<Operator.Modulo>();
                modulo.SetOperandType(typeof(Vector3));
                vfxGraph.AddChild(modulo);
                Assert.AreEqual(typeof(Vector3), modulo.inputSlots[0].property.type);
                for (int i = 0; i < 2; ++i)
                {
                    var absolute = ScriptableObject.CreateInstance<Operator.Absolute>();
                    vfxGraph.AddChild(absolute);
                    Assert.IsTrue(modulo.inputSlots[i].Link(absolute.outputSlots[0]));
                }
                Assert.IsTrue(setPosition.inputSlots[0].Link(modulo.outputSlots[0]));
                VFXTestCommon.ReimportVFXGraph(vfxGraph);
            }

            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetEntityId(), 0));

            var window = VFXTestCommon.GetWindow(asset);
            window.LoadAsset(asset, null);
            var viewController = window.graphView.controller;
            Assert.IsNotNull(viewController);
            yield return null;

            var operatorControllers = viewController.AllSlotContainerControllers.Where(t => t.model is VFXOperator).ToArray();
            Assert.AreEqual(3, operatorControllers.Length);
            var updateContext = viewController.graph.children.OfType<VFXBasicUpdate>().Single();
            var setPositionController = viewController.AllSlotContainerControllers.Single(t => t.model.GetParent() == updateContext);
            var controllers = operatorControllers.Cast<Controller>().Append(setPositionController).ToList();

            if (withGroupAndStickyNote)
            {
                viewController.AddStickyNote(Vector2.zero, null);
                yield return null;

                var stickyNote = viewController.stickyNotes.Single();
                viewController.GroupNodes(operatorControllers, new[] { stickyNote });
                yield return null;

                controllers.Add(stickyNote);
                controllers.Add(viewController.groupNodes.Single());
            }

            var subgraphPath = vfxPath + "block";
            VFXConvertSubgraph.ConvertToSubgraphBlock(window.graphView, controllers, Rect.zero, subgraphPath);
            viewController.ApplyChanges();
            yield return null;

            //Main Graph Content
            updateContext = viewController.graph.children.OfType<VFXBasicUpdate>().Single();
            Assert.IsFalse(updateContext.children.OfType<SetAttribute>().Any());
            Assert.AreEqual(1, updateContext.children.OfType<VFXSubgraphBlock>().Count());
            Assert.IsFalse(viewController.graph.children.OfType<VFXOperator>().Any());

            //Subgraph Content
            var subgraphAsset = AssetDatabase.LoadAssetAtPath<VisualEffectSubgraphBlock>(subgraphPath);
            var subgraph = (VFXGraph)subgraphAsset.GetResource().graph;

            var innerModulo = subgraph.children.OfType<Operator.Modulo>().Single();
            Assert.AreEqual(typeof(Vector3), innerModulo.GetOperandType());
            Assert.IsTrue(innerModulo.inputSlots.All(t => t.HasLink()));

            var innerSetPosition = subgraph.children.OfType<VFXBlockSubgraphContext>().Single().children.OfType<SetAttribute>().Single();
            Assert.AreEqual(innerModulo.outputSlots[0], innerSetPosition.inputSlots[0].LinkedSlots.Single());

            if (withGroupAndStickyNote)
            {
                Assert.AreEqual(1, subgraph.UIInfos.stickyNoteInfos.Length);
                var group = subgraph.UIInfos.groupInfos.Single();
                Assert.AreEqual(4, group.contents.Length);
                Assert.AreEqual(3, group.contents.Count(t => t.model is VFXOperator));
                Assert.AreEqual(1, group.contents.Count(t => t.isStickyNote));
            }

            window.Close();
            yield return null;
        }

        static readonly bool[] kConvertToSubGraphOperator_With_ExposedProperty_Select_Parameters = { true, false };

        static readonly bool[] kSubgraph_ActivationSlot_Unexpected_ExpressionCount_Variant = { false, true };
        //Regression test for case 1345426
        [UnityTest]
        public IEnumerator ConvertToSubGraphOperator()
        {
            VisualEffectAsset asset = VisualEffectAssetEditorUtility.CreateNewAsset(testAssetMainSubgraph);
            VisualEffectResource resource = asset.GetResource();

            var window = VFXTestCommon.GetWindow(resource, true, true);
            window.LoadAsset(AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(testAssetMainSubgraph), null);

            var viewController = VFXViewController.GetController(VisualEffectResource.GetResourceAtPath(testAssetMainSubgraph));

            var graph = viewController.graph;
            var add_A = ScriptableObject.CreateInstance<Operator.Add>();
            graph.AddChild(add_A);
            var add_B = ScriptableObject.CreateInstance<Operator.Add>();
            graph.AddChild(add_B);
            Assert.IsTrue(add_A.outputSlots[0].Link(add_B.inputSlots[1]));

            //Simple compilable system
            {
                var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();

                var spawnerInit = ScriptableObject.CreateInstance<VFXBasicInitialize>();
                var spawnerOutput = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();

                var blockAttributeDesc = VFXLibrary.GetBlocks().FirstOrDefault(o => o.variant.modelType == typeof(Block.SetAttribute));
                var blockAttribute = blockAttributeDesc.CreateInstance();
                blockAttribute.SetSettingValue("attribute", "position");
                spawnerInit.AddChild(blockAttribute);

                graph.AddChild(spawnerContext);
                graph.AddChild(spawnerInit);
                graph.AddChild(spawnerOutput);

                spawnerInit.LinkFrom(spawnerContext);
                spawnerOutput.LinkFrom(spawnerInit);
            }

            var initialize = viewController.graph.children.OfType<VFXBasicInitialize>().FirstOrDefault();
            Assert.IsNotNull(initialize);
            Assert.IsTrue(initialize.children.Any());

            var allSlot = initialize.children.SelectMany(o => o.inputSlots);
            var firstVector3 = allSlot.FirstOrDefault(o => o.property.type == typeof(Position));
            Assert.IsNotNull(firstVector3);
            Assert.IsTrue(add_B.outputSlots[0].Link(firstVector3[0][0]));
            Assert.IsTrue(firstVector3.HasLink(true));
            viewController.LightApplyChanges();

            yield return null;

            var controller = window.graphView.Query<VFXOperatorUI>().ToList().Where(t => t.controller.model is Operator.Add).Select(o => o.controller).Cast<Controller>();
            Assert.AreEqual(2, controller.Count());
            VFXConvertSubgraph.ConvertToSubgraphOperator(window.graphView, controller, Rect.zero, testSubgraphSubOperatorAssetName);

            for (int i = 0; i < 8; ++i)
                yield return null;

            //Check the status of the newly integrated subgraph, expecting one output
            var subgraph = viewController.graph.children.OfType<VFXSubgraphOperator>().FirstOrDefault();
            Assert.IsNotNull(subgraph);
            Assert.AreEqual(1, subgraph.outputSlots.Count);
            Assert.IsFalse(subgraph.outputSlots.Any(s => s == null));

            //If we reach here without any error or crash, the bug has been fixed
            yield return null;
        }

        //Extension of previous test: create two outputs in subgraph (instead of one), revert and restore
        [UnityTest, Description("Cover case 1345426")]
        public IEnumerator ConvertToSubGraphOperator_And_ModifySubgraph()
        {
            var previousTest = ConvertToSubGraphOperator();
            while (previousTest.MoveNext())
                yield return previousTest.Current;

            var resource = VisualEffectResource.GetResourceAtPath(testSubgraphSubOperatorAssetName);
            resource.WriteAsset();

            var oneOutputState = File.ReadAllText(testSubgraphSubOperatorAssetName);
            Assert.IsFalse(string.IsNullOrEmpty(oneOutputState));

            resource = VisualEffectResource.GetResourceAtPath(testSubgraphSubOperatorAssetName);
            Assert.IsNotNull(resource);
            var window = VFXTestCommon.GetViewWindow();
            window.LoadResource(resource, null);

            var viewController = window.graphView.controller;
            var graph = viewController.graph;
            Assert.IsNotNull(graph);

            var parameter = VFXLibrary.GetParameters().FirstOrDefault(o => o.modelType == typeof(Sphere));
            Assert.IsNotNull(parameter);
            var newParam = viewController.AddVFXParameter(Vector2.zero, parameter.variant);
            newParam.isOutput = true;
            var otherParamName = "programatically_new_name_test";
            newParam.SetSettingValue("m_ExposedName", otherParamName);
            viewController.ApplyChanges();

            resource.WriteAsset();

            yield return null;
            window.Close();

            var twoOutputState = File.ReadAllText(testSubgraphSubOperatorAssetName);
            Assert.IsFalse(string.IsNullOrEmpty(twoOutputState));
            Assert.AreNotEqual(oneOutputState, twoOutputState);
            Assert.IsTrue(twoOutputState.Contains(otherParamName));

            for (int i = 0; i < 16; ++i)
                yield return null;

            //Check the actual status, should have now two slots
            {
                AssetDatabase.ImportAsset(testAssetMainSubgraph);
                var mainAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(testAssetMainSubgraph);
                var mainGraph = mainAsset.GetResource().GetGraph();
                mainGraph.PrepareGraph();

                var subGraph = mainGraph.children.OfType<VFXSubgraphOperator>().FirstOrDefault();
                Assert.IsNotNull(subGraph);

                Assert.AreEqual(2, subGraph.outputSlots.Count);
                Assert.IsFalse(subGraph.outputSlots.Any(o => o == null));
            }
            yield return null;

            //Removing old slots, shouldn't get unexpected removed
            {
                File.WriteAllText(testSubgraphSubOperatorAssetName, oneOutputState);
                AssetDatabase.Refresh();

                AssetDatabase.ImportAsset(testAssetMainSubgraph);
                var mainAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(testAssetMainSubgraph);
                var mainGraph = mainAsset.GetResource().GetGraph();
                mainGraph.PrepareGraph();

                var subGraph = mainGraph.children.OfType<VFXSubgraphOperator>().FirstOrDefault();
                Assert.IsNotNull(subGraph);

                Assert.AreEqual(1, subGraph.outputSlots.Count);
                Assert.IsFalse(subGraph.outputSlots.Any(o => o == null));
            }
            yield return null;

            //Here, restore to the previous state, if one outputSlot is null, then we did a resync slot during compilation
            {
                File.WriteAllText(testSubgraphSubOperatorAssetName, twoOutputState);
                AssetDatabase.Refresh();

                AssetDatabase.ImportAsset(testAssetMainSubgraph);
                var mainAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(testAssetMainSubgraph);
                var mainGraph = mainAsset.GetResource().GetGraph();
                mainGraph.PrepareGraph();

                var subGraph = mainGraph.children.OfType<VFXSubgraphOperator>().FirstOrDefault();
                Assert.IsNotNull(subGraph);

                Assert.AreEqual(2, subGraph.outputSlots.Count);
                Assert.IsFalse(subGraph.outputSlots.Any(o => o == null));
            }
            yield return null;

            //Finally, open the source asset it will crash if the vfxasset is wrongly formed
            resource = VisualEffectResource.GetResourceAtPath(testAssetMainSubgraph);
            window = VFXTestCommon.GetViewWindow();
            window.LoadResource(resource, null);

            for (int i = 0; i < 16; ++i)
                yield return null;
        }

        [UnityTest][Description("(Non regression test for Jira case UUM-2272")]
        public IEnumerator ConvertToSubgraphOperator_AfterCopyPaste()
        {
            testAssetRandomFileName = $"Assets/TmpTests/random_{Guid.NewGuid()}.vfx";
            // Create default VFX Graph
            var templateString = File.ReadAllText(VFXTestCommon.simpleParticleSystemPath);
            File.WriteAllText(testAssetRandomFileName, templateString);
            AssetDatabase.ImportAsset(testAssetRandomFileName);

            // Open this vfx the same way it would be done by a user
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(testAssetRandomFileName);
            Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetEntityId(), 0));

            var window = VFXTestCommon.GetWindow(asset);
            window.LoadAsset(asset, null); //Should not be needed, without, viewController is null on Yamato. See UUM-11596.
            var viewController = window.graphView.controller;
            Assert.IsNotNull(viewController);
            viewController.ApplyChanges();
            yield return null;
            window.graphView.ExecuteCommand(ExecuteCommandEvent.GetPooled("SelectAll"));
            window.graphView.CopySelectionCallback();
            window.graphView.PasteCallback();
            yield return null;

            var addOperator = ScriptableObject.CreateInstance<Operator.Add>();
            viewController.graph.AddChild(addOperator);
            viewController.ApplyChanges();
            yield return null;

            VFXConvertSubgraph.ConvertToSubgraphOperator(window.graphView, window.graphView.Query<VFXOperatorUI>().ToList().Select(t => t.controller), Rect.zero, testSubgraphSubOperatorAssetName);
            yield return null;

            window.graphView.controller = null;
        }

        [UnityTest]
        public IEnumerator ConvertToSubGraphBlock_Nested()
        {
            string vfxPath;
            {
                var vfxGraph = VFXTestCommon.CreateGraph_And_System();
                vfxPath = AssetDatabase.GetAssetPath(vfxGraph);
                var update = vfxGraph.children.OfType<VFXBasicUpdate>().Single();
                var gravityDesc = VFXLibrary.GetBlocks().First(o => o.modelType == typeof(Gravity));
                var gravity = gravityDesc.CreateInstance();
                update.AddChild(gravity);
                VFXTestCommon.ReimportVFXGraph(vfxGraph);
            }

            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetEntityId(), 0));

            var window = VFXTestCommon.GetWindow(asset);
            window.LoadAsset(asset, null);
            var viewController = window.graphView.controller;
            Assert.IsNotNull(viewController);

            var firstSubgraphBlockPath = vfxPath + "block";
            var secondSubgraphBlockPath = firstSubgraphBlockPath.Replace(".vfxblock", "_bis.vfxblock");

            {
                var update = viewController.graph.children.OfType<VFXBasicUpdate>().Single();
                var gravityBlock = update.children.OfType<Gravity>().First();
                var controller = viewController.GetNodeController(gravityBlock, 0);
                VFXConvertSubgraph.ConvertToSubgraphBlock(window.graphView, new[] { controller }, Rect.zero, firstSubgraphBlockPath);
                viewController.ApplyChanges();
            }

            yield return null;

            {
                var update = viewController.graph.children.OfType<VFXBasicUpdate>().Single();
                var subgraphBlock = update.children.OfType<VFXSubgraphBlock>().First();
                var controller = viewController.GetNodeController(subgraphBlock, 0);
                VFXConvertSubgraph.ConvertToSubgraphBlock(window.graphView, new[] { controller }, Rect.zero, secondSubgraphBlockPath);
                viewController.ApplyChanges();
            }

            yield return null;

            //Basic check on expected shader generation output
            AssetDatabase.ImportAsset(vfxPath);
            var graph = asset.GetResource();
            bool foundGravityInSource = false;
            for (int shaderIndex = 0; shaderIndex < graph.GetShaderSourceCount(); ++shaderIndex)
            {
                if (!graph.GetShaderSourceName(shaderIndex).Contains("Update"))
                    continue;

                var source = graph.GetShaderSource(shaderIndex);
                if (source.Contains("Gravity"))
                {
                    foundGravityInSource = true;
                    break;
                }
            }
            Assert.IsTrue(foundGravityInSource);
        }

        [UnityTest]
        public IEnumerator ConvertToSubgraph_And_Undo()
        {
            string vfxPath;
            {
                var vfxGraph = VFXTestCommon.CreateGraph_And_System();
                vfxPath = AssetDatabase.GetAssetPath(vfxGraph);
                var update = vfxGraph.children.OfType<VFXBasicUpdate>().Single();
                var gravityDesc = VFXLibrary.GetBlocks().First(o => o.modelType == typeof(Gravity));
                var gravity = gravityDesc.CreateInstance();
                update.AddChild(gravity);
                VFXTestCommon.ReimportVFXGraph(vfxGraph);
            }

            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetEntityId(), 0));

            var window = VFXTestCommon.GetWindow(asset);
            window.LoadAsset(asset, null);
            var viewController = window.graphView.controller;
            Assert.IsNotNull(viewController);
            yield return null;

            Undo.IncrementCurrentGroup();
            {
                var subGraphBlockPath = vfxPath + "block";
                var update = viewController.graph.children.OfType<VFXBasicUpdate>().Single();
                var gravityBlock = update.children.OfType<Gravity>().First();
                var controller = viewController.GetNodeController(gravityBlock, 0);
                VFXConvertSubgraph.ConvertToSubgraphBlock(window.graphView, new[] { controller }, Rect.zero, subGraphBlockPath);
                viewController.ApplyChanges();
            }
            yield return null;
            {
                var update = viewController.graph.children.OfType<VFXBasicUpdate>().Single();
                Assert.IsFalse(update.children.OfType<Gravity>().Any());
                Assert.IsTrue(update.children.OfType<VFXSubgraphBlock>().Any());
            }

            Undo.PerformUndo();
            yield return null;
            {
                var update = viewController.graph.children.OfType<VFXBasicUpdate>().Single();
                Assert.IsTrue(update.children.OfType<Gravity>().Any(), "Undo Failure");
                Assert.IsFalse(update.children.OfType<VFXSubgraphBlock>().Any());
            }
        }

        [UnityTest][Description("(Non regression test for FB case #1419176")]
        public IEnumerator Rename_Asset_Dont_Lose_Subgraph()
        {
            testAssetRandomFileName = $"Assets/TmpTests/random_{Guid.NewGuid()}.vfx";
            // Create default VFX Graph
            var templateString = File.ReadAllText(VFXTestCommon.simpleParticleSystemPath);
            File.WriteAllText(testAssetRandomFileName, templateString);
            AssetDatabase.ImportAsset(testAssetRandomFileName);

            // Open this vfx the same way it would be done by a user
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(testAssetRandomFileName);
            Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetEntityId(), 0));

            var window = VFXTestCommon.GetWindow(asset);
            window.LoadAsset(asset, null); //Should not be needed, without, viewController is null on Yamato. See UUM-11596.
            var viewController = window.graphView.controller;
            Assert.IsNotNull(viewController);

            // Convert the first set attribute block into a subgraph block
            var initializeContext = viewController.graph.children.OfType<VFXBasicInitialize>().Single();
            var setAttributeBlock = initializeContext.children.OfType<SetAttribute>().First();
            var controller = viewController.GetNodeController(setAttributeBlock, 0);
            VFXConvertSubgraph.ConvertToSubgraphBlock(window.graphView, new [] { controller }, Rect.zero, testSubgraphBlockAssetName);
            viewController.ApplyChanges();

            var subGraphBlock = window.graphView.controller.AllSlotContainerControllers
                .Select(x => x.model)
                .OfType<VFXSubgraphBlock>()
                .Single();
            Assert.IsNotNull(subGraphBlock.subgraph);

            // Rename the asset
            var newFileName = $"zz-random_{Guid.NewGuid()}.vfx";
            var result = AssetDatabase.RenameAsset(testAssetRandomFileName, newFileName);
            Assert.IsEmpty(result);
            testAssetRandomFileName = $"Assets/TmpTests/{newFileName}";

            yield return null;
            yield return null;

            // Check the subgraph is still properly referenced
            subGraphBlock = window.graphView.controller.AllSlotContainerControllers
                .Select(x => x.model)
                .OfType<VFXSubgraphBlock>()
                .Single();
            Assert.IsNotNull(subGraphBlock.subgraph);

            window.Close();

            yield return null;
        }

        [UnityTest, Description("Repro from UUM-39696")]
        public IEnumerator Convert_To_Subgraph_Block_With_Different_Slot_Type()
        {
            var kSourceAsset = "Packages/com.unity.testing.visualeffectgraph/Tests/TestData/Subgraphs/SlotTypes/VFXSubGraph_Repro_39696.vfx_";
            var graph = VFXTestCommon.CopyTemporaryGraph(kSourceAsset);
            Assert.IsNotNull(graph);
            yield return null;

            var assetPath = AssetDatabase.GetAssetPath(graph);
            var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(assetPath);
            Assert.IsNotNull(asset);
            Assert.IsTrue(VisualEffectAssetEditor.OnOpenVFX(asset.GetEntityId(), 0));

            var window = VFXTestCommon.GetWindow(asset);
            window.LoadAsset(asset, null);
            var viewController = window.graphView.controller;
            Assert.IsNotNull(viewController);
            yield return null;

            var initializeContext = viewController.graph.children.OfType<VFXBasicInitialize>().Single();
            var setVelocityBlock = initializeContext.children.OfType<VelocityDirection>().First();

            var controller = viewController.GetNodeController(setVelocityBlock, 0);
            var subgraphPath = $"Assets/TmpTests/subgraph_39696_{Guid.NewGuid()}.vfxblock";

            VFXConvertSubgraph.ConvertToSubgraphBlock(window.graphView, new[] { controller }, Rect.zero, subgraphPath);
            viewController.ApplyChanges();
            yield return null;

            //Main Graph Content
            initializeContext = viewController.graph.children.OfType<VFXBasicInitialize>().Single();
            setVelocityBlock = initializeContext.children.OfType<VelocityDirection>().FirstOrDefault();
            Assert.IsNull(setVelocityBlock);

            var subgraphBlock = initializeContext.children.OfType<VFXSubgraphBlock>().Single();
            Assert.AreEqual(2u, subgraphBlock.inputSlots.Count);

            Assert.AreEqual(subgraphBlock.inputSlots[0].valueType, VFXValueType.Boolean);
            Assert.AreEqual(subgraphBlock.inputSlots[1].valueType, VFXValueType.Float);

            Assert.IsTrue(subgraphBlock.inputSlots[0].HasLink());
            Assert.IsTrue(subgraphBlock.inputSlots[1].HasLink());

            //Subgraph Content
            var subgraphContent = AssetDatabase.LoadAssetAtPath<VisualEffectSubgraph>(subgraphPath);
            var subgraph = (VFXGraph)subgraphContent.GetResource().graph;
            var blockSubgraphContext = subgraph.children.OfType<VFXBlockSubgraphContext>().Single();
            Assert.AreEqual(1u, blockSubgraphContext.children.Count());

            var innerSetVelocityBlock = blockSubgraphContext.children.OfType<VelocityDirection>().First();
            Assert.IsTrue(innerSetVelocityBlock.activationSlot.HasLink());

            foreach (var slot in innerSetVelocityBlock.inputSlots)
            {
                if (slot.name == "MinSpeed")
                    Assert.IsTrue(slot.HasLink());
                else
                    Assert.IsFalse(slot.HasLink(true));
            }

            var parameters = subgraph.children.OfType<VFXParameter>().ToList();
            Assert.AreEqual(2, parameters.Count);

            var enabled = parameters.First(o => o.exposedName == "enabled"); //There is an automatic dodge of reserved name, it shouldn't be _vfx_enabled here.
            var minSpeed = parameters.First(o => o.exposedName == "MinSpeed");

            Assert.IsTrue(enabled.exposed);
            Assert.IsTrue(minSpeed.exposed);

            Assert.AreEqual(typeof(bool), enabled.type);
            Assert.AreEqual(typeof(float), minSpeed.type);

            window.Close();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ConvertToSubGraphOperator_With_ExposedProperty([ValueSource(nameof(kConvertToSubGraphOperator_With_ExposedProperty_Select_Parameters))] bool includeParameter)
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var resource = graph.GetResource();

            var window = VFXTestCommon.GetWindow(resource, true, true);
            window.LoadAsset(resource.asset, null);
            yield return null;

            var add = ScriptableObject.CreateInstance<Operator.Add>();
            graph.AddChild(add);

            var parameter = ScriptableObject.CreateInstance<VFXParameter>();
            parameter.Init(typeof(float));
            graph.AddChild(parameter);

            parameter.SetSettingValue("m_Exposed", true);
            Assert.IsTrue(parameter.outputSlots[0].Link(add.inputSlots[1]));
            yield return null;

            var controllers = window.graphView.Query<VFXNodeUI>().ToList()
                .Where(t => t.controller.model != null)
                .Select(o => o.controller)
                .Cast<Controller>().ToArray();

            if (!includeParameter)
            {
                controllers = controllers.Where(o => o is not VFXParameterNodeController).ToArray();
                Assert.AreEqual(1, controllers.Count());
            }
            else
            {
                Assert.AreEqual(2, controllers.Count());
            }

            var guid = Guid.NewGuid().ToString();
            var subGraphPath = string.Format(VFXTestCommon.tempBasePath + "/vfx_{0}.vfxoperator", guid);
            VFXConvertSubgraph.ConvertToSubgraphOperator(window.graphView, controllers, Rect.zero, subGraphPath);

            //Check Main Graph Status
            Assert.IsFalse(graph.children.OfType<Operator.Add>().Any());
            Assert.AreEqual(1, graph.children.OfType<VFXParameter>().Count());

            var subgraphOperator = graph.children.OfType<VFXSubgraphOperator>().Single();
            var inputSlot = subgraphOperator.inputSlots.Single();
            Assert.IsTrue(inputSlot.HasLink());

            var linkedSlot = inputSlot.LinkedSlots.Single();
            Assert.AreEqual(linkedSlot.owner, parameter);

            //Check Subgraph Status
            var subgraphContent = AssetDatabase.LoadAssetAtPath<VisualEffectSubgraph>(subGraphPath);
            var subgraph = (VFXGraph)subgraphContent.GetResource().graph;
            Assert.IsNotNull(subgraph);

            Assert.AreEqual(2, subgraph.children.Count());
            var addInSubgraph = subgraph.children.OfType<Operator.Add>().Single();
            var parameterInSubgraph = subgraph.children.OfType<VFXParameter>().Single();
            var outputSlot = parameterInSubgraph.outputSlots.Single();
            Assert.IsTrue(outputSlot.HasLink());

            var linkedSlotInSubgraph = outputSlot.LinkedSlots.Single();
            Assert.AreEqual(linkedSlotInSubgraph.owner, addInSubgraph);

            window.Close();
            yield return null;
        }

        [UnityTest, Description("Converting a selection that includes an output property to a Subgraph Operator must not throw and must still clean up the source nodes")]
        public IEnumerator ConvertToSubGraphOperator_With_OutputProperty_In_Selection()
        {
            // Output properties only exist in subgraph operators, matching the reported repro.
            var subgraphOperator = VFXTestCommon.MakeTemporarySubGraphOperator();
            var resource = subgraphOperator.GetResource();

            var window = VFXTestCommon.GetViewWindow();
            window.LoadResource(resource, null);
            var graph = window.graphView.controller.graph;
            yield return null;

            // An operator fed by an exposed input parameter and feeding an output property.
            var add = ScriptableObject.CreateInstance<Operator.Add>();
            graph.AddChild(add);

            var inputParameter = ScriptableObject.CreateInstance<VFXParameter>();
            inputParameter.Init(typeof(float));
            graph.AddChild(inputParameter);
            inputParameter.SetSettingValue("m_Exposed", true);
            Assert.IsTrue(inputParameter.outputSlots[0].Link(add.inputSlots[1]));

            var outputParameter = ScriptableObject.CreateInstance<VFXParameter>();
            outputParameter.Init(typeof(float));
            graph.AddChild(outputParameter);
            outputParameter.isOutput = true;
            Assert.IsTrue(add.outputSlots[0].Link(outputParameter.inputSlots[0]));
            window.graphView.controller.LightApplyChanges();
            yield return null;

            var controllers = window.graphView.Query<VFXNodeUI>().ToList()
                .Where(t => t.controller.model != null)
                .Select(o => o.controller)
                .Cast<Controller>().ToArray();

            var guid = Guid.NewGuid().ToString();
            var subGraphPath = string.Format(VFXTestCommon.tempBasePath + "/vfx_{0}.vfxoperator", guid);
            VFXConvertSubgraph.ConvertToSubgraphOperator(window.graphView, controllers, Rect.zero, subGraphPath);

            // The operator moved into the subgraph and the source graph was cleaned up (previously the exception left it behind).
            Assert.IsFalse(graph.children.OfType<Operator.Add>().Any());
            Assert.AreEqual(1, graph.children.OfType<VFXSubgraphOperator>().Count());

            window.Close();
            yield return null;
        }

        [UnityTest, Description("Converting directly connected input and output properties to a Subgraph Operator removes both from the source graph (UUM-108536)")]
        public IEnumerator ConvertToSubGraphOperator_Removes_Directly_Connected_Properties()
        {
            var subgraphOperator = VFXTestCommon.MakeTemporarySubGraphOperator();
            var resource = subgraphOperator.GetResource();

            var window = VFXTestCommon.GetViewWindow();
            window.LoadResource(resource, null);
            var graph = window.graphView.controller.graph;
            yield return null;

            // Input property wired directly to an output property, with no logic in between.
            var inputParameter = ScriptableObject.CreateInstance<VFXParameter>();
            inputParameter.Init(typeof(float));
            graph.AddChild(inputParameter);
            inputParameter.SetSettingValue("m_Exposed", true);

            var outputParameter = ScriptableObject.CreateInstance<VFXParameter>();
            outputParameter.Init(typeof(float));
            graph.AddChild(outputParameter);
            outputParameter.isOutput = true;
            Assert.IsTrue(inputParameter.outputSlots[0].Link(outputParameter.inputSlots[0]));
            window.graphView.controller.LightApplyChanges();

            // Node UIs rebuild on a deferred pass; a single frame isn't always enough (e.g. under code coverage).
            Controller[] controllers = null;
            for (var i = 0; i < 64 && controllers == null; ++i)
            {
                yield return null;
                var nodeUIs = window.graphView.Query<VFXNodeUI>().ToList().Where(t => t.controller?.model != null).ToList();
                if (nodeUIs.Any(t => t.controller.model == inputParameter) && nodeUIs.Any(t => t.controller.model == outputParameter))
                    controllers = nodeUIs.Select(o => o.controller).Cast<Controller>().ToArray();
            }
            Assert.IsNotNull(controllers, "Both parameter nodes should be present before conversion.");

            var guid = Guid.NewGuid().ToString();
            var subGraphPath = string.Format(VFXTestCommon.tempBasePath + "/vfx_{0}.vfxoperator", guid);
            VFXConvertSubgraph.ConvertToSubgraphOperator(window.graphView, controllers, Rect.zero, subGraphPath);

            // Convert removes the parameter nodes (the blackboard definitions remain), so assert on node count.
            Assert.AreEqual(0, inputParameter.nodes.Count, "Input property node should be removed from the source graph.");
            Assert.AreEqual(0, outputParameter.nodes.Count, "Output property node should be removed from the source graph.");
            Assert.AreEqual(1, graph.children.OfType<VFXSubgraphOperator>().Count());

            window.Close();
            yield return null;
        }

        [UnityTest, Description("Repro from nested subgraph and live authoring")]
        public IEnumerator SubgraphNested_Unexpected_ExpressionCount()
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/TestData/Subgraphs/LiveCompilation/Repro_SubgraphNested_ExpressionCount.unitypackage";
            UnityEditor.AssetPackage.Utility.ImportPackageImmediately(packagePath);
            yield return null;

            var mainPath = "Assets/TmpTests/Repro_NestedSubgraph_ExpressionCount.vfx";
            var subgraphPath = "Assets/TmpTests/Repro_NestedSubgraph_ExpressionCount_B.vfxblock";
            var mainResource = VisualEffectResource.GetResourceAtPath(mainPath);
            var subgraphResource = VisualEffectResource.GetResourceAtPath(subgraphPath);

            Assert.IsNotNull(mainResource);
            Assert.IsNotNull(subgraphResource);
            yield return null;

            var subgraphWindow = VFXTestCommon.GetWindow(subgraphResource, true, true);
            subgraphWindow.LoadResource(subgraphResource);
            yield return null;

            var mainWindow = VFXTestCommon.GetWindow(mainResource, true, true);
            mainWindow.LoadResource(mainResource);
            yield return null;

            //Modify Subgraph
            var context = subgraphResource.GetGraph().children.OfType<VFXBlockSubgraphContext>().Single();
            var firstBlock = context.children.First();
            Assert.IsFalse((bool)firstBlock.activationSlot.value);
            firstBlock.activationSlot.value = true;
            yield return null;

            Assert.IsTrue(EditorUtility.IsDirty(subgraphResource));
            subgraphWindow.SaveChanges();
            Assert.IsFalse(EditorUtility.IsDirty(subgraphResource));

            //Modify main graph
            var aContext = mainResource.GetGraph().children.OfType<VFXContext>().First();
            aContext.position = aContext.position + new Vector2(1, 1);
            yield return null;

            Assert.IsTrue(EditorUtility.IsDirty(mainResource));
            mainWindow.SaveChanges();
            Assert.IsFalse(EditorUtility.IsDirty(mainResource));

            yield return null;
            Assert.IsFalse(mainResource.GetGraph().IsExpressionGraphDirty());
            mainResource.GetGraph().SetExpressionValueDirty();
            for (int i = 0; i < 4; ++i)
            {
                mainWindow.Repaint();
                yield return null;
            }
        }

        [UnityTest, Description("Repro from subgraph using activation slot and live authoring (repro from UUM-131445)")]
        public IEnumerator Subgraph_ActivationSlot_Unexpected_ExpressionCount([ValueSource(nameof(kSubgraph_ActivationSlot_Unexpected_ExpressionCount_Variant))] bool useVariant)
        {
            //N.B.: The "Bis" package doesn't use activation slot
            var packagePath = $"Packages/com.unity.testing.visualeffectgraph/Tests/TestData/Subgraphs/LiveCompilation/Repro_Subgraph_ActivationSlot{(useVariant ? "_Bis" : string.Empty)}.unitypackage";
            UnityEditor.AssetPackage.Utility.ImportPackageImmediately(packagePath);
            yield return null;

            var mainPath = $"Assets/TmpTests/Repro_Subgraph_ActivationSlot{(useVariant ? "_Bis" : string.Empty)}.vfx";
            var subgraphPath = $"Assets/TmpTests/Repro_Subgraph_ActivationSlot{(useVariant ? "_Bis" : string.Empty)}.vfxblock";
            var mainResource = VisualEffectResource.GetResourceAtPath(mainPath);
            var subgraphResource = VisualEffectResource.GetResourceAtPath(subgraphPath);

            Assert.IsNotNull(mainResource);
            Assert.IsNotNull(subgraphResource);
            yield return null;

            var subgraphWindow = VFXTestCommon.GetWindow(subgraphResource, true, true);
            subgraphWindow.LoadResource(subgraphResource);
            yield return null;

            var mainWindow = VFXTestCommon.GetWindow(mainResource, true, true);
            mainWindow.LoadResource(mainResource);
            yield return null;

            //Modify Subgraph
            var subgraphContext = subgraphResource.GetGraph().children.OfType<VFXBlockSubgraphContext>().Single();
            subgraphContext.position += new Vector2(1, 1);
            yield return null;

            //Modify main graph
            var mainGraphContext = mainResource.GetGraph().children.OfType<VFXContext>().First();
            mainGraphContext.position += new Vector2(1, 1);
            yield return null;

            Assert.IsTrue(EditorUtility.IsDirty(subgraphResource.GetGraph()));
            Assert.IsTrue(EditorUtility.IsDirty(mainResource.GetGraph()));

            subgraphWindow.SaveChanges();
            mainWindow.SaveChanges();

            Assert.IsFalse(EditorUtility.IsDirty(subgraphResource.GetGraph()));
            Assert.IsFalse(EditorUtility.IsDirty(mainResource.GetGraph()));

            yield return null;
            Assert.IsFalse(mainResource.GetGraph().IsExpressionGraphDirty());
            var inlineOperator = mainResource.GetGraph().children.OfType<VFXInlineOperator>().Single();
            inlineOperator.inputSlots[0].value = !(bool)inlineOperator.inputSlots[0].value;

            //Workaround, forcing dirty here indirectly fixes the issue because we won't go through UpdateValues but the full compilation
            //mainResource.GetGraph().SetExpressionGraphDirty();

            Assert.IsTrue(EditorUtility.IsDirty(mainResource.GetGraph()));
            for (int i = 0; i < 4; ++i)
            {
                mainWindow.Repaint();
                yield return null;
            }
        }

        [UnityTest, Description("Check simple subgraph setup and insure we are compiling indefinitely")]
        public IEnumerator Subgraph_Dont_Invalidate_MainGraph_Infinitely()
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/TestData/Subgraphs/LiveCompilation/Repro_Subgraph_Infinite_Compilation.unitypackage";
            UnityEditor.AssetPackage.Utility.ImportPackageImmediately(packagePath);
            yield return null;

            var mainPath = "Assets/TmpTests/Repro_Subgraph_Infinite_Compilation.vfx";
            var subgraphPath = "Assets/TmpTests/Repro_Subgraph_Infinite_Compilation_Subgraph.vfxblock";

            var mainResource = VisualEffectResource.GetResourceAtPath(mainPath);
            var subgraphResource = VisualEffectResource.GetResourceAtPath(subgraphPath);

            Assert.IsNotNull(mainResource);
            Assert.IsNotNull(subgraphResource);
            Assert.AreNotEqual(mainResource, subgraphResource);

            var mainGraph = mainResource.GetGraph();
            var subgraph = subgraphResource.GetGraph();
            Assert.IsNotNull(mainGraph);
            Assert.IsNotNull(subgraph);
            Assert.AreNotEqual(mainGraph, subgraph);

            var subgraphWindow = VFXTestCommon.GetWindow(subgraph, true, true);
            subgraphWindow.LoadResource(subgraphResource);
            yield return null;

            var mainWindow = VFXTestCommon.GetWindow(mainGraph, true, true);
            mainWindow.LoadResource(mainResource);
            yield return null;

            Assert.IsNotNull(mainWindow);
            Assert.IsNotNull(subgraphWindow);
            Assert.AreNotEqual(mainWindow, subgraphWindow);
            yield return null;

            Assert.IsFalse(EditorUtility.IsDirty(subgraph));
            var mainSubgraphContext = subgraph.children.Single();
            mainSubgraphContext.position = new Vector2(123, 456);
            Assert.IsTrue(EditorUtility.IsDirty(subgraph));
            subgraphWindow.graphView.OnSave();
            for (int i = 0; i < 8; ++i)
            {
                mainWindow.Focus();
                yield return null;
            }
            Assert.IsFalse(mainGraph.IsExpressionGraphDirty());
            Assert.AreEqual(VFXCompilationMode.Edition, VisualEffectAssetUtility.GetCompilationMode(mainResource.asset));
        }

        [UnityTest]
        public IEnumerator ConvertToSubGraphOperator_And_Delete_Subgraph()
        {
            //Initial Setup using common ConvertToSubGraphOperator
            VFXTestCommon.CloseAllVFXWindow();
            Assert.AreEqual(0, VFXViewWindow.GetAllWindows().Count);

            var previousTest = ConvertToSubGraphOperator();
            while (previousTest.MoveNext())
                yield return previousTest.Current;

            Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count);
            var activeWindowsOnMainGraph = VFXViewWindow.GetAllWindows().Single();

            //Sanity check first
            Assert.IsFalse(activeWindowsOnMainGraph.displayedResource.isSubgraph);

            var currentMainGraph = activeWindowsOnMainGraph.graphView.controller.model.GetGraph();
            Assert.IsNotNull(currentMainGraph);

            var vfxSubgraphOperator = currentMainGraph.children.OfType<VFXSubgraphOperator>().Single();
            Assert.IsNotNull(vfxSubgraphOperator);

            var subChildrenProperty = vfxSubgraphOperator.GetType().GetField("m_SubChildren", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(subChildrenProperty);

            var subChildren = (VFXModel[])subChildrenProperty.GetValue(vfxSubgraphOperator);
            Assert.IsNotNull(subChildren);
            Assert.AreEqual(3, subChildren.Length);

            var subGraphPath = AssetDatabase.GetAssetPath(vfxSubgraphOperator.subgraph);
            var subGraphGuid = AssetDatabase.GUIDFromAssetPath(subGraphPath);
            var currentAssetPath = AssetDatabase.GetAssetPath(currentMainGraph);
            var currentContent = File.ReadAllText(currentAssetPath);
            Assert.IsFalse(currentContent.Contains("fa71feae8df37b6479bb8bc6ab99f797"), "There is already a subgraph operator reference saved on disk");
            Assert.IsFalse(currentContent.Contains(subGraphGuid.ToString()), "The subgraph reference exist in main graph, makes this test useless");

            //Now, delete the subgraph, expect a recompile/update
            AssetDatabase.DeleteAsset(testSubgraphSubOperatorAssetName);

            currentMainGraph = activeWindowsOnMainGraph.graphView.controller.model.GetGraph();
            Assert.IsNotNull(currentMainGraph);

            vfxSubgraphOperator = currentMainGraph.children.OfType<VFXSubgraphOperator>().Single();
            Assert.IsNotNull(vfxSubgraphOperator);

            subChildren = (VFXModel[])subChildrenProperty.GetValue(vfxSubgraphOperator);
            Assert.IsNull(subChildren);
        }
    }
}
