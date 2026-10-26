using System.Linq;
using NUnit.Framework;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class SampleGradientNodeTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [UnityPlatform(RuntimePlatform.WindowsEditor)]
        public void PreviewShader_CompilesForD3D12WithDXC(int nodeVersion)
        {
            var graph = new GraphData();
            var node = new SampleGradient();
            node.ChangeVersion(nodeVersion);
            graph.AddNode(node);
            graph.ValidateGraph();

            node.SetOverrideActiveState(AbstractMaterialNode.ActiveState.ExplicitActive);

            var generator = new Generator(graph, node, GenerationMode.Preview, node.name);

            StringAssert.Contains("Unity_SampleGradientV" + nodeVersion, generator.generatedShader,
                "The preview shader does not contain the node's function, so there is nothing to compile.");

            var shaderCode = generator.generatedShader.Replace("HLSLPROGRAM", "HLSLPROGRAM\n#pragma use_dxc dx12");
            var shader = ShaderUtil.CreateShaderAsset(shaderCode, false);
            try
            {
                var subshader = ShaderUtil.GetShaderData(shader).GetSubshader(0);
                for (int passIndex = 0; passIndex < subshader.PassCount; ++passIndex)
                {
                    var info = subshader.GetPass(passIndex).CompileVariant(ShaderType.Fragment, new string[0],
                        ShaderCompilerPlatform.D3D12, BuildTarget.StandaloneWindows64, GraphicsTier.Tier1, true);

                    Assert.IsTrue(info.Success, string.Join("\n", info.Messages.Select(m => m.message)));
                    Assert.IsTrue(ContainsDxilChunk(info.ShaderData), "No DXIL in the output, so the use_dxc pragma did not select DXC.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shader);
            }
        }

        static bool ContainsDxilChunk(byte[] bytecode)
        {
            for (int i = 0; i + 4 <= bytecode.Length; ++i)
            {
                if (bytecode[i] == 'D' && bytecode[i + 1] == 'X' && bytecode[i + 2] == 'I' && bytecode[i + 3] == 'L')
                    return true;
            }

            return false;
        }
    }
}
