using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Profiling;
using Pool = UnityEngine.Pool;

namespace UnityEditor.ShaderGraph
{
    internal static class GenerationUtils
    {
        const string kErrorString = @"ERROR!";

        internal static List<FieldDescriptor> GetActiveFieldsFromConditionals(ConditionalField[] conditionalFields)
        {
            var fields = new List<FieldDescriptor>();
            if (conditionalFields != null)
            {
                foreach (ConditionalField conditionalField in conditionalFields)
                {
                    if (conditionalField.condition == true)
                    {
                        fields.Add(conditionalField.field);
                    }
                }
            }

            return fields;
        }

        internal static void GenerateSubShaderTags(Target target, SubShaderDescriptor descriptor, ShaderStringBuilder builder)
        {
            builder.AppendLine("Tags");
            using (builder.BlockScope())
            {
                // Pipeline tag
                if (!string.IsNullOrEmpty(descriptor.pipelineTag))
                    builder.AppendLine($"\"RenderPipeline\"=\"{descriptor.pipelineTag}\"");
                else
                    builder.AppendLine("// RenderPipeline: <None>");

                // Render Type
                if (!string.IsNullOrEmpty(descriptor.renderType))
                    builder.AppendLine($"\"RenderType\"=\"{descriptor.renderType}\"");
                else
                    builder.AppendLine("// RenderType: <None>");

                // Custom shader tags.
                if (!string.IsNullOrEmpty(descriptor.customTags))
                    builder.AppendLine(descriptor.customTags);

                // Render Queue
                if (!string.IsNullOrEmpty(descriptor.renderQueue))
                    builder.AppendLine($"\"Queue\"=\"{descriptor.renderQueue}\"");
                else
                    builder.AppendLine("// Queue: <None>");

                // DisableBatching tag
                if (!string.IsNullOrEmpty(descriptor.disableBatchingTag))
                    builder.AppendLine($"\"DisableBatching\"=\"{descriptor.disableBatchingTag}\"");
                else
                    builder.AppendLine("// DisableBatching: <None>");

                // ShaderGraphShader tag (so we can tell what shadergraph built)
                builder.AppendLine("\"ShaderGraphShader\"=\"true\"");

                if (target is IHasMetadata metadata)
                    builder.AppendLine($"\"ShaderGraphTargetId\"=\"{metadata.identifier}\"");

                // IgnoreProjector
                if(!string.IsNullOrEmpty(descriptor.IgnoreProjector))
                    builder.AppendLine($"\"IgnoreProjector\"=\"{descriptor.IgnoreProjector}\"");

                // PreviewType
                if(!string.IsNullOrEmpty(descriptor.PreviewType))
                    builder.AppendLine($"\"PreviewType\"=\"{descriptor.PreviewType}\"");

                // CanUseSpriteAtlas
                if(!string.IsNullOrEmpty(descriptor.CanUseSpriteAtlas))
                    builder.AppendLine($"\"CanUseSpriteAtlas\"=\"{descriptor.CanUseSpriteAtlas}\"");
            }
        }

        static bool IsFieldActive(FieldDescriptor field, IActiveFields activeFields, bool isOptional)
        {
            bool fieldActive = true;
            if (!activeFields.Contains(field) && isOptional)
                fieldActive = false; //if the field is optional and not inside of active fields
            return fieldActive;
        }

        internal static void GenerateShaderStruct(StructDescriptor shaderStruct, ActiveFields activeFields, bool humanReadable, out ShaderStringBuilder structBuilder)
        {
            structBuilder = new ShaderStringBuilder(humanReadable: humanReadable);
            structBuilder.AppendLine($"struct {shaderStruct.name}");
            using (structBuilder.BlockSemicolonScope())
            {
                foreach (var activeField in GetActiveFieldsAndKeyword(shaderStruct, activeFields))
                {
                    var subscript = activeField.field;
                    var keywordIfDefs = activeField.keywordIfDefs;

                    //if field is active:
                    if (subscript.HasPreprocessor())
                        structBuilder.AppendLine($"#if {subscript.preprocessor}");

                    //if in permutation, add permutation ifdef
                    if (!string.IsNullOrEmpty(keywordIfDefs))
                        structBuilder.AppendLine(keywordIfDefs);

                    //check for a semantic, build string if valid
                    string semantic = subscript.HasSemantic() ? $" : {subscript.semantic}" : string.Empty;
                    structBuilder.AppendLine($"{subscript.interpolation} {subscript.type} {subscript.name}{semantic};");

                    //if in permutation, add permutation endif
                    if (!string.IsNullOrEmpty(keywordIfDefs))
                        structBuilder.AppendLine("#endif"); //TODO: add debug collector

                    if (subscript.HasPreprocessor())
                        structBuilder.AppendLine("#endif");
                }
            }
        }

        struct PackedEntry
        {
            public struct Input
            {
                public FieldDescriptor field;
                public int startChannel;
                public int channelCount;
            }

            public Input[] inputFields;
            public FieldDescriptor packedField;
        }

        static IEnumerable<(FieldDescriptor field, string keywordIfDefs)> GetActiveFieldsAndKeyword(StructDescriptor shaderStruct, ActiveFields activeFields)
        {
            var activeFieldList = shaderStruct.fields
                .Select(currentField =>
                {
                    bool fieldIsActive;
                    var currentKeywordIfDefs = string.Empty;

                    if (activeFields.permutationCount > 0)
                    {
                        //find all active fields per permutation
                        var instances = activeFields.allPermutations.instances
                            .Where(i => IsFieldActive(currentField, i, currentField.subscriptOptions.HasFlag(StructFieldOptions.Optional))).ToList();
                        fieldIsActive = instances.Count > 0;
                        if (fieldIsActive)
                            currentKeywordIfDefs = KeywordUtil.GetKeywordPermutationSetConditional(instances.Select(i => i.permutationIndex).ToList());
                    }
                    else
                        fieldIsActive = IsFieldActive(currentField, activeFields.baseInstance, currentField.subscriptOptions.HasFlag(StructFieldOptions.Optional));
                    //else just find active fields

                    if (fieldIsActive)
                    {
                        return
                        (
                            field: currentField,
                            keywordIfDefs: currentKeywordIfDefs
                        );
                    }

                    return
                    (
                        field: null,
                        keywordIfDefs: null
                    );
                }).Where(o => o.field != null);
            return activeFieldList;
        }

        static IEnumerable<FieldDescriptor> GetActiveFields(StructDescriptor shaderStruct, ActiveFields activeFields)
        {
            return GetActiveFieldsAndKeyword(shaderStruct, activeFields).Select(o => o.field);
        }

        static IEnumerable<FieldDescriptor> GetActiveFields(StructDescriptor shaderStruct, IActiveFields activeFields)
        {
            var activeFieldList = shaderStruct.fields
                .Where(field => IsFieldActive(field, activeFields, field.subscriptOptions.HasFlag(StructFieldOptions.Optional)));
            return activeFieldList;
        }

        static PackedEntry[] GeneratePackingLayout(StructDescriptor shaderStruct, ActiveFields activeFields)
        {
            var activeFieldList = GetActiveFields(shaderStruct, activeFields);
            return GeneratePackingLayout(shaderStruct.name, activeFieldList);
        }

        static PackedEntry[] GeneratePackingLayout(StructDescriptor shaderStruct, IActiveFields activeFields)
        {
            var activeFieldList = GetActiveFields(shaderStruct, activeFields);
            return GeneratePackingLayout(shaderStruct.name, activeFieldList);
        }

        static PackedEntry[] GeneratePackingLayout(string baseStructName, IEnumerable<FieldDescriptor> activeFields)
        {
            const int kPreUnpackable = 0;
            const int kPackable = 1;
            const int kPostUnpackable = 2;

            var fieldCategorized = activeFields
                .GroupBy(subscript =>
                {
                    if (subscript.HasPreprocessor())
                    {
                        //special case, "UNITY_STEREO_INSTANCING_ENABLED" fields must be packed at the end of the struct because they are system generated semantics
                        if (subscript.preprocessor.Contains("INSTANCING"))
                            return kPostUnpackable;

                        //special case, "SHADER_STAGE_FRAGMENT" fields must be packed at the end of the struct,
                        //otherwise the vertex output struct will have different semantic ordering than the fragment input struct.
                        if (subscript.preprocessor.Contains("SHADER_STAGE_FRAGMENT"))
                            return kPostUnpackable;

                        return kPreUnpackable;
                    }

                    if (subscript.HasSemantic() || subscript.vectorCount == 0)
                        return kPreUnpackable;

                    return kPackable;
                }).OrderBy(o => o.Key);

            var packStructName = "Packed" + baseStructName;
            int currentInterpolatorIndex = 0;
            var packedEntries = new List<PackedEntry>();
            foreach (var collection in fieldCategorized)
            {
                var packingEnabled = collection.Key == kPackable;
                if (packingEnabled)
                {
                    var groupByInterpolator = collection.GroupBy(field => string.IsNullOrEmpty(field.interpolation) ? string.Empty : field.interpolation);
                    foreach (var collectionInterpolator in groupByInterpolator)
                    {
                        //OrderByDescending is stable sort
                        var elementToPack = collectionInterpolator.OrderByDescending(o => o.vectorCount).ToList();
                        var totalVectorCount = elementToPack.Sum(o => o.vectorCount);

                        const bool allowSplitting = true;
#pragma warning disable 162
                        int maxInterpolatorCount;
                        if (allowSplitting)
                            maxInterpolatorCount = ((totalVectorCount + 3) & ~0x03) >> 2;
                        else
                            maxInterpolatorCount = elementToPack.Count;
#pragma warning restore 162

                        var intermediateInterpolator = Enumerable.Range(0, maxInterpolatorCount).Select(_ =>
                            (
                                fields : new List<PackedEntry.Input>(),
                                vectorCount : 0
                            )
                        ).ToList();

                        const int kMaxVectorCount = 4;
                        //First Pass *without* channel splitting
                        int itElement = 0;
                        while (itElement < elementToPack.Count)
                        {
                            var currentElement = elementToPack[itElement];

                            var availableSlotIndex = intermediateInterpolator.FindIndex(o =>
                                o.vectorCount + currentElement.vectorCount <= kMaxVectorCount);

                            if (availableSlotIndex != -1)
                            {
                                elementToPack.RemoveAt(itElement);
                                var slot = intermediateInterpolator[availableSlotIndex];
                                slot.vectorCount += currentElement.vectorCount;
                                slot.fields.Add(new PackedEntry.Input()
                                {
                                    field = currentElement,
                                    startChannel = 0,
                                    channelCount = currentElement.vectorCount,
                                });
                                intermediateInterpolator[availableSlotIndex] = slot;
                            }
                            else
                            {
                                itElement++;
                            }
                        }

                        if (!allowSplitting && elementToPack.Count > 0)
                            throw new InvalidOperationException("Unexpected failure in interpolator packing algorithm.");

                        //Second Pass *with* channel splitting
                        foreach (var remainingElement in elementToPack)
                        {
                            int currentStartChannel = 0;
                            while (currentStartChannel < remainingElement.vectorCount)
                            {
                                var availableSlotIndex = intermediateInterpolator.FindIndex(o => o.vectorCount < kMaxVectorCount);
                                if (availableSlotIndex == -1)
                                    throw new InvalidOperationException("Unexpected failure in interpolator packing algorithm.");

                                var slot = intermediateInterpolator[availableSlotIndex];
                                var currentChannelCount = Math.Min(kMaxVectorCount - slot.vectorCount, kMaxVectorCount - (remainingElement.vectorCount - currentStartChannel));
                                slot.vectorCount += currentChannelCount;
                                slot.fields.Add(new PackedEntry.Input()
                                {
                                    field = remainingElement,
                                    startChannel = currentStartChannel,
                                    channelCount = currentChannelCount,
                                });
                                intermediateInterpolator[availableSlotIndex] = slot;
                                currentStartChannel += currentChannelCount;
                            }
                        }

                        packedEntries.AddRange(intermediateInterpolator
                            .Where(o => o.vectorCount > 0)
                            .Select(o =>
                            {
                                var allName = o.fields.Select(f =>
                                    f.channelCount == f.field.vectorCount
                                    ? f.field.name
                                    : f.field.name + ShaderSpliceUtil.GetChannelSwizzle(f.startChannel, f.channelCount));
                                var name = o.fields.Count == 1
                                    ? allName.First()
                                    : "packed_" + allName.Aggregate((a, b) => $"{a}_{b}");
                                return new PackedEntry()
                                {
                                    inputFields = o.fields.ToArray(),
                                    packedField = new FieldDescriptor
                                    (
                                        tag: packStructName,
                                        name: name,
                                        define: string.Empty,
                                        type: $"float{o.vectorCount}",
                                        semantic: $"INTERP{currentInterpolatorIndex++}",
                                        preprocessor: string.Empty,
                                        subscriptOptions: StructFieldOptions.Static,
                                        interpolation: collectionInterpolator.Key
                                    )
                                };
                            }));
                    }
                }
                else
                {
                    foreach (var field in collection)
                    {
                        var inputFields = new[]
                        {
                            new PackedEntry.Input()
                            {
                                field = field,
                                channelCount = 0,
                                startChannel = 0
                            }
                        };

                        //Auto add semantic if needed
                        if (!field.HasSemantic())
                        {
                            var newField = new FieldDescriptor
                            (
                                tag: packStructName,
                                name: field.name,
                                define: field.define,
                                type: field.type,
                                semantic: $"INTERP{currentInterpolatorIndex++}",
                                preprocessor: field.preprocessor,
                                subscriptOptions: StructFieldOptions.Static,
                                interpolation: field.interpolation
                            );
                            packedEntries.Add(new()
                            {
                                inputFields = inputFields,
                                packedField = newField
                            });
                        }
                        else
                        {
                            packedEntries.Add(new PackedEntry()
                            {
                                inputFields = inputFields,
                                packedField = field
                            });
                        }
                    }
                }
            }

            return packedEntries.ToArray();
        }

        internal static void GeneratePackedStruct(StructDescriptor shaderStruct, ActiveFields activeFields, out StructDescriptor packStruct)
        {
            var packingLayout = GeneratePackingLayout(shaderStruct, activeFields);
            var packStructName = "Packed" + shaderStruct.name;
            packStruct = new StructDescriptor()
            {
                name = packStructName,
                packFields = true,
                fields = packingLayout.Select(o => o.packedField).ToArray()
            };
        }

        internal static void GenerateInterpolatorFunctions(StructDescriptor shaderStruct, IActiveFields activeFields, bool humanReadable, out ShaderStringBuilder interpolatorBuilder)
        {
            //set up function string builders and struct builder
            var packBuilder = new ShaderStringBuilder(humanReadable: humanReadable);
            var unpackBuilder = new ShaderStringBuilder(humanReadable: humanReadable);
            interpolatorBuilder = new ShaderStringBuilder(humanReadable: humanReadable);
            string packedStruct = "Packed" + shaderStruct.name;

            //declare function headers
            packBuilder.AppendLine($"{packedStruct} Pack{shaderStruct.name} ({shaderStruct.name} input)");
            packBuilder.AppendLine("{");
            packBuilder.IncreaseIndent();
            packBuilder.AppendLine($"{packedStruct} output;");
            packBuilder.AppendLine($"ZERO_INITIALIZE({packedStruct}, output);");

            unpackBuilder.AppendLine($"{shaderStruct.name} Unpack{shaderStruct.name} ({packedStruct} input)");
            unpackBuilder.AppendLine("{");
            unpackBuilder.IncreaseIndent();
            unpackBuilder.AppendLine($"{shaderStruct.name} output;");

            var packingLayout = GeneratePackingLayout(shaderStruct, activeFields);
            foreach (var packEntry in packingLayout)
            {
                int firstPackedChannel = 0;
                foreach (var input in packEntry.inputFields)
                {
                    if (input.field.HasPreprocessor())
                    {
                        packBuilder.AppendLine($"#if {input.field.preprocessor}");
                        unpackBuilder.AppendLine($"#if {input.field.preprocessor}");
                    }

                    var packedChannels = string.Empty;
                    var unpackedChannel = string.Empty;
                    if (input.channelCount != 0)
                    {
                        if (firstPackedChannel != 0 || input.channelCount != packEntry.packedField.vectorCount)
                            packedChannels = $".{ShaderSpliceUtil.GetChannelSwizzle(firstPackedChannel, input.channelCount)}";
                        if (input.startChannel != 0 || input.channelCount != input.field.vectorCount)
                            unpackedChannel = $".{ShaderSpliceUtil.GetChannelSwizzle(input.startChannel, input.channelCount)}";
                    }

                    packBuilder.AppendLine($"output.{packEntry.packedField.name}{packedChannels} = input.{input.field.name}{unpackedChannel};");
                    unpackBuilder.AppendLine($"output.{input.field.name}{unpackedChannel} = input.{packEntry.packedField.name}{packedChannels};");
                    firstPackedChannel += input.field.vectorCount;

                    if (input.field.HasPreprocessor())
                    {
                        packBuilder.AppendLine("#endif");
                        unpackBuilder.AppendLine("#endif");
                    }
                }
            }

            //close function declarations
            packBuilder.AppendLine("return output;");
            packBuilder.DecreaseIndent();
            packBuilder.AppendLine("}");
            packBuilder.AppendNewLine();

            unpackBuilder.AppendLine("return output;");
            unpackBuilder.DecreaseIndent();
            unpackBuilder.AppendLine("}");
            unpackBuilder.AppendNewLine();

            interpolatorBuilder.Concat(packBuilder);
            interpolatorBuilder.Concat(unpackBuilder);
        }

        internal static void GetUpstreamNodesForShaderPass(AbstractMaterialNode outputNode, PassDescriptor pass, out List<AbstractMaterialNode> vertexNodes, out List<AbstractMaterialNode> pixelNodes)
        {
            // Traverse Graph Data
            vertexNodes = Pool.ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(vertexNodes, outputNode, NodeUtils.IncludeSelf.Include);

            pixelNodes = Pool.ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(pixelNodes, outputNode, NodeUtils.IncludeSelf.Include);
        }

        internal static void GetActiveFieldsAndPermutationsForNodes(PassDescriptor pass,
            KeywordCollector keywordCollector, List<AbstractMaterialNode> vertexNodes, List<AbstractMaterialNode> pixelNodes,
            bool[] texCoordNeedsDerivs,
            List<int>[] vertexNodePermutations, List<int>[] pixelNodePermutations,
            ActiveFields activeFields, out ShaderGraphRequirementsPerKeyword graphRequirements)
        {
            // Initialize requirements
            ShaderGraphRequirementsPerKeyword pixelRequirements = new ShaderGraphRequirementsPerKeyword();
            ShaderGraphRequirementsPerKeyword vertexRequirements = new ShaderGraphRequirementsPerKeyword();
            graphRequirements = new ShaderGraphRequirementsPerKeyword();

            // Evaluate all Keyword permutations
            if (keywordCollector.permutations.Count > 0)
            {
                for (int i = 0; i < keywordCollector.permutations.Count; i++)
                {
                    // Get active nodes for this permutation
                    var localVertexNodes = Pool.HashSetPool<AbstractMaterialNode>.Get();
                    var localPixelNodes = Pool.HashSetPool<AbstractMaterialNode>.Get();

                    localVertexNodes.EnsureCapacity(vertexNodes.Count);
                    localPixelNodes.EnsureCapacity(pixelNodes.Count);

                    foreach (var vertexNode in vertexNodes)
                    {
                        NodeUtils.DepthFirstCollectNodesFromNode(localVertexNodes, vertexNode, NodeUtils.IncludeSelf.Include, keywordCollector.permutations[i]);
                    }

                    foreach (var pixelNode in pixelNodes)
                    {
                        NodeUtils.DepthFirstCollectNodesFromNode(localPixelNodes, pixelNode, NodeUtils.IncludeSelf.Include, keywordCollector.permutations[i]);
                    }

                    // Track each vertex node in this permutation
                    foreach (AbstractMaterialNode vertexNode in localVertexNodes)
                    {
                        int nodeIndex = vertexNodes.IndexOf(vertexNode);

                        if (vertexNodePermutations[nodeIndex] == null)
                            vertexNodePermutations[nodeIndex] = new List<int>();
                        vertexNodePermutations[nodeIndex].Add(i);
                    }

                    // Track each pixel node in this permutation
                    foreach (AbstractMaterialNode pixelNode in localPixelNodes)
                    {
                        int nodeIndex = pixelNodes.IndexOf(pixelNode);

                        if (pixelNodePermutations[nodeIndex] == null)
                            pixelNodePermutations[nodeIndex] = new List<int>();
                        pixelNodePermutations[nodeIndex].Add(i);
                    }

                    // Get requirements for this permutation
                    vertexRequirements[i].SetRequirements(ShaderGraphRequirements.FromNodes(localVertexNodes, ShaderStageCapability.Vertex, false, texCoordNeedsDerivs));
                    pixelRequirements[i].SetRequirements(ShaderGraphRequirements.FromNodes(localPixelNodes, ShaderStageCapability.Fragment, false, texCoordNeedsDerivs));

                    // Add active fields
                    var conditionalFields = GetActiveFieldsFromConditionals(GetConditionalFieldsFromPixelRequirements(pixelRequirements[i].requirements));
                    if (activeFields[i].Contains(Fields.GraphVertex))
                    {
                        conditionalFields.AddRange(GetActiveFieldsFromConditionals(GetConditionalFieldsFromVertexRequirements(vertexRequirements[i].requirements)));
                    }
                    foreach (var field in conditionalFields)
                    {
                        activeFields[i].Add(field);
                    }
                }
            }
            // No Keywords
            else
            {
                // Get requirements
                vertexRequirements.baseInstance.SetRequirements(ShaderGraphRequirements.FromNodes(vertexNodes, ShaderStageCapability.Vertex, false, texCoordNeedsDerivs));
                pixelRequirements.baseInstance.SetRequirements(ShaderGraphRequirements.FromNodes(pixelNodes, ShaderStageCapability.Fragment, false, texCoordNeedsDerivs));

                // Add active fields
                var conditionalFields = GetActiveFieldsFromConditionals(GetConditionalFieldsFromPixelRequirements(pixelRequirements.baseInstance.requirements));
                if (activeFields.baseInstance.Contains(Fields.GraphVertex))
                {
                    conditionalFields.AddRange(GetActiveFieldsFromConditionals(GetConditionalFieldsFromVertexRequirements(vertexRequirements.baseInstance.requirements)));
                }
                foreach (var field in conditionalFields)
                {
                    activeFields.baseInstance.Add(field);
                }
            }

            // Build graph requirements
            graphRequirements.UnionWith(pixelRequirements);
            graphRequirements.UnionWith(vertexRequirements);
        }

        static ConditionalField[] GetConditionalFieldsFromVertexRequirements(ShaderGraphRequirements requirements)
        {
            return new ConditionalField[]
            {
                new ConditionalField(StructFields.VertexDescriptionInputs.ScreenPosition,                               requirements.requiresScreenPosition),
                new ConditionalField(StructFields.VertexDescriptionInputs.NDCPosition,                                  requirements.requiresNDCPosition),
                new ConditionalField(StructFields.VertexDescriptionInputs.PixelPosition,                                requirements.requiresPixelPosition),

                new ConditionalField(StructFields.VertexDescriptionInputs.VertexColor,                                  requirements.requiresVertexColor),

                new ConditionalField(StructFields.VertexDescriptionInputs.ObjectSpaceNormal,                            (requirements.requiresNormal & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.ViewSpaceNormal,                              (requirements.requiresNormal & NeededCoordinateSpace.View) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.WorldSpaceNormal,                             (requirements.requiresNormal & NeededCoordinateSpace.World) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.TangentSpaceNormal,                           (requirements.requiresNormal & NeededCoordinateSpace.Tangent) > 0),

                new ConditionalField(StructFields.VertexDescriptionInputs.ObjectSpaceViewDirection,                     (requirements.requiresViewDir & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.ViewSpaceViewDirection,                       (requirements.requiresViewDir & NeededCoordinateSpace.View) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.WorldSpaceViewDirection,                      (requirements.requiresViewDir & NeededCoordinateSpace.World) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.TangentSpaceViewDirection,                    (requirements.requiresViewDir & NeededCoordinateSpace.Tangent) > 0),

                new ConditionalField(StructFields.VertexDescriptionInputs.ObjectSpaceTangent,                           (requirements.requiresTangent & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.ViewSpaceTangent,                             (requirements.requiresTangent & NeededCoordinateSpace.View) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.WorldSpaceTangent,                            (requirements.requiresTangent & NeededCoordinateSpace.World) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.TangentSpaceTangent,                          (requirements.requiresTangent & NeededCoordinateSpace.Tangent) > 0),

                new ConditionalField(StructFields.VertexDescriptionInputs.ObjectSpaceBiTangent,                         (requirements.requiresBitangent & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.ViewSpaceBiTangent,                           (requirements.requiresBitangent & NeededCoordinateSpace.View) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.WorldSpaceBiTangent,                          (requirements.requiresBitangent & NeededCoordinateSpace.World) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.TangentSpaceBiTangent,                        (requirements.requiresBitangent & NeededCoordinateSpace.Tangent) > 0),

                new ConditionalField(StructFields.VertexDescriptionInputs.ObjectSpacePosition,                          (requirements.requiresPosition & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.ViewSpacePosition,                            (requirements.requiresPosition & NeededCoordinateSpace.View) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.WorldSpacePosition,                           (requirements.requiresPosition & NeededCoordinateSpace.World) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.TangentSpacePosition,                         (requirements.requiresPosition & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.AbsoluteWorldSpacePosition,                   (requirements.requiresPosition & NeededCoordinateSpace.AbsoluteWorld) > 0),

                new ConditionalField(StructFields.VertexDescriptionInputs.ObjectSpacePositionPredisplacement,           (requirements.requiresPositionPredisplacement & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.ViewSpacePositionPredisplacement,             (requirements.requiresPositionPredisplacement & NeededCoordinateSpace.View) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.WorldSpacePositionPredisplacement,            (requirements.requiresPositionPredisplacement & NeededCoordinateSpace.World) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.TangentSpacePositionPredisplacement,          (requirements.requiresPositionPredisplacement & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.AbsoluteWorldSpacePositionPredisplacement,    (requirements.requiresPositionPredisplacement & NeededCoordinateSpace.AbsoluteWorld) > 0),

                new ConditionalField(StructFields.VertexDescriptionInputs.uv0,                                          requirements.requiresMeshUVs.Contains(UVChannel.UV0)),
                new ConditionalField(StructFields.VertexDescriptionInputs.uv1,                                          requirements.requiresMeshUVs.Contains(UVChannel.UV1)),
                new ConditionalField(StructFields.VertexDescriptionInputs.uv2,                                          requirements.requiresMeshUVs.Contains(UVChannel.UV2)),
                new ConditionalField(StructFields.VertexDescriptionInputs.uv3,                                          requirements.requiresMeshUVs.Contains(UVChannel.UV3)),

                new ConditionalField(GeneratorDerivativeUtils.uv0Ddx,                                                   requirements.requiresMeshUVDerivatives.Contains(UVChannel.UV0)),
                new ConditionalField(GeneratorDerivativeUtils.uv0Ddy,                                                   requirements.requiresMeshUVDerivatives.Contains(UVChannel.UV0)),
                new ConditionalField(GeneratorDerivativeUtils.uv1Ddx,                                                   requirements.requiresMeshUVDerivatives.Contains(UVChannel.UV1)),
                new ConditionalField(GeneratorDerivativeUtils.uv1Ddy,                                                   requirements.requiresMeshUVDerivatives.Contains(UVChannel.UV1)),
                new ConditionalField(GeneratorDerivativeUtils.uv2Ddx,                                                   requirements.requiresMeshUVDerivatives.Contains(UVChannel.UV2)),
                new ConditionalField(GeneratorDerivativeUtils.uv2Ddy,                                                   requirements.requiresMeshUVDerivatives.Contains(UVChannel.UV2)),
                new ConditionalField(GeneratorDerivativeUtils.uv3Ddx,                                                   requirements.requiresMeshUVDerivatives.Contains(UVChannel.UV3)),
                new ConditionalField(GeneratorDerivativeUtils.uv3Ddy,                                                   requirements.requiresMeshUVDerivatives.Contains(UVChannel.UV3)),

                new ConditionalField(StructFields.VertexDescriptionInputs.TimeParameters,                               requirements.requiresTime),

                new ConditionalField(StructFields.VertexDescriptionInputs.BoneWeights,                                  requirements.requiresVertexSkinning),
                new ConditionalField(StructFields.VertexDescriptionInputs.BoneIndices,                                  requirements.requiresVertexSkinning),
                new ConditionalField(StructFields.VertexDescriptionInputs.VertexID,                                     requirements.requiresVertexID),
                new ConditionalField(StructFields.VertexDescriptionInputs.InstanceID,                                   requirements.requiresInstanceID),

                new ConditionalField(Fields.ObjectToWorld, requirements.requiresTransforms.Contains(NeededTransform.ObjectToWorld)),
                new ConditionalField(Fields.WorldToObject, requirements.requiresTransforms.Contains(NeededTransform.WorldToObject)),
            };
        }

        static ConditionalField[] GetConditionalFieldsFromPixelRequirements(ShaderGraphRequirements requirements)
        {
            return new ConditionalField[]
            {
                new ConditionalField(StructFields.SurfaceDescriptionInputs.ScreenPosition,                              requirements.requiresScreenPosition),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.NDCPosition,                                 requirements.requiresNDCPosition),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.PixelPosition,                               requirements.requiresPixelPosition),

                new ConditionalField(StructFields.SurfaceDescriptionInputs.VertexColor,                                 requirements.requiresVertexColor),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.FaceSign,                                    requirements.requiresFaceSign),

                new ConditionalField(StructFields.SurfaceDescriptionInputs.ObjectSpaceNormal,                           (requirements.requiresNormal & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.ViewSpaceNormal,                             (requirements.requiresNormal & NeededCoordinateSpace.View) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.WorldSpaceNormal,                            (requirements.requiresNormal & NeededCoordinateSpace.World) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.TangentSpaceNormal,                          (requirements.requiresNormal & NeededCoordinateSpace.Tangent) > 0),

                new ConditionalField(StructFields.SurfaceDescriptionInputs.ObjectSpaceViewDirection,                    (requirements.requiresViewDir & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.ViewSpaceViewDirection,                      (requirements.requiresViewDir & NeededCoordinateSpace.View) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.WorldSpaceViewDirection,                     (requirements.requiresViewDir & NeededCoordinateSpace.World) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.TangentSpaceViewDirection,                   (requirements.requiresViewDir & NeededCoordinateSpace.Tangent) > 0),

                new ConditionalField(StructFields.SurfaceDescriptionInputs.ObjectSpaceTangent,                          (requirements.requiresTangent & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.ViewSpaceTangent,                            (requirements.requiresTangent & NeededCoordinateSpace.View) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.WorldSpaceTangent,                           (requirements.requiresTangent & NeededCoordinateSpace.World) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.TangentSpaceTangent,                         (requirements.requiresTangent & NeededCoordinateSpace.Tangent) > 0),

                new ConditionalField(StructFields.SurfaceDescriptionInputs.ObjectSpaceBiTangent,                        (requirements.requiresBitangent & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.ViewSpaceBiTangent,                          (requirements.requiresBitangent & NeededCoordinateSpace.View) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.WorldSpaceBiTangent,                         (requirements.requiresBitangent & NeededCoordinateSpace.World) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.TangentSpaceBiTangent,                       (requirements.requiresBitangent & NeededCoordinateSpace.Tangent) > 0),

                new ConditionalField(StructFields.SurfaceDescriptionInputs.ObjectSpacePosition,                         (requirements.requiresPosition & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.ViewSpacePosition,                           (requirements.requiresPosition & NeededCoordinateSpace.View) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.WorldSpacePosition,                          (requirements.requiresPosition & NeededCoordinateSpace.World) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.TangentSpacePosition,                        (requirements.requiresPosition & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.AbsoluteWorldSpacePosition,                  (requirements.requiresPosition & NeededCoordinateSpace.AbsoluteWorld) > 0),

                new ConditionalField(StructFields.SurfaceDescriptionInputs.ObjectSpacePositionPredisplacement,          (requirements.requiresPositionPredisplacement & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.ViewSpacePositionPredisplacement,            (requirements.requiresPositionPredisplacement & NeededCoordinateSpace.View) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.WorldSpacePositionPredisplacement,           (requirements.requiresPositionPredisplacement & NeededCoordinateSpace.World) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.TangentSpacePositionPredisplacement,         (requirements.requiresPositionPredisplacement & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.AbsoluteWorldSpacePositionPredisplacement,   (requirements.requiresPositionPredisplacement & NeededCoordinateSpace.AbsoluteWorld) > 0),

                new ConditionalField(StructFields.SurfaceDescriptionInputs.uv0,                                         requirements.requiresMeshUVs.Contains(UVChannel.UV0)),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.uv1,                                         requirements.requiresMeshUVs.Contains(UVChannel.UV1)),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.uv2,                                         requirements.requiresMeshUVs.Contains(UVChannel.UV2)),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.uv3,                                         requirements.requiresMeshUVs.Contains(UVChannel.UV3)),

                new ConditionalField(StructFields.SurfaceDescriptionInputs.TimeParameters,                              requirements.requiresTime),

                new ConditionalField(StructFields.SurfaceDescriptionInputs.BoneWeights,                                 requirements.requiresVertexSkinning),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.BoneIndices,                                 requirements.requiresVertexSkinning),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.VertexID,                                    requirements.requiresVertexID),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.InstanceID,                                  requirements.requiresInstanceID),

                new ConditionalField(Fields.ObjectToWorld, requirements.requiresTransforms.Contains(NeededTransform.ObjectToWorld)),
                new ConditionalField(Fields.WorldToObject, requirements.requiresTransforms.Contains(NeededTransform.WorldToObject)),
            };
        }

        internal static void AddRequiredFields(FieldCollection passRequiredFields, IActiveFieldsSet activeFields)
        {
            if (passRequiredFields != null)
            {
                foreach (FieldCollection.Item requiredField in passRequiredFields)
                {
                    activeFields.AddAll(requiredField.field);
                }
            }
        }

        internal static void ApplyFieldDependencies(IActiveFields activeFields, DependencyCollection dependencies)
        {
            // add active fields to queue
            Queue<FieldDescriptor> fieldsToPropagate = new Queue<FieldDescriptor>();
            foreach (var f in activeFields.fields)
            {
                fieldsToPropagate.Enqueue(f);
            }

            // foreach field in queue:
            while (fieldsToPropagate.Count > 0)
            {
                FieldDescriptor field = fieldsToPropagate.Dequeue();
                if (activeFields.Contains(field))           // this should always be true
                {
                    if (dependencies == null)
                        return;

                    // find all dependencies of field that are not already active
                    foreach (DependencyCollection.Item d in dependencies.Where(d => (d.dependency.field == field) && !activeFields.Contains(d.dependency.dependsOn)))
                    {
                        // activate them and add them to the queue
                        activeFields.Add(d.dependency.dependsOn);
                        fieldsToPropagate.Enqueue(d.dependency.dependsOn);
                    }
                }
            }
        }

        internal static List<MaterialSlot> FindMaterialSlotsOnNode(IEnumerable<int> slots, AbstractMaterialNode node)
        {
            if (slots == null)
                return null;

            var activeSlots = new List<MaterialSlot>();
            foreach (var id in slots)
            {
                MaterialSlot slot = node.FindSlot<MaterialSlot>(id);
                if (slot != null)
                {
                    activeSlots.Add(slot);
                }
            }
            return activeSlots;
        }

        internal static string AdaptNodeOutput(AbstractMaterialNode node, int outputSlotId, ConcreteSlotValueType convertToType)
        {
            var outputSlot = node.FindOutputSlot<MaterialSlot>(outputSlotId);

            if (outputSlot == null)
                return kErrorString;

            var convertFromType = outputSlot.concreteValueType;
            var rawOutput = node.GetVariableNameForSlot(outputSlotId);
            if (convertFromType == convertToType)
                return rawOutput;

            switch (convertToType)
            {
                case ConcreteSlotValueType.Boolean:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("((bool) {0})", rawOutput);
                        case ConcreteSlotValueType.Vector2:
                        case ConcreteSlotValueType.Vector3:
                        case ConcreteSlotValueType.Vector4:
                            return string.Format("((bool) {0}.x)", rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Vector1:
                    if (convertFromType == ConcreteSlotValueType.Boolean)
                        return string.Format("(($precision) {0})", rawOutput);
                    else
                        return string.Format("({0}).x", rawOutput);
                case ConcreteSlotValueType.Vector2:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Boolean:
                            return string.Format("((($precision) {0}).xx)", rawOutput);
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("({0}.xx)", rawOutput);
                        case ConcreteSlotValueType.Vector3:
                        case ConcreteSlotValueType.Vector4:
                            return string.Format("({0}.xy)", rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Vector3:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Boolean:
                            return string.Format("((($precision) {0}).xxx)", rawOutput);
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("({0}.xxx)", rawOutput);
                        case ConcreteSlotValueType.Vector2:
                            return string.Format("($precision3({0}, 0.0))", rawOutput);
                        case ConcreteSlotValueType.Vector4:
                            return string.Format("({0}.xyz)", rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Vector4:
                    switch (convertFromType)
                    {
                        case ConcreteSlotValueType.Boolean:
                            return string.Format("((($precision) {0}).xxxx)", rawOutput);
                        case ConcreteSlotValueType.Vector1:
                            return string.Format("({0}.xxxx)", rawOutput);
                        case ConcreteSlotValueType.Vector2:
                            return string.Format("($precision4({0}, 0.0, 1.0))", rawOutput);
                        case ConcreteSlotValueType.Vector3:
                            return string.Format("($precision4({0}, 1.0))", rawOutput);
                        default:
                            return kErrorString;
                    }
                case ConcreteSlotValueType.Matrix3:
                    return rawOutput;
                case ConcreteSlotValueType.Matrix2:
                    return rawOutput;
                case ConcreteSlotValueType.PropertyConnectionState:
                    return node.GetConnnectionStateVariableNameForSlot(outputSlotId);
                default:
                    return kErrorString;
            }
        }

        internal static string AdaptNodeOutputForPreview(AbstractMaterialNode node, int outputSlotId)
        {
            string rawOutput = node.GetVariableNameForSlot(outputSlotId);
            return AdaptNodeOutputForPreview(node, outputSlotId, rawOutput);
        }

        internal static string AdaptNodeOutputForPreview(AbstractMaterialNode node, int slotId, string variableName)
        {
            var slot = node.FindSlot<MaterialSlot>(slotId);

            if (slot == null)
                return kErrorString;

            var convertFromType = slot.concreteValueType;

            // preview is always dimension 4
            switch (convertFromType)
            {
                case ConcreteSlotValueType.Vector1:
                    return string.Format("half4({0}, {0}, {0}, 1.0)", variableName);
                case ConcreteSlotValueType.Vector2:
                    return string.Format("half4({0}.x, {0}.y, 0.0, 1.0)", variableName);
                case ConcreteSlotValueType.Vector3:
                    return string.Format("half4({0}.x, {0}.y, {0}.z, 1.0)", variableName);
                case ConcreteSlotValueType.Vector4:
                    return string.Format("half4({0}.x, {0}.y, {0}.z, 1.0)", variableName);
                case ConcreteSlotValueType.Boolean:
                    return string.Format("half4({0}, {0}, {0}, 1.0)", variableName);
                default:
                    return "half4(0, 0, 0, 0)";
            }
        }

        static void GenerateSpaceTranslationSurfaceInputs(
            NeededCoordinateSpace neededSpaces,
            InterpolatorType interpolatorType,
            ShaderStringBuilder builder,
            string format = "float3 {0};")
        {
            if ((neededSpaces & NeededCoordinateSpace.Object) > 0)
                builder.AppendLine(format, CoordinateSpace.Object.ToVariableName(interpolatorType));

            if ((neededSpaces & NeededCoordinateSpace.World) > 0)
                builder.AppendLine(format, CoordinateSpace.World.ToVariableName(interpolatorType));

            if ((neededSpaces & NeededCoordinateSpace.View) > 0)
                builder.AppendLine(format, CoordinateSpace.View.ToVariableName(interpolatorType));

            if ((neededSpaces & NeededCoordinateSpace.Tangent) > 0)
                builder.AppendLine(format, CoordinateSpace.Tangent.ToVariableName(interpolatorType));

            if ((neededSpaces & NeededCoordinateSpace.AbsoluteWorld) > 0)
                builder.AppendLine(format, CoordinateSpace.AbsoluteWorld.ToVariableName(interpolatorType));
        }

        internal static void GeneratePropertiesBlock(ShaderStringBuilder sb, PropertyCollector propertyCollector, KeywordCollector keywordCollector, GenerationMode mode, List<GraphInputData> graphInputs)
        {
            sb.AppendLine("Properties");
            using (sb.BlockScope())
            {
                if (graphInputs == null || graphInputs.Count == 0)
                {
                    foreach (var prop in propertyCollector.properties.Where(x => x.shouldGeneratePropertyBlock))
                    {
                        prop.AppendPropertyBlockStrings(sb);
                    }

                    // Keywords use hardcoded state in preview
                    // Do not add them to the Property Block
                    if (mode == GenerationMode.Preview)
                        return;

                    foreach (var key in keywordCollector.keywords.Where(x => x.generatePropertyBlock))
                    {
                        key.AppendPropertyBlockStrings(sb);
                    }
                }
                else
                {
                    var propertyInputs = propertyCollector.properties.Where(x => x.shouldGeneratePropertyBlock).ToList();
                    var keywordInputs = keywordCollector.keywords.Where(x => x.generatePropertyBlock).ToList();
                    foreach (var input in graphInputs)
                    {
                        if (input.isKeyword && mode != GenerationMode.Preview)
                        {
                            var keyword = keywordInputs.FirstOrDefault(x => x.referenceName.CompareTo(input.referenceName) == 0);
                            if (keyword != null)
                            {
                                keyword.AppendPropertyBlockStrings(sb);
                                keywordInputs.Remove(keyword);
                            }
                        }
                        else if (!input.isKeyword)
                        {
                            var property = propertyInputs.FirstOrDefault(x => x.referenceName.CompareTo(input.referenceName) == 0);
                            if (property != null)
                            {
                                property.AppendPropertyBlockStrings(sb);
                                propertyInputs.Remove(property);
                            }
                        }
                    }

                    foreach (var property in propertyInputs)
                    {
                        property.AppendPropertyBlockStrings(sb);
                    }

                    if (mode != GenerationMode.Preview)
                    {
                        foreach (var keyword in keywordInputs)
                        {
                            keyword.AppendPropertyBlockStrings(sb);
                        }
                    }
                }
            }
        }

        internal static void GenerateSurfaceInputStruct(ShaderStringBuilder sb, ShaderGraphRequirements requirements, string structName)
        {
            sb.AppendLine($"struct {structName}");
            using (sb.BlockSemicolonScope())
            {
                GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, InterpolatorType.Normal, sb);
                GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, InterpolatorType.Tangent, sb);
                GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, InterpolatorType.BiTangent, sb);
                GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, InterpolatorType.ViewDirection, sb);
                GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, InterpolatorType.Position, sb);
                GenerateSpaceTranslationSurfaceInputs(requirements.requiresPositionPredisplacement, InterpolatorType.PositionPredisplacement, sb);

                if (requirements.requiresVertexColor)
                    sb.AppendLine("float4 {0};", ShaderGeneratorNames.VertexColor);

                if (requirements.requiresScreenPosition)
                    sb.AppendLine("float4 {0};", ShaderGeneratorNames.ScreenPosition);

                if (requirements.requiresNDCPosition)
                    sb.AppendLine("float2 {0};", ShaderGeneratorNames.NDCPosition);

                if (requirements.requiresPixelPosition)
                    sb.AppendLine("float2 {0};", ShaderGeneratorNames.PixelPosition);

                if (requirements.requiresFaceSign)
                    sb.AppendLine("float {0};", ShaderGeneratorNames.FaceSign);

                foreach (var channel in requirements.requiresMeshUVs.Distinct())
                    sb.AppendLine("half4 {0};", channel.GetUVName());

                if (requirements.requiresTime)
                {
                    sb.AppendLine("float3 {0};", ShaderGeneratorNames.TimeParameters);
                }

                if (requirements.requiresVertexSkinning)
                {
                    sb.AppendLine("uint4 {0};", ShaderGeneratorNames.BoneIndices);
                    sb.AppendLine("float4 {0};", ShaderGeneratorNames.BoneWeights);
                }

                if (requirements.requiresVertexID)
                {
                    sb.AppendLine("uint {0};", ShaderGeneratorNames.VertexID);
                }

                if (requirements.requiresInstanceID)
                {
                    sb.AppendLine("uint {0};", ShaderGeneratorNames.InstanceID);
                }
            }
        }

        internal static void GenerateSurfaceInputTransferCode(ShaderStringBuilder sb, ShaderGraphRequirements requirements, string structName, string variableName)
        {
            sb.AppendLine($"{structName} {variableName};");

            GenerateSpaceTranslationSurfaceInputs(requirements.requiresNormal, InterpolatorType.Normal, sb, $"{variableName}.{{0}} = IN.{{0}};");
            GenerateSpaceTranslationSurfaceInputs(requirements.requiresTangent, InterpolatorType.Tangent, sb, $"{variableName}.{{0}} = IN.{{0}};");
            GenerateSpaceTranslationSurfaceInputs(requirements.requiresBitangent, InterpolatorType.BiTangent, sb, $"{variableName}.{{0}} = IN.{{0}};");
            GenerateSpaceTranslationSurfaceInputs(requirements.requiresViewDir, InterpolatorType.ViewDirection, sb, $"{variableName}.{{0}} = IN.{{0}};");
            GenerateSpaceTranslationSurfaceInputs(requirements.requiresPosition, InterpolatorType.Position, sb, $"{variableName}.{{0}} = IN.{{0}};");
            GenerateSpaceTranslationSurfaceInputs(requirements.requiresPositionPredisplacement, InterpolatorType.PositionPredisplacement, sb, $"{variableName}.{{0}} = IN.{{0}};");

            if (requirements.requiresVertexColor)
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.VertexColor} = IN.{ShaderGeneratorNames.VertexColor};");

            if (requirements.requiresScreenPosition)
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.ScreenPosition} = IN.{ShaderGeneratorNames.ScreenPosition};");

            if (requirements.requiresNDCPosition)
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.NDCPosition} = IN.{ShaderGeneratorNames.NDCPosition};");

            if (requirements.requiresPixelPosition)
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.PixelPosition} = IN.{ShaderGeneratorNames.PixelPosition};");

            if (requirements.requiresFaceSign)
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.FaceSign} = IN.{ShaderGeneratorNames.FaceSign};");

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
                sb.AppendLine($"{variableName}.{channel.GetUVName()} = IN.{channel.GetUVName()};");

            if (requirements.requiresTime)
            {
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.TimeParameters} = IN.{ShaderGeneratorNames.TimeParameters};");
            }

            if (requirements.requiresVertexSkinning)
            {
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.BoneIndices} = IN.{ShaderGeneratorNames.BoneIndices};");
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.BoneWeights} = IN.{ShaderGeneratorNames.BoneWeights};");
            }

            if (requirements.requiresVertexID)
            {
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.VertexID} = IN.{ShaderGeneratorNames.VertexID};");
            }

            if (requirements.requiresInstanceID)
            {
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.InstanceID} = IN.{ShaderGeneratorNames.InstanceID};");
            }
        }

        internal static void GenerateSurfaceDescriptionStruct(ShaderStringBuilder surfaceDescriptionStruct, List<MaterialSlot> slots, string structName = "SurfaceDescription", IActiveFieldsSet activeFields = null, bool isSubgraphOutput = false, bool virtualTextureFeedback = false)
        {
            surfaceDescriptionStruct.AppendLine("struct {0}", structName);
            using (surfaceDescriptionStruct.BlockSemicolonScope())
            {
                if (slots != null)
                {
                    if (isSubgraphOutput)
                    {
                        var firstSlot = slots.FirstOrDefault();
                        if (firstSlot != null)
                        {
                            var hlslName = $"{NodeUtils.GetHLSLSafeName(firstSlot.shaderOutputName)}_{firstSlot.id}";
                            surfaceDescriptionStruct.AppendLine("{0} {1};", firstSlot.concreteValueType.ToShaderString(firstSlot.owner.concretePrecision), hlslName);
                            surfaceDescriptionStruct.AppendLine("{0} {1};", ConcreteSlotValueType.Vector4.ToShaderString(firstSlot.owner.concretePrecision), "Out");
                        }
                        else
                            surfaceDescriptionStruct.AppendLine("{0} {1};", ConcreteSlotValueType.Vector4.ToShaderString(ConcretePrecision.Single), "Out");
                    }
                    else
                    {
                        foreach (var slot in slots)
                        {
                            string hlslName = NodeUtils.GetHLSLSafeName(slot.shaderOutputName);

                            surfaceDescriptionStruct.AppendLine("{0} {1};", slot.concreteValueType.ToShaderString(slot.owner.concretePrecision), hlslName);

                            if (activeFields != null)
                            {
                                var structField = new FieldDescriptor(structName, hlslName, "");
                                activeFields.AddAll(structField);
                            }
                        }
                    }
                }

                // TODO: move this into the regular FieldDescriptor system with a conditional, doesn't belong as a special case here
                if (virtualTextureFeedback)
                {
                    surfaceDescriptionStruct.AppendLine("{0} {1};", ConcreteSlotValueType.Vector4.ToShaderString(ConcretePrecision.Single), "VTPackedFeedback");

                    if (!isSubgraphOutput && activeFields != null)
                    {
                        var structField = new FieldDescriptor(structName, "VTPackedFeedback", "");
                        activeFields.AddAll(structField);
                    }
                }
            }
        }

        internal static void GenerateSurfaceDescriptionFunction(
            List<AbstractMaterialNode> nodes,
            List<int>[] keywordPermutationsPerNode,
            AbstractMaterialNode rootNode,
            GraphData graph,
            ShaderStringBuilder surfaceDescriptionFunction,
            FunctionRegistry functionRegistry,
            PropertyCollector shaderProperties,
            KeywordCollector shaderKeywords,
            GenerationMode mode,
            string functionName = "PopulateSurfaceData",
            string surfaceDescriptionName = "SurfaceDescription",
            Vector1ShaderProperty outputIdProperty = null,
            IEnumerable<MaterialSlot> slots = null,
            string graphInputStructName = "SurfaceDescriptionInputs",
            bool virtualTextureFeedback = false)
        {
            if (graph == null)
                return;

            graph.CollectShaderProperties(shaderProperties, mode);

            if (mode == GenerationMode.VFX)
            {
                const string k_GraphProperties = "GraphProperties";
                surfaceDescriptionFunction.AppendLine(String.Format("{0} {1}(SurfaceDescriptionInputs IN, {2} PROP)", surfaceDescriptionName, functionName, k_GraphProperties), false);
            }
            else
                surfaceDescriptionFunction.AppendLine(String.Format("{0} {1}(SurfaceDescriptionInputs IN)", surfaceDescriptionName, functionName), false);

            using (surfaceDescriptionFunction.BlockScope())
            {
                surfaceDescriptionFunction.AppendLine("{0} surface = ({0})0;", surfaceDescriptionName);
                for (int i = 0; i < nodes.Count; i++)
                {
                    GenerateDescriptionForNode(nodes[i], keywordPermutationsPerNode[i], functionRegistry, surfaceDescriptionFunction,
                        shaderProperties, shaderKeywords,
                        graph, mode);
                }

                functionRegistry.builder.currentNode = null;
                surfaceDescriptionFunction.currentNode = null;

                GenerateSurfaceDescriptionRemap(graph, rootNode, slots,
                    surfaceDescriptionFunction, mode);

                if (virtualTextureFeedback)
                {
                    VirtualTexturingFeedbackUtils.GenerateVirtualTextureFeedback(
                        nodes,
                        keywordPermutationsPerNode,
                        surfaceDescriptionFunction,
                        shaderKeywords);
                }

                surfaceDescriptionFunction.AppendLine("return surface;");
            }
        }

        static void GenerateDescriptionForNode(
            AbstractMaterialNode activeNode,
            List<int> keywordPermutations,
            FunctionRegistry functionRegistry,
            ShaderStringBuilder descriptionFunction,
            PropertyCollector shaderProperties,
            KeywordCollector shaderKeywords,
            GraphData graph,
            GenerationMode mode)
        {
            if (activeNode is IGeneratesFunction functionNode)
            {
                functionRegistry.builder.currentNode = activeNode;
                Profiler.BeginSample("GenerateNodeFunction");
                functionNode.GenerateNodeFunction(functionRegistry, mode);
                Profiler.EndSample();
            }

            if (activeNode is IGeneratesBodyCode bodyNode)
            {
                if (keywordPermutations != null)
                    descriptionFunction.AppendLine(KeywordUtil.GetKeywordPermutationSetConditional(keywordPermutations));

                descriptionFunction.currentNode = activeNode;
                Profiler.BeginSample("GenerateNodeCode");
                bodyNode.GenerateNodeCode(descriptionFunction, mode);
                Profiler.EndSample();
                descriptionFunction.ReplaceInCurrentMapping(PrecisionUtil.Token, activeNode.concretePrecision.ToShaderString());

                if (keywordPermutations != null)
                    descriptionFunction.AppendLine("#endif");
            }

            activeNode.CollectShaderProperties(shaderProperties, mode);

            if (activeNode is SubGraphNode subGraphNode)
            {
                subGraphNode.CollectShaderKeywords(shaderKeywords, mode);
            }
        }

        static void GenerateSurfaceDescriptionRemap(
            GraphData graph,
            AbstractMaterialNode rootNode,
            IEnumerable<MaterialSlot> slots,
            ShaderStringBuilder surfaceDescriptionFunction,
            GenerationMode mode)
        {
            if (rootNode == null)
            {
                foreach (var input in slots)
                {
                    if (input != null)
                    {
                        var node = input.owner;
                        var foundEdges = graph.GetEdges(input.slotReference).ToArray();
                        var hlslName = NodeUtils.GetHLSLSafeName(input.shaderOutputName);
                        if (foundEdges.Any())
                            surfaceDescriptionFunction.AppendLine($"surface.{hlslName} = {node.GetSlotValue(input.id, mode, node.concretePrecision)};");
                        else
                            surfaceDescriptionFunction.AppendLine($"surface.{hlslName} = {input.GetDefaultValue(mode, node.concretePrecision)};");
                    }
                }
            }
            else if (rootNode is SubGraphOutputNode)
            {
                var slot = slots.FirstOrDefault();
                if (slot != null)
                {
                    var foundEdges = graph.GetEdges(slot.slotReference).ToArray();
                    var hlslName = $"{NodeUtils.GetHLSLSafeName(slot.shaderOutputName)}_{slot.id}";
                    if (foundEdges.Any())
                        surfaceDescriptionFunction.AppendLine($"surface.{hlslName} = {rootNode.GetSlotValue(slot.id, mode, rootNode.concretePrecision)};");
                    else
                        surfaceDescriptionFunction.AppendLine($"surface.{hlslName} = {slot.GetDefaultValue(mode, rootNode.concretePrecision)};");
                    surfaceDescriptionFunction.AppendLine($"surface.Out = all(isfinite(surface.{hlslName})) ? {GenerationUtils.AdaptNodeOutputForPreview(rootNode, slot.id, "surface." + hlslName)} : float4(1.0f, 0.0f, 1.0f, 1.0f);");
                }
            }
            else
            {
                var slot = rootNode.GetOutputSlots<MaterialSlot>().FirstOrDefault();
                if (slot != null)
                {
                    string slotValue;
                    string previewOutput;
                    if (rootNode.isActive)
                    {
                        slotValue = rootNode.GetSlotValue(slot.id, mode, rootNode.concretePrecision);
                        previewOutput = GenerationUtils.AdaptNodeOutputForPreview(rootNode, slot.id);
                    }
                    else
                    {
                        slotValue = rootNode.GetSlotValue(slot.id, mode, rootNode.concretePrecision);
                        previewOutput = "float4(0.0f, 0.0f, 0.0f, 0.0f)";
                    }
                    surfaceDescriptionFunction.AppendLine($"surface.Out = all(isfinite({slotValue})) ? {previewOutput} : float4(1.0f, 0.0f, 1.0f, 1.0f);");
                }
            }
        }

        const string k_VertexDescriptionStructName = "VertexDescription";
        internal static void GenerateVertexDescriptionStruct(ShaderStringBuilder builder, List<MaterialSlot> slots, string structName = k_VertexDescriptionStructName, IActiveFieldsSet activeFields = null)
        {
            builder.AppendLine("struct {0}", structName);
            using (builder.BlockSemicolonScope())
            {
                foreach (var slot in slots)
                {
                    string hlslName = NodeUtils.ConvertToValidHLSLIdentifier(slot.shaderOutputName);
                    builder.AppendLine("{0} {1};", slot.concreteValueType.ToShaderString(slot.owner.concretePrecision), hlslName);

                    if (activeFields != null)
                    {
                        var structField = new FieldDescriptor(structName, hlslName, "");
                        activeFields.AddAll(structField);
                    }
                }
            }
        }

        internal static void GenerateVertexDescriptionFunction(
            GraphData graph,
            ShaderStringBuilder builder,
            FunctionRegistry functionRegistry,
            PropertyCollector shaderProperties,
            KeywordCollector shaderKeywords,
            GenerationMode mode,
            AbstractMaterialNode rootNode,
            List<AbstractMaterialNode> nodes,
            List<int>[] keywordPermutationsPerNode,
            List<MaterialSlot> slots,
            string graphInputStructName = "VertexDescriptionInputs",
            string functionName = "PopulateVertexData",
            string graphOutputStructName = k_VertexDescriptionStructName)
        {
            if (graph == null)
                return;

            graph.CollectShaderProperties(shaderProperties, mode);

            if (mode == GenerationMode.VFX)
            {
                const string k_GraphProperties = "GraphProperties";
                builder.AppendLine("{0} {1}({2} IN, {3} PROP)", graphOutputStructName, functionName, graphInputStructName, k_GraphProperties);
            }
            else
                builder.AppendLine("{0} {1}({2} IN)", graphOutputStructName, functionName, graphInputStructName);

            using (builder.BlockScope())
            {
                builder.AppendLine("{0} description = ({0})0;", graphOutputStructName);
                Profiler.BeginSample("GenerateNodeDescriptions");
                for (int i = 0; i < nodes.Count; i++)
                {
                    GenerateDescriptionForNode(nodes[i], keywordPermutationsPerNode[i], functionRegistry, builder,
                        shaderProperties, shaderKeywords,
                        graph, mode);
                }
                Profiler.EndSample();

                functionRegistry.builder.currentNode = null;
                builder.currentNode = null;

                if (slots.Count != 0)
                {
                    foreach (var slot in slots)
                    {
                        var isSlotConnected = graph.GetEdges(slot.slotReference).Any();
                        var slotName = NodeUtils.ConvertToValidHLSLIdentifier(slot.shaderOutputName);
                        var slotValue = isSlotConnected ?
                            ((AbstractMaterialNode)slot.owner).GetSlotValue(slot.id, mode, slot.owner.concretePrecision) : slot.GetDefaultValue(mode, slot.owner.concretePrecision);
                        builder.AppendLine("description.{0} = {1};", slotName, slotValue);
                    }
                }

                builder.AppendLine("return description;");
            }
        }

        internal static string GetSpliceCommand(string command, string token)
        {
            return !string.IsNullOrEmpty(command) ? command : $"// {token}: <None>";
        }

        internal static string GetDefaultTemplatePath(string templateName)
        {
            var basePath = "Packages/com.unity.shadergraph/Editor/Generation/Templates/";
            string templatePath = Path.Combine(basePath, templateName);

            if (File.Exists(templatePath))
                return templatePath;

            throw new FileNotFoundException(string.Format(@"Cannot find a template with name ""{0}"".", templateName));
        }

        internal static string[] defaultDefaultSharedTemplateDirectories = new string[]
        {
            "Packages/com.unity.shadergraph/Editor/Generation/Templates"
        };

        internal static string[] GetDefaultSharedTemplateDirectories()
        {
            return defaultDefaultSharedTemplateDirectories;
        }

        // Returns null if no 'CustomEditor "___"' line should be added, otherwise the name of the ShaderGUI class.
        // Note that it's okay to add an "invalid" ShaderGUI (no class found) as Unity will simply take no action if that's the case, unless if its BaseShaderGUI.
        public static string FinalCustomEditorString(ICanChangeShaderGUI canChangeShaderGUI)
        {
            string finalOverrideName = canChangeShaderGUI.ShaderGUIOverride;
            if (string.IsNullOrEmpty(finalOverrideName))
                return null;

            // Do not add to the final shader if the base ShaderGUI is wanted, as errors will occur.
            if (finalOverrideName.Equals("BaseShaderGUI") || finalOverrideName.Equals("UnityEditor.BaseShaderGUI"))
                return null;

            return finalOverrideName;
        }
    }
}
