using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine.VFX;
using UnityEditor.VFX;
using UnityEngine;

namespace UnityEditor.VFX.Test
{
    [TestFixture]
    public class VFXShaderGraphTest
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
            Assert.AreEqual("Output Particle ShaderGraph\nQuad", output.name);

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
    }
}
