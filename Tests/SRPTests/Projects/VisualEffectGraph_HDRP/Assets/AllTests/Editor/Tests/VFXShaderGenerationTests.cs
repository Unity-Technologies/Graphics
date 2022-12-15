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
using UnityEngine.TestTools;

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
                uniformMapper = new VFXUniformMapper(new VFXExpressionMapper(), true, true),
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
        public void ShaderGraph_Properties_With_Legacy_Format()
        {
            // This SG file has no category (even the default one for uncategorized properties)
            var sg = GetShaderGraphFromTempFile("Assets/AllTests/VFXTests/GraphicsTests/Shadergraph/Unlit/sg-for-autotest-legacy-format.shadergraph_1");

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

            Assert.AreEqual(1, outputContext.inputSlots.Count);
            CollectionAssert.AreEqual(new [] { "Color_test" }, outputContext.inputSlots.Select(x => x.name));

            graph.GetResource().WriteAsset();
            var vfxPath = AssetDatabase.GetAssetPath(graph);
            AssetDatabase.ImportAsset(vfxPath);
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

        string ShaderGraphOutputPrepare(string shaderGraphPath, VFXAbstractRenderedOutput.BlendMode blendMode, VFXAbstractParticleOutput.SortActivationMode sortMode = VFXAbstractParticleOutput.SortActivationMode.Auto)
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var initContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var updateContext = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            var outputContext = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();

            var shaderGraph = string.IsNullOrEmpty(shaderGraphPath) ? null : GetShaderGraphFromTempFile(shaderGraphPath);
            outputContext.SetSettingValue("blendMode", blendMode);
            outputContext.SetSettingValue("shaderGraph", shaderGraph);
            outputContext.SetSettingValue("sort", sortMode);

            graph.AddChild(spawnerContext);
            graph.AddChild(initContext);
            graph.AddChild(updateContext);
            graph.AddChild(outputContext);

            spawnerContext.LinkTo(initContext);
            initContext.LinkTo(updateContext);
            updateContext.LinkTo(outputContext);

            graph.GetResource().WriteAsset();
            var vfxPath = AssetDatabase.GetAssetPath(graph);
            AssetDatabase.ImportAsset(vfxPath);

            return vfxPath;
        }

        void ShaderGraphSortingVerify(string vfxPath, bool expectingSorting)
        {
            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            var outputContext = ((VFXGraph)vfxAsset.GetResource().graph).children.OfType<VFXPlanarPrimitiveOutput>().FirstOrDefault();
            Assert.IsNotNull(outputContext);

            var generatedComputeShaders = AssetDatabase.LoadAllAssetsAtPath(vfxPath).OfType<ComputeShader>().ToArray();
            if (expectingSorting)
            {
                Assert.IsTrue(outputContext.HasSorting());
                Assert.AreEqual(3, generatedComputeShaders.Length);
            }
            else
            {
                Assert.IsFalse(outputContext.HasSorting());
                Assert.AreEqual(2, generatedComputeShaders.Length);
            }
        }

        [Test]
        public void ShaderGraph_Opaque_Expecting_No_Sorting()
        {
            var vfxPath = ShaderGraphOutputPrepare("Assets/AllTests/VFXTests/GraphicsTests/Shadergraph/Unlit/sg-for-autotest-unlit-opaque.shadergraph_1", VFXAbstractRenderedOutput.BlendMode.Alpha);
            ShaderGraphSortingVerify(vfxPath, false);
        }

        [Test]
        public void ShaderGraph_Opaque_Expecting_Sorting_When_Forced()
        {
            var vfxPath = ShaderGraphOutputPrepare("Assets/AllTests/VFXTests/GraphicsTests/Shadergraph/Unlit/sg-for-autotest-unlit-opaque.shadergraph_1", VFXAbstractRenderedOutput.BlendMode.Alpha, VFXAbstractParticleOutput.SortActivationMode.On);
            ShaderGraphSortingVerify(vfxPath, true);
        }

        [Test]
        public void ShaderGraph_AlphaBlend_Expecting_Sorting()
        {
            var vfxPath = ShaderGraphOutputPrepare("Assets/AllTests/VFXTests/GraphicsTests/Shadergraph/Unlit/sg-for-autotest-unlit-alphablend.shadergraph_1", VFXAbstractRenderedOutput.BlendMode.Opaque);
            ShaderGraphSortingVerify(vfxPath, true);
        }

        [Test]
        public void ShaderGraph_Null_Opaque_Expecting_Sorting_When_Forced()
        {
            var vfxPath = ShaderGraphOutputPrepare(null, VFXAbstractRenderedOutput.BlendMode.Opaque, VFXAbstractParticleOutput.SortActivationMode.On);
            ShaderGraphSortingVerify(vfxPath, true);
        }

        [Test]
        public void ShaderGraph_Null_AlphaBlend_Expecting_Sorting()
        {
            var vfxPath = ShaderGraphOutputPrepare(null, VFXAbstractRenderedOutput.BlendMode.Alpha);
            ShaderGraphSortingVerify(vfxPath, true);
        }

        [Test]
        public void ShaderGraph_Null_AlphaBlend_Expecting_No_Sorting()
        {
            var vfxPath = ShaderGraphOutputPrepare(null, VFXAbstractRenderedOutput.BlendMode.Additive);
            ShaderGraphSortingVerify(vfxPath, false);
        }

        public class WrapperWindow : EditorWindow
        {
            public Action onGUIDelegate;
            public bool testRun;

            public void OnGUI()
            {
                try
                {
                    onGUIDelegate.Invoke();
                }
                catch (Exception exception)
                {
                    Debug.LogError(exception);
                }
                testRun = true;
                Close();
            }
        }

        [UnityTest, Description("Cover UUM-8053")]
        public IEnumerator ShaderGraph_Unlit_Transparent_Inspector()
        {
            var vfxPath = ShaderGraphOutputPrepare("Assets/AllTests/VFXTests/GraphicsTests/Shadergraph/Unlit/sg-for-autotest-unlit-alphablend.shadergraph_1", VFXAbstractRenderedOutput.BlendMode.Alpha, VFXAbstractParticleOutput.SortActivationMode.On);
            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            var outputContext = ((VFXGraph)vfxAsset.GetResource().graph).children.OfType<VFXPlanarPrimitiveOutput>().FirstOrDefault();

            var window = ScriptableObject.CreateInstance<WrapperWindow>();
            window.position = new Rect(0, 0, 512, 512);
            window.onGUIDelegate += () =>
            {
                var editor = Editor.CreateEditor(outputContext);
                editor.serializedObject.Update();
                editor.OnInspectorGUI();
                editor.serializedObject.ApplyModifiedProperties();
            };
            window.Show();
            yield return null;

            Assert.IsTrue(window.testRun);
            ScriptableObject.DestroyImmediate(window);
        }

        private ShaderGraphVfxAsset GetShaderGraphFromTempFile(string tempFile)
        {
            var extension = Path.GetExtension(tempFile);
            var sgPath = Path.Combine(VFXTestCommon.tempBasePath,  Path.GetFileName(tempFile).Replace(extension, ".shadergraph"));
            var sgContent = File.ReadAllText(tempFile);
            if (!Directory.Exists(VFXTestCommon.tempBasePath))
            {
                Directory.CreateDirectory(VFXTestCommon.tempBasePath);
            }

            File.WriteAllText(sgPath, sgContent);
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<ShaderGraphVfxAsset>(sgPath);
        }
    }
}
#endif
