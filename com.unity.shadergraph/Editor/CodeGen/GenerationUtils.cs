using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using Data.Util;

namespace UnityEditor.ShaderGraph
{
    static class GenerationUtils
    {
        

        internal static List<IField> GetActiveFieldsFromConditionals(ConditionalField[] conditionalFields)
        {
            var fields = new List<IField>();
            foreach(ConditionalField conditionalField in conditionalFields)
            {
                if(conditionalField.condition == true)
                {
                    fields.Add(conditionalField.field);
                }
            }

            return fields;
        }

        internal static ActiveFields ToActiveFields(this List<IField> fields)
        {
            var activeFields = new ActiveFields();
            var baseFields = activeFields.baseInstance;

            foreach(IField field in fields)
                baseFields.Add(field);
            
            return activeFields;
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

        static bool IsFieldActive(IField field, IActiveFields activeFields, bool isOptional)
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
                foreach(SubscriptDescriptor subscript in shaderStruct.subscripts)
                {
                    bool fieldIsActive;
                    var keywordIfDefs = string.Empty;

                    if (activeFields.permutationCount > 0)
                    {
                        //find all active fields per permutation
                        var instances = activeFields.allPermutations.instances
                            .Where(i => IsFieldActive(subscript, i, subscript.subscriptOptions.HasFlag(SubscriptOptions.Optional))).ToList();
                        fieldIsActive = instances.Count > 0;
                        if (fieldIsActive)
                            keywordIfDefs = KeywordUtil.GetKeywordPermutationSetConditional(instances.Select(i => i.permutationIndex).ToList());
                    }
                    else
                        fieldIsActive = IsFieldActive(subscript, activeFields.baseInstance, subscript.subscriptOptions.HasFlag(SubscriptOptions.Optional));
                        //else just find active fields

                    if (fieldIsActive)
                    {
                        //if field is active:
                        if(subscript.hasPreprocessor())
                            structBuilder.AppendLine($"#if {subscript.preprocessor}");

                        //if in permutation, add permutation ifdef
                        if(!string.IsNullOrEmpty(keywordIfDefs))
                            structBuilder.AppendLine(keywordIfDefs);
                        
                        //check for a semantic, build string if valid
                        string semantic = subscript.hasSemantic() ? $" : {subscript.semantic}" : string.Empty;
                        structBuilder.AppendLine($"{subscript.type} {subscript.name}{semantic};");

                        //if in permutation, add permutation endif
                        if (!string.IsNullOrEmpty(keywordIfDefs))
                            structBuilder.AppendLine("#endif"); //TODO: add debug collector 

                        if(subscript.hasPreprocessor())
                            structBuilder.AppendLine("#endif");                        
                    }            
                }
            }
        }

        internal static void GeneratePackedStruct(StructDescriptor shaderStruct, ActiveFields activeFields, out StructDescriptor packStruct)
        {
            packStruct = new StructDescriptor() { name = "Packed" + shaderStruct.name, interpolatorPack = true,
                subscripts = new SubscriptDescriptor[]{} };
            List<SubscriptDescriptor> packedSubscripts = new List<SubscriptDescriptor>();
            List<int> packedCounts = new List<int>();

            foreach(SubscriptDescriptor subscript in shaderStruct.subscripts)
            {
                var fieldIsActive = false;
                var keywordIfDefs = string.Empty;

                if (activeFields.permutationCount > 0)
                {
                    //find all active fields per permutation
                    var instances = activeFields.allPermutations.instances
                        .Where(i => IsFieldActive(subscript, i, subscript.subscriptOptions.HasFlag(SubscriptOptions.Optional))).ToList();
                    fieldIsActive = instances.Count > 0;
                    if (fieldIsActive)
                        keywordIfDefs = KeywordUtil.GetKeywordPermutationSetConditional(instances.Select(i => i.permutationIndex).ToList());
                }
                else
                    fieldIsActive = IsFieldActive(subscript, activeFields.baseInstance, subscript.subscriptOptions.HasFlag(SubscriptOptions.Optional));
                    //else just find active fields

                if (fieldIsActive)
                {
                    //if field is active:
                    if(subscript.hasSemantic() || subscript.vectorCount == 0)  
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
                        var packedSubscript = new SubscriptDescriptor(packStruct.name, "interp" + interpIndex, "", subscript.type,
                            "TEXCOORD" + interpIndex, subscript.preprocessor, SubscriptOptions.Static);
                        packedSubscripts.Add(packedSubscript);                        
                    }
                }            
            }
            packStruct.subscripts = packedSubscripts.ToArray();
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

            foreach(SubscriptDescriptor subscript in shaderStruct.subscripts)
            {
                if(IsFieldActive(subscript, activeFields, subscript.subscriptOptions.HasFlag(SubscriptOptions.Optional)))
                {
                    int vectorCount = subscript.vectorCount;
                    if(subscript.hasPreprocessor())
                    {
                        packBuilder.AppendLine($"#if {subscript.preprocessor}");
                        unpackBuilder.AppendLine($"#if {subscript.preprocessor}");
                    }
                    if(subscript.hasSemantic() || vectorCount == 0)
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
                    
                    if(subscript.hasPreprocessor())
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

        internal static void GetUpstreamNodesForShaderPass(AbstractMaterialNode outputNode, ShaderPass pass, out List<AbstractMaterialNode> vertexNodes, out List<AbstractMaterialNode> pixelNodes)
        {
            // Traverse Graph Data
            vertexNodes = Graphing.ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(vertexNodes, outputNode, NodeUtils.IncludeSelf.Include, pass.vertexPorts);

            pixelNodes = Graphing.ListPool<AbstractMaterialNode>.Get();
            NodeUtils.DepthFirstCollectNodesFromNode(pixelNodes, outputNode, NodeUtils.IncludeSelf.Include, pass.pixelPorts);
        }

        internal static void GetActiveFieldsAndPermutationsForNodes(AbstractMaterialNode outputNode, ShaderPass pass, 
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
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ScreenPosition,          requirements.requiresScreenPosition),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ScreenPosition,           requirements.requiresScreenPosition &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.VertexColor,             requirements.requiresVertexColor),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.VertexColor,              requirements.requiresVertexColor &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.FaceSign,                requirements.requiresFaceSign),

                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceNormal,       (requirements.requiresNormal & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceNormal,         (requirements.requiresNormal & NeededCoordinateSpace.View) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceNormal,        (requirements.requiresNormal & NeededCoordinateSpace.World) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceNormal,      (requirements.requiresNormal & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceNormal,        (requirements.requiresNormal & NeededCoordinateSpace.Object) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceNormal,          (requirements.requiresNormal & NeededCoordinateSpace.View) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceNormal,         (requirements.requiresNormal & NeededCoordinateSpace.World) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceNormal,       (requirements.requiresNormal & NeededCoordinateSpace.Tangent) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceViewDirection,(requirements.requiresViewDir & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceViewDirection,  (requirements.requiresViewDir & NeededCoordinateSpace.View) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceViewDirection, (requirements.requiresViewDir & NeededCoordinateSpace.World) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceViewDirection,(requirements.requiresViewDir & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceViewDirection, (requirements.requiresViewDir & NeededCoordinateSpace.Object) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceViewDirection,   (requirements.requiresViewDir & NeededCoordinateSpace.View) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceViewDirection,  (requirements.requiresViewDir & NeededCoordinateSpace.World) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceViewDirection,(requirements.requiresViewDir & NeededCoordinateSpace.Tangent) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),

                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceTangent,      (requirements.requiresTangent & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceTangent,        (requirements.requiresTangent & NeededCoordinateSpace.View) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceTangent,       (requirements.requiresTangent & NeededCoordinateSpace.World) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceTangent,     (requirements.requiresTangent & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceTangent,       (requirements.requiresTangent & NeededCoordinateSpace.Object) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceTangent,         (requirements.requiresTangent & NeededCoordinateSpace.View) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceTangent,        (requirements.requiresTangent & NeededCoordinateSpace.World) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceTangent,      (requirements.requiresTangent & NeededCoordinateSpace.Tangent) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpaceBiTangent,    (requirements.requiresBitangent & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpaceBiTangent,      (requirements.requiresBitangent & NeededCoordinateSpace.View) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpaceBiTangent,     (requirements.requiresBitangent & NeededCoordinateSpace.World) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpaceBiTangent,   (requirements.requiresBitangent & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpaceBiTangent,     (requirements.requiresBitangent & NeededCoordinateSpace.Object) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpaceBiTangent,       (requirements.requiresBitangent & NeededCoordinateSpace.View) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpaceBiTangent,      (requirements.requiresBitangent & NeededCoordinateSpace.World) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpaceBiTangent,    (requirements.requiresBitangent & NeededCoordinateSpace.Tangent) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),

                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ObjectSpacePosition,     (requirements.requiresPosition & NeededCoordinateSpace.Object) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.ViewSpacePosition,       (requirements.requiresPosition & NeededCoordinateSpace.View) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.WorldSpacePosition,      (requirements.requiresPosition & NeededCoordinateSpace.World) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TangentSpacePosition,    (requirements.requiresPosition & NeededCoordinateSpace.Tangent) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.AbsoluteWorldSpacePosition,(requirements.requiresPosition & NeededCoordinateSpace.AbsoluteWorld) > 0),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ObjectSpacePosition,     (requirements.requiresPosition & NeededCoordinateSpace.Object) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.ViewSpacePosition,       (requirements.requiresPosition & NeededCoordinateSpace.View) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.WorldSpacePosition,      (requirements.requiresPosition & NeededCoordinateSpace.World) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.TangentSpacePosition,    (requirements.requiresPosition & NeededCoordinateSpace.Tangent) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.AbsoluteWorldSpacePosition,(requirements.requiresPosition & NeededCoordinateSpace.AbsoluteWorld) > 0 &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv0,                     requirements.requiresMeshUVs.Contains(UVChannel.UV0)),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv1,                     requirements.requiresMeshUVs.Contains(UVChannel.UV1)),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv2,                     requirements.requiresMeshUVs.Contains(UVChannel.UV2)),
                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.uv3,                     requirements.requiresMeshUVs.Contains(UVChannel.UV3)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv0,                      requirements.requiresMeshUVs.Contains(UVChannel.UV0) &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv1,                      requirements.requiresMeshUVs.Contains(UVChannel.UV1) &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv2,                      requirements.requiresMeshUVs.Contains(UVChannel.UV2) &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.uv3,                      requirements.requiresMeshUVs.Contains(UVChannel.UV3) &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),

                new ConditionalField(MeshTarget.ShaderStructs.SurfaceDescriptionInputs.TimeParameters,          requirements.requiresTime),
                new ConditionalField(MeshTarget.ShaderStructs.VertexDescriptionInputs.TimeParameters,           requirements.requiresTime &&
                                                                                                                activeFields.Contains(DefaultFields.GraphVertex)),
            };
        }

        internal static void AddRequiredFields(
            IField[] passRequiredFields,            // fields the pass requires
            IActiveFieldsSet activeFields)
        {
            if (passRequiredFields != null)
            {
                foreach (var requiredField in passRequiredFields)
                {
                    activeFields.AddAll(requiredField);
                }
            }
        }

        internal static void ApplyFieldDependencies(IActiveFields activeFields, FieldDependency[] dependencies)
        {
            // add active fields to queue
            Queue<IField> fieldsToPropagate = new Queue<IField>();
            foreach (var f in activeFields.fields)
            {
                fieldsToPropagate.Enqueue(f);
            }

            // foreach field in queue:
            while (fieldsToPropagate.Count > 0)
            {
                IField field = fieldsToPropagate.Dequeue();
                if (activeFields.Contains(field))           // this should always be true
                {
                    if(dependencies == null)
                        return;
                        
                    // find all dependencies of field that are not already active
                    foreach (FieldDependency d in dependencies.Where(d => (d.field == field) && !activeFields.Contains(d.dependsOn)))
                    {
                        // activate them and add them to the queue
                        activeFields.Add(d.dependsOn);
                        fieldsToPropagate.Enqueue(d.dependsOn);
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

        internal static string GetSpliceCommand(string command, string token)
        {
            return !string.IsNullOrEmpty(command) ? command : $"// {token}: <None>";
        }

        internal static string GetDefaultTemplatePath(string templateName)
        {
            var basePath = "Packages/com.unity.shadergraph/Editor/Templates/";
            string templatePath = Path.Combine(basePath, templateName);

            if (File.Exists(templatePath))
                return templatePath;

            throw new FileNotFoundException(string.Format(@"Cannot find a template with name ""{0}"".", templateName));
        }

        internal static string GetDefaultSharedTemplateDirectory()
        {
            return "Packages/com.unity.shadergraph/Editor/Templates";
        }
    }
}
