using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using Data.Util;

namespace UnityEditor.ShaderGraph
{
    internal static class GenerationUtils
    {
        const string kErrorString = @"ERROR!";

        internal static List<FieldDescriptor> GetActiveFieldsFromConditionals(ConditionalField[] conditionalFields)
        {
            var fields = new List<FieldDescriptor>();
            foreach(ConditionalField conditionalField in conditionalFields)
            {
                if(conditionalField.condition == true)
                {
                    fields.Add(conditionalField.field);
                }
            }

            return fields;
        }

        internal static void GenerateSubShaderTags(IMasterNode masterNode, SubShaderDescriptor descriptor, ShaderStringBuilder builder)
        {
            builder.AppendLine("Tags");
            using (builder.BlockScope())
            {
                // Pipeline tag
                if(!string.IsNullOrEmpty(descriptor.pipelineTag))
                    builder.AppendLine($"\"RenderPipeline\"=\"{descriptor.pipelineTag}\"");
                else
                    builder.AppendLine("// RenderPipeline: <None>");

                // Render Type
                string renderType = !string.IsNullOrEmpty(descriptor.renderTypeOverride) ?
                    descriptor.renderTypeOverride : masterNode?.renderTypeTag;
                if(!string.IsNullOrEmpty(renderType))
                    builder.AppendLine($"\"RenderType\"=\"{renderType}\"");
                else
                    builder.AppendLine("// RenderType: <None>");

                // Render Queue
                string renderQueue = !string.IsNullOrEmpty(descriptor.renderQueueOverride) ?
                    descriptor.renderQueueOverride : masterNode?.renderQueueTag;
                if(!string.IsNullOrEmpty(renderQueue))
                    builder.AppendLine($"\"Queue\"=\"{renderQueue}\"");
                else
                    builder.AppendLine("// Queue: <None>");
            }
        }

        static bool IsFieldActive(FieldDescriptor field, IActiveFields activeFields, bool isOptional)
        {
            bool fieldActive = true;
            if (!activeFields.Contains(field) && isOptional)
                fieldActive = false; //if the field is optional and not inside of active fields
            return fieldActive;
        }

        internal static void GenerateShaderStruct(StructDescriptor shaderStruct, ActiveFields activeFields, out ShaderStringBuilder structBuilder)
        {
            structBuilder = new ShaderStringBuilder();
            structBuilder.AppendLine($"struct {shaderStruct.name}");
            using(structBuilder.BlockSemicolonScope())
            {
                foreach(FieldDescriptor subscript in shaderStruct.fields)
                {
                    bool fieldIsActive;
                    var keywordIfDefs = string.Empty;

                    if (activeFields.permutationCount > 0)
                    {
                        //find all active fields per permutation
                        var instances = activeFields.allPermutations.instances
                            .Where(i => IsFieldActive(subscript, i, subscript.subscriptOptions.HasFlag(StructFieldOptions.Optional))).ToList();
                        fieldIsActive = instances.Count > 0;
                        if (fieldIsActive)
                            keywordIfDefs = KeywordUtil.GetKeywordPermutationSetConditional(instances.Select(i => i.permutationIndex).ToList());
                    }
                    else
                        fieldIsActive = IsFieldActive(subscript, activeFields.baseInstance, subscript.subscriptOptions.HasFlag(StructFieldOptions.Optional));
                        //else just find active fields

                    if (fieldIsActive)
                    {
                        //if field is active:
                        if(subscript.HasPreprocessor())
                            structBuilder.AppendLine($"#if {subscript.preprocessor}");

                        //if in permutation, add permutation ifdef
                        if(!string.IsNullOrEmpty(keywordIfDefs))
                            structBuilder.AppendLine(keywordIfDefs);

                        //check for a semantic, build string if valid
                        string semantic = subscript.HasSemantic() ? $" : {subscript.semantic}" : string.Empty;
                        structBuilder.AppendLine($"{subscript.type} {subscript.name}{semantic};");

                        //if in permutation, add permutation endif
                        if (!string.IsNullOrEmpty(keywordIfDefs))
                            structBuilder.AppendLine("#endif"); //TODO: add debug collector

                        if(subscript.HasPreprocessor())
                            structBuilder.AppendLine("#endif");
                    }
                }
            }
        }

        internal static void GeneratePackedStruct(StructDescriptor shaderStruct, ActiveFields activeFields, out StructDescriptor packStruct)
        {
            packStruct = new StructDescriptor() { name = "Packed" + shaderStruct.name, packFields = true,
                fields = new FieldDescriptor[]{} };
            List<FieldDescriptor> packedSubscripts = new List<FieldDescriptor>();
            List<int> packedCounts = new List<int>();

            foreach(FieldDescriptor subscript in shaderStruct.fields)
            {
                var fieldIsActive = false;
                var keywordIfDefs = string.Empty;

                if (activeFields.permutationCount > 0)
                {
                    //find all active fields per permutation
                    var instances = activeFields.allPermutations.instances
                        .Where(i => IsFieldActive(subscript, i, subscript.subscriptOptions.HasFlag(StructFieldOptions.Optional))).ToList();
                    fieldIsActive = instances.Count > 0;
                    if (fieldIsActive)
                        keywordIfDefs = KeywordUtil.GetKeywordPermutationSetConditional(instances.Select(i => i.permutationIndex).ToList());
                }
                else
                    fieldIsActive = IsFieldActive(subscript, activeFields.baseInstance, subscript.subscriptOptions.HasFlag(StructFieldOptions.Optional));
                    //else just find active fields

                if (fieldIsActive)
                {
                    //if field is active:
                    if(subscript.HasSemantic() || subscript.vectorCount == 0)
                        packedSubscripts.Add(subscript);
                    else
                    {
                        // pack float field
                        int vectorCount = subscript.vectorCount;
                        // super simple packing: use the first interpolator that has room for the whole value
                        int interpIndex = packedCounts.FindIndex(x => (x + vectorCount <= 4));
                        int firstChannel;
                        if (interpIndex < 0)
                        {
                            // allocate a new interpolator
                            interpIndex = packedCounts.Count;
                            firstChannel = 0;
                            packedCounts.Add(vectorCount);
                        }
                        else
                        {
                            // pack into existing interpolator
                            firstChannel = packedCounts[interpIndex];
                            packedCounts[interpIndex] += vectorCount;
                        }
                        var packedSubscript = new FieldDescriptor(packStruct.name, "interp" + interpIndex, "", subscript.type,
                            "TEXCOORD" + interpIndex, subscript.preprocessor, StructFieldOptions.Static);
                        packedSubscripts.Add(packedSubscript);
                    }
                }
            }
            packStruct.fields = packedSubscripts.ToArray();
        }

        internal static void GenerateInterpolatorFunctions(StructDescriptor shaderStruct, IActiveFields activeFields, out ShaderStringBuilder interpolatorBuilder)
        {
            //set up function string builders and struct builder
            List<int> packedCounts = new List<int>();
            var packBuilder = new ShaderStringBuilder();
            var unpackBuilder = new ShaderStringBuilder();
            interpolatorBuilder = new ShaderStringBuilder();
            string packedStruct = "Packed" + shaderStruct.name;

            //declare function headers
            packBuilder.AppendLine($"{packedStruct} Pack{shaderStruct.name} ({shaderStruct.name} input)");
            packBuilder.AppendLine("{");
            packBuilder.IncreaseIndent();
            packBuilder.AppendLine($"{packedStruct} output;");

            unpackBuilder.AppendLine($"{shaderStruct.name} Unpack{shaderStruct.name} ({packedStruct} input)");
            unpackBuilder.AppendLine("{");
            unpackBuilder.IncreaseIndent();
            unpackBuilder.AppendLine($"{shaderStruct.name} output;");

            foreach(FieldDescriptor subscript in shaderStruct.fields)
            {
                if(IsFieldActive(subscript, activeFields, subscript.subscriptOptions.HasFlag(StructFieldOptions.Optional)))
                {
                    int vectorCount = subscript.vectorCount;
                    if(subscript.HasPreprocessor())
                    {
                        packBuilder.AppendLine($"#if {subscript.preprocessor}");
                        unpackBuilder.AppendLine($"#if {subscript.preprocessor}");
                    }
                    if(subscript.HasSemantic() || vectorCount == 0)
                    {
                        packBuilder.AppendLine($"output.{subscript.name} = input.{subscript.name};");
                        unpackBuilder.AppendLine($"output.{subscript.name} = input.{subscript.name};");
                    }
                    else
                    {
                        // pack float field
                        // super simple packing: use the first interpolator that has room for the whole value
                        int interpIndex = packedCounts.FindIndex(x => (x + vectorCount <= 4));
                        int firstChannel;
                        if (interpIndex < 0)
                        {
                            // allocate a new interpolator
                            interpIndex = packedCounts.Count;
                            firstChannel = 0;
                            packedCounts.Add(vectorCount);
                        }
                        else
                        {
                            // pack into existing interpolator
                            firstChannel = packedCounts[interpIndex];
                            packedCounts[interpIndex] += vectorCount;
                        }
                        // add code to packer and unpacker -- add subscript to packedstruct
                        string packedChannels = ShaderSpliceUtil.GetChannelSwizzle(firstChannel, vectorCount);
                        packBuilder.AppendLine($"output.interp{interpIndex}.{packedChannels} =  input.{subscript.name};");
                        unpackBuilder.AppendLine($"output.{subscript.name} = input.interp{interpIndex}.{packedChannels};");
                    }

                    if(subscript.HasPreprocessor())
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

            unpackBuilder.AppendLine("return output;");
            unpackBuilder.DecreaseIndent();
            unpackBuilder.AppendLine("}");

            interpolatorBuilder.Concat(packBuilder);
            interpolatorBuilder.Concat(unpackBuilder);
        }

        internal static void GetUpstreamNodesForShaderPass(AbstractMaterialNode outputNode, PassDescriptor pass, out List<AbstractMaterialNode> vertexNodes, out List<AbstractMaterialNode> pixelNodes)
        {
            // Traverse Graph Data
            vertexNodes = Graphing.ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(vertexNodes, outputNode, NodeUtils.IncludeSelf.Include, pass.vertexPorts);

            pixelNodes = Graphing.ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(pixelNodes, outputNode, NodeUtils.IncludeSelf.Include, pass.pixelPorts);
        }

        internal static void GetActiveFieldsAndPermutationsForNodes(AbstractMaterialNode outputNode, PassDescriptor pass,
            KeywordCollector keywordCollector,  List<AbstractMaterialNode> vertexNodes, List<AbstractMaterialNode> pixelNodes,
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
                for(int i = 0; i < keywordCollector.permutations.Count; i++)
                {
                    // Get active nodes for this permutation
                    var localVertexNodes = Graphing.ListPool<AbstractMaterialNode>.Get();
                    var localPixelNodes = Graphing.ListPool<AbstractMaterialNode>.Get();
                    NodeUtils.DepthFirstCollectNodesFromNode(localVertexNodes, outputNode, NodeUtils.IncludeSelf.Include, pass.vertexPorts, keywordCollector.permutations[i]);
                    NodeUtils.DepthFirstCollectNodesFromNode(localPixelNodes, outputNode, NodeUtils.IncludeSelf.Include, pass.pixelPorts, keywordCollector.permutations[i]);

                    // Track each vertex node in this permutation
                    foreach(AbstractMaterialNode vertexNode in localVertexNodes)
                    {
                        int nodeIndex = vertexNodes.IndexOf(vertexNode);

                        if(vertexNodePermutations[nodeIndex] == null)
                            vertexNodePermutations[nodeIndex] = new List<int>();
                        vertexNodePermutations[nodeIndex].Add(i);
                    }

                    // Track each pixel node in this permutation
                    foreach(AbstractMaterialNode pixelNode in localPixelNodes)
                    {
                        int nodeIndex = pixelNodes.IndexOf(pixelNode);

                        if(pixelNodePermutations[nodeIndex] == null)
                            pixelNodePermutations[nodeIndex] = new List<int>();
                        pixelNodePermutations[nodeIndex].Add(i);
                    }

                    // Get requirements for this permutation
                    vertexRequirements[i].SetRequirements(ShaderGraphRequirements.FromNodes(localVertexNodes, ShaderStageCapability.Vertex, false));
                    pixelRequirements[i].SetRequirements(ShaderGraphRequirements.FromNodes(localPixelNodes, ShaderStageCapability.Fragment, false));

                    // Add active fields
                    var conditionalFields = GetActiveFieldsFromConditionals(GetConditionalFieldsFromGraphRequirements(vertexRequirements[i].requirements, activeFields[i]));
                    conditionalFields.AddRange(GetActiveFieldsFromConditionals(GetConditionalFieldsFromGraphRequirements(pixelRequirements[i].requirements, activeFields[i])));
                    foreach(var field in conditionalFields)
                    {
                        activeFields[i].Add(field);
                    }
                }
            }
            // No Keywords
            else
            {
                // Get requirements
                vertexRequirements.baseInstance.SetRequirements(ShaderGraphRequirements.FromNodes(vertexNodes, ShaderStageCapability.Vertex, false));
                pixelRequirements.baseInstance.SetRequirements(ShaderGraphRequirements.FromNodes(pixelNodes, ShaderStageCapability.Fragment, false));

                // Add active fields
                var conditionalFields = GetActiveFieldsFromConditionals(GetConditionalFieldsFromGraphRequirements(vertexRequirements.baseInstance.requirements, activeFields.baseInstance));
                conditionalFields.AddRange(GetActiveFieldsFromConditionals(GetConditionalFieldsFromGraphRequirements(pixelRequirements.baseInstance.requirements, activeFields.baseInstance)));
                foreach(var field in conditionalFields)
                {
                    activeFields.baseInstance.Add(field);
                }
            }

            // Build graph requirements
            graphRequirements.UnionWith(pixelRequirements);
            graphRequirements.UnionWith(vertexRequirements);
        }

        static ConditionalField[] GetConditionalFieldsFromGraphRequirements(ShaderGraphRequirements requirements, IActiveFields activeFields)
        {
            return new ConditionalField[]
            {
                new ConditionalField(StructFields.SurfaceDescriptionInputs.ScreenPosition,          requirements.requiresScreenPosition),
                new ConditionalField(StructFields.VertexDescriptionInputs.ScreenPosition,           requirements.requiresScreenPosition &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.VertexColor,             requirements.requiresVertexColor),
                new ConditionalField(StructFields.VertexDescriptionInputs.VertexColor,              requirements.requiresVertexColor &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.FaceSign,                requirements.requiresFaceSign),

                new ConditionalField(StructFields.SurfaceDescriptionInputs.ObjectSpaceNormal,       (requirements.requiresNormal & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.ViewSpaceNormal,         (requirements.requiresNormal & NeededCoordinateSpace.View) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.WorldSpaceNormal,        (requirements.requiresNormal & NeededCoordinateSpace.World) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.TangentSpaceNormal,      (requirements.requiresNormal & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.ObjectSpaceNormal,        (requirements.requiresNormal & NeededCoordinateSpace.Object) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.ViewSpaceNormal,          (requirements.requiresNormal & NeededCoordinateSpace.View) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.WorldSpaceNormal,         (requirements.requiresNormal & NeededCoordinateSpace.World) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.TangentSpaceNormal,       (requirements.requiresNormal & NeededCoordinateSpace.Tangent) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),

                new ConditionalField(StructFields.SurfaceDescriptionInputs.ObjectSpaceViewDirection,(requirements.requiresViewDir & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.ViewSpaceViewDirection,  (requirements.requiresViewDir & NeededCoordinateSpace.View) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.WorldSpaceViewDirection, (requirements.requiresViewDir & NeededCoordinateSpace.World) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.TangentSpaceViewDirection,(requirements.requiresViewDir & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.ObjectSpaceViewDirection, (requirements.requiresViewDir & NeededCoordinateSpace.Object) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.ViewSpaceViewDirection,   (requirements.requiresViewDir & NeededCoordinateSpace.View) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.WorldSpaceViewDirection,  (requirements.requiresViewDir & NeededCoordinateSpace.World) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.TangentSpaceViewDirection,(requirements.requiresViewDir & NeededCoordinateSpace.Tangent) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),

                new ConditionalField(StructFields.SurfaceDescriptionInputs.ObjectSpaceTangent,      (requirements.requiresTangent & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.ViewSpaceTangent,        (requirements.requiresTangent & NeededCoordinateSpace.View) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.WorldSpaceTangent,       (requirements.requiresTangent & NeededCoordinateSpace.World) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.TangentSpaceTangent,     (requirements.requiresTangent & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.ObjectSpaceTangent,       (requirements.requiresTangent & NeededCoordinateSpace.Object) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.ViewSpaceTangent,         (requirements.requiresTangent & NeededCoordinateSpace.View) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.WorldSpaceTangent,        (requirements.requiresTangent & NeededCoordinateSpace.World) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.TangentSpaceTangent,      (requirements.requiresTangent & NeededCoordinateSpace.Tangent) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),

                new ConditionalField(StructFields.SurfaceDescriptionInputs.ObjectSpaceBiTangent,    (requirements.requiresBitangent & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.ViewSpaceBiTangent,      (requirements.requiresBitangent & NeededCoordinateSpace.View) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.WorldSpaceBiTangent,     (requirements.requiresBitangent & NeededCoordinateSpace.World) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.TangentSpaceBiTangent,   (requirements.requiresBitangent & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.ObjectSpaceBiTangent,     (requirements.requiresBitangent & NeededCoordinateSpace.Object) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.ViewSpaceBiTangent,       (requirements.requiresBitangent & NeededCoordinateSpace.View) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.WorldSpaceBiTangent,      (requirements.requiresBitangent & NeededCoordinateSpace.World) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.TangentSpaceBiTangent,    (requirements.requiresBitangent & NeededCoordinateSpace.Tangent) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),

                new ConditionalField(StructFields.SurfaceDescriptionInputs.ObjectSpacePosition,     (requirements.requiresPosition & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.ViewSpacePosition,       (requirements.requiresPosition & NeededCoordinateSpace.View) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.WorldSpacePosition,      (requirements.requiresPosition & NeededCoordinateSpace.World) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.TangentSpacePosition,    (requirements.requiresPosition & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.AbsoluteWorldSpacePosition,(requirements.requiresPosition & NeededCoordinateSpace.AbsoluteWorld) > 0),
                new ConditionalField(StructFields.VertexDescriptionInputs.ObjectSpacePosition,     (requirements.requiresPosition & NeededCoordinateSpace.Object) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.ViewSpacePosition,       (requirements.requiresPosition & NeededCoordinateSpace.View) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.WorldSpacePosition,      (requirements.requiresPosition & NeededCoordinateSpace.World) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.TangentSpacePosition,    (requirements.requiresPosition & NeededCoordinateSpace.Tangent) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.AbsoluteWorldSpacePosition,(requirements.requiresPosition & NeededCoordinateSpace.AbsoluteWorld) > 0 &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),

                new ConditionalField(StructFields.SurfaceDescriptionInputs.uv0,                     requirements.requiresMeshUVs.Contains(UVChannel.UV0)),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.uv1,                     requirements.requiresMeshUVs.Contains(UVChannel.UV1)),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.uv2,                     requirements.requiresMeshUVs.Contains(UVChannel.UV2)),
                new ConditionalField(StructFields.SurfaceDescriptionInputs.uv3,                     requirements.requiresMeshUVs.Contains(UVChannel.UV3)),
                new ConditionalField(StructFields.VertexDescriptionInputs.uv0,                      requirements.requiresMeshUVs.Contains(UVChannel.UV0) &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.uv1,                      requirements.requiresMeshUVs.Contains(UVChannel.UV1) &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.uv2,                      requirements.requiresMeshUVs.Contains(UVChannel.UV2) &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),
                new ConditionalField(StructFields.VertexDescriptionInputs.uv3,                      requirements.requiresMeshUVs.Contains(UVChannel.UV3) &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),

                new ConditionalField(StructFields.SurfaceDescriptionInputs.TimeParameters,          requirements.requiresTime),
                new ConditionalField(StructFields.VertexDescriptionInputs.TimeParameters,           requirements.requiresTime &&
                                                                                                                activeFields.Contains(Fields.GraphVertex)),

                new ConditionalField(StructFields.VertexDescriptionInputs.BoneWeights,              requirements.requiresVertexSkinning),
                new ConditionalField(StructFields.VertexDescriptionInputs.BoneIndices,              requirements.requiresVertexSkinning),
            };
        }

        internal static void AddRequiredFields(FieldCollection passRequiredFields,IActiveFieldsSet activeFields)
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
                    if(dependencies == null)
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
                case ConcreteSlotValueType.Vector1:
                    return string.Format("({0}).x", rawOutput);
                case ConcreteSlotValueType.Vector2:
                    switch (convertFromType)
                    {
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
                default:
                    return kErrorString;
            }
        }

        internal static string AdaptNodeOutputForPreview(AbstractMaterialNode node, int outputSlotId)
        {
            var rawOutput = node.GetVariableNameForSlot(outputSlotId);
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

        internal static void GeneratePropertiesBlock(ShaderStringBuilder sb, PropertyCollector propertyCollector, KeywordCollector keywordCollector, GenerationMode mode)
        {
            sb.AppendLine("Properties");
            using (sb.BlockScope())
            {
                foreach (var prop in propertyCollector.properties.Where(x => x.generatePropertyBlock))
                {
                    sb.AppendLine(prop.GetPropertyBlockString());
                }

                // Keywords use hardcoded state in preview
                // Do not add them to the Property Block
                if(mode == GenerationMode.Preview)
                    return;

                foreach (var key in keywordCollector.keywords.Where(x => x.generatePropertyBlock))
                {
                    sb.AppendLine(key.GetPropertyBlockString());
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

                if (requirements.requiresVertexColor)
                    sb.AppendLine("float4 {0};", ShaderGeneratorNames.VertexColor);

                if (requirements.requiresScreenPosition)
                    sb.AppendLine("float4 {0};", ShaderGeneratorNames.ScreenPosition);

                if (requirements.requiresFaceSign)
                    sb.AppendLine("float {0};", ShaderGeneratorNames.FaceSign);

                foreach (var channel in requirements.requiresMeshUVs.Distinct())
                    sb.AppendLine("half4 {0};", channel.GetUVName());

                if (requirements.requiresTime)
                {
                    sb.AppendLine("float3 {0};", ShaderGeneratorNames.TimeParameters);
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

            if (requirements.requiresVertexColor)
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.VertexColor} = IN.{ShaderGeneratorNames.VertexColor};");

            if (requirements.requiresScreenPosition)
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.ScreenPosition} = IN.{ShaderGeneratorNames.ScreenPosition};");

            if (requirements.requiresFaceSign)
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.FaceSign} = IN.{ShaderGeneratorNames.FaceSign};");

            foreach (var channel in requirements.requiresMeshUVs.Distinct())
                sb.AppendLine($"{variableName}.{channel.GetUVName()} = IN.{channel.GetUVName()};");

            if (requirements.requiresTime)
            {
                sb.AppendLine($"{variableName}.{ShaderGeneratorNames.TimeParameters} = IN.{ShaderGeneratorNames.TimeParameters};");
            }
        }

        internal static void GenerateSurfaceDescriptionStruct(ShaderStringBuilder surfaceDescriptionStruct, List<MaterialSlot> slots, string structName = "SurfaceDescription", IActiveFieldsSet activeFields = null, bool isSubgraphOutput = false)
        {
            surfaceDescriptionStruct.AppendLine("struct {0}", structName);
            using (surfaceDescriptionStruct.BlockSemicolonScope())
            {
                if(slots != null)
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
                            surfaceDescriptionStruct.AppendLine("{0} {1};", ConcreteSlotValueType.Vector4.ToShaderString(ConcretePrecision.Float), "Out");
                        
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
            string graphInputStructName = "SurfaceDescriptionInputs")
        {
            if (graph == null)
                return;

            graph.CollectShaderProperties(shaderProperties, mode);

            surfaceDescriptionFunction.AppendLine(String.Format("{0} {1}(SurfaceDescriptionInputs IN)", surfaceDescriptionName, functionName), false);
            using (surfaceDescriptionFunction.BlockScope())
            {
                surfaceDescriptionFunction.AppendLine("{0} surface = ({0})0;", surfaceDescriptionName);
                for(int i = 0; i < nodes.Count; i++)
                {
                    GenerateDescriptionForNode(nodes[i], keywordPermutationsPerNode[i], functionRegistry, surfaceDescriptionFunction,
                        shaderProperties, shaderKeywords,
                        graph, mode);
                }

                functionRegistry.builder.currentNode = null;
                surfaceDescriptionFunction.currentNode = null;

                GenerateSurfaceDescriptionRemap(graph, rootNode, slots,
                    surfaceDescriptionFunction, mode);

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
                functionNode.GenerateNodeFunction(functionRegistry, mode);
                functionRegistry.builder.ReplaceInCurrentMapping(PrecisionUtil.Token, activeNode.concretePrecision.ToShaderString());
            }

            if (activeNode is IGeneratesBodyCode bodyNode)
            {
                if(keywordPermutations != null)
                    descriptionFunction.AppendLine(KeywordUtil.GetKeywordPermutationSetConditional(keywordPermutations));

                descriptionFunction.currentNode = activeNode;
                bodyNode.GenerateNodeCode(descriptionFunction, mode);
                descriptionFunction.ReplaceInCurrentMapping(PrecisionUtil.Token, activeNode.concretePrecision.ToShaderString());

                if(keywordPermutations != null)
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
            if (rootNode is IMasterNode)
            {
                var usedSlots = slots ?? rootNode.GetInputSlots<MaterialSlot>();
                foreach (var input in usedSlots)
                {
                    if (input != null)
                    {
                        var foundEdges = graph.GetEdges(input.slotReference).ToArray();
                        var hlslName = NodeUtils.GetHLSLSafeName(input.shaderOutputName);
                        if (foundEdges.Any())
                            surfaceDescriptionFunction.AppendLine($"surface.{hlslName} = {rootNode.GetSlotValue(input.id, mode, rootNode.concretePrecision)};");
                        else
                            surfaceDescriptionFunction.AppendLine($"surface.{hlslName} = {input.GetDefaultValue(mode, rootNode.concretePrecision)};");
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
            else if (rootNode.hasPreview)
            {
                var slot = rootNode.GetOutputSlots<MaterialSlot>().FirstOrDefault();
                if (slot != null)
                {
                    var slotValue = rootNode.GetSlotValue(slot.id, mode, rootNode.concretePrecision);
                    surfaceDescriptionFunction.AppendLine($"surface.Out = all(isfinite({slotValue})) ? {GenerationUtils.AdaptNodeOutputForPreview(rootNode, slot.id)} : float4(1.0f, 0.0f, 1.0f, 1.0f);");
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
                    string hlslName = NodeUtils.GetHLSLSafeName(slot.shaderOutputName);
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

            builder.AppendLine("{0} {1}({2} IN)", graphOutputStructName, functionName, graphInputStructName);
            using (builder.BlockScope())
            {
                builder.AppendLine("{0} description = ({0})0;", graphOutputStructName);
                for(int i = 0; i < nodes.Count; i++)
                {
                    GenerateDescriptionForNode(nodes[i], keywordPermutationsPerNode[i], functionRegistry, builder,
                        shaderProperties, shaderKeywords,
                        graph, mode);
                }

                functionRegistry.builder.currentNode = null;
                builder.currentNode = null;

                if(slots.Count != 0)
                {
                    foreach (var slot in slots)
                    {
                        var isSlotConnected = slot.owner.owner.GetEdges(slot.slotReference).Any();
                        var slotName = NodeUtils.GetHLSLSafeName(slot.shaderOutputName);
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

        internal static string GetDefaultSharedTemplateDirectory()
        {
            return "Packages/com.unity.shadergraph/Editor/Generation/Templates";
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
