#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.UI;
using UnityEditor.VFX.Block.Test;
using System.IO;

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

            var eventContextDesc = VFXLibrary.GetContexts().Where(t => t.model.contextType == VFXContextType.Event).First();
            var eventContext = viewController.AddVFXContext(new Vector2(300, 100), eventContextDesc);

            var spawnerContextDesc = VFXLibrary.GetContexts().Where(t => t.model.contextType == VFXContextType.Spawner).First();
            var spawnerContext = viewController.AddVFXContext(new Vector2(300, 100), spawnerContextDesc);

            var initContextDesc = VFXLibrary.GetContexts().Where(t => t.model.contextType == VFXContextType.Init).First();
            var initContext = viewController.AddVFXContext(new Vector2(300, 100), initContextDesc);

            var updateContextDesc = VFXLibrary.GetContexts().Where(t => t.model.contextType == VFXContextType.Update).First();
            var updateContext = viewController.AddVFXContext(new Vector2(300, 1000), updateContextDesc);

            var outputContextDesc = VFXLibrary.GetContexts().Where(t => t.model.contextType == VFXContextType.Output && t.model.name.Contains("Particle")).First();
            var outputContext = viewController.AddVFXContext(new Vector2(300, 2000), outputContextDesc);

            viewController.ApplyChanges();

            var contextControllers = new List<VFXContextController>();

            contextControllers.Add(viewController.allChildren.OfType<VFXContextController>().First(t => t.model == eventContext) as VFXContextController);
            contextControllers.Add(viewController.allChildren.OfType<VFXContextController>().First(t => t.model == spawnerContext) as VFXContextController);
            contextControllers.Add(viewController.allChildren.OfType<VFXContextController>().First(t => t.model == initContext) as VFXContextController);
            contextControllers.Add(viewController.allChildren.OfType<VFXContextController>().First(t => t.model == updateContext) as VFXContextController);
            contextControllers.Add(viewController.allChildren.OfType<VFXContextController>().First(t => t.model == outputContext) as VFXContextController);

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

        void CreateDataEdges(VFXViewController viewController, VFXContextController updateContext, List<VFXParameter> parameters)
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
            var referenceBlock = VFXLibrary.GetBlocks().Where(t => t.model is Block.KillSphere).First();
            var referenceOperator = VFXLibrary.GetOperators().Where(t => t.model is Operator.DistanceToSphere).First();
            var referenceContext = VFXLibrary.GetContexts().Where(t => t.model is VFXBasicUpdate).First();

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

            var contextController = viewController.nodes.Where(t => t is VFXContextController ctxController && ctxController.model == newContext).First() as VFXContextController;
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
                destContext = VFXLibrary.GetContexts().Where(t => t.model is VFXPlanarPrimitiveOutput).First();
            }
            else
            {
                destContext = VFXLibrary.GetContexts().Where(t => t.model.contextType == type).First();
            }

            var allBlocks = VFXLibrary.GetBlocks().Where(t => t.AcceptParent(destContext.model));

            var batchCount = (uint)Math.Ceiling((double)allBlocks.Count() / kMaximumBlockPerContext);
            for (var batch = 0u; batch < batchCount; batch++)
            {
                yield return new CreateAllBlockParam()
                {
                    name = string.Format("{0}_Batch_{1}", type, batch.ToString()),
                    destContext = destContext,
                    blocks = allBlocks.Skip((int)batch * (int)kMaximumBlockPerContext).Take((int)kMaximumBlockPerContext)
                };
            }
        }

        static IEnumerable<CreateAllBlockParam> GenerateCreateBlockParams(IEnumerable<VFXContextType> types)
        {
            return types.SelectMany(t => GenerateCreateBlockParams(t));
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

            var contextController = viewController.nodes.Where(t => t is VFXContextController && (t as VFXContextController).model == newContext).First() as VFXContextController;
            Assert.AreEqual(contextController.model, newContext);

            // Adding every block compatible with an init context
            var newBlocks  = new List<VFXBlock>();
            foreach (var block in blocks)
            {
                var newBlock = block.CreateInstance();
                contextController.AddBlock(0, newBlock);
                newBlocks.Add(newBlock);
            }

            viewController.ApplyChanges();

            //We are expecting the same list from block controllers than initial block model
            var intersection = contextController.blockControllers.Select(x => x.model).Intersect(newBlocks);
            Assert.AreEqual(newBlocks.Count, intersection.Count());
            Assert.AreEqual(newBlocks.Count, contextController.blockControllers.Count());

            return contextController;
        }

        [Test]
        public void ExpandRetractAndSetPropertyValue()
        {
            var viewController = StartEditTestAsset();

            var initContextDesc = VFXLibrary.GetContexts().Where(t => typeof(VFXBasicInitialize).IsAssignableFrom(t.modelType)).First();

            var newContext = viewController.AddVFXContext(new Vector2(300, 100), initContextDesc);
            viewController.ApplyChanges();

            Assert.AreEqual(viewController.allChildren.Where(t => t is VFXContextController).Count(), 1);

            var contextController = viewController.allChildren.Where(t => t is VFXContextController).First() as VFXContextController;

            Assert.AreEqual(contextController.model, newContext);

            // Adding every block compatible with an init context

            var blockDesc = new VFXModelDescriptor<VFXBlock>(ScriptableObject.CreateInstance<AllType>());

            var newBlock = blockDesc.CreateInstance();
            contextController.AddBlock(0, newBlock);

            Assert.IsTrue(newBlock is AllType);
            viewController.ApplyChanges();

            Assert.AreEqual(contextController.blockControllers.Where(t => t.model == newBlock).Count(), 1);

            var blockController = contextController.blockControllers.Where(t => t.model == newBlock).First();

            Assert.NotNull(blockController);

            Assert.NotZero(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).name == "aVector3").Count());

            VFXSlot slot = blockController.model.inputSlots.First(t => t.name == "aVector3");


            var aVector3Controller = blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).name == "aVector3").First() as VFXContextDataInputAnchorController;

            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.z").Count(), 1);

            aVector3Controller.ExpandPath();
            viewController.ApplyChanges();

            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.z").Count(), 1);


            aVector3Controller.RetractPath();
            viewController.ApplyChanges();

            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.x").Count(), 1);
            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.y").Count(), 1);
            Assert.AreEqual(blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.z").Count(), 1);

            aVector3Controller.SetPropertyValue(new Vector3(1.2f, 3.4f, 5.6f));

            Assert.AreEqual(slot.value, new Vector3(1.2f, 3.4f, 5.6f));

            aVector3Controller.ExpandPath();
            viewController.ApplyChanges();

            var vector3yController = blockController.inputPorts.Where(t => t is VFXContextDataInputAnchorController && (t as VFXContextDataInputAnchorController).path == "aVector3.y").First() as VFXContextDataInputAnchorController;

            vector3yController.SetPropertyValue(7.8f);

            Assert.AreEqual(slot.value, new Vector3(1.2f, 7.8f, 5.6f));
        }

        [Test]
        public void CreateAllOperatorsTest()
        {
            var viewController = StartEditTestAsset();
            CreateAllOperators(viewController);
        }

        [UnityTest]
        public IEnumerator CollapseTest()
        {
            var viewController = StartEditTestAsset();

            var builtInItem = VFXLibrary.GetOperators().Where(t => typeof(VFXDynamicBuiltInParameter).IsAssignableFrom(t.modelType)).First();

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

        List<VFXOperator> CreateAllOperators(VFXViewController viewController)
        {
            var operators = new List<VFXOperator>();

            int cpt = 0;
            foreach (var op in VFXLibrary.GetOperators())
            {
                operators.Add(viewController.AddVFXOperator(new Vector2(700, 150 * cpt), op));
                ++cpt;
            }

            return operators;
        }

        List<VFXParameter> CreateAllParameters(VFXViewController viewController)
        {
            var parameters = new List<VFXParameter>();

            int cpt = 0;
            foreach (var param in VFXLibrary.GetParameters())
            {
                parameters.Add(viewController.AddVFXParameter(new Vector2(-400, 150 * cpt), param));
                ++cpt;
            }

            return parameters;
        }

        [Test]
        public void CreateAllParametersTest()
        {
            var viewController = StartEditTestAsset();
            CreateAllParameters(viewController);
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
            var allFlowEdges = window.graphView.controller.allChildren.OfType<VFXFlowEdgeController>().ToArray();
            var flowEdgeToDelete = allFlowEdges.Where(o => o.output.context.model.contextType == VFXContextType.Init && o.input.context.model.contextType == VFXContextType.Update).ToArray();
            Assert.AreEqual(1u, flowEdgeToDelete.Length);
            window.graphView.controller.Remove(flowEdgeToDelete);
            window.graphView.controller.NotifyUpdate(); //<= This function will indirectly try to access system name before update (called by VFXView.Update
            yield return null;

            window.autoCompile = bckpAutoCompile;
        }
    }
}
#endif
