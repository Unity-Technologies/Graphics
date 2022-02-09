#if !UNITY_EDITOR_OSX || MAC_FORCE_TESTS
using System;
using System.Collections;
using System.Text;
using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor.VFX.Block;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.VersionControl;

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
                uniformMapper = new VFXUniformMapper(new VFXExpressionMapper(), true),
                graphicsBufferUsage = new ReadOnlyDictionary<VFXExpression, Type>(new Dictionary<VFXExpression, Type>())
            };
            HashSet<string> dependencies = new HashSet<string>();
            var stringBuilder = VFXCodeGenerator.Build(updateContext, VFXCompilationMode.Runtime, contextCompiledData, dependencies);

            var code = stringBuilder.ToString();
            Assert.IsTrue(code.Contains(VFXBlockSourceVariantTest.sourceCodeVariant[0]));
            Assert.IsTrue(code.Contains(VFXBlockSourceVariantTest.sourceCodeVariant[1]));
        }

        [Test]
        public void Change_ShaderGraph_Properties_Order()
        {
            string vfxPath;

            // Create VFX Graph
            {
                var sg = GetShaderGraphFromTempFile("Assets/AllTests/VFXTests/GraphicsTests/Shadergraph/Unlit/sg-for-autotest.shadergraph_1");

                var graph = VFXTestCommon.MakeTemporaryGraph();
                var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
                var initContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
                var updateContext = ScriptableObject.CreateInstance<VFXBasicUpdate>();
                var outputContext = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();

                outputContext.SetSettingValue("shaderGraph", sg);

                graph.AddChild(spawnerContext);
                graph.AddChild(initContext);
                graph.AddChild(updateContext);
                graph.AddChild(outputContext);

                spawnerContext.LinkTo(initContext);
                initContext.LinkTo(updateContext);
                updateContext.LinkTo(outputContext);

                Assert.AreEqual(2, outputContext.inputSlots.Count);
                CollectionAssert.AreEqual(new [] { "extraTexture", "alphaOffset"}, outputContext.inputSlots.Select(x => x.name));

                graph.GetResource().WriteAsset();
                vfxPath = AssetDatabase.GetAssetPath(graph);
                AssetDatabase.ImportAsset(vfxPath);
            }

            // Overwrite shader graph and check exposed properties
            {
                GetShaderGraphFromTempFile("Assets/AllTests/VFXTests/GraphicsTests/Shadergraph/Unlit/sg-for-autotest.shadergraph_2");
                var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
                var updatedOutputContext = ((VFXGraph)vfxAsset.GetResource().graph).children.OfType<VFXPlanarPrimitiveOutput>().Single();

                Assert.AreEqual(3, updatedOutputContext.inputSlots.Count);
                CollectionAssert.AreEqual(new [] { "_Float", "alphaOffset", "extraTexture"}, updatedOutputContext.inputSlots.Select(x => x.name));
            }
        }

        [Test]
        public void Set_ShaderGraph_Null_No_Properties()
        {
            string vfxPath;

            // Create VFX Graph
            {
                var sg = GetShaderGraphFromTempFile("Assets/AllTests/VFXTests/GraphicsTests/Shadergraph/Unlit/sg-for-autotest.shadergraph_1");

                var graph = VFXTestCommon.MakeTemporaryGraph();
                var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
                var initContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
                var updateContext = ScriptableObject.CreateInstance<VFXBasicUpdate>();
                var outputContext = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();

                outputContext.SetSettingValue("shaderGraph", sg);

                graph.AddChild(spawnerContext);
                graph.AddChild(initContext);
                graph.AddChild(updateContext);
                graph.AddChild(outputContext);

                spawnerContext.LinkTo(initContext);
                initContext.LinkTo(updateContext);
                updateContext.LinkTo(outputContext);

                Assert.AreEqual(2, outputContext.inputSlots.Count);
                CollectionAssert.AreEqual(new [] { "extraTexture", "alphaOffset"}, outputContext.inputSlots.Select(x => x.name));

                graph.GetResource().WriteAsset();
                vfxPath = AssetDatabase.GetAssetPath(graph);
                AssetDatabase.ImportAsset(vfxPath);
            }

            // Remove reference to shader graph and check there are no more properties
            {
                var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
                var outputContext = ((VFXGraph)vfxAsset.GetResource().graph).children.OfType<VFXPlanarPrimitiveOutput>().Single();
                outputContext.SetSettingValue("shaderGraph", null);

                Assert.AreEqual(1, outputContext.inputSlots.Count);
                CollectionAssert.AreEqual(new [] { "mainTexture" }, outputContext.inputSlots.Select(x => x.name));
            }
        }

        private ShaderGraphVfxAsset GetShaderGraphFromTempFile(string tempFile)
        {
            var extension = Path.GetExtension(tempFile);
            var sgPath = Path.Combine(VFXTestCommon.tempBasePath,  Path.GetFileName(tempFile).Replace(extension, ".shadergraph"));
            var sgContent = File.ReadAllText(tempFile);
            File.WriteAllText(sgPath, sgContent);
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(sgPath);
        }
    }
}
#endif
