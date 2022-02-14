using System.Collections.Generic;
using UnityEditor.ShaderGraph;

using UnityEditor.ShaderFoundry;
using PassIdentifier = UnityEngine.Rendering.PassIdentifier;
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

        internal LegacyTemplateProvider(Target target, AssetCollection assetCollection = null)
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

            var legacyLinker = new LegacyTemplateLinker(m_assetCollection);
            legacyLinker.SetLegacy(m_Target, subShaderDescriptor);
            builder.SetLinker(legacyLinker);

            var subPassIndex = 0;
            foreach (var pass in subShaderDescriptor.passes)
            {
                var legacyPassDescriptor = pass.descriptor;
                var passBuilder = new TemplatePass.Builder(Container);
                passBuilder.ReferenceName = legacyPassDescriptor.referenceName;
                passBuilder.DisplayName = legacyPassDescriptor.displayName;
                passBuilder.SetPassIdentifier((uint)subShaderIndex, (uint)subPassIndex);
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
            // To build the customization point interface, merge the interface of all passes together.
            foreach(var legacyPass in subShaderDescriptor.passes)
                ExtractVertexAndFragmentPostFields(legacyPass.descriptor, vertexContext, fragmentContext);

            vertexCustomizationPoint = BuildVertexCustomizationPoint(vertexContext);
            builder.AddCustomizationPoint(vertexCustomizationPoint);

            surfaceCustomizationPoint = BuildFragmentCustomizationPoint(fragmentContext);
            builder.AddCustomizationPoint(surfaceCustomizationPoint);
        }

        CustomizationPoint BuildVertexCustomizationPoint(PostFieldsContext context)
        {
            var vertexPreBlock = BuildVertexPreBlock();
            var vertexPostBlock = BuildVertexPostBlock(context.Fields);

            var nameMappings = new List<NameOverride>();
            nameMappings.Add(new NameOverride { Source = "ObjectSpacePosition", Destination = "Position" });
            nameMappings.Add(new NameOverride { Source = "ObjectSpaceNormal", Destination = "Normal" });
            nameMappings.Add(new NameOverride { Source = "ObjectSpaceTangent", Destination = "Tangent" });
            var defaultVariableValues = new Dictionary<string, string>();
            var vertexMainBlock = BuildMainBlock(LegacyCustomizationPoints.VertexDescriptionFunctionName, vertexPreBlock, vertexPostBlock, nameMappings, defaultVariableValues, context.Fields);
            var vertexMainBlockInstance = new BlockInstance.Builder(Container, vertexMainBlock).Build();

            var vertexBuilder = new CustomizationPoint.Builder(Container, LegacyCustomizationPoints.VertexDescriptionCPName);
            return BuildCustomizationPoint(vertexBuilder, vertexPreBlock, vertexPostBlock, new List<BlockInstance> { vertexMainBlockInstance });
        }

        CustomizationPoint BuildFragmentCustomizationPoint(PostFieldsContext context)
        {
            var fragmentPreBlock = BuildFragmentPreBlock();
            var fragmentPostBlock = BuildFragmentPostBlock(context.Fields);

            var nameMappings = new List<NameOverride>();
            nameMappings.Add(new NameOverride { Source = "ObjectSpaceNormal", Destination = "NormalOS" });
            nameMappings.Add(new NameOverride { Source = "WorldSpaceNormal", Destination = "NormalWS" });
            nameMappings.Add(new NameOverride { Source = "TangentSpaceNormal", Destination = "NormalTS" });
            // Need to create the default outputs for the fragment output. This isn't currently part of the field descriptors.
            var defaultVariableValues = new Dictionary<string, string>();
            var fragmentMainBlock = BuildMainBlock(LegacyCustomizationPoints.SurfaceDescriptionFunctionName, fragmentPreBlock, fragmentPostBlock, nameMappings, defaultVariableValues, context.Fields);
            var fragmentMainBlockInstance = new BlockInstance.Builder(Container, fragmentMainBlock).Build();

            var fragmentBuilder = new CustomizationPoint.Builder(Container, LegacyCustomizationPoints.SurfaceDescriptionCPName);
            return BuildCustomizationPoint(fragmentBuilder, fragmentPreBlock, fragmentPostBlock, new List<BlockInstance> { fragmentMainBlockInstance });
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

        BlockInstance BuildSimpleBlockInstance(Block block)
        {
            var builder = new BlockInstance.Builder(Container, block);
            return builder.Build();
        }

        ShaderFunction BuildDummyFunction(string name, ShaderType inputType, ShaderType outputType)
        {
            var builder = new ShaderFunction.Builder(Container, name, outputType);
            builder.AddInput(inputType, "input");
            builder.AddLine($"{outputType.Name} output;");
            builder.AddLine($"return output;");
            return builder.Build();
        }

        Block BuildVertexPreBlock()
        {
            var blockName = $"Pre{LegacyCustomizationPoints.VertexDescriptionCPName}";
            List<FieldDescriptor> fields = new List<FieldDescriptor>();
            fields.AddRange(UnityEditor.ShaderGraph.Structs.VertexDescriptionInputs.fields);

            var builder = new Block.Builder(Container, blockName);
            var outputType = BuildType(blockName + "Outputs", ExtractFields(fields));
            var entryPointFn = BuildDummyFunction("dummy", ShaderType.Invalid, outputType);
            builder.SetEntryPointFunction(entryPointFn);
            return builder.Build();
        }

        Block BuildVertexPostBlock(List<FieldDescriptor> fields)
        {
            var blockName = $"Post{LegacyCustomizationPoints.VertexDescriptionCPName}";
            var builder = new Block.Builder(Container, blockName);
            var inputType = BuildType(blockName + "Inputs", ExtractFields(fields));
            var entryPointFn = BuildDummyFunction("dummy", inputType, ShaderType.Invalid);
            builder.SetEntryPointFunction(entryPointFn);
            return builder.Build();
        }

        Block BuildFragmentPreBlock()
        {
            var blockName = $"Pre{LegacyCustomizationPoints.SurfaceDescriptionCPName}";
            var builder = new Block.Builder(Container, blockName);
            var outputFields = ExtractFields(UnityEditor.ShaderGraph.Structs.SurfaceDescriptionInputs.fields);
            var outputType = BuildType(blockName + "Outputs", outputFields);
            var entryPointFn = BuildDummyFunction("dummy", ShaderType.Invalid, outputType);
            builder.SetEntryPointFunction(entryPointFn);
            return builder.Build();
        }

        Block BuildFragmentPostBlock(List<FieldDescriptor> fields)
        {
            var blockName = $"Post{LegacyCustomizationPoints.SurfaceDescriptionCPName}";
            var builder = new Block.Builder(Container, blockName);
            var inputType = BuildType(blockName + "Inputs", ExtractFields(fields));
            var entryPointFn = BuildDummyFunction("dummy", inputType, ShaderType.Invalid);
            builder.SetEntryPointFunction(entryPointFn);
            return builder.Build();
        }

        BlockVariable CloneVariable(BlockVariable variable)
        {
            var builder = new BlockVariable.Builder(Container);
            builder.Type = variable.Type;
            builder.Name = variable.Name;
            foreach (var attribute in variable.Attributes)
                builder.AddAttribute(attribute);
            return builder.Build();
        }

        CustomizationPoint BuildCustomizationPoint(CustomizationPoint.Builder builder, Block preBlock, Block postBlock, List<BlockInstance> defaultBlockInstances)
        {
            foreach (var output in preBlock.Outputs)
                builder.AddInput(CloneVariable(output));
            foreach (var input in postBlock.Inputs)
                builder.AddOutput(CloneVariable(input));
            foreach(var blockInstance in defaultBlockInstances)
                builder.AddDefaultBlockInstance(blockInstance);
            return builder.Build();
        }

        class NameOverride
        {
            public string Source;
            public string Destination;
        }

        Block BuildMainBlock(string blockName, Block preBlock, Block postBlock, List<NameOverride> nameMappings, Dictionary<string, string> defaultVariableValues, List<FieldDescriptor> fieldDescriptors)
        {
            var mainBlockBuilder = new Block.Builder(Container, blockName);

            // Collect some values for quick lookups by name
            var availableInputs = new Dictionary<string, BlockOutput>();
            foreach (var prop in preBlock.Outputs)
                availableInputs[prop.Name] = prop;
            var fieldDescriptorsByName = new Dictionary<string, FieldDescriptor>();
            foreach (var fieldDescriptor in fieldDescriptors)
                fieldDescriptorsByName[fieldDescriptor.name] = fieldDescriptor;
            var nameMappingsByOutputName = new Dictionary<string, NameOverride>();
            foreach(var mapping in nameMappings)
                nameMappingsByOutputName[mapping.Destination] = mapping;
            
            // Build the input/output type from the matching fields
            var inputBuilder = new ShaderType.StructBuilder(mainBlockBuilder, $"{blockName}DefaultIn");
            var outputBuilder = new ShaderType.StructBuilder(mainBlockBuilder, $"{blockName}DefaultOut");

            HashSet<string> declaredInputs = new HashSet<string>();
            var variableExpressions = new Dictionary<string, string>();
            // For every potential output, find out if it exists, and if so what its default expression is
            foreach (var output in postBlock.Inputs)
            {
                // First check if this is a variable remapping (i.e. one input name is getting remapped to a different output name)
                if(nameMappingsByOutputName.TryGetValue(output.Name, out var mapping))
                {
                    BlockOutput inputProp;
                    availableInputs.TryGetValue(mapping.Source, out inputProp);
                    if (inputProp.IsValid)
                    {
                        outputBuilder.AddField(output.Type, output.Name);
                        // Add the input if we haven't already declared it
                        if(!declaredInputs.Contains(inputProp.Name))
                        {
                            inputBuilder.AddField(inputProp.Type, inputProp.Name);
                            declaredInputs.Add(inputProp.Name);
                        }
                        variableExpressions[output.Name] = $"input.{mapping.Source};";
                    }
                }
                // Next see if this is a manually set default value
                else if(defaultVariableValues.TryGetValue(output.Name, out var defaultValue))
                {
                    variableExpressions[output.Name] = defaultValue;
                    outputBuilder.AddField(output.Type, output.Name);
                }
                // Finally, check if this has a default value we can extract from a field descriptor
                else if (fieldDescriptorsByName.TryGetValue(output.Name, out var fieldDescriptor))
                {
                    var defaultValueStr = GetDefaultValueString(output.Type, fieldDescriptor);
                    if (defaultValueStr != null)
                    {
                        variableExpressions[output.Name] = defaultValueStr;
                        outputBuilder.AddField(output.Type, output.Name);
                    }
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

            // Declare the expression for every output field
            foreach(var field in outType.StructFields)
            {
                if(variableExpressions.TryGetValue(field.Name, out var expression))
                    fnBuilder.AddLine($"output.{field.Name} = {expression};");
            }

            fnBuilder.AddLine($"return output;");
            var entryPointFunction = fnBuilder.Build();
            mainBlockBuilder.AddFunction(entryPointFunction);
            mainBlockBuilder.SetEntryPointFunction(entryPointFunction);

            return mainBlockBuilder.Build();
        }

        void ExtractVertex(Template template, TemplatePass.Builder passBuilder, CustomizationPoint vertexCustomizationPoint, List<FieldDescriptor> vertexFields)
        {
            var stageType = UnityEditor.Rendering.ShaderType.Vertex;
            var vertexPreBlock = BuildVertexPreBlock();
            var vertexPostBlock = BuildVertexPostBlock(vertexFields);

            passBuilder.AppendBlockInstance(BuildSimpleBlockInstance(vertexPreBlock), stageType);
            passBuilder.AppendCustomizationPoint(vertexCustomizationPoint, stageType);
            passBuilder.AppendBlockInstance(BuildSimpleBlockInstance(vertexPostBlock), stageType);
        }

        void ExtractFragment(Template template, TemplatePass.Builder passBuilder, CustomizationPoint surfaceCustomizationPoint, List<FieldDescriptor> fragmentFields)
        {
            var stageType = UnityEditor.Rendering.ShaderType.Fragment;
            var fragmentPreBlock = BuildFragmentPreBlock();
            var fragmentPostBlock = BuildFragmentPostBlock(fragmentFields);

            passBuilder.AppendBlockInstance(BuildSimpleBlockInstance(fragmentPreBlock), stageType);
            passBuilder.AppendCustomizationPoint(surfaceCustomizationPoint, stageType);
            passBuilder.AppendBlockInstance(BuildSimpleBlockInstance(fragmentPostBlock), stageType);
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

        List<StructField> ExtractFields(IEnumerable<FieldDescriptor> fields)
        {
            var visitedNames = new HashSet<string>();

            var fieldTypes = BuildFieldTypes();
            var results = new List<StructField>();
            foreach (var field in fields)
            {
                // Don't visit a name twice. This can happen currently due to the vertex attributes and inputs being merged together
                if (visitedNames.Contains(field.name))
                    continue;
                visitedNames.Add(field.name);

                // Some fields have a type set, some don't. Try to look up the type on
                // the field and if not fallback to checking the lookup map
                ShaderType fieldType = FindType(field.type);
                if (!fieldType.IsValid)
                    fieldTypes.TryGetValue(field.name, out fieldType);

                if (!fieldType.IsValid)
                    continue;

                var builder = new StructField.Builder(Container, field.name, fieldType);
                results.Add(builder.Build());
            }
            return results;
        }

        string GetDefaultValueString(ShaderType fieldType, FieldDescriptor fieldDescriptor)
        {
            // A color value might be bound to a different actual shader type, if so we have to only grab the relevant values
            string GetColorDefaultValueString(ShaderType fieldType, UnityEngine.Color color)
            {
                var builder = new ShaderStringBuilder();
                builder.Append(fieldType.Name);
                builder.Append("(");
                for(var i = 0; i < fieldType.VectorDimension; ++i)
                {
                    if (i != 0)
                        builder.Append(", ");
                    builder.Append(color[i].ToString());
                }
                builder.Append(")");
                return builder.ToString();
            }

            // It seems like the only way to get at the default value current is to extract it from the control on the block field.
            if (fieldDescriptor is BlockFieldDescriptor blockField)
            {
                switch (blockField.control)
                {
                    case FloatControl floatControl:
                        return floatControl.value.ToString();
                    case Vector2Control vec2Control:
                        return vec2Control.value.ToString();
                    case Vector3Control vec3Control:
                        return vec3Control.value.ToString();
                    case Vector4Control vec4Control:
                        return vec4Control.value.ToString();
                    case ColorControl colorControl:
                        return GetColorDefaultValueString(fieldType, colorControl.value);
                    case ColorRGBAControl colorRGBAControl:
                        return GetColorDefaultValueString(fieldType, colorRGBAControl.value);
                    case VertexColorControl vertexColorControl:
                        return GetColorDefaultValueString(fieldType, vertexColorControl.value);
                    default:
                        return null;
                }
            }
            return null;
        }

        ShaderType BuildType(string name, IEnumerable<StructField> fields)
        {
            var builder = new ShaderType.StructBuilder(Container, name);
            foreach (var field in fields)
                builder.AddField(field);
            return builder.Build();
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
