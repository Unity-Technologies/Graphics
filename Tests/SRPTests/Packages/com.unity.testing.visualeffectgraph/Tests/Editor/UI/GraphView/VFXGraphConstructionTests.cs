using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VFX.UI;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.Operator;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using UnityEngine.UIElements.TestFramework;

namespace UnityEditor.VFX.Test.UI.GraphView
{
    [TestFixture]
    public class VFXGraphConstructionTests
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
        public void CreateFlowEdgesTest()
        {
            var viewController = VFXTestCommon.StartEditTestAsset();

            // Todo: find a way to retrieve the context type
            var allContexts = VFXLibrary.GetContexts().ToArray();
            var eventContextDesc = allContexts.First(t => t.modelType == typeof(VFXBasicEvent));
            var eventContext = viewController.AddVFXContext(new Vector2(300, 100), eventContextDesc.variant);

            var spawnerContextDesc = allContexts.First(t => t.modelType == typeof(VFXBasicSpawner));
            var spawnerContext = viewController.AddVFXContext(new Vector2(300, 100), spawnerContextDesc.variant);

            var initContextDesc = allContexts.First(t => t.modelType == typeof(VFXBasicInitialize));
            var initContext = viewController.AddVFXContext(new Vector2(300, 100), initContextDesc.variant);

            var updateContextDesc = allContexts.First(t => t.modelType == typeof(VFXBasicUpdate));
            var updateContext = viewController.AddVFXContext(new Vector2(300, 1000), updateContextDesc.variant);

            var outputContextDesc = allContexts.First(t => t.modelType == typeof(VFXComposedParticleOutput));
            var outputContext = viewController.AddVFXContext(new Vector2(300, 2000), outputContextDesc.variant);

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
            var referenceBlock = VFXLibrary.GetBlocks().First(t => t.model is CollisionShape);
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

                var controller = VFXTestCommon.StartEditTestAsset();

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
                var window = VFXTestCommon.GetViewWindow();
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
                viewController.AddVFXOperator(new Vector2(300, 2000), op.variant);

            if (applyChanges)
                viewController.ApplyChanges();
        }

        void CreateAllBlocksExperiment(VFXViewController viewController, VFXModelDescriptor<VFXContext> context, IEnumerable<VFXModelDescriptor<VFXBlock>> blocks, bool applyChanges)
        {
            var newContext = viewController.AddVFXContext(new Vector2(300, 2000), context.variant);
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
            VFXModelDescriptor<VFXContext> destContext = null;
            if (type == VFXContextType.Output)
            {
                //Exception: VFXStaticMeshOutput doesn't accept any block, fallback on VFXPlanarPrimitiveOutput
                destContext = VFXLibrary.GetContexts().First(t => t.modelType == typeof(VFXPlanarPrimitiveOutput));
            }
            else
            {
                destContext = VFXLibrary.GetContexts().First(t => t.model.contextType == type);
            }

            var allBlocks = GetAllBlocks(true, x => destContext.model.AcceptChild(x.model));
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
            var viewController = VFXTestCommon.StartEditTestAsset();
            CreateAllBlocks(viewController, param.destContext, param.blocks);
        }

        //Reduced selection for CreateAllDataEdgesTest, only testing block outputs
        // Todo: find a way to retrieve the context type
        static CreateAllBlockParam[] kCreateAllBlockParamOutput = kCreateAllBlockParam./*Where(o => o.destContext.model.contextType == VFXContextType.Output).*/ToArray();
        [Test]
        public void CreateAllDataEdgesTest([ValueSource(nameof(kCreateAllBlockParamOutput))] CreateAllBlockParam param)
        {
            var viewController = VFXTestCommon.StartEditTestAsset();
            var contextController = CreateAllBlocks(viewController, param.destContext, param.blocks);
            CreateDataEdges(viewController, contextController, CreateAllParameters(viewController));
        }

        VFXContextController CreateAllBlocks(VFXViewController viewController, VFXModelDescriptor<VFXContext> context, IEnumerable<VFXModelDescriptor<VFXBlock>> blocks)
        {
            var newContext = viewController.AddVFXContext(new Vector2(300, 2000), context.variant);
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
        public void CreateAllOperatorsTest()
        {
            var viewController = VFXTestCommon.StartEditTestAsset();
            Assert.DoesNotThrow(() =>
            {
                var operators = CreateAllOperators(viewController);
                Debug.Log($"Created {operators.Length} operators");
            });
        }

        VFXOperator[] CreateAllOperators(VFXViewController viewController)
        {
            return VFXLibrary.GetOperators()
                .SelectMany(x => new [] {x}.Concat(x.subVariantDescriptors))
                .Select((x, i) => viewController.AddVFXOperator(new Vector2(700, 150 * i), x.variant))
                .ToArray();
        }

        VFXParameter[] CreateAllParameters(VFXViewController viewController)
        {
            return VFXLibrary.GetParameters()
                .Select((x, i) => viewController.AddVFXParameter(new Vector2(-400, 150 * i), x.variant))
                .ToArray();
        }

        [Test]
        public void CreateAllParametersTest()
        {
            var viewController = VFXTestCommon.StartEditTestAsset();
            Assert.DoesNotThrow(() => CreateAllParameters(viewController));
        }

        [UnityTest]
        public IEnumerator Check_VFXNodeProvider_Listing_SkinnedMeshSampling_From_SkinnedMeshRenderer()
        {
            var vfxController = VFXTestCommon.StartEditTestAsset();

            var op = ScriptableObject.CreateInstance<VFXInlineOperator>();
            op.SetSettingValue("m_Type", (SerializableType)typeof(SkinnedMeshRenderer));

            var inlineSkinnedMeshRendererDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.modelType == typeof(VFXInlineOperator) && o.name == typeof(SkinnedMeshRenderer).UserFriendlyName());
            Assert.IsNotNull(inlineSkinnedMeshRendererDesc.variant);
            var window = VFXTestCommon.GetViewWindow();
            var skinnedMeshInlineOperator = vfxController.AddVFXOperator(new Vector2(4, 4), inlineSkinnedMeshRendererDesc.variant);

            vfxController.ApplyChanges();
            yield return null;

            var skinnedMeshInlineUI = window.graphView.GetAllNodes().FirstOrDefault(o => o.controller.model == skinnedMeshInlineOperator);
            Assert.IsNotNull(skinnedMeshInlineUI);

            var dataAnchor = skinnedMeshInlineUI.outputContainer.Children().OfType<VFXDataAnchor>().FirstOrDefault();
            Assert.IsNotNull(dataAnchor);

            var nodeProvider = dataAnchor.BuildNodeProviderForInternalTest(vfxController, new[] { typeof(VFXOperator) });
            var descriptors = nodeProvider.GetDescriptorsForInternalTest().ToArray();
            Assert.IsNotEmpty(descriptors);

            var operatorDescriptors = descriptors.Where(o => o.modelType.IsSubclassOf(typeof(VFXOperator))).ToArray();
            Assert.IsNotEmpty(operatorDescriptors);

            var skinnedMeshSampleDescriptor = operatorDescriptors.Where(o => o.modelType == typeof(SampleMesh)).ToArray();
            Assert.AreEqual(1u, skinnedMeshSampleDescriptor.Length);
            Assert.IsTrue(skinnedMeshSampleDescriptor[0].name.Contains("Skin"));
        }

        private static VFXModelDescriptor<VFXBlock>[] GetAllBlocks(bool filterOut, Predicate<VFXModelDescriptor<VFXBlock>> predicate)
        {
            if (filterOut)
            {
                return VFXLibrary.GetBlocks()
                    .Where(x => predicate(x))
                    .SelectMany(x => new [] { x }.Concat(x.subVariantDescriptors))
                    .Cast<VFXModelDescriptor<VFXBlock>>()
                    .ToArray();
            }
            else
            {
                return VFXLibrary.GetBlocks()
                    .Where(x => predicate(x))
                    .SelectMany(x => new [] { x }.Concat(x.subVariantDescriptors))
                    .Cast<VFXModelDescriptor<VFXBlock>>()
                    .ToArray();
            }
        }
    }
}
