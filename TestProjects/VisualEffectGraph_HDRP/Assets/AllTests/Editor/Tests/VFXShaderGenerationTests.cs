#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.Text;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using System.Collections.Generic;
using UnityEditor.VFX.Block;
using UnityEngine.TestTools;
using UnityEditor.VFX.UI;
using System.Collections;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXShaderGenerationTests
    {
        [OneTimeTearDown]
        public void TearDown()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        [Test]
        public void GraphUsingGPUConstant()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var updateContext = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var blockSetVelocity = ScriptableObject.CreateInstance<SetAttribute>();
            blockSetVelocity.SetSettingValue("attribute", "velocity");

            var attributeParameter = ScriptableObject.CreateInstance<VFXAttributeParameter>();
            attributeParameter.SetSettingValue("attribute", "color");

            var add = ScriptableObject.CreateInstance<Operator.Add>();
            var length = ScriptableObject.CreateInstance<Operator.Length>();
            var float4 = VFXLibrary.GetParameters().First(o => o.name == "Vector4").CreateInstance();

            graph.AddChild(updateContext);
            updateContext.AddChild(blockSetVelocity);
            graph.AddChild(attributeParameter);
            graph.AddChild(add);
            graph.AddChild(float4);
            graph.AddChild(length);

            graph.RecompileIfNeeded();

            attributeParameter.outputSlots[0].Link(blockSetVelocity.inputSlots[0]);
            graph.RecompileIfNeeded();

            attributeParameter.outputSlots[0].Link(add.inputSlots[0]);
            float4.outputSlots[0].Link(add.inputSlots[1]);
            add.outputSlots[0].Link(length.inputSlots[0]);
            length.outputSlots[0].Link(blockSetVelocity.inputSlots[0]);
            graph.RecompileIfNeeded();
        }

        void GraphWithImplicitBehavior_Internal(VFXBlock[] initBlocks)
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var initContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var updateContext = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var outputContext = ScriptableObject.CreateInstance<VFXPointOutput>();

            graph.AddChild(spawnerContext);
            graph.AddChild(initContext);
            graph.AddChild(updateContext);
            graph.AddChild(outputContext);

            spawnerContext.LinkTo(initContext);
            initContext.LinkTo(updateContext);
            updateContext.LinkTo(outputContext);

            foreach (var initBlock in initBlocks)
            {
                initContext.AddChild(initBlock);
            }

            graph.RecompileIfNeeded();
        }

        [Test]
        public void GraphWithImplicitBehavior()
        {
            var testCasesGraphWithImplicitBehavior = new[]
            {
                new[] { ScriptableObject.CreateInstance<SetAttribute>() },
                new[] { ScriptableObject.CreateInstance<SetAttribute>() },
                new[] { ScriptableObject.CreateInstance<SetAttribute>() as VFXBlock, ScriptableObject.CreateInstance<SetAttribute>() as VFXBlock },
                new VFXBlock[] {},
            };

            testCasesGraphWithImplicitBehavior[0][0].SetSettingValue("attribute", "velocity");
            testCasesGraphWithImplicitBehavior[1][0].SetSettingValue("attribute", "lifetime");
            testCasesGraphWithImplicitBehavior[2][0].SetSettingValue("attribute", "velocity");
            testCasesGraphWithImplicitBehavior[2][1].SetSettingValue("attribute", "lifetime");
            foreach (var currentTest in testCasesGraphWithImplicitBehavior)
            {
                GraphWithImplicitBehavior_Internal(currentTest);
            }
        }

        class VFXBlockSourceVariantTest : VFXBlock
        {
            public override VFXContextType compatibleContexts
            {
                get
                {
                    return VFXContextType.InitAndUpdate;
                }
            }

            public override VFXDataType compatibleData
            {
                get
                {
                    return VFXDataType.Particle;
                }
            }

            [VFXSetting]
            public bool switchSourceCode = false;

            public static string[] sourceCodeVariant = { "/*rlbtmxcxbitlahdw*/", "/*qxrkittomkkiouqf*/" };

            public override string source
            {
                get
                {
                    return switchSourceCode ? sourceCodeVariant[0] : sourceCodeVariant[1];
                }
            }
        }

        [Test]
        public void DifferentSettingsGenerateDifferentFunction()
        {
            var graph = ScriptableObject.CreateInstance<VFXGraph>();
            var updateContext = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            graph.AddChild(updateContext);

            var blockA = ScriptableObject.CreateInstance<VFXBlockSourceVariantTest>();
            blockA.SetSettingValue("switchSourceCode", true);
            var blockB = ScriptableObject.CreateInstance<VFXBlockSourceVariantTest>();
            blockB.SetSettingValue("switchSourceCode", false);
            updateContext.AddChild(blockA);
            updateContext.AddChild(blockB);

            var contextCompiledData = new VFXContextCompiledData()
            {
                gpuMapper = new VFXExpressionMapper(),
                uniformMapper = new VFXUniformMapper(new VFXExpressionMapper(), true)
            };
            HashSet<string> dependencies = new HashSet<string>();
            var stringBuilder = VFXCodeGenerator.Build(updateContext, VFXCompilationMode.Runtime, contextCompiledData, dependencies);

            var code = stringBuilder.ToString();
            Assert.IsTrue(code.Contains(VFXBlockSourceVariantTest.sourceCodeVariant[0]));
            Assert.IsTrue(code.Contains(VFXBlockSourceVariantTest.sourceCodeVariant[1]));
        }

        public static readonly bool[] Create_Simple_Graph_Then_Remove_Edget_Between_Init_And_Update_TestCase = { true, false };

        //Cover issue from 1315593 with system name
        [UnityTest]
        public IEnumerator Create_Simple_Graph_Then_Remove_Edget_Between_Init_And_Update([ValueSource(nameof(Create_Simple_Graph_Then_Remove_Edget_Between_Init_And_Update_TestCase))] bool autoCompile)
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

            //The issue was actually in VFXView
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
            window.graphView.controller.NotifyUpdate(); //<= This function will try to update system name

            yield return null;

            window.autoCompile = bckpAutoCompile;
        }
    }
}
#endif
