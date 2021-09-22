using System.Collections.Generic;
using UnityEditor.ShaderGraph;

using UnityEditor.ShaderFoundry;
using BlockInput = UnityEditor.ShaderFoundry.BlockVariable;
using BlockOutput = UnityEditor.ShaderFoundry.BlockVariable;
using BlockProperty = UnityEditor.ShaderFoundry.BlockVariable;

namespace UnityEditor.ShaderFoundry
{
    internal class LegacyTemplateProvider : ITemplateProvider
    {
        internal AssetCollection m_assetCollection = new AssetCollection();
        internal Target m_Target;
        internal ShaderContainer m_Container;

        internal Target LegacyTarget => m_Target;
        internal ShaderContainer Container => m_Container;

        const string precisionQualifier = "float";

        internal LegacyTemplateProvider(Target target, AssetCollection assetCollection)
        {
            m_Target = target;
            m_assetCollection = assetCollection;
        }

        string ITemplateProvider.Name
        {
            get { return m_Target.displayName; }
        }

        IEnumerable<CustomizationPoint> ITemplateProvider.GetCustomizationPoints()
        {
            // ToDo
            return null;
        }

        void ITemplateProvider.ConfigureSettings(TemplateProviderSettings settings)
        {
            // ToDo
        }

        IEnumerable<Template> ITemplateProvider.GetTemplates(ShaderContainer container)
        {
            m_Container = container;
            var results = new List<Template>();

            TargetSetupContext context = new TargetSetupContext(m_assetCollection);
            LegacyTarget.Setup(ref context);

            var subShaderIndex = 0;
            foreach (var subShader in context.subShaders)
            {
                var template = BuildTemplate(subShader, subShaderIndex);
                results.Add(template);
                ++subShaderIndex;
            }
            return results;
        }

        Template BuildTemplate(SubShaderDescriptor subShaderDescriptor, int subShaderIndex)
        {
            var builder = new Template.Builder(Container, $"{subShaderDescriptor.pipelineTag}");

            CustomizationPoint vertexCustomizationPoint, surfaceCustomizationPoint;
            BuildTemplateCustomizationPoints(builder, subShaderDescriptor, out vertexCustomizationPoint, out surfaceCustomizationPoint);
            var result = builder.Build();

            var legacyLinker = new SandboxLegacyTemplateLinker(m_assetCollection);
            legacyLinker.SetLegacy(m_Target, subShaderDescriptor);
            builder.SetLinker(legacyLinker);

            var subPassIndex = 0;
            foreach (var pass in subShaderDescriptor.passes)
            {
                var legacyPassDescriptor = pass.descriptor;
                var passBuilder = new TemplatePass.Builder(Container);
                passBuilder.ReferenceName = legacyPassDescriptor.referenceName;
                passBuilder.DisplayName = legacyPassDescriptor.displayName;
                passBuilder.PassIdentifier = new UnityEditor.ShaderFoundry.PassIdentifier(subShaderIndex, subPassIndex);
                ++subPassIndex;

                BuildLegacyTemplateEntryPoints(result, legacyPassDescriptor, passBuilder, vertexCustomizationPoint, surfaceCustomizationPoint);

                builder.AddPass(passBuilder.Build());
            }

            return builder.Build();
        }

        void BuildTemplateCustomizationPoints(Template.Builder builder, SubShaderDescriptor subShaderDescriptor, out CustomizationPoint vertexCustomizationPoint, out CustomizationPoint surfaceCustomizationPoint)
        {
            var vertexContext = new PostFieldsContext();
            var fragmentContext = new PostFieldsContext();
            foreach(var legacyPass in subShaderDescriptor.passes)
                ExtractVertexAndFragmentPostFields(legacyPass.descriptor, vertexContext, fragmentContext);

            var vertexBuilder = new CustomizationPoint.Builder(Container, LegacyCustomizationPoints.VertexDescriptionCPName);
            vertexCustomizationPoint = BuildCustomizationPoint(vertexBuilder, BuildVertexPreBlock(), BuildVertexPostBlock(vertexContext.Fields));
            builder.AddCustomizationPoint(vertexCustomizationPoint);

            var fragmentBuilder = new CustomizationPoint.Builder(Container, LegacyCustomizationPoints.SurfaceDescriptionCPName);
            surfaceCustomizationPoint = BuildCustomizationPoint(fragmentBuilder, BuildFragmentPreBlock(), BuildFragmentPostBlock(fragmentContext.Fields));
            builder.AddCustomizationPoint(surfaceCustomizationPoint);
        }

        void BuildLegacyTemplateEntryPoints(Template template, PassDescriptor legacyPassDescriptor, TemplatePass.Builder passBuilder, CustomizationPoint vertexCustomizationPoint, CustomizationPoint surfaceCustomizationPoint)
        {
            var vertexContext = new PostFieldsContext();
            var fragmentContext = new PostFieldsContext();
            ExtractVertexAndFragmentPostFields(legacyPassDescriptor, vertexContext, fragmentContext);

            ExtractVertex(template, passBuilder, vertexCustomizationPoint, vertexContext.Fields);
            ExtractFragment(template, passBuilder, surfaceCustomizationPoint, fragmentContext.Fields);
        }

        // Context object for collecting the "post fields" from a pass.
        // This is meant to uniquely collect fields so it can also be used to merge the results across multiple passes
        internal class PostFieldsContext
        {
            internal List<FieldDescriptor> Fields = new List<FieldDescriptor>();
            internal HashSet<string> FieldNames = new HashSet<string>();
        }

        void ExtractVertexAndFragmentPostFields(UnityEditor.ShaderGraph.PassDescriptor legacyPassDescriptor, PostFieldsContext vertexContext, PostFieldsContext fragmentContext)
        {
            void ExtractContext(TargetActiveBlockContext targetActiveBlockContext, IEnumerable<BlockFieldDescriptor> validBlocks, PostFieldsContext context)
            {
                if (validBlocks != null)
                {
                    foreach (var blockFieldDescriptor in validBlocks)
                    {
                        if (targetActiveBlockContext.activeBlocks.Contains(blockFieldDescriptor) && !context.FieldNames.Contains(blockFieldDescriptor.name))
                        {
                            context.FieldNames.Add(blockFieldDescriptor.name);
                            context.Fields.Add(blockFieldDescriptor);
                        }
                    }
                }
            }

            var targetActiveBlockContext = new TargetActiveBlockContext(new List<BlockFieldDescriptor>(), legacyPassDescriptor);
            LegacyTarget.GetActiveBlocks(ref targetActiveBlockContext);
            
            ExtractContext(targetActiveBlockContext, legacyPassDescriptor.validVertexBlocks, vertexContext);
            ExtractContext(targetActiveBlockContext, legacyPassDescriptor.validPixelBlocks, fragmentContext);
        }

        BlockDescriptor BuildSimpleBlockDesc(Block block)
        {
            var builder = new BlockDescriptor.Builder(Container, block);
            return builder.Build();
        }

        Block BuildVertexPreBlock()
        {
            List<FieldDescriptor> fields = new List<FieldDescriptor>();
            fields.AddRange(UnityEditor.ShaderGraph.Structs.VertexDescriptionInputs.fields);

            var builder = new Block.Builder(Container, $"Pre{LegacyCustomizationPoints.VertexDescriptionCPName}");
            foreach(var output in ExtractFields(fields))
                builder.AddOutput(output);
            return builder.Build();
        }

        Block BuildVertexPostBlock(List<FieldDescriptor> fields)
        {
            var builder = new Block.Builder(Container, $"Post{LegacyCustomizationPoints.VertexDescriptionCPName}");
            foreach (var input in ExtractFields(fields))
                builder.AddInput(input);
            return builder.Build();
        }

        Block BuildFragmentPreBlock()
        {
            var builder = new Block.Builder(Container, $"Pre{LegacyCustomizationPoints.SurfaceDescriptionCPName}");
            var outputs = ExtractFields(UnityEditor.ShaderGraph.Structs.SurfaceDescriptionInputs.fields);
            foreach (var output in outputs)
                builder.AddOutput(output);
            return builder.Build();
        }

        Block BuildFragmentPostBlock(List<FieldDescriptor> fields)
        {
            var builder = new Block.Builder(Container, $"Post{LegacyCustomizationPoints.SurfaceDescriptionCPName}");
            foreach (var input in ExtractFields(fields))
                builder.AddInput(input);
            return builder.Build();
        }

        CustomizationPoint BuildCustomizationPoint(CustomizationPoint.Builder builder, Block preBlock, Block postBlock)
        {
            foreach (var output in preBlock.Outputs)
                builder.AddInput(output.Clone(Container));
            foreach (var input in postBlock.Inputs)
                builder.AddOutput(input.Clone(Container));
            return builder.Build();
        }

        Block BuildMainBlock(string blockName, Block preBlock, Block postBlock, List<BlockVariableNameOverride> nameMappings, Dictionary<string, string> defaultVariableValues)
        {
            var mainBlockBuilder = new Block.Builder(Container, blockName);

            if (nameMappings == null)
            {
                mainBlockBuilder.SetEntryPointFunction(ShaderFunction.Invalid);
            }
            else
            {
                // Collect the available inputs/outputs for this block
                var availableInputs = new Dictionary<string, BlockOutput>();
                var availableOutputs = new Dictionary<string, BlockInput>();
                foreach (var prop in preBlock.Outputs)
                    availableInputs[prop.ReferenceName] = prop;
                foreach (var prop in postBlock.Inputs)
                    availableOutputs[prop.ReferenceName] = prop;

                // Build the input/output type from the matching fields
                var inputBuilder = new ShaderType.StructBuilder(mainBlockBuilder, $"{blockName}DefaultIn");
                var outputBuilder = new ShaderType.StructBuilder(mainBlockBuilder, $"{blockName}DefaultOut");
                HashSet<string> declaredInputs = new HashSet<string>();
                HashSet<string> declaredOutputs = new HashSet<string>();

                // Check for any name remappings (e.g. ObjectSpacePosition -> Position).
                // If we find any then declare the appropriate inputs, outputs, and struct fields
                foreach (var mapping in nameMappings)
                {
                    BlockOutput inputProp;
                    BlockInput outputProp;
                    availableInputs.TryGetValue(mapping.SourceName, out inputProp);
                    availableOutputs.TryGetValue(mapping.DestinationName, out outputProp);
                    if (inputProp.IsValid && outputProp.IsValid)
                    {
                        inputBuilder.AddField(inputProp.Type, inputProp.ReferenceName);
                        outputBuilder.AddField(outputProp.Type, outputProp.ReferenceName);
                        mainBlockBuilder.AddInput(inputProp.Clone(Container));
                        mainBlockBuilder.AddOutput(outputProp.Clone(Container));
                        declaredInputs.Add(inputProp.ReferenceName);
                        declaredOutputs.Add(outputProp.ReferenceName);
                    }
                }
                // Also handle setting default values for outputs
                foreach(var defaultVariableValue in defaultVariableValues)
                {
                    BlockInput outputProp;
                    if(availableOutputs.TryGetValue(defaultVariableValue.Key, out outputProp) && !declaredOutputs.Contains(defaultVariableValue.Key))
                    {
                        declaredOutputs.Add(defaultVariableValue.Key);
                        outputBuilder.AddField(outputProp.Type, outputProp.ReferenceName);
                        mainBlockBuilder.AddOutput(outputProp.Clone(Container));
                    }
                }

                var inType = inputBuilder.Build();
                var outType = outputBuilder.Build();
                mainBlockBuilder.AddType(inType);
                mainBlockBuilder.AddType(outType);

                // Build the actual function
                var fnBuilder = new UnityEditor.ShaderFoundry.ShaderFunction.Builder(mainBlockBuilder, $"{blockName}Default", outType);
                fnBuilder.AddInput(inType, "input");

                fnBuilder.AddLine($"{outType.Name} output;");
                foreach(var mapping in nameMappings)
                {
                    BlockOutput inputProp;
                    BlockInput outputProp;
                    availableInputs.TryGetValue(mapping.SourceName, out inputProp);
                    availableOutputs.TryGetValue(mapping.DestinationName, out outputProp);
                    // Write a copy line for all matching input/outputs
                    if(inputProp.IsValid && outputProp.IsValid)
                    {
                        fnBuilder.AddLine($"output.{mapping.DestinationName} = input.{mapping.SourceName};");
                    }
                }
                foreach(var defaultVariableValue in defaultVariableValues)
                {
                    if(availableOutputs.TryGetValue(defaultVariableValue.Key, out var dummy))
                    {
                        fnBuilder.AddLine($"output.{defaultVariableValue.Key} = {defaultVariableValue.Value};");
                    }
                }

                fnBuilder.AddLine($"return output;");
                var entryPointFunction = fnBuilder.Build();
                mainBlockBuilder.AddFunction(entryPointFunction);
                mainBlockBuilder.SetEntryPointFunction(entryPointFunction);
            }

            return mainBlockBuilder.Build();
        }

        void ExtractVertex(Template template, TemplatePass.Builder passBuilder, CustomizationPoint vertexCustomizationPoint, List<FieldDescriptor> vertexFields)
        {
            BlockVariableNameOverride BuildSimpleNameOverride(string sourceName, string destinationName, ShaderContainer container)
            {
                var builder = new BlockVariableNameOverride.Builder(container);
                builder.SourceName = sourceName;
                builder.DestinationName = destinationName;
                return builder.Build();
            }
            var vertexPreBlock = BuildVertexPreBlock();
            var vertexPostBlock = BuildVertexPostBlock(vertexFields);

            var nameMappings = new List<BlockVariableNameOverride>();
            nameMappings.Add(BuildSimpleNameOverride("ObjectSpacePosition", "Position", Container));
            nameMappings.Add(BuildSimpleNameOverride("ObjectSpaceNormal", "Normal", Container));
            nameMappings.Add(BuildSimpleNameOverride("ObjectSpaceTangent", "Tangent", Container));
            var defaultVariableValues = new Dictionary<string, string>();
            var vertexMainBlock = BuildMainBlock(LegacyCustomizationPoints.VertexDescriptionFunctionName, vertexPreBlock, vertexPostBlock, nameMappings, defaultVariableValues);
        
            var id0 = passBuilder.AddBlock(BuildSimpleBlockDesc(vertexPreBlock), UnityEditor.Rendering.ShaderType.Vertex);
            var id1 = passBuilder.AddBlock(BuildSimpleBlockDesc(vertexMainBlock), UnityEditor.Rendering.ShaderType.Vertex);
            var id2 = passBuilder.AddBlock(BuildSimpleBlockDesc(vertexPostBlock), UnityEditor.Rendering.ShaderType.Vertex);
            passBuilder.SetCustomizationPointBlocks(vertexCustomizationPoint, UnityEditor.Rendering.ShaderType.Vertex, id1, id1);
        }

        void ExtractFragment(Template template, TemplatePass.Builder passBuilder, CustomizationPoint surfaceCustomizationPoint, List<FieldDescriptor> fragmentFields)
        {
            var fragmentPreBlock = BuildFragmentPreBlock();
            var fragmentPostBlock = BuildFragmentPostBlock(fragmentFields);

            var nameMappings = new List<BlockVariableNameOverride>();
            // Need to create the default outputs for the fragment output. This isn't currently part of the field descriptors.
            var defaultVariableValues = new Dictionary<string, string>
            {
                { "BaseColor", "float3(0, 0, 0)" },
                { "NormalTS", "float3(0, 0, 0)" },
                { "Smoothness", "0.5f" },
                { "Occlusion", "1" },
                { "Emission", "float3(0, 0, 0)" },
                { "Metallic", "0" },
                { "Alpha", "1" },
                { "AlphaClipThreshold", "0.5f" },
            };
            var fragmentMainBlock = BuildMainBlock(LegacyCustomizationPoints.SurfaceDescriptionFunctionName, fragmentPreBlock, fragmentPostBlock, nameMappings, defaultVariableValues);

            var id0 = passBuilder.AddBlock(BuildSimpleBlockDesc(fragmentPreBlock), UnityEditor.Rendering.ShaderType.Fragment);
            var id1 = passBuilder.AddBlock(BuildSimpleBlockDesc(fragmentMainBlock), UnityEditor.Rendering.ShaderType.Fragment);
            var id2 = passBuilder.AddBlock(BuildSimpleBlockDesc(fragmentPostBlock), UnityEditor.Rendering.ShaderType.Fragment);
            passBuilder.SetCustomizationPointBlocks(surfaceCustomizationPoint, UnityEditor.Rendering.ShaderType.Fragment, id1, id1);
        }

        // BlockFields don't have types set. We need this temporarily to resolve them
        Dictionary<string, ShaderType> BuildFieldTypes()
        {
            var results = new Dictionary<string, ShaderType>()
            {
                { UnityEditor.ShaderGraph.BlockFields.VertexDescription.Position.name, Container._float3},
                { UnityEditor.ShaderGraph.BlockFields.VertexDescription.Normal.name, Container._float3},
                { UnityEditor.ShaderGraph.BlockFields.VertexDescription.Tangent.name, Container._float3},
                { UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.BaseColor.name, Container._float3 },
                { UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.NormalTS.name, Container._float3 },
                { UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.NormalOS.name, Container._float3 },
                { UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.NormalWS.name, Container._float3 },
                { UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.Metallic.name, Container._float },
                { UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.Specular.name, Container._float },
                { UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.Smoothness.name, Container._float },
                { UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.Occlusion.name, Container._float },
                { UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.Emission.name, Container._float3 },
                { UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.Alpha.name, Container._float },
                { UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.AlphaClipThreshold.name, Container._float },
                { UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.CoatMask.name, Container._float },
                { UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.CoatSmoothness.name, Container._float },
            };

            return results;
        }

        List<BlockVariable> ExtractFields(IEnumerable<FieldDescriptor> fields)
        {
            var visitedNames = new HashSet<string>();

            var fieldTypes = BuildFieldTypes();
            var results = new List<BlockVariable>();
            foreach (var field in fields)
            {
                // Don't visit a name twice. This can happen currently due to the vertex attributes and inputs being merged together
                if (visitedNames.Contains(field.name))
                    continue;
                visitedNames.Add(field.name);

                // Some fields have a type set, some don't. Try to look up the type on
                // the field and if not fallback to checking the lookup map
                ShaderType fieldType = FindType(field.type);
                if(!fieldType.IsValid)
                    fieldTypes.TryGetValue(field.name, out fieldType);

                if (!fieldType.IsValid)
                    continue;

                var builder = new BlockVariable.Builder(Container);
                builder.DisplayName = field.name;
                builder.ReferenceName = field.name;
                builder.Type = fieldType;
                var prop = builder.Build();
                results.Add(prop);
            }
            return results;
        }

        ShaderType FindType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return ShaderType.Invalid;
            string concreteTypeName = typeName.Replace("$precision", precisionQualifier);
            return m_Container.GetType(concreteTypeName);
        }
    }
}
