using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX.Block;
using UnityEngine.TestTools;
using System.Collections;
using UnityEditor.Rendering;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXCodeGenerationTest
    {
        [OneTimeSetUp]
        public void Init()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
        }

        [OneTimeTearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
        }

        static string Dump(List<(string label, string system, string type)> inputs)
        {
            var entries = new List<string>();
            foreach (var input in inputs)
            {
                entries.Add($"{input.system}, {input.label}, {input.type}");
            }
            entries.Sort();

            var output = new StringBuilder(512);
            foreach (var entry in entries)
            {
                output.Append(entry);
                output.AppendLine();
            }
            return output.ToString();
        }

        [Test]
        public void Migration_ShaderGraph_Output_To_Composed()
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/VFXShaderGraphOutput_Migration.unitypackage";
            var vfxPath = VFXTestCommon.tempBasePath + "/SG_Outputs.vfx";

            AssetDatabase.ImportPackageImmediately(packagePath);
            AssetDatabase.SaveAssets(); //ease potential debug while looking at final data
            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            Assert.IsNotNull(vfxAsset);
            var vfxGraph = vfxAsset.GetOrCreateResource().GetOrCreateGraph();

            var collectedOutput = new List<(string label, string system, string type)>();
            foreach (var output in vfxGraph.children.OfType<VFXAbstractParticleOutput>())
            {
                collectedOutput.Add((output.label, output.GetData().title, output.GetType().Name));
            }

            var expectedOutput = new List<(string label, string system, string type)>()
            {
                ("Quad From Unlit", "Classic (New)", nameof(VFXComposedParticleOutput)),
                ("Mesh From Unlit", "Classic (New)", nameof(VFXComposedParticleOutput)),
                ("Strip Quad From Unlit", "Strip (New)", nameof(VFXComposedParticleStripOutput)),
                ("Mesh From Unlit", "Classic (Old)", nameof(VFXMeshOutput)),
                ("Quad From Unlit", "Classic (Old)", nameof(VFXPlanarPrimitiveOutput)),
                ("Strip Quad From Unlit", "Strip (Old)", nameof(VFXQuadStripOutput)),

#if VFX_TESTS_HAS_URP
                ("Quad From Lit URP", "Classic (Old)", "VFXURPLitPlanarPrimitiveOutput"),
                ("Mesh From Lit URP", "Classic (Old)", "VFXURPLitMeshOutput"),
                ("Quad From Lit URP", "Classic (New)", nameof(VFXComposedParticleOutput)),
                ("Mesh From Lit URP", "Classic (New)", nameof(VFXComposedParticleOutput)),
                ("Strip Quad From Lit URP", "Strip (Old)", "VFXURPLitQuadStripOutput"),
                ("Strip Quad From Lit URP", "Strip (New)", nameof(VFXComposedParticleStripOutput)),
#endif

#if VFX_TESTS_HAS_HDRP

                ("Mesh From Lit HDRP", "Classic (Old)", "VFXLitMeshOutput"),
                ("Quad From Lit HDRP", "Classic (Old)", "VFXLitPlanarPrimitiveOutput"),
                ("Quad From Lit HDRP", "Classic (New)", nameof(VFXComposedParticleOutput)),
                ("Mesh From Lit HDRP", "Classic (New)", nameof(VFXComposedParticleOutput)),
                ("Strip Quad From Lit HDRP", "Strip (Old)", "VFXLitQuadStripOutput"),
                ("Strip Quad From Lit HDRP", "Strip (New)", nameof(VFXComposedParticleStripOutput)),
#endif
            };

            var collectedDump = Dump(collectedOutput);
            var expectedDump = Dump(expectedOutput);
            Assert.AreEqual(expectedDump, collectedDump);
        }

        [Test]
        public void Composed_Output_Fallback_ShaderGraph()
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();

            var output = ScriptableObject.CreateInstance<VFXComposedParticleOutput>();
            output.SetSettingValue("m_Topology", new ParticleTopologyPlanarPrimitive());
            graph.AddChild(output);
            Assert.AreEqual("Output Particle".AppendLabel("Shader Graph") + "\nQuad", output.name);

            var contextInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            contextInitialize.LinkTo(output);
            graph.AddChild(contextInitialize);

            var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            spawner.LinkTo(contextInitialize);
            graph.AddChild(spawner);
            var assetPath = AssetDatabase.GetAssetPath(graph);
            AssetDatabase.ImportAsset(assetPath); ;

            var defaultShaderGraph = VFXShaderGraphHelpers.GetShaderGraph(output);
            Assert.IsNotNull(defaultShaderGraph);
            Assert.AreEqual(VFXResources.errorFallbackShaderGraph, defaultShaderGraph);
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            Assert.AreEqual(4, allAssets.Length);
            var material = allAssets.OfType<Material>();
            var visualEffectAsset = allAssets.OfType<VisualEffectAsset>();
            var shader = allAssets.OfType<Shader>();
            var computeShader = allAssets.OfType<ComputeShader>();
            Assert.IsNotNull(material);
            Assert.IsNotNull(visualEffectAsset);
            Assert.IsNotNull(shader);
            Assert.IsNotNull(computeShader);
        }

        static IEnumerable<string> allCompilationMode
        {
            get
            {
                foreach (var mode in Enum.GetValues(typeof(VFXCompilationMode)))
                    yield return mode.ToString();
            }
        }

        [Test]
        public void Constant_Folding_With_ShaderKeyword([ValueSource("allCompilationMode")] string compilationModeName)
        {
            VFXCompilationMode compilationMode;
            Enum.TryParse(compilationModeName, out compilationMode);

            string vfxPath = "Packages/com.unity.testing.visualeffectgraph/Scenes/025_ShaderKeywords_Constant_MultiCompile.vfx";
            Assert.IsTrue(AssetDatabase.AssetPathExists(vfxPath));

            var vfx = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath).GetResource();
            vfx.GetOrCreateGraph().SetCompilationMode(compilationMode);

            Assert.AreEqual(5, vfx.GetShaderSourceCount());

            int shaderCount = 0;
            for (int index = 0; index < 5; index++)
            {
                var source = vfx.GetShaderSource(index);
                if (source.Contains("#pragma kernel CSMain"))
                    continue;

                //Instancing is expected to be enabled for most of SG Output
                Assert.IsTrue(source.Contains("#pragma multi_compile_instancing"));

                var pragmaList = VFXTestShaderSrcUtils.GetPragmaListFromSource(source);

                var allVariants = pragmaList
                    .Where(o => o.type != null && (o.type.StartsWith("multi_compile") || o.type.StartsWith("shader_feature")))
                    .SelectMany(o => o.values)
                    .ToList();
                Assert.AreNotEqual(0, allVariants.Count);

                var matchingVariant = allVariants.Where(o => o.Contains("_SMOOTH") || o.Contains("_COLOR")).ToList();

                if (compilationMode == VFXCompilationMode.Runtime)
                {
                    Assert.AreEqual(0, matchingVariant.Count);
                }
                else
                {
                    //In edition, we are keeping locally matching variant (two colors with 8 choices + smooth)
                    Assert.AreEqual(8 + 8 + 1, matchingVariant.Count);
                }

                shaderCount++;
            }
            Assert.AreEqual(4, shaderCount);
            vfx.GetOrCreateGraph().SetCompilationMode(VFXCompilationMode.Runtime);
        }

        private IEnumerable CheckCompilation(VFXGraph vfxGraph)
        {
            var resource = vfxGraph.GetResource();
            EditorUtility.SetDirty(resource);
            var path = AssetDatabase.GetAssetPath(vfxGraph);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

            for (int i = 0; i < 4; ++i)
                yield return null;

            while (ShaderUtil.anythingCompiling)
                yield return null;

            var computeShaders = AssetDatabase.LoadAllAssetsAtPath(path).OfType<ComputeShader>().ToArray();
            Assert.AreEqual(3, computeShaders.Length);

            foreach (var computeShader in computeShaders)
            {
                var messages = ShaderUtil.GetComputeShaderMessages(computeShader);
                foreach (var message in messages)
                    Assert.AreNotEqual(ShaderCompilerMessageSeverity.Error, message.severity, message.message);

                Assert.AreEqual(0, computeShader.FindKernel("CSMain"));
                Assert.IsTrue(computeShader.IsSupported(0));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Combinatory_Position_Shape()
        {
            var vfxGraph = VFXTestCommon.CreateGraph_And_System();
            var initialize = vfxGraph.children.OfType<VFXBasicInitialize>().First();

            var orientations = new List<PositionBase.Orientation>(Enum.GetValues(typeof(PositionBase.Orientation)).OfType<PositionBase.Orientation>());
            orientations.Add(PositionBase.Orientation.Axes | PositionBase.Orientation.Direction);

            foreach (PositionBase.PositionMode positionMode in Enum.GetValues(typeof(PositionBase.PositionMode)))
            foreach (PositionBase.SpawnMode spawnMode in Enum.GetValues(typeof(PositionBase.SpawnMode)))
            foreach (PositionShapeBase.Type shape in Enum.GetValues(typeof(PositionShapeBase.Type)))
            foreach (PositionBase.Orientation orientation in orientations)
            {
                var positionShape = ScriptableObject.CreateInstance<PositionShape>();
                positionShape.SetSettingValue("positionMode", positionMode);
                positionShape.SetSettingValue("spawnMode", spawnMode);
                positionShape.SetSettingValue("shape", shape);

                //Cover maximum of written values
                positionShape.SetSettingValue("compositionPosition", AttributeCompositionMode.Blend);
                positionShape.SetSettingValue("compositionAxes", AttributeCompositionMode.Blend);
                positionShape.SetSettingValue("compositionDirection", AttributeCompositionMode.Blend);
                positionShape.SetSettingValue("applyOrientation", orientation);

                initialize.AddChild(positionShape);
            }

            foreach (var yield in CheckCompilation(vfxGraph))
            {
                yield return yield;
            }
        }

        [UnityTest]
        public IEnumerator Combinatory_Collision_Shape()
        {
            var vfxGraph = VFXTestCommon.CreateGraph_And_System();
            var initialize = vfxGraph.children.OfType<VFXBasicInitialize>().First();

            foreach (CollisionBase.Behavior behavior in new[] { CollisionBase.Behavior.Collision, CollisionBase.Behavior.Kill })
            foreach (CollisionBase.Mode mode in Enum.GetValues(typeof(CollisionBase.Mode)))
            foreach (CollisionBase.RadiusMode radiusMode in Enum.GetValues(typeof(CollisionBase.RadiusMode)))
            foreach (CollisionShapeBase.Type shape in Enum.GetValues(typeof(CollisionShapeBase.Type)))
            {
                var collisionShape = ScriptableObject.CreateInstance<CollisionShape>();
                collisionShape.SetSettingValue("behavior", behavior);
                collisionShape.SetSettingValue("mode", mode);
                collisionShape.SetSettingValue("radiusMode", radiusMode);
                collisionShape.SetSettingValue("shape", shape);

                //Cover maximum of written values
                collisionShape.SetSettingValue("roughSurface", true);

                initialize.AddChild(collisionShape);
            }

            foreach (CollisionBase.Behavior behavior in new[] { CollisionBase.Behavior.Collision, CollisionBase.Behavior.Kill })
            foreach (CollisionBase.Mode mode in Enum.GetValues(typeof(CollisionBase.Mode)))
            foreach (CollisionBase.RadiusMode radiusMode in Enum.GetValues(typeof(CollisionBase.RadiusMode)))
            foreach (Type type in new[] { typeof(CollisionSDF), typeof(CollisionDepth) })
            {
                var collisionShape = ScriptableObject.CreateInstance<CollisionShape>();
                collisionShape.SetSettingValue("behavior", behavior);
                collisionShape.SetSettingValue("mode", mode);
                collisionShape.SetSettingValue("radiusMode", radiusMode);

                //Cover maximum of written values
                collisionShape.SetSettingValue("roughSurface", true);
                initialize.AddChild(collisionShape);
            }

            foreach (var yield in CheckCompilation(vfxGraph))
            {
                yield return yield;
            }
        }

        [UnityTest, Description("UUM-69751")]
        public IEnumerator ShaderGraph_With_Gradient_In_Blackboard()
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/VFXSG_Gradient_Repro_69751.unitypackage";
            AssetDatabase.ImportPackageImmediately(packagePath);
            AssetDatabase.SaveAssets();
            yield return null;

            var scenePath = VFXTestCommon.tempBasePath + "Repro_69751.unity";
            SceneManagement.EditorSceneManager.OpenScene(scenePath);
            for (int i = 0; i < 4; i++)
                yield return null;

            var vfxPath = VFXTestCommon.tempBasePath + "Repro_69751.vfx";
            var objets = AssetDatabase.LoadAllAssetsAtPath(vfxPath);

            var shaders = objets.OfType<Shader>().ToArray();
            Assert.AreEqual(2u, shaders.Length);
            foreach (var shader in shaders)
            {
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader));
            }
        }
    }
}
