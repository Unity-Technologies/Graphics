using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.Compilation
{
    [TestFixture]
    public class VFXParticleOutputCompilationTests
    {
        [OneTimeSetUp]
        public void Init()
        {
            VFXTestCommon.CloseAllUnecessaryWindows();
        }

        [TearDown]
        public void CleanUp()
        {
            VFXTestCommon.DeleteAllTemporaryGraph();
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
            VFXTestCommon.ReimportVFXGraph(graph);
            var assetPath = AssetDatabase.GetAssetPath(graph);

            var defaultShaderGraph = VFXShaderGraphHelpers.GetShaderGraph(output);
            Assert.IsNotNull(defaultShaderGraph);
            Assert.AreEqual(VFXResources.defaultResources.errorFallbackShaderGraph, defaultShaderGraph);
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

        [UnityTest, Description("UUM-69751")]
        public IEnumerator ShaderGraph_With_Gradient_In_Blackboard()
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/VFXSG_Gradient_Repro_69751.unitypackage";
            AssetPackage.Utility.ImportPackageImmediately(packagePath);
            AssetDatabase.SaveAssets();
            yield return null;

            var scenePath = VFXTestCommon.tempBasePath + "Repro_69751.unity";
            SceneManagement.EditorSceneManager.OpenScene(scenePath);
            for (int i = 0; i < 4; i++)
                yield return null;

            var vfxPath = VFXTestCommon.tempBasePath + "Repro_69751.vfx";
            var objets = AssetDatabase.LoadAllAssetsAtPath(vfxPath);

            var shaders = objets.OfType<Shader>().ToArray();
            var materials = objets.OfType<Material>().ToArray();

            int relevantMaterialCount = 0;
            foreach (var material in materials)
            {
                var shaderData = ShaderUtil.GetShaderData(material.shader);
                if (shaders.Contains(material.shader))
                    relevantMaterialCount++;

                int passCount = shaderData.ActiveSubshader.PassCount;
                for (int pass = 0; pass < passCount; pass++)
                {
                    ShaderUtil.CompilePass(material, pass, true);
                }
            }
            yield return null;

            Assert.AreEqual(2u, shaders.Length);
            Assert.AreEqual(2u, relevantMaterialCount);
            foreach (var shader in shaders)
            {
                var allMessages = ShaderUtil.GetShaderMessages(shader);
                Assert.AreEqual(0, allMessages.Length, allMessages.Length > 0 ? allMessages[0].message : string.Empty);

                Assert.IsFalse(ShaderUtil.ShaderHasError(shader));
                Assert.IsFalse(ShaderUtil.ShaderHasWarnings(shader));
            }
        }

        [UnityTest, Description("UUM-92778")]
        public IEnumerator ShaderGraph_Strip_Without_Any_Warnings()
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/Repro_92778.unitypackage";
            AssetPackage.Utility.ImportPackageImmediately(packagePath);
            AssetDatabase.SaveAssets();
            yield return null;

            var scenePath = VFXTestCommon.tempBasePath + "UUM-92778.unity";
            SceneManagement.EditorSceneManager.OpenScene(scenePath);
            for (int i = 0; i < 4; i++)
                yield return null;

            var vfxPath = VFXTestCommon.tempBasePath + "UUM-92778.vfx";
            var objets = AssetDatabase.LoadAllAssetsAtPath(vfxPath);

            var shaders = objets.OfType<Shader>().ToArray();
            var materials = objets.OfType<Material>().ToArray();

            int relevantMaterialCount = 0;
            foreach (var material in materials)
            {
                var shaderData = ShaderUtil.GetShaderData(material.shader);
                if (shaders.Contains(material.shader))
                    relevantMaterialCount++;

                int passCount = shaderData.ActiveSubshader.PassCount;
                for (int pass = 0; pass < passCount; pass++)
                {
                    ShaderUtil.CompilePass(material, pass, true);
                }
            }
            yield return null;

            Assert.AreEqual(3u, shaders.Length);
            Assert.AreEqual(3u, relevantMaterialCount);
            foreach (var shader in shaders)
            {
                var allMessages = ShaderUtil.GetShaderMessages(shader);
                Assert.AreEqual(0, allMessages.Length, allMessages.Length > 0 ? allMessages[0].message : string.Empty);

                Assert.IsFalse(ShaderUtil.ShaderHasError(shader));
                Assert.IsFalse(ShaderUtil.ShaderHasWarnings(shader));
            }
        }

        public static bool[] kChangeMeshLodCount =  { false, true };

		[UnityTest, Description("UUM-97850")]
        public IEnumerator Output_Mesh_With_Strip_System([ValueSource(nameof(kChangeMeshLodCount))] bool changeMeshLodCount)
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/Repro_97850.unitypackage";
            AssetPackage.Utility.ImportPackageImmediately(packagePath);
            AssetDatabase.SaveAssets();
            yield return null;

            var vfxPath = VFXTestCommon.tempBasePath + "Repro_97850.vfx";

            if (changeMeshLodCount)
            {
                var graph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath)
                    .GetResource()
                    .GetGraph();
                Assert.IsNotNull(graph);
                var meshOutput = graph.children.OfType<VFXMeshOutput>().SingleOrDefault();
                Assert.IsNotNull(meshOutput);
                meshOutput.SetSettingValue("MeshCount", 2u);
                Assert.AreEqual(1, meshOutput.meshCount); //Strip is expected to override this behavior
                AssetDatabase.ImportAsset(vfxPath);
            }

            var objets = AssetDatabase.LoadAllAssetsAtPath(vfxPath);

            var shaders = objets.OfType<Shader>().ToArray();
            var materials = objets.OfType<Material>().ToArray();

            int relevantMaterialCount = 0;
            foreach (var material in materials)
            {
                var shaderData = ShaderUtil.GetShaderData(material.shader);
                if (shaders.Contains(material.shader))
                    relevantMaterialCount++;

                int passCount = shaderData.ActiveSubshader.PassCount;
                for (int pass = 0; pass < passCount; pass++)
                {
                    ShaderUtil.CompilePass(material, pass, true);
                }
            }
            Assert.AreEqual(1u, shaders.Length);
            Assert.AreEqual(1u, relevantMaterialCount);
            yield return null;

            foreach (var shader in shaders)
            {
                var allMessages = ShaderUtil.GetShaderMessages(shader);
                Assert.AreEqual(0, allMessages.Length, allMessages.Length > 0 ? allMessages[0].message : string.Empty);

                Assert.IsFalse(ShaderUtil.ShaderHasError(shader));
                Assert.IsFalse(ShaderUtil.ShaderHasWarnings(shader));
            }
        }

        [UnityTest, Description("UUM-97805")]
        public IEnumerator Check_Validate_Graph_With_Sample_Gradient()
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/Repro_97805.unitypackage";
            AssetPackage.Utility.ImportPackageImmediately(packagePath);
            AssetDatabase.SaveAssets();
            yield return null;

            var vfxPath = VFXTestCommon.tempBasePath + "SharedVFX/VFXgraphs/StatusFX_VFXg.vfx";
            var objets = AssetDatabase.LoadAllAssetsAtPath(vfxPath);

            var shaders = objets.OfType<Shader>().ToArray();
            Assert.AreEqual(2u, shaders.Length); //N.B: There is one output which is actually an unplugged GPUEvent

            var computeShaders = objets.OfType<ComputeShader>().ToArray();
#if VFX_TESTS_HAS_URP
            //The SG from the repo is only targeting URP and uses alpha blend
            Assert.AreEqual(6u, computeShaders.Length);
#else
            Assert.AreEqual(5u, computeShaders.Length);
#endif
            yield return null;

        }
    }
}
