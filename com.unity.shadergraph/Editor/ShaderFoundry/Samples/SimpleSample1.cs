using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal static class SimpleSample1
    {
        // Menu item to make it easier to run and output the test to a file
        //[MenuItem("Test/FoundrySimpleTest")]
        internal static void RunSimpleTest()
        {
            const string ShaderName = "SimpleSample1";
            // To build a shader, you need to provide a container and a target.
            // The shader code will be in the shader builder passed in.
            var target = SimpleSampleBuilder.GetTarget();

            var container = new ShaderContainer();
            SimpleSampleBuilder.BuildCommonTypes(container);

            var primaryShaderID = string.Empty;
            var generatedShader = SimpleSampleBuilder.Build(container, target, ShaderName, BuildSample, primaryShaderID);

            DisplayTestResult(generatedShader.shaderName, generatedShader.codeString);
        }

        internal static void BuildSample(ShaderContainer container, CustomizationPoint vertexCP, CustomizationPoint surfaceCP, out CustomizationPointInstance vertexCPInst, out CustomizationPointInstance surfaceCPInst)
        {
            // This sample overrides only the SurfaceDescription customization point.
            // This CP is composed of three blocks for an example of how blocks can be composed and
            // how input/output names can be overridden. You can just as easily only create one block to start.

            // Currently don't provide any blocks for the vertex customization point.
            vertexCPInst = CustomizationPointInstance.Invalid;

            // Build the blocks we're going to use.

            // GlobalsProvider redirects the global _TimeParameters into an available input
            var globalsProviderBlock = BuildGlobalsProviderBlock(container);
            // UvScroll scrolls 'uv' by time and a scroll speed property
            var uvScrollBlock = BuildUvScrollBlock(container);
            // AlbedoColor outputs a color variable from sampling a texture and color property
            var albedoColorBlock = BuildAlbedoColorBlock(container);

            // Now build the descriptors for each block. Blocks can be re-used multiple times within a shader.
            // The block descriptors add any unique data about the call. Currently there is no unique data,
            // but plans for manually re-mapping data between blocks is under way.
            var globalsProviderBlockDesc = SimpleSampleBuilder.BuildSimpleBlockInstance(container, globalsProviderBlock);
            var uvScrollBlockDesc = SimpleSampleBuilder.BuildSimpleBlockInstance(container, uvScrollBlock);
            var albedoColorBlockDesc = SimpleSampleBuilder.BuildSimpleBlockInstance(container, albedoColorBlock);

            // The order of these block is what determines how the inputs/outputs are resolved
            var cpDescBuilder = new CustomizationPointInstance.Builder(container, surfaceCP);
            cpDescBuilder.BlockInstances.Add(globalsProviderBlockDesc);
            cpDescBuilder.BlockInstances.Add(uvScrollBlockDesc);
            cpDescBuilder.BlockInstances.Add(albedoColorBlockDesc);

            surfaceCPInst = cpDescBuilder.Build();
        }

        internal static Block BuildGlobalsProviderBlock(ShaderContainer container)
        {
            // A sample block that redirects some global variables into available inputs
            const string BlockName = "GlobalsProvider";
            var blockBuilder = new Block.Builder(container, BlockName);

            var inputTypeBuilder = new ShaderType.StructBuilder(blockBuilder, $"{BlockName}Input");
            var outputTypeBuilder = new ShaderType.StructBuilder(blockBuilder, $"{BlockName}Output");

            // Make an output for 'TimeParameters'
            var timeParamsOutputBuilder = new StructField.Builder(container, "TimeParameters", container._float4);
            var timeParamsOutput = timeParamsOutputBuilder.Build();
            outputTypeBuilder.AddField(timeParamsOutput);

            var inputType = inputTypeBuilder.Build();
            var outputType = outputTypeBuilder.Build();

            // Simple copy the global '_TimeParameters' into the outputs
            var entryPointFnBuilder = new ShaderFunction.Builder(blockBuilder, $"{BlockName}Main", outputType);
            entryPointFnBuilder.AddInput(inputType, "inputs");
            entryPointFnBuilder.AddLine($"{outputType.Name} outputs;");
            entryPointFnBuilder.AddLine($"outputs.{timeParamsOutput.Name} = _TimeParameters;");
            entryPointFnBuilder.AddLine($"return outputs;");
            var entryPointFn = entryPointFnBuilder.Build();

            // Setup the block from the inputs, outputs, types, functions
            blockBuilder.AddType(inputType);
            blockBuilder.AddType(outputType);
            blockBuilder.SetEntryPointFunction(entryPointFn);
            return blockBuilder.Build();
        }

        internal static Block BuildUvScrollBlock(ShaderContainer container)
        {
            // Make a sample block that takes in uv and scrolls it by time and a property
            const string BlockName = "UvScroll";
            var blockBuilder = new Block.Builder(container, BlockName);

            var inputTypeBuilder = new ShaderType.StructBuilder(blockBuilder, $"{BlockName}Input");
            var outputTypeBuilder = new ShaderType.StructBuilder(blockBuilder, $"{BlockName}Output");

            // Make the uv0 variable. We can use the same variable as the input and output.
            var uvBuilder = new StructField.Builder(container, "uv0", container._float4);
            var uv0 = uvBuilder.Build();
            inputTypeBuilder.AddField(uv0);
            outputTypeBuilder.AddField(uv0);

            // Take in 'TimeParameters' as a variable
            var timeParametersBuilder = new StructField.Builder(container, "TimeParameters", container._float4);
            var timeParameters = timeParametersBuilder.Build();
            inputTypeBuilder.AddField(timeParameters);

            // Make an input for the scroll speed to use. This input will also be a property.
            // For convenience, an input can be tagged with the [Property] attribute which will automatically add it as a property.
            var scrollSpeedBuilder = new StructField.Builder(container, "_ScrollSpeed", container._float2);

            // Setup the material property info. We need to mark the default expression, the property type, and that it is a property.
            var scrollSpeedPropData = new SimpleSampleBuilder.PropertyAttributeData { DisplayName = "ScrollSpeed", DefaultValue = "(0.5, 1, 0, 0)" };
            SimpleSampleBuilder.MarkAsProperty(container, scrollSpeedBuilder, scrollSpeedPropData);
            var scrollSpeed = scrollSpeedBuilder.Build();
            inputTypeBuilder.AddField(scrollSpeed);

            var inputType = inputTypeBuilder.Build();
            var outputType = outputTypeBuilder.Build();

            // Build a function that takes in uv0, scales it by time and a speed, and then outputs it.
            var entryPointFnBuilder = new ShaderFunction.Builder(blockBuilder, $"{BlockName}Main", outputType);
            entryPointFnBuilder.AddInput(inputType, "inputs");
            entryPointFnBuilder.AddLine($"{outputType.Name} outputs;");
            entryPointFnBuilder.AddLine($"float4 uv0 = inputs.{uv0.Name};");
            entryPointFnBuilder.AddLine($"uv0.xy += inputs.{scrollSpeed.Name} * inputs.{timeParameters.Name}[0];");
            entryPointFnBuilder.AddLine($"outputs.{uv0.Name} = uv0;");
            entryPointFnBuilder.AddLine($"return outputs;");
            var entryPointFn = entryPointFnBuilder.Build();

            // Setup the block from the inputs, outputs, types, functions
            blockBuilder.AddType(inputType);
            blockBuilder.AddType(outputType);
            blockBuilder.SetEntryPointFunction(entryPointFn);
            return blockBuilder.Build();
        }

        internal static Block BuildAlbedoColorBlock(ShaderContainer container)
        {
            const string BlockName = "AlbedoColor";
            var blockBuilder = new Block.Builder(container, BlockName);

            var inputTypeBuilder = new ShaderType.StructBuilder(blockBuilder, $"{BlockName}Input");
            var outputTypeBuilder = new ShaderType.StructBuilder(blockBuilder, $"{BlockName}Output");

            // Take in uv as an input
            var uv0InputBuilder = new StructField.Builder(container, "uv0", container._float4);
            var uv0Input = uv0InputBuilder.Build();
            inputTypeBuilder.AddField(uv0Input);

            // Make an input for Color. This input will also be a property.
            // For convenience, an input can be tagged with the [Property] attribute which will automatically add it as a property.
            var colorInputBuilder = new StructField.Builder(container, "_Color", container._float4);
            var colorInputPropData = new SimpleSampleBuilder.PropertyAttributeData { DisplayName = "Color", Type = "Color", DefaultValue = "(1, 0, 0, 1)" };
            SimpleSampleBuilder.MarkAsProperty(container, colorInputBuilder, colorInputPropData);
            var colorInput = colorInputBuilder.Build();
            inputTypeBuilder.AddField(colorInput);

            // Make a texture for albedo color. Creating a texture is complicated so it's delegated to a helper.
            string albedoTexRefName = "_AlbedoTex";
            var albedoTexBuilder = new StructField.Builder(container, albedoTexRefName, container.GetType("UnityTexture2D"));
            var albedoTexPropData = new SimpleSampleBuilder.PropertyAttributeData { DisplayName = "AlbedoTex", DefaultValue = "\"white\" {}" };
            SimpleSampleBuilder.MarkAsProperty(container, albedoTexBuilder, albedoTexPropData);
            var albedoTex = albedoTexBuilder.Build();
            inputTypeBuilder.AddField(albedoTex);

            // Create an output for a float3 BaseColor.
            var colorOutBuilder = new StructField.Builder(container, "BaseColor", container._float3);
            var colorOut = colorOutBuilder.Build();
            outputTypeBuilder.AddField(colorOut);

            var inputType = inputTypeBuilder.Build();
            var outputType = outputTypeBuilder.Build();

            // Build the entry point function that samples the texture with the given uv and combines that with the color property.
            var entryPointFnBuilder = new ShaderFunction.Builder(blockBuilder, "SurfaceFn", outputType);
            entryPointFnBuilder.AddInput(inputType, "inputs");
            entryPointFnBuilder.AddLine($"{outputType.Name} outputs;");
            entryPointFnBuilder.AddLine($"float2 transformedUv = inputs.{albedoTexRefName}.GetTransformedUV(inputs.{uv0Input.Name}.xy);");
            entryPointFnBuilder.AddLine($"float4 {albedoTexRefName}Sample = tex2D(inputs.{albedoTexRefName}, transformedUv);");
            entryPointFnBuilder.AddLine($"outputs.{colorOut.Name} = inputs.{colorInput.Name}.xyz * {albedoTexRefName}Sample.xyz;");
            entryPointFnBuilder.AddLine($"return outputs;");
            var entryPointFn = entryPointFnBuilder.Build();

            // Setup the block from the inputs, outputs, types, functions
            blockBuilder.AddType(inputType);
            blockBuilder.AddType(outputType);
            blockBuilder.SetEntryPointFunction(entryPointFn);
            return blockBuilder.Build();
        }

        static void DisplayTestResult(string testName, string code)
        {
            string tempPath = string.Format($"Temp/FoundryTest_{testName}.shader");
            if (ShaderGraph.GraphUtil.WriteToFile(tempPath, code))
                ShaderGraph.GraphUtil.OpenFile(tempPath);
        }
    }
}
