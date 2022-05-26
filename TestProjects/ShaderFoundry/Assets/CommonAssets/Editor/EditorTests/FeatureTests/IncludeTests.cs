using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class IncludeTests // : BlockTestRenderer
    {
        [Test]
        public void BlockInclude_ShowsUpInGeneratedShader()
        {
            var container = new ShaderContainer();

            var blocks = new List<Block>();

            // We may need to add actual used inputs and outputs to the block, if the linker implements block culling..
            var blockBuilder = BlockBuilderUtilities.DefineSimpleBlock(container, "MyBlock", container._float4, "inValue", container._float4, "outValue", "inputs.inValue");

            // TODO: we need to make quotes be added by the generator... shouldn't have to add them to the string specified here
            blockBuilder.AddInclude(new IncludeDescriptor.Builder(container, "\"MyIncludeFile\"").Build());
            blocks.Add(blockBuilder.Build());

            var shaderCode = BlockTestRenderer.BuildSimpleSurfaceShader(container, "TestShader", blocks);
            Debug.Log(shaderCode);

            string expectedCode = "#include \"MyIncludeFile\"";
            Assert.AreNotEqual(-1, shaderCode.IndexOf(expectedCode), $"Expected code missing: {expectedCode}");
        }

        [Test]
        public void BlockFunctionInclude_ShowsUpInGeneratedShader()
        {
            var container = new ShaderContainer();

            var blocks = new List<Block>();

            // We may need to add actual used inputs and outputs to the block, if the linker implements block culling..
            var blockBuilder = BlockBuilderUtilities.DefineSimpleBlock(container, "MyBlock", container._float4, "inValue", container._float4, "outValue", "DoStuff(inputs.inValue)");

            var func = new ShaderFunction.Builder(blockBuilder, "DoStuff", container._float4);
            func.AddInput(container._float4, "val");
            func.AddLine("return val;");
            func.AddInclude(new IncludeDescriptor.Builder(container, "\"MyIncludeFile\"").Build());
            func.Build();

            blocks.Add(blockBuilder.Build());

            var shaderCode = BlockTestRenderer.BuildSimpleSurfaceShader(container, "TestShader", blocks);
            Debug.Log(shaderCode);

            string expectedCode = "#include \"MyIncludeFile\"";
            Assert.AreNotEqual(-1, shaderCode.IndexOf(expectedCode), $"Expected code missing: {expectedCode}");
        }
    }
}
