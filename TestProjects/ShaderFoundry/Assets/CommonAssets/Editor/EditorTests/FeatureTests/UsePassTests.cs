using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class UsePassTests // : BlockTestRenderer
    {
        [Test]
        public void NoUsePass_NotInGeneratedShader()
        {
            var container = new ShaderContainer();

            SubShaderDescriptor ModifySubShader(SubShaderDescriptor subShader)
            {
                subShader.usePassList = null;
                return subShader;
            }

            var shaderCode = BlockTestRenderer.BuildSimpleSurfaceShader(container, "TestShader", Enumerable.Empty<Block>(), ModifySubShader);

            string expectedCode = "UsePass";
            Assert.AreEqual(-1, shaderCode.IndexOf(expectedCode), $"Unexpected code present: {expectedCode}");
        }

        [Test]
        public void UsePass_ShowsUpInGeneratedShader()
        {
            var container = new ShaderContainer();

            SubShaderDescriptor ModifySubShader(SubShaderDescriptor subShader)
            {
                subShader.usePassList = new List<string>()
                {
                    "MyOtherShader/MYSHADERPASS"
                };
                return subShader;
            }

            var shaderCode = BlockTestRenderer.BuildSimpleSurfaceShader(container, "TestShader", Enumerable.Empty<Block>(), ModifySubShader);

            string expectedCode = "UsePass \"MyOtherShader/MYSHADERPASS\"";
            Assert.AreNotEqual(-1, shaderCode.IndexOf(expectedCode), $"Missing expected code: {expectedCode}");
        }
    }
}
