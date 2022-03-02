using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    class BlockTestRenderer : FoundryTestRenderer
    {
        // This is to make it easier to inject custom types for all tests if needed.
        internal static ShaderContainer CreateContainer()
        {
            var container = new ShaderContainer();

            var builder = new ShaderType.StructBuilder(container, "VTPropertyWithTextureType");
            builder.DeclaredExternally();
            builder.AddInclude("Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl");
            builder.Build();

            return container;
        }

        // Builds a simple shader using a single block to add to the surface customization point.
        internal static string BuildSimpleSurfaceBlockShader(ShaderContainer container, string shaderName, Block block)
        {
            var builder = new ShaderFoundry.ShaderBuilder();
            SimpleBlockShaderBuilder.BuildCallback callback = (ShaderContainer container, CustomizationPoint vertexCP, CustomizationPoint surfaceCP, out CustomizationPointInstance vertexCPInst, out CustomizationPointInstance surfaceCPInst) =>
            {
                vertexCPInst = CustomizationPointInstance.Invalid;

                var colorBlockInstance = SimpleBlockShaderBuilder.BuildSimpleBlockInstance(container, block);

                // The order of these block is what determines how the inputs/outputs are resolved
                var cpDescBuilder = new CustomizationPointInstance.Builder(container, surfaceCP);
                cpDescBuilder.BlockInstances.Add(colorBlockInstance);

                surfaceCPInst = cpDescBuilder.Build();
            };
            SimpleBlockShaderBuilder.Build(container, shaderName, callback, builder);
            return builder.ToString();
        }

        internal Shader BuildSimpleSurfaceBlockShaderObject(ShaderContainer container, string shaderName, Block block)
        {
            var shaderCode = BuildSimpleSurfaceBlockShader(container, shaderName, block);
            var shader = ShaderUtil.CreateShaderAsset(shaderCode);
            CheckForShaderErrors(shader);
            return shader;
        }

        internal int TestSurfaceBlockIsConstantColor(ShaderContainer container, string shaderName, Block block, Color expectedColor, SetupMaterialDelegate setupMaterial = null, int expectedIncorrectPixels = 0, int errorThreshold = 0, bool compareAlpha = true, bool reportArtifacts = true)
        {
            GraphicsFormat oldFormat = defaultFormat;
            defaultFormat = GraphicsFormat.R32G32B32A32_SFloat;

            var shaderCode = BuildSimpleSurfaceBlockShader(container, shaderName, block);
            var shader = ShaderUtil.CreateShaderAsset(shaderCode);

            ResetTestReporting();
            int result = TestShaderIsConstantColor(shader, shaderName, expectedColor, setupMaterial, expectedIncorrectPixels, errorThreshold, compareAlpha, reportArtifacts);
            ReportTests();

            UnityEngine.Object.DestroyImmediate(shader);
            defaultFormat = oldFormat;
            return result;
        }
    }
}
