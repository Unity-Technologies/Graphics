using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Test
{
    internal static class VFXShaderGenerationTestHelpers
    {
        internal static string ShaderGraphOutputPrepare(string shaderGraphPath, VFXAbstractRenderedOutput.BlendMode blendMode, VFXAbstractParticleOutput.SortActivationMode sortMode = VFXAbstractParticleOutput.SortActivationMode.Auto)
        {
            var graph = VFXTestCommon.MakeTemporaryGraph();
            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var initContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            var updateContext = ScriptableObject.CreateInstance<VFXBasicUpdate>();

            VFXAbstractParticleOutput outputContext = null;
            if (shaderGraphPath != null)
            {
                outputContext = ScriptableObject.CreateInstance<VFXComposedParticleOutput>();
                outputContext.SetSettingValue("m_Topology", new ParticleTopologyPlanarPrimitive());
            }
            else
            {
                outputContext = ScriptableObject.CreateInstance<VFXPlanarPrimitiveOutput>();
            }

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

        internal static void ShaderGraphSortingVerify(string vfxPath, bool expectingSorting)
        {
            var vfxAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(vfxPath);
            var outputContext = ((VFXGraph)vfxAsset.GetResource().graph).children.OfType<VFXAbstractParticleOutput>().FirstOrDefault();
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

        internal static ShaderGraphVfxAsset GetShaderGraphFromTempFile(string tempFile)
        {
            var extension = Path.GetExtension(tempFile);
            var sgPath = Path.Combine(VFXTestCommon.tempBasePath,  Path.GetFileName(tempFile).Replace(extension, ".shadergraph"));
            var sgContent = File.ReadAllText(FileUtil.PathToAbsolutePath(tempFile));
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
