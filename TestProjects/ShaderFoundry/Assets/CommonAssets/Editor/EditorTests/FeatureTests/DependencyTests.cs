using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class DependencyTests // : BlockTestRenderer
    {
        [Test]
        public void NoDependency_NotInGeneratedShader()
        {
            var container = new ShaderContainer();

            SubShaderDescriptor ModifySubShader(SubShaderDescriptor subShader)
            {
                subShader.shaderDependencies = null;
                return subShader;
            }

            var shaderCode = BlockTestRenderer.BuildSimpleSurfaceShader(container, "TestShader", Enumerable.Empty<Block>(), ModifySubShader);

            string expectedCode = "Dependency";
            Assert.AreEqual(-1, shaderCode.IndexOf(expectedCode), $"Unexpected code is present: {expectedCode}");
        }

        [Test]
        public void Dependency_ShowsUpInGeneratedShader()
        {
            var container = new ShaderContainer();

            SubShaderDescriptor ModifySubShader(SubShaderDescriptor subShader)
            {
                subShader.shaderDependencies = new List<ShaderGraph.ShaderDependency>()
                {
                    new ShaderGraph.ShaderDependency()
                    {
                        dependencyName = "MyDependency",
                        shaderName = "MyShader"
                    }
                };
                return subShader;
            }

            var shaderCode = BlockTestRenderer.BuildSimpleSurfaceShader(container, "TestShader", Enumerable.Empty<Block>(), ModifySubShader);

            string expectedCode = "Dependency \"MyDependency\" = \"MyShader\"";
            Assert.AreNotEqual(-1, shaderCode.IndexOf(expectedCode), $"Missing expected code: {expectedCode}");
        }
    }
}
