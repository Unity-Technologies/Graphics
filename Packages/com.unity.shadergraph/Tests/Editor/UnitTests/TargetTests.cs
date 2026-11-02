using System.Linq;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class TargetTests
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.unityLogger.logHandler = new ConsoleLogHandler();
        }

        [Test]
        public void CanCreateBlankGraph()
        {
            GraphData graph = new GraphData();
            graph.AddContexts();

            Assert.IsNotNull(graph.activeTargets);
            Assert.AreEqual(0, graph.activeTargets.Count());
        }

        public static bool s_ForceVFXFakeTargetVisible = false;
#if !VFX_GRAPH_10_0_0_OR_NEWER
        //A barebone VFXTarget for testing coverage.
        sealed class VFXTarget : UnityEditor.ShaderGraph.Target
        {
            public VFXTarget()
            {
                displayName = "Fake VFX Target"; //Should not be displayed outside the test runner.
                isHidden = !s_ForceVFXFakeTargetVisible;
            }

            public override void GetActiveBlocks(ref UnityEditor.ShaderGraph.TargetActiveBlockContext context)
            {
                context.AddBlock(ShaderGraph.BlockFields.SurfaceDescription.BaseColor);
                context.AddBlock(ShaderGraph.BlockFields.SurfaceDescription.Alpha);
            }

            public override void GetFields(ref TargetFieldContext context)
            {
            }

            public override void Setup(ref TargetSetupContext context)
            {
            }

            public override bool IsActive() => false;
            public override bool WorksWithSRP(UnityEngine.Rendering.RenderPipelineAsset scriptableRenderPipeline)
            {
                return false;
            }

            public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, System.Action onChange, System.Action<System.String> registerUndo)
            {
            }
        }
#endif

        [Test]
        public void CanInitializeOutputTargets()
        {
            s_ForceVFXFakeTargetVisible = true;
            GraphData graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(new[] { new VFXTarget() }, null);

            Assert.IsNotNull(graph.activeTargets);
            Assert.AreEqual(1, graph.activeTargets.Count());
            Assert.AreEqual(typeof(VFXTarget), graph.activeTargets.ElementAt(0).GetType());
            s_ForceVFXFakeTargetVisible = false;
        }

        [Test]
        public void CanAddTarget()
        {
            s_ForceVFXFakeTargetVisible = true;
            GraphData graph = new GraphData();
            graph.AddContexts();

            var vfxTarget = graph.allPotentialTargets.FirstOrDefault(x => x is VFXTarget);
            graph.SetTargetActive(vfxTarget);

            Assert.IsNotNull(graph.activeTargets);
            Assert.AreEqual(1, graph.activeTargets.Count());
            Assert.AreEqual(vfxTarget, graph.activeTargets.ElementAt(0));
            s_ForceVFXFakeTargetVisible = false;
        }

        [Test]
        public void ActiveTargetsArePotentialTargets()
        {
            s_ForceVFXFakeTargetVisible = true;
            GraphData graph = new GraphData();
            graph.AddContexts();

            var vfxTarget = new VFXTarget();
            graph.SetTargetActive(vfxTarget);
            Assert.IsTrue(graph.allPotentialTargets.Contains(vfxTarget));

            s_ForceVFXFakeTargetVisible = false;
        }

        [Test]
        public void GetTargetIndexWorks()
        {
            s_ForceVFXFakeTargetVisible = true;

            GraphData graph = new GraphData();
            graph.AddContexts();

            int targetIndex = graph.GetTargetIndexByKnownType(typeof(VFXTarget));
            Assert.IsTrue(targetIndex >= 0);

            var vfxTarget = new VFXTarget();
            graph.SetTargetActive(vfxTarget);

            var targetIndex2 = graph.GetTargetIndex(vfxTarget);
            Assert.AreEqual(targetIndex, targetIndex2);

            var nonActiveVFXTarget = new VFXTarget();
            Assert.AreEqual(-1, graph.GetTargetIndex(nonActiveVFXTarget));

            s_ForceVFXFakeTargetVisible = false;
        }

        [Test]
        public void CanRemoveTarget()
        {
            s_ForceVFXFakeTargetVisible = true;
            GraphData graph = new GraphData();
            graph.AddContexts();

            var vfxTarget = new VFXTarget();
            graph.InitializeOutputs(new[] { vfxTarget }, null);

            graph.SetTargetInactive(vfxTarget);

            Assert.IsNotNull(graph.activeTargets);
            Assert.AreEqual(0, graph.activeTargets.Count());
            s_ForceVFXFakeTargetVisible = false;
        }

        [Test]
        public void CanSetBlockActive()
        {
            s_ForceVFXFakeTargetVisible = true;
            GraphData graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(new[] { new VFXTarget() }, new BlockFieldDescriptor[] { BlockFields.SurfaceDescription.BaseColor, BlockFields.SurfaceDescription.NormalTS });

            // Block active state should match VFX Target's default GetActiveBlocks
            var blocks = graph.GetNodes<BlockNode>().ToList();
            Assert.AreEqual(2, blocks.Count);
            Assert.AreEqual(BlockFields.SurfaceDescription.BaseColor, blocks[0].descriptor);
            Assert.AreEqual(true, blocks[0].isActive);
            Assert.AreEqual(BlockFields.SurfaceDescription.NormalTS, blocks[1].descriptor);
            Assert.AreEqual(false, blocks[1].isActive);
            s_ForceVFXFakeTargetVisible = false;
        }

        [Test]
        public void CanUpdateBlockActiveState()
        {
            s_ForceVFXFakeTargetVisible = true;
            GraphData graph = new GraphData();
            graph.AddContexts();
            graph.InitializeOutputs(new[] { new VFXTarget() }, new BlockFieldDescriptor[] { BlockFields.SurfaceDescription.BaseColor, BlockFields.SurfaceDescription.NormalTS });

            // Remove VFX target
            var vfxTarget = graph.allPotentialTargets.FirstOrDefault(x => x is VFXTarget);
            graph.SetTargetInactive(vfxTarget);

            var activeBlocks = graph.GetActiveBlocksForAllActiveTargets();
            graph.UpdateActiveBlocks(activeBlocks);

            // All blocks should be inactive as there are no active targets
            var blocks = graph.GetNodes<BlockNode>().ToList();
            Assert.AreEqual(2, blocks.Count);
            Assert.AreEqual(BlockFields.SurfaceDescription.BaseColor, blocks[0].descriptor);
            Assert.AreEqual(false, blocks[0].isActive);
            Assert.AreEqual(BlockFields.SurfaceDescription.NormalTS, blocks[1].descriptor);
            Assert.AreEqual(false, blocks[1].isActive);
            s_ForceVFXFakeTargetVisible = false;
        }

        abstract class TestShaderTarget : UnityEditor.ShaderGraph.Target
        {
            public override void GetActiveBlocks(ref UnityEditor.ShaderGraph.TargetActiveBlockContext context)
            {
                context.AddBlock(ShaderGraph.BlockFields.SurfaceDescription.BaseColor);
                context.AddBlock(ShaderGraph.BlockFields.SurfaceDescription.Alpha);
            }

            public override void GetFields(ref TargetFieldContext context)
            {
            }

            public static PassDescriptor BuildPass()
            {
                PassDescriptor pass = new PassDescriptor()
                {
                    // Definition
                    displayName = "DepthOnly",
                    referenceName = "SHADERPASS_DEPTHONLY",
                    lightMode = "DepthOnly",
                    useInPreview = true,

                    // Template
                    passTemplatePath = "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Templates/ShaderPass.template",
                    sharedTemplateDirectories = GenerationUtils.GetDefaultSharedTemplateDirectories(),

                    // Port Mask
                    validVertexBlocks = new BlockFieldDescriptor[] { BlockFields.VertexDescription.Position, BlockFields.VertexDescription.Normal, BlockFields.VertexDescription.Tangent },
                    validPixelBlocks = new BlockFieldDescriptor[] { BlockFields.SurfaceDescription.Alpha, BlockFields.SurfaceDescription.AlphaClipThreshold },

                    // Fields
                    structs = new StructCollection { { Structs.Attributes }, { Structs.SurfaceDescriptionInputs }, { Structs.VertexDescriptionInputs } },
                    fieldDependencies = FieldDependencies.Default,

                    // Conditional State
                    renderStates = null,
                    pragmas = new PragmaCollection { { Pragma.Target(ShaderModel.Target30) }, { Pragma.MultiCompileInstancing }, { Pragma.Vertex("vert") }, { Pragma.Fragment("frag") } },
                    defines = null,
                    keywords = null,
                    includes = null,
                    customInterpolators = null,
                };
                return pass;
            }

            public static SubShaderDescriptor BuildSubShader()
            {
                SubShaderDescriptor result = new SubShaderDescriptor()
                {
                    pipelineTag = "TestPipeline",
                    renderType = "Opaque",
                    renderQueue = "Geometry",
                    generatesPreview = true,
                    passes = new PassCollection()
                };
                result.passes.Add(BuildPass());
                return result;
            }

            public override bool IsActive() => true;

            public override bool WorksWithSRP(UnityEngine.Rendering.RenderPipelineAsset scriptableRenderPipeline) => true;

            public override void GetPropertiesGUI(ref TargetPropertyGUIContext context, System.Action onChange, System.Action<System.String> registerUndo)
            {
            }
        }

        sealed class MultiShaderTarget : TestShaderTarget
        {
            public MultiShaderTarget()
            {
                displayName = "MultiShader Test Target";
                isHidden = false;
            }

            public override void Setup(ref TargetSetupContext context)
            {
                context.AddSubShader(BuildSubShader()); // primary shader
                var ss2 = BuildSubShader();
                ss2.additionalShaderID = "{Name}-second";
                context.AddSubShader(ss2);
            }
        }

        [Test]
        public void CanBuildMultipleShaders()
        {
            GraphData graph = new GraphData();
            graph.AddContexts();

            var multiTarget = new MultiShaderTarget();
            graph.SetTargetActive(multiTarget);
            Assert.IsTrue(graph.allPotentialTargets.Contains(multiTarget));

            graph.OnEnable();
            graph.ValidateGraph();

            var generator = new Generator(graph, graph.outputNode, GenerationMode.ForReals, "MyTestShader");
            var generatedShaders = generator.allGeneratedShaders.ToList();

            Assert.AreEqual(2, generatedShaders.Count);
            Assert.IsTrue(generatedShaders[0].shaderName == "MyTestShader");
            Assert.IsTrue(generatedShaders[1].shaderName == "MyTestShader-second");
            Assert.IsTrue(generatedShaders[0].codeString.Contains("Shader \"MyTestShader\""));
            Assert.IsTrue(generatedShaders[1].codeString.Contains("Shader \"MyTestShader-second\""));

            // save graph to file on disk and import it...
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/multiShaderTest.ShaderGraph");
            FileUtilities.WriteShaderGraphToDisk(path, graph);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate | ImportAssetOptions.DontDownloadFromCacheServer);

            // check that we actually have two shader assets in the import result
            // (they won't work, but they should exist)
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            int shaderCount = assets.OfType<Shader>().Count();
            Assert.AreEqual(2, shaderCount);

            AssetDatabase.DeleteAsset(path);
        }

        sealed class DependencyShaderNameTarget : TestShaderTarget
        {
            public DependencyShaderNameTarget()
            {
                displayName = "DependencyShaderName Test Target";
                isHidden = false;
            }

            public override void Setup(ref TargetSetupContext context)
            {
                var ss = BuildSubShader();
                ss.shaderDependencies = new()
                {
                    new ShaderDependency() { dependencyName = "TestDep1", shaderName = "Name-blah" },
                    new ShaderDependency() { dependencyName = "TestDep2", shaderName = "{Name}-blah" }
                };
                context.AddSubShader(ss);
            }
        }

        [Test]
        public void ShaderNamesAreCorrectReplacedForDependencies()
        {
            GraphData graph = new GraphData();
            graph.AddContexts();

            var testTarget = new DependencyShaderNameTarget();
            graph.SetTargetActive(testTarget);
            graph.OnEnable();
            graph.ValidateGraph();

            var generator = new Generator(graph, graph.outputNode, GenerationMode.ForReals, "MyTestShader");
            var shaderCodeString = generator.generatedShader;
            Assert.IsTrue(shaderCodeString.Contains("Dependency \"TestDep1\" = \"Name-blah\""));
            Assert.IsTrue(shaderCodeString.Contains("Dependency \"TestDep2\" = \"MyTestShader-blah\""));
        }
    }
}
