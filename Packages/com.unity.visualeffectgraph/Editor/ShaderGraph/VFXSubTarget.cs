using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    static class VFXSubTarget
    {
        // See: VFXShaderWriter.TypeToUniformCode
        // TODO: Collapse these two maps into one
        public static readonly Dictionary<Type, Type> kVFXShaderPropertyMap = new Dictionary<Type, Type>
        {
            { typeof(float),     typeof(Vector1ShaderProperty) },
            { typeof(Vector2),   typeof(Vector2ShaderProperty) },
            { typeof(Vector3),   typeof(Vector3ShaderProperty) },
            { typeof(Vector4),   typeof(Vector4ShaderProperty) },
            { typeof(int),       typeof(Vector1ShaderProperty) },
            { typeof(uint),      typeof(Vector1ShaderProperty) },
            { typeof(Matrix4x4), typeof(Matrix4ShaderProperty) },
            { typeof(bool),      typeof(BooleanShaderProperty) },
        };

        public static readonly Dictionary<Type, ShaderValueType> kVFXShaderValueTypeMap = new Dictionary<Type, ShaderValueType>
        {
            { typeof(float),     ShaderValueType.Float   },
            { typeof(Vector2),   ShaderValueType.Float2  },
            { typeof(Vector3),   ShaderValueType.Float3  },
            { typeof(Vector4),   ShaderValueType.Float4  },
            { typeof(int),       ShaderValueType.Integer },
            { typeof(uint),      ShaderValueType.Uint    },
            { typeof(Matrix4x4), ShaderValueType.Matrix4 },
            { typeof(bool),      ShaderValueType.Float   }, // NOTE: Map boolean to float for VFX interpolator due to how ShaderGraph handles semantics for boolean interpolator.
        };

        public static ShaderValueType VFXTypeToSGType(VFXValueType t)
        {
            return kVFXShaderValueTypeMap[VFXExpression.TypeToType(t)];
        }

        public static FieldDescriptor VFXAttributeToFieldDescriptor(VFXAttribute attribute)
        {
            var type = VFXExpression.TypeToType(attribute.type);

            if (!kVFXShaderValueTypeMap.TryGetValue(type, out var shaderValueType))
                return null;

            return new FieldDescriptor("VFXAttributes", attribute.name, "", shaderValueType);
        }

        static class VFXFields
        {
            public const string kTag = "OutputType";
            public static FieldDescriptor ParticleMesh = new FieldDescriptor(kTag, "Mesh", "VFX_PARTICLE_MESH 1");
            public static FieldDescriptor ParticlePlanarPrimitive = new FieldDescriptor(kTag, "PlanarPrimitive", "VFX_PARTICLE_PLANAR_PRIMITIVE 1");
        }

        internal static void GetFields(ref TargetFieldContext fieldsContext, VFXContext context)
        {
            fieldsContext.AddField(Fields.GraphVFX);

            // Select the primitive implementation.
            switch (context.taskType)
            {
                case VFXTaskType.ParticleMeshOutput:
                    fieldsContext.AddField(VFXFields.ParticleMesh);
                    break;
                case VFXTaskType.ParticleTriangleOutput:
                case VFXTaskType.ParticleOctagonOutput:
                case VFXTaskType.ParticleQuadOutput:
                    fieldsContext.AddField(VFXFields.ParticlePlanarPrimitive);
                    break;
            }
        }

        enum VFXAttributeType
        {
            Current,
            Source
        }

        static readonly string[] kVFXAttributeStructNames =
        {
            "InternalAttributesElement",
            "InternalSourceAttributesElement"
        };

        public static IEnumerable<FieldDescriptor> GetVFXInterpolators(string structName, VFXContext context, VFXContextCompiledData contextData)
        {
            if (contextData.SGInputs != null)
            {
                bool requiresInterpolation = context is VFXAbstractParticleOutput output && output.HasStrips();
                string interpolationStr = requiresInterpolation ? string.Empty : "nointerpolation";

                foreach (var interp in contextData.SGInputs.interpolators)
                {
                    var (exp, name) = (interp.Key, interp.Value);

                    if (!VFXSubTarget.kVFXShaderValueTypeMap.TryGetValue(VFXExpression.TypeToType(exp.valueType), out var shaderValueType))
                        throw new Exception($"Unsupported interpolator type for {name}: {exp.valueType}");

                    yield return new FieldDescriptor(structName, name, "", shaderValueType, subscriptOptions: StructFieldOptions.Static, interpolation: interpolationStr);
                }
            }
        }

        static StructDescriptor GenerateVFXAttributesStruct(VFXContext context, VFXAttributeType attributeType)
        {
            IEnumerable<VFXAttributeInfo> attributeInfos;

            if (attributeType == VFXAttributeType.Current)
            {
                attributeInfos = context.GetData().GetAttributes().Where(a =>
                    (context.GetData().IsCurrentAttributeUsed(a.attrib, context)) ||
                    (context.contextType == VFXContextType.Init && context.GetData().IsAttributeStored(a.attrib))); // In init, needs to declare all stored attributes for intialization
            }
            else
            {
                attributeInfos = context.GetData().GetAttributes().Where(a => (context.GetData().IsSourceAttributeUsed(a.attrib, context)));
            }

            var attributes = attributeInfos.Select(a => a.attrib);

            var attributeFieldDescriptors = new List<FieldDescriptor>();
            foreach (var attribute in attributes)
            {
                var afd = VFXSubTarget.VFXAttributeToFieldDescriptor(attribute);
                attributeFieldDescriptors.Add(afd);
            }

            return new StructDescriptor
            {
                name = kVFXAttributeStructNames[(int)attributeType],
                fields = attributeFieldDescriptors.ToArray()
            };
        }

        static void GenerateVFXAdditionalCommands(VFXContext context, VFXSRPBinder srp, VFXSRPBinder.ShaderGraphBinder shaderGraphBinder, VFXContextCompiledData contextData,
            out AdditionalCommandDescriptor srpCommonInclude,
            out AdditionalCommandDescriptor loadAttributeDescriptor,
            out AdditionalCommandDescriptor blockFunctionDescriptor,
            out AdditionalCommandDescriptor blockCallFunctionDescriptor,
            out AdditionalCommandDescriptor interpolantsGenerationDescriptor,
            out AdditionalCommandDescriptor buildVFXFragInputsDescriptor,
            out AdditionalCommandDescriptor pixelPropertiesAssignDescriptor,
            out AdditionalCommandDescriptor defineSpaceDescriptor,
            out AdditionalCommandDescriptor parameterBufferDescriptor,
            out AdditionalCommandDescriptor additionalDefinesDescriptor,
            out AdditionalCommandDescriptor loadPositionAttributeDescriptor,
            out AdditionalCommandDescriptor loadCropFactorAttributesDescriptor,
            out AdditionalCommandDescriptor loadTexcoordAttributesDescriptor,
            out AdditionalCommandDescriptor loadCurrentFrameIndexParameterDescriptor,
            out AdditionalCommandDescriptor vertexPropertiesGenerationDescriptor,
            out AdditionalCommandDescriptor setInstancingIndicesDescriptor,
            out AdditionalCommandDescriptor fillGraphValuesDescriptor,
            out AdditionalCommandDescriptor additionalFragInputs)
        {
            // TODO: Clean all of this up. Currently just an adapter between VFX Code Gen + SG Code Gen and *everything* has been stuffed here.

            // Overwrite uniform names from the system uniform mapper
            var particleData = context.GetData() as VFXDataParticle;
            var systemUniformMapper = particleData.systemUniformMapper;
            var graphValuesLayout = particleData.graphValuesLayout;
            contextData.uniformMapper.OverrideUniformsNamesWithOther(systemUniformMapper);

            // SRP Common Include
            srpCommonInclude = new AdditionalCommandDescriptor("VFXSRPCommonInclude", string.Format("#include \"{0}\"", srp.runtimePath + "/VFXCommon.hlsl"));

            // Load Attributes
            loadAttributeDescriptor = new AdditionalCommandDescriptor("VFXLoadAttribute", VFXCodeGenerator.GenerateLoadAttribute(".", context, contextData).ToString());

            // Graph Blocks
            VFXCodeGenerator.BuildContextBlocks(context, contextData, out var blockFunction, out var blockCallFunction);

            blockFunctionDescriptor = new AdditionalCommandDescriptor("VFXGeneratedBlockFunction", blockFunction);
            blockCallFunctionDescriptor = new AdditionalCommandDescriptor("VFXProcessBlocks", blockCallFunction);

            // Vertex Input
            VFXCodeGenerator.BuildVertexProperties(context, contextData, out var vertexPropertiesGeneration);
            vertexPropertiesGenerationDescriptor = new AdditionalCommandDescriptor("VFXVertexPropertiesGeneration", vertexPropertiesGeneration);

            // Interpolator
            VFXCodeGenerator.BuildInterpolatorBlocks(context, contextData, out var interpolatorsGeneration);
            interpolantsGenerationDescriptor = new AdditionalCommandDescriptor("VFXInterpolantsGeneration", interpolatorsGeneration);

            // Frag Inputs - Only VFX will know if frag inputs come from interpolator or the CBuffer.
            VFXCodeGenerator.BuildFragInputsGeneration(context, contextData, shaderGraphBinder.useFragInputs, out var buildFragInputsGeneration);
            buildVFXFragInputsDescriptor = new AdditionalCommandDescriptor("VFXSetFragInputs", buildFragInputsGeneration);

            VFXCodeGenerator.BuildPixelPropertiesAssign(context, contextData, shaderGraphBinder.useFragInputs, out var pixelPropertiesAssign);
            pixelPropertiesAssignDescriptor = new AdditionalCommandDescriptor("VFXPixelPropertiesAssign", pixelPropertiesAssign);

            VFXCodeGenerator.BuildFillGraphValues(contextData, graphValuesLayout, systemUniformMapper, out var fillGraphValues);
            fillGraphValuesDescriptor = new AdditionalCommandDescriptor("VFXLoadGraphValues", fillGraphValues);

            // Define coordinate space
            var defineSpaceDescriptorContent = string.Empty;
            if (context.GetData() is ISpaceable)
            {
                var spaceable = context.GetData() as ISpaceable;
                defineSpaceDescriptorContent =
                    $"#define {(spaceable.space == VFXCoordinateSpace.World ? "VFX_WORLD_SPACE" : "VFX_LOCAL_SPACE")} 1";
            }
            defineSpaceDescriptor = new AdditionalCommandDescriptor("VFXDefineSpace", defineSpaceDescriptorContent);

            //Texture used as input of the shaderGraph will be declared by the shaderGraph generation
            //However, if we are sampling a texture (or a mesh), we have to declare them before the VFX code generation.
            //Thus, remove texture used in SG from VFX declaration and let the remainder.
            var shaderGraphOutput = context as VFXShaderGraphParticleOutput;
            if (shaderGraphOutput == null)
                throw new InvalidOperationException("Unexpected null VFXShaderGraphParticleOutput");
            var shaderGraphObject = shaderGraphOutput.GetOrRefreshShaderGraphObject();
            if (shaderGraphObject == null)
                throw new InvalidOperationException("Unexpected null GetOrRefreshShaderGraphObject");
            var texureUsedInternallyInSG = shaderGraphObject.textureInfos.Select(o =>
            {
                return o.name;
            });
            var textureExposedFromSG = context.inputSlots.Where(o =>
            {
                return VFXExpression.IsTexture(o.property.type);
            }).Select(o => o.property.name);

            var filteredTextureInSG = texureUsedInternallyInSG.Concat(textureExposedFromSG).ToArray();

            // GraphValues + Buffers + Textures
            VFXCodeGenerator.BuildParameterBuffer(contextData, filteredTextureInSG, out var parameterBuffer, out var needsGraphValueStruct);
            parameterBufferDescriptor = new AdditionalCommandDescriptor("VFXParameterBuffer", parameterBuffer);

            // Defines & Headers - Not all are necessary, however some important ones are mixed in like indirect draw, strips, flipbook, particle strip info...
            ShaderStringBuilder additionalDefines = new ShaderStringBuilder();
            // TODO: Need to add defines for current/source usage (i.e. scale).

            var allCurrentAttributes = context.GetData().GetAttributes().Where(a =>
                (context.GetData().IsCurrentAttributeUsed(a.attrib, context)) ||
                (context.contextType == VFXContextType.Init && context.GetData().IsAttributeStored(a.attrib))); // In init, needs to declare all stored attributes for intialization

            var allSourceAttributes = context.GetData().GetAttributes().Where(a => (context.GetData().IsSourceAttributeUsed(a.attrib, context)));

            foreach (var attribute in allCurrentAttributes)
                additionalDefines.AppendLine("#define VFX_USE_{0}_{1} 1", attribute.attrib.name.ToUpper(System.Globalization.CultureInfo.InvariantCulture), "CURRENT");
            foreach (var attribute in allSourceAttributes)
                additionalDefines.AppendLine("#define VFX_USE_{0}_{1} 1", attribute.attrib.name.ToUpper(System.Globalization.CultureInfo.InvariantCulture), "SOURCE");
            foreach (var header in context.additionalDataHeaders)
                additionalDefines.AppendLine(header);
            foreach (var define in context.additionalDefines)
                additionalDefines.AppendLine(define.Contains(' ') ? $"#define {define}" : $"#define {define} 1");
            if(needsGraphValueStruct)
                additionalDefines.AppendLine($"#define VFX_USE_GRAPH_VALUES 1");
            foreach (string s in VFXCodeGenerator.GetInstancingAdditionalDefines(context, particleData))
                additionalDefines.AppendLine(s);

            additionalDefinesDescriptor = new AdditionalCommandDescriptor("VFXDefines", additionalDefines.ToString());

            // Load Position Attribute
            loadPositionAttributeDescriptor = new AdditionalCommandDescriptor("VFXLoadPositionAttribute", VFXCodeGenerator.GenerateLoadAttribute("position", context, contextData).ToString().ToString());

            // Load Crop Factor Attribute
            var mainParameters = contextData.gpuMapper.CollectExpression(-1).ToArray();
            var expressionToName = context.GetData().GetAttributes().ToDictionary(o => new VFXAttributeExpression(o.attrib) as VFXExpression, o => (new VFXAttributeExpression(o.attrib)).GetCodeString(null));
            expressionToName = expressionToName.Union(contextData.uniformMapper.expressionToCode).ToDictionary(s => s.Key, s => s.Value);
            loadCropFactorAttributesDescriptor = new AdditionalCommandDescriptor("VFXLoadCropFactorParameter", VFXCodeGenerator.GenerateLoadParameter("cropFactor", mainParameters, expressionToName).ToString().ToString());
            loadTexcoordAttributesDescriptor = new AdditionalCommandDescriptor("VFXLoadTexcoordParameter", VFXCodeGenerator.GenerateLoadParameter("texCoord", mainParameters, expressionToName).ToString().ToString());
            loadCurrentFrameIndexParameterDescriptor = new AdditionalCommandDescriptor("VFXLoadCurrentFrameIndexParameter", VFXCodeGenerator.GenerateLoadParameter("currentFrameIndex", mainParameters, expressionToName).ToString().ToString());

            //Set VFX Instancing indices
            setInstancingIndicesDescriptor = new AdditionalCommandDescriptor("VFXInitInstancing", VFXCodeGenerator.GenerateSetInstancingIndices(context).ToString());

            additionalFragInputs = GenerateFragInputs(context, contextData);
        }

        static AdditionalCommandDescriptor GenerateFragInputs(VFXContext context, VFXContextCompiledData contextData)
        {
            var builder = new ShaderStringBuilder();

            if (contextData.SGInputs != null)
                foreach (var input in contextData.SGInputs.fragInputs)
                {
                    var (name, exp) = (input.Key, input.Value);
                    builder.AppendLine($"{VFXTypeToSGType(exp.valueType).ToShaderString("float")} {name};");
                }

            return new AdditionalCommandDescriptor("FragInputsVFX", builder.ToString());
        }

        static readonly (PragmaDescriptor oldDesc, PragmaDescriptor newDesc)[] k_CommonPragmaReplacement =
        {
            //Irrelevant general multicompile instancing (VFX will append them when needed)
            ( Pragma.MultiCompileInstancing, VFXSRPBinder.ShaderGraphBinder.kPragmaDescriptorNone),
            ( Pragma.DOTSInstancing, VFXSRPBinder.ShaderGraphBinder.kPragmaDescriptorNone),
            ( Pragma.InstancingOptions(InstancingOptions.RenderingLayer), VFXSRPBinder.ShaderGraphBinder.kPragmaDescriptorNone ),
            ( Pragma.InstancingOptions(InstancingOptions.NoLightProbe), VFXSRPBinder.ShaderGraphBinder.kPragmaDescriptorNone ),
            ( Pragma.InstancingOptions(InstancingOptions.NoLodFade), VFXSRPBinder.ShaderGraphBinder.kPragmaDescriptorNone ),
        };

        static PragmaCollection ApplyPragmaModifier(PragmaCollection pragmas, VFXSRPBinder.ShaderGraphBinder shaderGraphSRPInfo, bool addPragmaRequireCubeArray)
        {
            var pragmasReplacement = k_CommonPragmaReplacement;
            if (shaderGraphSRPInfo.pragmasReplacement != null)
                pragmasReplacement = shaderGraphSRPInfo.pragmasReplacement.Concat(pragmasReplacement).ToArray();

            var overridenPragmas = new PragmaCollection();
            foreach (var pragma in pragmas)
            {
                var currentPragma = pragma;

                if (pragmasReplacement != null)
                {
                    var replacement = pragmasReplacement.FirstOrDefault(o => o.oldDesc.value == pragma.descriptor.value);
                    if (replacement.newDesc.value == VFXSRPBinder.ShaderGraphBinder.kPragmaDescriptorNone.value)
                        continue; //Skip this irrelevant pragmas, kPragmaDescriptorNone shouldn't be null/empty

                    if (!string.IsNullOrEmpty(replacement.newDesc.value))
                        currentPragma = new PragmaCollection.Item(replacement.newDesc, pragma.fieldConditions);
                }
                overridenPragmas.Add(currentPragma.descriptor, currentPragma.fieldConditions);
            }

            if (addPragmaRequireCubeArray)
            {
                overridenPragmas.Add(new PragmaDescriptor() { value = "require cubearray" });
            }

            return overridenPragmas;
        }

        static readonly (KeywordDescriptor oldDesc, KeywordDescriptor newDesc)[] k_CommonKeywordReplacement =
        {
            (new KeywordDescriptor() {referenceName = Rendering.BuiltIn.ShaderGraph.BuiltInFields.VelocityPrecomputed.define}, VFXSRPBinder.ShaderGraphBinder.kKeywordDescriptorNone)
        };

        static KeywordCollection ApplyKeywordModifier(KeywordCollection keywords, VFXSRPBinder.ShaderGraphBinder shaderGraphSRPInfo)
        {
            if (keywords != null)
            {
                var keywordsReplacement = k_CommonKeywordReplacement;
                if (shaderGraphSRPInfo.keywordsReplacement != null)
                    keywordsReplacement = keywordsReplacement.Concat(shaderGraphSRPInfo.keywordsReplacement).ToArray();

                var overridenKeywords = new KeywordCollection();
                foreach (var keyword in keywords)
                {
                    var currentKeyword = keyword;

                    var replacement = keywordsReplacement.FirstOrDefault(o => o.oldDesc.referenceName == keyword.descriptor.referenceName);
                    if (replacement.newDesc.referenceName == VFXSRPBinder.ShaderGraphBinder.kPragmaDescriptorNone.value)
                        continue; //Skip this irrelevant pragmas, kPragmaDescriptorNone shouldn't be null/empty

                    if (!string.IsNullOrEmpty(replacement.newDesc.referenceName))
                        currentKeyword = new KeywordCollection.Item(replacement.newDesc, currentKeyword.fieldConditions);

                    overridenKeywords.Add(currentKeyword.descriptor, currentKeyword.fieldConditions);
                }
                return overridenKeywords;
            }
            return keywords;
        }

        internal static SubShaderDescriptor PostProcessSubShader(SubShaderDescriptor subShaderDescriptor, VFXContext context, VFXContextCompiledData data)
        {
            var srp = VFXLibrary.currentSRPBinder;
            if (srp == null)
                return subShaderDescriptor;

            var shaderGraphSRPInfo = srp.GetShaderGraphDescriptor(context, data);

            var attributesStruct = GenerateVFXAttributesStruct(context, VFXAttributeType.Current);
            var sourceAttributesStruct = GenerateVFXAttributesStruct(context, VFXAttributeType.Source);

            // Defer to VFX to generate various misc. code-gen that ShaderGraph currently can't handle.
            // We use the AdditionalCommand descriptors for ShaderGraph generation to splice these in.
            // ( i.e. VFX Graph Block Function declaration + calling, Property Mapping, etc. )
            GenerateVFXAdditionalCommands(
                context, srp, shaderGraphSRPInfo, data,
                out var srpCommonInclude,
                out var loadAttributeDescriptor,
                out var blockFunctionDescriptor,
                out var blockCallFunctionDescriptor,
                out var interpolantsGenerationDescriptor,
                out var buildVFXFragInputs,
                out var pixelPropertiesAssignDescriptor,
                out var defineSpaceDescriptor,
                out var parameterBufferDescriptor,
                out var additionalDefinesDescriptor,
                out var loadPositionAttributeDescriptor,
                out var loadCropFactorAttributesDescriptor,
                out var loadTexcoordAttributesDescriptor,
                out var loadCurrentFrameIndexParameterDescriptor,
                out var vertexPropertiesGenerationDescriptor,
                out var setInstancingIndicesDescriptor,
                out var fillGraphValuesDescriptor,
                out var fragInputsDescriptor
            );

            // Omit META and MV or Shadow Pass if disabled on the context.
            var filteredPasses = subShaderDescriptor.passes.AsEnumerable();

            filteredPasses = filteredPasses.Where(o => o.descriptor.lightMode != "META");
            var outputContext = (VFXAbstractParticleOutput)context;
            if (!outputContext.hasMotionVector)
                filteredPasses = filteredPasses.Where(o => o.descriptor.lightMode != "MotionVectors");

            if (!outputContext.hasShadowCasting)
                filteredPasses = filteredPasses.Where(o => o.descriptor.lightMode != "ShadowCaster");

            var passes = filteredPasses.ToArray();

            var addPragmaRequireCubeArray = data.uniformMapper.textures.Any(o => o.valueType == VFXValueType.TextureCubeArray);

            PassCollection vfxPasses = new PassCollection();
            for (int i = 0; i < passes.Length; i++)
            {
                var passDescriptor = passes[i].descriptor;

                passDescriptor.pragmas = ApplyPragmaModifier(passDescriptor.pragmas, shaderGraphSRPInfo, addPragmaRequireCubeArray);
                passDescriptor.keywords = ApplyKeywordModifier(passDescriptor.keywords, shaderGraphSRPInfo);

                // Warning: We are replacing the struct provided in the regular pass. It is ok as for now the VFX editor don't support
                // tessellation or raytracing
                passDescriptor.structs = new StructCollection();
                passDescriptor.structs.Add(shaderGraphSRPInfo.structs);
                passDescriptor.structs.Add(attributesStruct);
                passDescriptor.structs.Add(sourceAttributesStruct);

                // Add additional VFX dependencies
                passDescriptor.fieldDependencies = passDescriptor.fieldDependencies == null ? new DependencyCollection() : new DependencyCollection { passDescriptor.fieldDependencies }; // Duplicate fieldDependencies to avoid side effects (static list modification)
                passDescriptor.fieldDependencies.Add(shaderGraphSRPInfo.fieldDependencies);

                passDescriptor.additionalCommands = new AdditionalCommandCollection
                {
                    srpCommonInclude,
                    loadAttributeDescriptor,
                    blockFunctionDescriptor,
                    blockCallFunctionDescriptor,
                    interpolantsGenerationDescriptor,
                    buildVFXFragInputs,
                    pixelPropertiesAssignDescriptor,
                    defineSpaceDescriptor,
                    parameterBufferDescriptor,
                    additionalDefinesDescriptor,
                    loadPositionAttributeDescriptor,
                    loadCropFactorAttributesDescriptor,
                    loadTexcoordAttributesDescriptor,
                    loadCurrentFrameIndexParameterDescriptor,
                    vertexPropertiesGenerationDescriptor,
                    setInstancingIndicesDescriptor,
                    fillGraphValuesDescriptor,
                    fragInputsDescriptor
                };

                vfxPasses.Add(passDescriptor, passes[i].fieldConditions);
            }

            subShaderDescriptor.passes = vfxPasses;

            return subShaderDescriptor;
        }
    }
}
