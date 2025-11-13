#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;
using Moq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Search;
using UnityEditor.SearchService;
using UnityEditor.VFX.UI;
using UnityEditor.VFX.Block.Test;
using UnityEngine.UIElements;
using UnityEngine.VFX;
using UnityEditor.VFX.Block;
using UnityEditor.VFX.Operator;

using Debug = UnityEngine.Debug;
using RangeAttribute = UnityEngine.RangeAttribute;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXGUITests
    {
        const string TempDirectoryName = "Assets/TmpTests";

        [OneTimeSetUp]
        public void Init()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
        }

        [OneTimeTearDown]
        public void DestroyTestAssets()
        {
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
            VFXTestCommon.DeleteAllTemporaryGraph();
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
            var vfxController = VFXTestCommon.StartEditTestAsset();
            var sphereOperatorDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Sphere");

            var window = EditorWindow.GetWindow<VFXViewWindow>(null, true);
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
        public IEnumerator Check_VFXNodeProvider_Listing_SkinnedMeshSampling_From_SkinnedMeshRenderer()
        {
            var vfxController = VFXTestCommon.StartEditTestAsset();

            var op = ScriptableObject.CreateInstance<VFXInlineOperator>();
            op.SetSettingValue("m_Type", (SerializableType)typeof(SkinnedMeshRenderer));

            var inlineSkinnedMeshRendererDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.modelType == typeof(VFXInlineOperator) && o.name == typeof(SkinnedMeshRenderer).UserFriendlyName());
            Assert.IsNotNull(inlineSkinnedMeshRendererDesc.variant);
            var window = EditorWindow.GetWindow<VFXViewWindow>(null, true);
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

        [UnityTest]
        public IEnumerator Check_Focus_On_Clear_Selection_When_No_Selection()
        {
            // Prepare
            var vfxController = VFXTestCommon.StartEditTestAsset();
            var sphereOperatorDesc = VFXLibrary.GetOperators().FirstOrDefault(o => o.name == "Sphere");

            var window = EditorWindow.GetWindow<VFXViewWindow>(null, true);
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
        [Description("When a subgraph is entered in-place but that subgraph is already opened, then current tab is left unchanged and the tab with that subgraph is focused")]
        public IEnumerator Open_Subgraph_In_Same_Tab_When_Its_Already_Opened()
        {
            // Prepare
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
            var window = CreateSimpleVFXGraph();
            var graphTitle = window.titleContent.text;

            yield return null;

            // Get Set Lifetime node and convert it to subgraph block
            var controllers = GetBlocks(window, "Set".Label(false).AppendLiteral("Lifetime")).Take(1);
            var subgraphFileName = TempDirectoryName + $"/subgraph_{GUID.Generate()}.vfxblock";
            VFXConvertSubgraph.ConvertToSubgraphBlock(window.graphView, controllers, Rect.zero, subgraphFileName);

            // Open created subgraph
            AssetDatabase.ImportAsset(subgraphFileName);
            var vfx = AssetDatabase.LoadAssetAtPath<VisualEffectSubgraph>(subgraphFileName);
            var subgraphWindow = VFXViewWindow.GetWindow(vfx.GetOrCreateResource(), true);

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
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
            var window = CreateSimpleVFXGraph();

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

        [UnityTest]
        [Description("When a subgraph is entered in-place a back button is available and allow to reload original graph in that same tab")]
        public IEnumerator Open_Subgraph_In_Same_Tab_And_Go_Back()
        {
            // Prepare
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
            var window = CreateSimpleVFXGraph();
            var originalTitle = window.titleContent.text;

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
            Assert.AreEqual(true, window.CanPopResource());

            // Go back to original graph
            window.PopResource();

            yield return null;

            Assert.AreEqual(1, VFXViewWindow.GetAllWindows().Count);
            Assert.AreEqual(originalTitle, window.titleContent.text);
        }

        [UnityTest]
        [Description("If we go back to original graph but that graph is already opened, then the current tab is left unchanged and the focus is given to the opened graph window")]
        public IEnumerator Open_Subgraph_In_Same_Tab_And_Go_Back_And_Original_Graph_Is_Opened()
        {
            // Prepare
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
            var window = CreateSimpleVFXGraph();
            var originalTitle = window.titleContent.text;
            var originalResource = window.displayedResource;

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

            // Open the original resource
            var originalWindow = VFXViewWindow.GetWindow(originalResource, true);
            originalWindow.LoadResource(originalResource);
            Assert.AreEqual(2, VFXViewWindow.GetAllWindows().Count);

            // Go back to original graph
            window.PopResource();

            yield return null;

            Assert.AreEqual(2, VFXViewWindow.GetAllWindows().Count);
            Assert.AreEqual(originalTitle, originalWindow.titleContent.text);
            Assert.AreEqual(Path.GetFileNameWithoutExtension(subgraphFileName), window.titleContent.text);
        }

        [UnityTest]
        [Description("When a subgraph has been entered, and the original graph has been deleted, then the current tab cannot go back anymore")]
        public IEnumerator Open_Subgraph_In_Same_Tab_And_Go_Back_And_Original_Graph_Is_Deleted()
        {
            // Prepare
            VFXViewWindow.GetAllWindows().ToList().ForEach(x => x.Close());
            var window = CreateSimpleVFXGraph();
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

        private VFXViewWindow CreateSimpleVFXGraph()
        {
            //Create a new vfx based on the usual template
            System.IO.Directory.CreateDirectory(TempDirectoryName);
            var templateString = System.IO.File.ReadAllText(VFXTestCommon.simpleParticleSystemPath);
            var fileName = TempDirectoryName + $"/{GUID.Generate()}.vfx";
            System.IO.File.WriteAllText(fileName, templateString);
            AssetDatabase.ImportAsset(fileName);

            var vfx = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(fileName);
            VFXViewWindow window = VFXViewWindow.GetWindow(vfx, true);
            window.LoadResource(vfx.GetOrCreateResource());

            return window;
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
        public IEnumerator Check_Delayed_Field_Correctly_Saved()
        {
            // Prepare
            var vfxController = VFXTestCommon.StartEditTestAsset();
            var initializeContextDesc = VFXLibrary.GetContexts().FirstOrDefault(o => o.name == "Initialize Particle");

            var window = EditorWindow.GetWindow<VFXViewWindow>(null, true);
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

        [UnityTest]
        public IEnumerator Drag_And_Drop_VFX_With_Circular_Dependency()
        {
            // Create main graph
            var window1 = CreateSimpleVFXGraph();
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

            window1 = VFXViewWindow.GetWindow((VisualEffectAsset)null, true);//.CreateWindow<VFXViewWindow>();
            window1.LoadResource(mainGraph);
            yield return null;
            Assert.AreEqual(DragAndDropVisualMode.Rejected, window1.graphView.GetDragAndDropModeForVisualEffectObject(subgraphResource.asset), "Should not be able to create a circular dependency");
        }

        public class TestEditor : EditorWindow
        {

        }

        [Test]
        public void Check_NumericPropertyRM_Float_MinAttribute()
        {
            CheckNumericPropertyRM(x => new FloatPropertyRM(x, 60f), new MinAttribute(0f), 0f, new List<(float, float)> { (-5f, 0f), (5f, 5f )});
        }

        [Test]
        public void Check_NumericPropertyRM_Int_MinAttribute()
        {
            CheckNumericPropertyRM(x => new IntPropertyRM(x, 60f), new MinAttribute(0f), 0, new List<(int, int)> { (-1, 0), (1, 1 )});
        }

        [Test]
        public void Check_NumericPropertyRM_UInt_MinAttribute()
        {
            CheckNumericPropertyRM<uint, long>(x => new UintPropertyRM(x, 60f), new MinAttribute(1f), 1U, new List<(uint, uint)> { (0, 1), (5, 5 )});
        }

        [Test]
        public void Check_NumericPropertyRM_Float_RangeAttribute()
        {
            CheckNumericPropertyRM(x => new FloatPropertyRM(x, 60f), new RangeAttribute(1f, 100f), 1f, new List<(float, float)> { (-5f, 1f), (105f, 100f )});
            CheckNumericPropertyRM(x => new FloatPropertyRM(x, 60f), new MinMaxAttribute(1f, 100f), 1f, new List<(float, float)> { (-5f, 1f), (105f, 100f )});
        }

        [Test]
        public void Check_NumericPropertyRM_Int_RangeAttribute()
        {
            CheckNumericPropertyRM(x => new IntPropertyRM(x, 60f), new RangeAttribute(1f, 100f), 1, new List<(int, int)> { (-5, 1), (105, 100 )});
            CheckNumericPropertyRM(x => new IntPropertyRM(x, 60f), new MinMaxAttribute(1f, 100f), 1, new List<(int, int)> { (-5, 1), (105, 100 )});
        }

        [Test]
        public void Check_NumericPropertyRM_UInt_RangeAttribute()
        {
            CheckNumericPropertyRM(x => new UintPropertyRM(x, 60f), new RangeAttribute(1f, 100f), 1U, new List<(uint, uint)> { (0, 1), (105, 100 )});
            CheckNumericPropertyRM(x => new UintPropertyRM(x, 60f), new MinMaxAttribute(1f, 100f), 1U, new List<(uint, uint)> { (0, 1), (105, 100 )});
        }

        [UnityTest]
        public IEnumerator Check_ObjectPropertyRMTextureSearch()
        {
            try
            {
                var viewController = VFXTestCommon.StartEditTestAsset();
                var texture2DDesc = VFXLibrary.GetOperators().Single(x => x.name == "Texture2D");
                viewController.AddVFXOperator(Vector2.zero, texture2DDesc.variant);
                viewController.ApplyChanges();
                yield return null;

                var window = VFXViewWindow.GetWindow(viewController.graph.GetResource(), true, true);
                var texture2DNodeUI = window.graphView.Q<VFXOperatorUI>();
                var button = texture2DNodeUI.Q<VisualElement>(null, "unity-object-field__selector");

                VFXGUITestHelper.SendDoubleClick(button, 1);
                yield return null;

                Assert.IsTrue(EditorWindow.HasOpenInstances<ObjectSelector>());

                if (ObjectSelectorSearch.HasEngineOverride())
                {
                    // Work around a bug in ObjectSelector which property searchFilter do not return correct value when the search mode is advanced
                    var searchWindow = EditorWindow.GetWindowDontShow<SearchPickerWindow>();
                    Assert.AreEqual("t:Texture2D or t:RenderTexture", searchWindow.context.searchText);
                }
                else
                {
                    Assert.AreEqual("t:Texture2D t:RenderTexture", ObjectSelector.get.searchFilter);
                }
            }
            finally
            {
                if (EditorWindow.HasOpenInstances<ObjectSelector>())
                {
                    EditorWindow.GetWindow<ObjectSelector>().Close();
                }

                if (EditorWindow.HasOpenInstances<SearchPickerWindow>())
                {
                    EditorWindow.GetWindow<SearchPickerWindow>().Close();
                }
            }
        }

        [UnityTest, Description("Covers: UUM-61929")]
        public IEnumerator Check_Selection_Is_Emptied_On_Delete()
        {
            // Prepare
            EditorWindow.GetWindow<InspectorWindow>(); // Show the inspector because the issue was triggered by our inspector editor
            EditorWindow.GetWindow<ProjectBrowser>(); // Show the project browser to select asset (so that the selection is not empty)

            var window = CreateSimpleVFXGraph();
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
            var window = CreateSimpleVFXGraph();
            yield return null;
            var assetPath = AssetDatabase.GetAssetPath(window.displayedResource.asset);

            // Act
            window.graphView.SaveAs(assetPath);

            for (var i = 0; i < 10; i++)
                yield return null;

            // Assert
            Assert.IsNotNull(window.displayedResource, "The displayedResource is null, which mean the No Asset window is displayed");
        }

        private void CheckNumericPropertyRM<T,U>(Func<IPropertyRMProvider, NumericPropertyRM<T, U>> creator, UnityEngine.PropertyAttribute attribute, T initialValue, List<(T, T)> testCases)
        {
            // Arrange
            var editor = EditorWindow.GetWindow<TestEditor>();
            try
            {
                var propertyRMProviderMock = new Mock<IPropertyRMProvider>();
                propertyRMProviderMock.SetupProperty(x => x.value, initialValue);
                propertyRMProviderMock.SetupGet(x => x.name).Returns("Mocked property");
                propertyRMProviderMock.SetupGet(x => x.attributes).Returns(new VFXPropertyAttributes(attribute));
                var numericPropertyRM = creator(propertyRMProviderMock.Object);
                editor.rootVisualElement.Add(numericPropertyRM);
                numericPropertyRM.SetValue(initialValue);

                foreach (var testCase in testCases)
                {
                    // Act
                    numericPropertyRM.Q<TextValueField<U>>().value = (U)Convert.ChangeType(testCase.Item1, typeof(U));
                    // Assert
                    propertyRMProviderMock.Object.value = testCase.Item2;
                }
            }
            finally
            {
                editor.Close();
            }
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
#endif
