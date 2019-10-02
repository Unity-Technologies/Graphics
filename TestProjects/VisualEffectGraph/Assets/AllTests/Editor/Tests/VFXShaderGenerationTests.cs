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

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXShaderGenerationTests
    {
        string tempFilePath = "Assets/TmpTests/vfxTest.vfx";

        VFXGraph MakeTemporaryGraph()
        {
            if (System.IO.File.Exists(tempFilePath))
            {
                AssetDatabase.DeleteAsset(tempFilePath);
            }
            var asset = VisualEffectAssetEditorUtility.CreateNewAsset(tempFilePath);

            VisualEffectResource resource = asset.GetResource(); // force resource creation

            VFXGraph graph = ScriptableObject.CreateInstance<VFXGraph>();

            graph.visualEffectResource = resource;

            return graph;
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(tempFilePath);
        }

        [Test]
        public void GraphUsingGPUConstant()
        {
            var graph = MakeTemporaryGraph();
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
            var graph = MakeTemporaryGraph();
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
            var stringBuilder = VFXCodeGenerator.Build(updateContext, VFXCompilationMode.Runtime, contextCompiledData,dependencies);

            var code = stringBuilder.ToString();
            Assert.IsTrue(code.Contains(VFXBlockSourceVariantTest.sourceCodeVariant[0]));
            Assert.IsTrue(code.Contains(VFXBlockSourceVariantTest.sourceCodeVariant[1]));
        }
    }
}
#endif
