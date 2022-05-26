using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class CustomEditorTests // : BlockTestRenderer
    {
        [Test]
        public void NoCustomEditor_NotInGeneratedShader()
        {
            var container = new ShaderContainer();

            SubShaderDescriptor ModifySubShader(SubShaderDescriptor subShader)
            {
                subShader.shaderCustomEditor = null;
                return subShader;
            }

            var shaderCode = BlockTestRenderer.BuildSimpleSurfaceShader(container, "TestShader", Enumerable.Empty<Block>(), ModifySubShader);

            string expectedCode = "CustomEditor";
            Assert.AreEqual(-1, shaderCode.IndexOf(expectedCode), $"Unexpected code present: {expectedCode}");
        }

        [Test]
        public void GeneralCustomEditor_ShowsUpInGeneratedShader()
        {
            var container = new ShaderContainer();

            SubShaderDescriptor ModifySubShader(SubShaderDescriptor subShader)
            {
                subShader.shaderCustomEditor = "MyCustomEditor";
                return subShader;
            }

            var shaderCode = BlockTestRenderer.BuildSimpleSurfaceShader(container, "TestShader", Enumerable.Empty<Block>(), ModifySubShader);

            string expectedCode = "CustomEditorForRenderPipeline \"MyCustomEditor\" \"\"";
            Assert.AreNotEqual(-1, shaderCode.IndexOf(expectedCode), $"Missing expected code: {expectedCode}");
        }

        [Test]
        public void RenderPipelineCustomEditor_ShowsUpInGeneratedShader()
        {
            var container = new ShaderContainer();

            SubShaderDescriptor ModifySubShader(SubShaderDescriptor subShader)
            {
                subShader.shaderCustomEditors = new List<UnityEditor.ShaderGraph.ShaderCustomEditor>()
                {
                    new UnityEditor.ShaderGraph.ShaderCustomEditor()
                    {
                        shaderGUI = "MyCustomEditor",
                        renderPipelineAssetType = "MyRenderPipeline",
                    }
                };
                return subShader;
            }

            var shaderCode = BlockTestRenderer.BuildSimpleSurfaceShader(container, "TestShader", Enumerable.Empty<Block>(), ModifySubShader);

            string expectedCode = "CustomEditorForRenderPipeline \"MyCustomEditor\" \"MyRenderPipeline\"";
            Assert.AreNotEqual(-1, shaderCode.IndexOf(expectedCode), $"Missing expected code: {expectedCode}");
        }
    }
}
