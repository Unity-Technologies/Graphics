using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class FallbackTests // : BlockTestRenderer
    {
        [Test]
        public void NoFallbackSet_DefaultFallbackIsUsedInGeneratedShader()
        {
            var container = new ShaderContainer();
            var shaderCode = BlockTestRenderer.BuildSimpleSurfaceShader(container, "TestShader", Enumerable.Empty<Block>());

            // LegacyTemplateProvider injects the shadergraph fallback to emulate ShaderGraph generator behavior
            string expectedCode = "FallBack \"Hidden/Shader Graph/FallbackError\"";
            Assert.AreNotEqual(-1, shaderCode.IndexOf(expectedCode), $"Missing expected code: {expectedCode}");
        }

        [Test]
        public void FallbackSet_IsUsedInGeneratedShader()
        {
            var container = new ShaderContainer();

            SubShaderDescriptor ModifySubShader(SubShaderDescriptor subShader)
            {
                subShader.shaderFallback = "MyShaderFallback";
                return subShader;
            }

            var shaderCode = BlockTestRenderer.BuildSimpleSurfaceShader(container, "TestShader", Enumerable.Empty<Block>(), ModifySubShader);

            string expectedCode = "FallBack \"MyShaderFallback\"";
            Assert.AreNotEqual(-1, shaderCode.IndexOf(expectedCode), $"Missing expected code: {expectedCode}");
        }
    }
}
