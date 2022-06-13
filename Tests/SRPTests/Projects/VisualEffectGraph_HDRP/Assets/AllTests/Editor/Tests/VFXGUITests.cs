#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;

using UnityEditor.VFX.UI;
using UnityEditor.VFX.Block.Test;
using UnityEngine.UIElements;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.Operator;

using Debug = UnityEngine.Debug;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXGUITests
    {
        [OneTimeTearDown]
        public void DestroyTestAssets()
        {
            var window = EditorWindow.GetWindow<VFXViewWindow>();
            window.Close();
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [Test]
        public void CreateFlowEdgesTest()
        {
            var viewController = StartEditTestAsset();

            var allContexts = VFXLibrary.GetContexts().ToArray();
            var eventContextDesc = allContexts.First(t => t.model.contextType == VFXContextType.Event);
            var eventContext = viewController.AddVFXContext(new Vector2(300, 100), eventContextDesc);

            var spawnerContextDesc = allContexts.First(t => t.model.contextType == VFXContextType.Spawner);
            var spawnerContext = viewController.AddVFXContext(new Vector2(300, 100), spawnerContextDesc);

            var initContextDesc = allContexts.First(t => t.model.contextType == VFXContextType.Init);
            var initContext = viewController.AddVFXContext(new Vector2(300, 100), initContextDesc);

            var updateContextDesc = allContexts.First(t => t.model.contextType == VFXContextType.Update);
            var updateContext = viewController.AddVFXContext(new Vector2(300, 1000), updateContextDesc);

            var outputContextDesc = allContexts.First(t => t.model.contextType == VFXContextType.Output && t.model.name.Contains("Particle"));
            var outputContext = viewController.AddVFXContext(new Vector2(300, 2000), outputContextDesc);

            viewController.ApplyChanges();

            var contextControllers = new List<VFXContextController>(5);

            var allContextControllers = viewController.allChildren.OfType<VFXContextController>().ToArray();
            contextControllers.Add(allContextControllers.First(t => t.model == eventContext));
            contextControllers.Add(allContextControllers.First(t => t.model == spawnerContext));
            contextControllers.Add(allContextControllers.First(t => t.model == initContext));
            contextControllers.Add(allContextControllers.First(t => t.model == updateContext));
            contextControllers.Add(allContextControllers.First(t => t.model == outputContext));

            CreateFlowEdges(viewController, contextControllers); ;
        }

        void CreateFlowEdges(VFXViewController viewController, IList<VFXContextController> contextControllers)
        {
            for (int i = 0; i < contextControllers.Count() - 1; ++i)
            {
                VFXFlowEdgeController edgeController = new VFXFlowEdgeController(contextControllers[i + 1].flowInputAnchors.First(), contextControllers[i].flowOutputAnchors.First());
                viewController.AddElement(edgeController);
            }

            viewController.ApplyChanges();
        }

        void CreateDataEdges(VFXViewController viewController, VFXContextController updateContext, IEnumerable<VFXParameter> parameters)
        {
            viewController.ApplyChanges();
            foreach (var param in parameters)
            {
                VFXParameterNodeController paramController = viewController.allChildren.OfType<VFXParameterNodeController>().First(t => t.model == param);

                VFXDataAnchorController outputAnchor = paramController.outputPorts.First() as VFXDataAnchorController;
                System.Type type = outputAnchor.portType;

                bool found = false;
                foreach (var block in updateContext.blockControllers)
                {
                    foreach (var anchor in block.inputPorts)
                    {
                        if (anchor.portType == type)
                        {
                            found = true;
                            Assert.IsTrue((anchor as VFXDataAnchorController).model.Link(outputAnchor.model));
                            break;
                        }
                    }
                    if (found)
                        break;
                }
            }
        }

        private static VFXViewController StartEditTestAsset()
        {
            var window = EditorWindow.GetWindow<VFXViewWindow>();
            window.Show();
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var viewController = VFXViewController.GetController(graph.GetResource(), true);
            window.graphView.controller = viewController;
            return viewController;
        }

        public struct CreateAllBlockParam
        {
            internal string name;
            internal VFXModelDescriptor<VFXContext> destContext;
            internal IEnumerable<VFXModelDescriptor<VFXBlock>> blocks;

            public override string ToString()
            {
                return name;
            }
        }

        //N.B.: See this thread https://unity.slack.com/archives/G1BTWN88Z/p1638288103229000?thread_ts=1638282196.222000&cid=G1BTWN88Z
        //Avoid the creation of too much blocks in context, it can times out in ApplyChanges
        //If resolved kMaximumBlockPerContext can be replaced by uint.MaxValue
        static readonly uint kMaximumBlockPerContext = 256u;

        public static bool[] kApplyChange = { true, false };

        //[UnityTest] Not really a test but helper to profile the controller invalidation.
        public IEnumerator ExperimentCreateAllBlocksTiming([ValueSource(nameof(kApplyChange))] bool applyChanges, [ValueSource(nameof(kApplyChange))] bool blocks)
        {
            var referenceBlock = VFXLibrary.GetBlocks().First(t => t.model is KillSphere);
            var referenceOperator = VFXLibrary.GetOperators().First(t => t.model is DistanceToSphere);
            var referenceContext = VFXLibrary.GetContexts().First(t => t.model is VFXBasicUpdate);

            var param = new CreateAllBlockParam()
            {
                name = "Test",
                destContext = referenceContext
            };

            var results = new List<(int, double)>();

            int modelCount = 1;
            while (modelCount < 512)
            {
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                if (modelCount >= 256)
                    modelCount += 128;
                else
                    modelCount *= 2;

                var controller = StartEditTestAsset();

                if (blocks)
                {
                    param.blocks = Enumerable.Repeat(referenceBlock, modelCount);
                    CreateAllBlocksExperiment(controller, param.destContext, param.blocks, applyChanges);
                }
                else
                {
                    var operators = Enumerable.Repeat(referenceOperator, modelCount);
                    CreateAllOperatorsExperiment(controller, operators, applyChanges);
                }

                watch.Stop();
                var stopwatchElapsed = watch.Elapsed;
                results.Add((modelCount, stopwatchElapsed.TotalMilliseconds));

                //Clean up for next experiment
                System.GC.Collect();
                var window = EditorWindow.GetWindow<VFXViewWindow>();
                window.Close();
                VFXTestCommon.DeleteAllTemporaryGraph();

                for (int i = 0; i < 8; ++i)
                    yield return null;
            }

            var report = new System.Text.StringBuilder();
            report.AppendFormat("ApplyChange : {0} - {1}", applyChanges, blocks ? "Blocks" : "Operators");
            report.AppendLine();
            foreach (var result in results)
            {
                report.AppendFormat("{0};{1}", result.Item1, result.Item2);
                report.AppendLine();
            }
            Debug.Log(report);
        }
        void CreateAllOperatorsExperiment(VFXViewController viewController, IEnumerable<VFXModelDescriptor<VFXOperator>> operators, bool applyChanges)
        {
            foreach (var op in operators)
                viewController.AddVFXOperator(new Vector2(300, 2000), op);

            if (applyChanges)
                viewController.ApplyChanges();
        }

        void CreateAllBlocksExperiment(VFXViewController viewController, VFXModelDescriptor<VFXContext> context, IEnumerable<VFXModelDescriptor<VFXBlock>> blocks, bool applyChanges)
        {
            var newContext = viewController.AddVFXContext(new Vector2(300, 2000), context);
            //if (applyChanges) //Needed for retrieving the following contextController
            viewController.ApplyChanges();

            var contextController = viewController.nodes.OfType<VFXContextController>().First(x => x.model == newContext);
            foreach (var block in blocks)
            {
                var newBlock = block.CreateInstance();
                contextController.AddBlock(0, newBlock);
            }

            if (applyChanges)
                viewController.ApplyChanges();
        }

        static IEnumerable<CreateAllBlockParam> GenerateCreateBlockParams(VFXContextType type)
        {
            VFXModelDescriptor<VFXContext> destContext;
            if (type == VFXContextType.Output)
            {
                //Exception: VFXStaticMeshOutput doesn't accept any block, fallback on VFXPlanarPrimitiveOutput
                destContext = VFXLibrary.GetContexts().First(t => t.model is VFXPlanarPrimitiveOutput);
            }
            else
            {
                destContext = VFXLibrary.GetContexts().First(t => t.model.contextType == type);
            }

            var allBlocks = GetAllBlocks(true, x => x.AcceptParent(destContext.model));
            var batchCount = (uint)Math.Ceiling((double)allBlocks.Length / kMaximumBlockPerContext);
            for (var batch = 0u; batch < batchCount; batch++)
            {
                yield return new CreateAllBlockParam()
                {
                    name = $"{type}_Batch_{batch.ToString()}",
                    destContext = destContext,
                    blocks = allBlocks.Skip((int)batch * (int)kMaximumBlockPerContext).Take((int)kMaximumBlockPerContext)
                };
            }
        }

        static IEnumerable<CreateAllBlockParam> GenerateCreateBlockParams(IEnumerable<VFXContextType> types)
        {
            return types.SelectMany(GenerateCreateBlockParams);
        }

        static readonly CreateAllBlockParam[] kCreateAllBlockParam = GenerateCreateBlockParams(new []
        {
            VFXContextType.Event,
            VFXContextType.Spawner,
            VFXContextType.Init,
            VFXContextType.Update,
            VFXContextType.Output
        }).ToArray();

        [Test]
        public void CreateAllBlocksTest([ValueSource(nameof(kCreateAllBlockParam))] CreateAllBlockParam param)
        {
            var viewController = StartEditTestAsset();
            CreateAllBlocks(viewController, param.destContext, param.blocks);
        }

        //Reduced selection for CreateAllDataEdgesTest, only testing block outputs
        static CreateAllBlockParam[] kCreateAllBlockParamOutput = kCreateAllBlockParam.Where(o => o.destContext.model.contextType == VFXContextType.Output).ToArray();
        [Test]
        public void CreateAllDataEdgesTest([ValueSource(nameof(kCreateAllBlockParamOutput))] CreateAllBlockParam param)
        {
            var viewController = StartEditTestAsset();
            var contextController = CreateAllBlocks(viewController, param.destContext, param.blocks);
            CreateDataEdges(viewController, contextController, CreateAllParameters(viewController));
        }

        VFXContextController CreateAllBlocks(VFXViewController viewController, VFXModelDescriptor<VFXContext> context, IEnumerable<VFXModelDescriptor<VFXBlock>> blocks)
        {
            var newContext = viewController.AddVFXContext(new Vector2(300, 2000), context);
            viewController.ApplyChanges();

            var contextController = viewController.nodes
                .OfType<VFXContextController>()
                .First(x =>  x.model == newContext);
            Assert.AreEqual(contextController.model, newContext);

            // Adding every block compatible with an init context
            var newBlocks = blocks.Select(x =>
            {
                var newBlock = x.CreateInstance();
                contextController.AddBlock(0, newBlock);
                return newBlock;
            }).ToArray();
            Debug.Log($"Number of blocks = {newBlocks.Length}");

            viewController.ApplyChanges();

            //We are expecting the same list from block controllers than initial block model
            var intersection = contextController.blockControllers.Select(x => x.model).Intersect(newBlocks).ToArray();
            Assert.AreEqual(newBlocks.Length, intersection.Length);
            Assert.AreEqual(newBlocks.Length, contextController.blockControllers.Count);

            return contextController;
        }

        [Test]
        public void ExpandRetractAndSetPropertyValue()
        {
            var viewController = StartEditTestAsset();

            var initContextDesc = VFXLibrary.GetContexts().First(t => typeof(VFXBasicInitialize).IsAssignableFrom(t.modelType));

            var newContext = viewController.AddVFXContext(new Vector2(300, 100), initContextDesc);
            viewController.ApplyChanges();

            var allChildContextControllers = viewController.allChildren.OfType<VFXContextController>().ToArray();
            Assert.AreEqual(1, allChildContextControllers.Length);

            var contextController = allChildContextControllers.First();

            Assert.AreEqual(contextController.model, newContext);

            // Adding every block compatible with an init context

            var blockDesc = new VFXModelDescriptor<VFXBlock>(ScriptableObject.CreateInstance<AllType>());

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

            aVector3Controller.SetPropertyValue(new Vector3(1.2f, 3.4f, 5.6f));

            Assert.AreEqual(slot.value, new Vector3(1.2f, 3.4f, 5.6f));

            aVector3Controller.ExpandPath();
            viewController.ApplyChanges();

            var vector3yController = blockController.inputPorts.OfType<VFXContextDataInputAnchorController>().First(x => x.path == "aVector3.y");

            vector3yController.SetPropertyValue(7.8f);

            Assert.AreEqual(slot.value, new Vector3(1.2f, 7.8f, 5.6f));
        }

        [Test]
        public void CreateAllOperatorsTest()
        {
            var viewController = StartEditTestAsset();
            Assert.DoesNotThrow(() =>CreateAllOperators(viewController));
        }

        [UnityTest]
        public IEnumerator CollapseTest()
        {
            var viewController = StartEditTestAsset();

            var builtInItem = VFXLibrary.GetOperators().First(t => typeof(VFXDynamicBuiltInParameter).IsAssignableFrom(t.modelType));

            var builtIn = viewController.AddVFXOperator(Vector2.zero, builtInItem);

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

        VFXOperator[] CreateAllOperators(VFXViewController viewController)
        {
            return VFXLibrary.GetOperators()
                .Select((x, i) => viewController.AddVFXOperator(new Vector2(700, 150 * i), x))
                .ToArray();
        }

        VFXParameter[] CreateAllParameters(VFXViewController viewController)
        {
            return VFXLibrary.GetParameters()
                .Select((x, i) => viewController.AddVFXParameter(new Vector2(-400, 150 * i), x))
                .ToArray();
        }

        [Test]
        public void CreateAllParametersTest()
        {
            var viewController = StartEditTestAsset();
            Assert.DoesNotThrow(() => CreateAllParameters(viewController));
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
            var window = EditorWindow.GetWindow<VFXViewWindow>();
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
            var vfxController = StartEditTestAsset();
            var sphereOperatorDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Sphere");

            var window = EditorWindow.GetWindow<VFXViewWindow>(null, true);
            var sphereOperator = vfxController.AddVFXOperator(new Vector2(4, 4), sphereOperatorDesc);
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
        public IEnumerator Check_VFXNodeProvider_Listing_SkinnedMeshSampling_From_SkinnedMeshRenderer()
        {
            var vfxController = StartEditTestAsset();

            var op = ScriptableObject.CreateInstance<VFXInlineOperator>();
            op.SetSettingValue("m_Type", (SerializableType)typeof(SkinnedMeshRenderer));

            var inlineSkinnedMeshRendererDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.model is VFXInlineOperator && o.name == VFXTypeExtension.UserFriendlyName(typeof(SkinnedMeshRenderer)));
            Assert.IsNotNull(inlineSkinnedMeshRendererDesc);
            var window = EditorWindow.GetWindow<VFXViewWindow>(null, true);
            var skinnedMeshInlineOperator = vfxController.AddVFXOperator(new Vector2(4, 4), inlineSkinnedMeshRendererDesc);

            vfxController.ApplyChanges();
            yield return null;

            var skinnedMeshInlineUI = window.graphView.GetAllNodes().FirstOrDefault(o => o.controller.model == skinnedMeshInlineOperator);
            Assert.IsNotNull(skinnedMeshInlineUI);

            var dataAnchor = skinnedMeshInlineUI.outputContainer.Children().OfType<VFXDataAnchor>().FirstOrDefault();
            Assert.IsNotNull(dataAnchor);

            var nodeProvider = dataAnchor.BuildNodeProviderForInternalTest(vfxController, new[] { typeof(VFXOperator) });
            var descriptors = nodeProvider.GetDescriptorsForInternalTest().ToArray();
            Assert.IsNotEmpty(descriptors);

            var operatorDescriptors = descriptors.Select(o => o.modelDescriptor).OfType<VFXModelDescriptor<VFXOperator>>().ToArray();
            Assert.IsNotEmpty(operatorDescriptors);

            var skinnedMeshSampleDescriptor = operatorDescriptors.Where(o => o.model is Operator.SampleMesh).ToArray();
            Assert.AreEqual(1u, skinnedMeshSampleDescriptor.Length);
            Assert.IsTrue(skinnedMeshSampleDescriptor[0].name.Contains("Skin"));
        }

        [UnityTest]
        public IEnumerator Check_Focus_On_Clear_Selection_When_No_Selection()
        {
            // Prepare
            var vfxController = StartEditTestAsset();
            var sphereOperatorDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Sphere");

            var window = EditorWindow.GetWindow<VFXViewWindow>(null, true);
            var sphereOperator = vfxController.AddVFXOperator(new Vector2(4, 4), sphereOperatorDesc);
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
            var vfxController = StartEditTestAsset();
            var initializeContextDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name == "Initialize Particle");

            var window = EditorWindow.GetWindow<VFXViewWindow>(null, true);
            var initializeContext = vfxController.AddVFXContext(new Vector2(4, 4), initializeContextDesc) as VFXBasicInitialize;
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

        private static VFXModelDescriptor<VFXBlock>[] GetAllBlocks(bool filterOut, Predicate<VFXModelDescriptor<VFXBlock>> predicate)
        {
            if (filterOut)
            {
                return VFXLibrary.GetBlocks()
                    .Where(x => predicate(x))
                    .GroupBy(x => x.category)
                    .Select(x => x.First())
                    .ToArray();
            }
            else
            {
                return VFXLibrary.GetBlocks()
                    .Where(x => predicate(x))
                    .ToArray();
            }
        }
    }
}
#endif
