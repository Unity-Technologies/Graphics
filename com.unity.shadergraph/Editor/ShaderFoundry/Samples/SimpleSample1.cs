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
            var shaderBuilder = new ShaderBuilder();
            SimpleSampleBuilder.Build(container, target, ShaderName, BuildSample, shaderBuilder);

            var code = shaderBuilder.ToString();
            DisplayTestResult(ShaderName, code);
        }

        internal static void BuildSample(ShaderContainer container, CustomizationPoint vertexCP, CustomizationPoint surfaceCP, out CustomizationPointDescriptor vertexCPDesc, out CustomizationPointDescriptor surfaceCPDesc)
        {
            // This sample overrides only the SurfaceDescription customization point.
            // This CP is composed of three blocks for an example of how blocks can be composed and
            // how input/output names can be overridden. You can just as easily only create one block to start.

            // Currently don't provide any blocks for the vertex customization point.
            vertexCPDesc = CustomizationPointDescriptor.Invalid;

            // Build the blocks we're going to use.

            // GlobalsProvider redirects the global _TimeParameters into an available input
            var globalsProviderBlock = BuildGlobalsProviderBlock(container);
            // UvScroll scrolls 'uv' by time and a scroll speed property
            var uvScrollBlock = BuildUvScrollBlock(container);
            // AlbedoColor outputs a color variable from sampling a texture and color property
            var albedoColorBlock = BuildAlbedoColorBlock(container);

            // Now build the descriptors for each block. Blocks can be re-used multiple times within a shader.
            // The block descriptors add any unique data about the call, such as name overrides

            // GlobalsProvider is not doing anything special
            var globalsProviderBlockDesc = SimpleSampleBuilder.BuildSimpleBlockDescriptor(container, globalsProviderBlock);

            // The input variable is uv0. Remap this variable to the block's 'uv' variable
            var uvScrollBlockDescBuilder = new BlockDescriptor.Builder(uvScrollBlock);
            var uvNameOverrideBuilder = new BlockVariableNameOverride.Builder();
            uvNameOverrideBuilder.SourceName = "uv0";
            uvNameOverrideBuilder.DestinationName = "uv";
            uvScrollBlockDescBuilder.AddInputOverride(uvNameOverrideBuilder.Build(container));
            var uvScrollBlockDesc = uvScrollBlockDescBuilder.Build(container);

            // AlbedoColor output's "Color" but we want it to map to the available output "BaseColor"
            var albedoColorBlockDescBuilder = new BlockDescriptor.Builder(albedoColorBlock);
            var colorNameOverrideBuilder = new BlockVariableNameOverride.Builder();
            colorNameOverrideBuilder.SourceName = "Color";
            colorNameOverrideBuilder.DestinationName = "BaseColor";
            albedoColorBlockDescBuilder.AddOutputOverride(colorNameOverrideBuilder.Build(container));
            var albedoColorBlockDesc = albedoColorBlockDescBuilder.Build(container);

            // The order of these block is what determines how the inputs/outputs are resolved
            var cpDescBuilder = new CustomizationPointDescriptor.Builder(surfaceCP);
            cpDescBuilder.BlockDescriptors.Add(globalsProviderBlockDesc);
            cpDescBuilder.BlockDescriptors.Add(uvScrollBlockDesc);
            cpDescBuilder.BlockDescriptors.Add(albedoColorBlockDesc);

            surfaceCPDesc = cpDescBuilder.Build(container);
        }

        internal static Block BuildGlobalsProviderBlock(ShaderContainer container)
        {
            // A sample block that redirects some global variables into available inputs
            const string BlockName = "GlobalsProvider";

            var inputVariables = new List<BlockVariable>();
            var outputVariables = new List<BlockVariable>();

            // Make an output for 'TimeParameters'
            var timeParamsOutputBuilder = new BlockVariable.Builder();
            timeParamsOutputBuilder.Type = container._float4;
            timeParamsOutputBuilder.ReferenceName = "TimeParameters";
            var timeParamsOutput = timeParamsOutputBuilder.Build(container);
            outputVariables.Add(timeParamsOutput);

            var inputType = SimpleSampleBuilder.BuildStructFromVariables(container, $"{BlockName}Input", inputVariables);
            var outputType = SimpleSampleBuilder.BuildStructFromVariables(container, $"{BlockName}Output", outputVariables);

            // Simple copy the global '_TimeParameters' into the outputs
            var entryPointFnBuilder = new ShaderFunction.Builder($"{BlockName}Main", outputType);
            entryPointFnBuilder.AddInput(inputType, "inputs");
            entryPointFnBuilder.AddLine($"{outputType.Name} outputs;");
            entryPointFnBuilder.AddLine($"outputs.{timeParamsOutput.ReferenceName} = _TimeParameters;");
            entryPointFnBuilder.AddLine($"return outputs;");
            var entryPointFn = entryPointFnBuilder.Build(container);

            // Setup the block from the inputs, outputs, types, functions
            var blockBuilder = new Block.Builder(BlockName);
            foreach (var variable in inputVariables)
                blockBuilder.AddInput(variable);
            foreach (var variable in outputVariables)
                blockBuilder.AddOutput(variable);
            blockBuilder.AddType(inputType);
            blockBuilder.AddType(outputType);
            blockBuilder.SetEntryPointFunction(entryPointFn);
            return blockBuilder.Build(container);
        }

        internal static Block BuildUvScrollBlock(ShaderContainer container)
        {
            // Make a sample block that takes in uv and scrolls it by time and a property
            const string BlockName = "UvScroll";

            var inputVariables = new List<BlockVariable>();
            var outputVariables = new List<BlockVariable>();

            // Make the uv variable. We can use the same variable as the input and output.
            var uvBuilder = new BlockVariable.Builder();
            uvBuilder.Type = container._float4;
            uvBuilder.ReferenceName = "uv";
            var uv = uvBuilder.Build(container);
            inputVariables.Add(uv);
            outputVariables.Add(uv);

            // Take in 'TimeParameters' as a variable
            var timeParametersBuilder = new BlockVariable.Builder();
            timeParametersBuilder.Type = container._float4;
            timeParametersBuilder.ReferenceName = "TimeParameters";
            var timeParameters = timeParametersBuilder.Build(container);
            inputVariables.Add(timeParameters);

            // Make an input for the scroll speed to use. This input will also be a property.
            // For convenience, an input can be tagged with the [Property] attribute which will automatically add it as a property.
            var scrollSpeedBuilder = new BlockVariable.Builder();
            scrollSpeedBuilder.Type = container._float2;
            scrollSpeedBuilder.ReferenceName = "_ScrollSpeed";
            scrollSpeedBuilder.DisplayName = "ScrollSpeed";
            // Setup the material property info. We need to mark the default expression, the property type, and that it is a property.
            scrollSpeedBuilder.DefaultExpression = "(1, 1, 0, 0)";
            SimpleSampleBuilder.MarkAsProperty(container, scrollSpeedBuilder, "Vector");
            var scrollSpeed = scrollSpeedBuilder.Build(container);
            inputVariables.Add(scrollSpeed);

            var inputType = SimpleSampleBuilder.BuildStructFromVariables(container, $"{BlockName}Input", inputVariables);
            var outputType = SimpleSampleBuilder.BuildStructFromVariables(container, $"{BlockName}Output", outputVariables);

            // Build a function that takes in uv, scales it by time and a speed, and then outputs it.
            var entryPointFnBuilder = new ShaderFunction.Builder($"{BlockName}Main", outputType);
            entryPointFnBuilder.AddInput(inputType, "inputs");
            entryPointFnBuilder.AddLine($"{outputType.Name} outputs;");
            entryPointFnBuilder.AddLine($"float4 uv = inputs.{uv.ReferenceName};");
            entryPointFnBuilder.AddLine($"uv.xy += inputs.{scrollSpeed.ReferenceName} * inputs.{timeParameters.ReferenceName}[0];");
            entryPointFnBuilder.AddLine($"outputs.{uv.ReferenceName} = uv;");
            entryPointFnBuilder.AddLine($"return outputs;");
            var entryPointFn = entryPointFnBuilder.Build(container);

            // Setup the block from the inputs, outputs, types, functions
            var blockBuilder = new Block.Builder(BlockName);
            foreach (var variable in inputVariables)
                blockBuilder.AddInput(variable);
            foreach (var variable in outputVariables)
                blockBuilder.AddOutput(variable);
            blockBuilder.AddType(inputType);
            blockBuilder.AddType(outputType);
            blockBuilder.SetEntryPointFunction(entryPointFn);
            return blockBuilder.Build(container);
        }

        internal static Block BuildAlbedoColorBlock(ShaderContainer container)
        {
            const string BlockName = "AlbedoColor";

            var inputVariables = new List<BlockVariable>();
            var outputVariables = new List<BlockVariable>();
            var propertyVariables = new List<BlockVariable>();

            // Take in uv as an input
            var uvInputBuilder = new BlockVariable.Builder();
            uvInputBuilder.ReferenceName = "uv";
            uvInputBuilder.Type = container._float4;
            var uvInput = uvInputBuilder.Build(container);
            inputVariables.Add(uvInput);

            // Make an input for Color. This input will also be a property.
            // For convenience, an input can be tagged with the [Property] attribute which will automatically add it as a property.
            var colorInputBuilder = new BlockVariable.Builder();
            colorInputBuilder.ReferenceName = "_Color";
            colorInputBuilder.DisplayName = "Color";
            colorInputBuilder.Type = container._float4;
            // Setup the material property info. We need to mark the default expression, the property type, and that it is a property.
            SimpleSampleBuilder.MarkAsProperty(container, colorInputBuilder, "Color");
            colorInputBuilder.DefaultExpression = "(1, 0, 0, 1)";
            var colorInput = colorInputBuilder.Build(container);
            inputVariables.Add(colorInput);

            // Make a texture for albedo color. Creating a texture is complicated so it's delegated to a helper.
            string albedoTexRefName = "_AlbedoTex";
            SimpleSampleBuilder.BuildTexture2D(container, albedoTexRefName, "AlbedoTex", inputVariables, propertyVariables);

            // Create an output for a float3 color.
            var colorOutBuilder = new BlockVariable.Builder();
            colorOutBuilder.ReferenceName = "Color";
            colorOutBuilder.Type = container._float3;
            var colorOut = colorOutBuilder.Build(container);
            outputVariables.Add(colorOut);

            var inputType = SimpleSampleBuilder.BuildStructFromVariables(container, $"{BlockName}Input", inputVariables);
            var outputType = SimpleSampleBuilder.BuildStructFromVariables(container, $"{BlockName}Output", outputVariables);

            // Build the entry point function that samples the texture with the given uv and combines that with the color property.
            var entryPointFnBuilder = new ShaderFunction.Builder("SurfaceFn", outputType);
            entryPointFnBuilder.AddInput(inputType, "inputs");
            entryPointFnBuilder.AddLine($"{outputType.Name} outputs;");
            entryPointFnBuilder.AddLine($"UnityTexture2D {albedoTexRefName}Tex = UnityBuildTexture2DStruct({albedoTexRefName});");
            entryPointFnBuilder.AddLine($"float4 {albedoTexRefName}Sample = SAMPLE_TEXTURE2D({albedoTexRefName}Tex.tex, {albedoTexRefName}Tex.samplerstate, {albedoTexRefName}Tex.GetTransformedUV(inputs.{uvInput.ReferenceName}));");
            entryPointFnBuilder.AddLine($"outputs.{colorOut.ReferenceName} = inputs.{colorInput.ReferenceName} * {albedoTexRefName}Sample.xyz;");
            entryPointFnBuilder.AddLine($"return outputs;");
            var entryPointFn = entryPointFnBuilder.Build(container);

            // Setup the block from the inputs, outputs, types, functions
            var blockBuilder = new Block.Builder(BlockName);
            foreach (var variable in inputVariables)
                blockBuilder.AddInput(variable);
            foreach (var variable in outputVariables)
                blockBuilder.AddOutput(variable);
            foreach (var variable in propertyVariables)
                blockBuilder.AddProperty(variable);
            blockBuilder.AddType(inputType);
            blockBuilder.AddType(outputType);
            blockBuilder.SetEntryPointFunction(entryPointFn);
            return blockBuilder.Build(container);
        }

        static void DisplayTestResult(string testName, string code)
        {
            string tempPath = string.Format($"Temp/FoundryTest_{testName}.shader");
            if (ShaderGraph.GraphUtil.WriteToFile(tempPath, code))
                ShaderGraph.GraphUtil.OpenFile(tempPath);
        }
    }
}
