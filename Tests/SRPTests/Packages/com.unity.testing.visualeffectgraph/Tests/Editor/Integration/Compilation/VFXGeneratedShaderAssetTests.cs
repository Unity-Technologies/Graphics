using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test.Integration.Compilation
{
    [TestFixture]
    public class VFXGeneratedShaderAssetTests
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

        public struct ShaderIndexCase
        {
            public string Name;
            internal Func<VFXAbstractRenderedOutput> fnGenerateOutput;

            public override string ToString()
            {
                return Name;
            }
        }

        public static ShaderIndexCase[] kShaderIndexCase = new[]
        {
            new ShaderIndexCase()
            {
                Name = "VFXComposedParticleOutput_Planar", fnGenerateOutput = () =>
                {
                    var output = ScriptableObject.CreateInstance<VFXComposedParticleOutput>();
                    output.SetSettingValue("m_Topology", new ParticleTopologyPlanarPrimitive());
                    return output;
                },
            },
            new ShaderIndexCase()
            {
                Name = "VFXComposedParticleOutput_Mesh", fnGenerateOutput = () =>
                {
                    var output = ScriptableObject.CreateInstance<VFXComposedParticleOutput>();
                    output.SetSettingValue("m_Topology", new ParticleTopologyMesh());
                    return output;
                },
            },
            new ShaderIndexCase()
            {
                Name = "VFXMeshOutput", fnGenerateOutput = () =>
                {
                    var output = ScriptableObject.CreateInstance<VFXMeshOutput>();
                    return output;
                },
            },
            new ShaderIndexCase()
            {
                Name = "VFXPlanarPrimitiveOutput", fnGenerateOutput = () =>
                {
                    var output = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();
                    return output;
                },
            },
        };

        [Test]
        public void CheckShaderIndexBehavior([ValueSource("kShaderIndexCase")] ShaderIndexCase shaderIndexCase)
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();

            var spawner = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            graph.AddChild(spawner);

            var contextInitialize = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            graph.AddChild(contextInitialize);
            spawner.LinkTo(contextInitialize);

            var contextUpdate = ScriptableObject.CreateInstance<VFXBasicUpdate>();
            graph.AddChild(contextUpdate);
            contextInitialize.LinkTo(contextUpdate);

            var output = ScriptableObject.CreateInstance<VFXComposedParticleOutput>();
            output.SetSettingValue("m_Topology", new ParticleTopologyPlanarPrimitive());
            graph.AddChild(output);
            contextUpdate.LinkTo(output);

            var vfxPath = AssetDatabase.GetAssetPath(graph);
            VFXTestCommon.ReimportVFXGraph(graph);

            var allAssets = AssetDatabase.LoadAllAssetsAtPath(vfxPath);
            Assert.AreEqual(5, allAssets.Length);

            var resource = allAssets.OfType<VisualEffectAsset>().Single().GetResource();
            var invalidPathChars = System.IO.Path.GetInvalidPathChars();
            Assert.AreNotEqual(0, invalidPathChars.Length);

            foreach (var subAsset in allAssets)
            {
                if (subAsset is VisualEffectAsset)
                    continue;
                var shaderIndex = resource.GetShaderIndex(subAsset);
                Assert.IsTrue(shaderIndex >= 0);

                var shaderPath = resource.GetShaderSourceName(shaderIndex);
                Assert.IsFalse(string.IsNullOrEmpty(shaderPath));
                foreach (var invalidChar in invalidPathChars)
                    Assert.IsFalse(shaderPath.Contains(invalidChar), "Found an occurence of '{0}' in '{1}", invalidChar, shaderPath);
            }
        }

        [Test]
        public void CheckContextLetterInShaderName()
        {
            var packagePath = "Packages/com.unity.testing.visualeffectgraph/Tests/Editor/Data/DualOutput_ContextLetters.unitypackage";
            var vfxPath = VFXTestCommon.tempBasePath + "/DualOutput.vfx";

            AssetPackage.Utility.ImportPackageImmediately(packagePath);
            AssetDatabase.SaveAssets(); //ease potential debug while looking at final data

            var allAssets = AssetDatabase.LoadAllAssetsAtPath(vfxPath);
            var resource = allAssets.OfType<VisualEffectAsset>().Single().GetResource();

            int shaderAndMaterialCount = 0;
            foreach (var subAsset in allAssets)
            {
                if (subAsset is VisualEffectAsset)
                    continue;
                var shaderIndex = resource.GetShaderIndex(subAsset);
                Assert.IsTrue(shaderIndex >= 0);
                var shaderPath = resource.GetShaderSourceName(shaderIndex);
                if (shaderPath.Contains("Render Quad"))
                {
                    shaderAndMaterialCount++;
                    Assert.IsTrue(shaderPath.Contains("A ") || shaderPath.Contains("B "));
                }
            }
            Assert.AreEqual(8, shaderAndMaterialCount);
        }
    }
}
