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
            public static FieldDescriptor RaytracingVFX = new FieldDescriptor(string.Empty, "RaytracingVFX", string.Empty);
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
            if (((VFXAbstractParticleOutput)context).isRayTraced && fieldsContext.pass.lightMode.Contains("DXR"))
            {
                fieldsContext.AddField(VFXFields.RaytracingVFX);
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

        public static IEnumerable<(string name, ShaderValueType type, string interpolation)> GetVFXInterpolators(VFXContext context, VFXTaskCompiledData taskData)
        {
            if (taskData.SGInputs != null)
            {
                bool requiresInterpolation = context is VFXAbstractParticleOutput output && output.HasStrips();
                string interpolationStr = requiresInterpolation ? string.Empty : "nointerpolation";

                foreach (var interp in taskData.SGInputs.interpolators)
                {
                    var (exp, name) = (interp.Key, interp.Value);

                    if (!VFXSubTarget.kVFXShaderValueTypeMap.TryGetValue(VFXExpression.TypeToType(exp.valueType), out var shaderValueType))
                        throw new Exception($"Unsupported interpolator type for {name}: {exp.valueType}");

                    yield return (name, shaderValueType, interpolationStr);
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

        static void GenerateVFXAdditionalCommands(VFXContext context, VFXSRPBinder srp, VFXSRPBinder.ShaderGraphBinder shaderGraphBinder, VFXTaskCompiledData taskData,
            out AdditionalCommandDescriptor srpCommonInclude,
            out AdditionalCommandDescriptor perBlockDefines,
            out AdditionalCommandDescriptor perBlockIncludes,
            out AdditionalCommandDescriptor loadAttributeDescriptor,
            out AdditionalCommandDescriptor blockFunctionDescriptor,
            out AdditionalCommandDescriptor blockCallFunctionDescriptor,
            out AdditionalCommandDescriptor interpolantsGenerationDescriptor,
            out AdditionalCommandDescriptor buildFragInputsGenerationRTDescriptor,
            out AdditionalCommandDescriptor buildVFXFragInputsDescriptor,
            out AdditionalCommandDescriptor pixelPropertiesAssignDescriptor,
            out AdditionalCommandDescriptor defineSpaceDescriptor,
            out AdditionalCommandDescriptor parameterBufferDescriptor,
            out AdditionalCommandDescriptor additionalDefinesDescriptor,
            out AdditionalCommandDescriptor loadPositionAttributeDescriptor,
            out AdditionalCommandDescriptor loadCropFactorAttributesDescriptor,
            out AdditionalCommandDescriptor loadTexcoordAttributesDescriptor,
            out AdditionalCommandDescriptor loadRayTracedScalingAttributesDescriptor,
            out AdditionalCommandDescriptor loadCurrentFrameIndexParameterDescriptor,
            out AdditionalCommandDescriptor vertexPropertiesGenerationDescriptor,
            out AdditionalCommandDescriptor setInstancingIndicesDescriptor,
            out AdditionalCommandDescriptor fillGraphValuesDescriptor,
            out AdditionalCommandDescriptor loadContextDataDescriptor,
            out AdditionalCommandDescriptor additionalFragInputs)
        {
            // TODO: Clean all of this up. Currently just an adapter between VFX Code Gen + SG Code Gen and *everything* has been stuffed here.

            // Overwrite uniform names from the system uniform mapper
            var particleData = context.GetData() as VFXDataParticle;
            var systemUniformMapper = particleData.systemUniformMapper;
            var graphValuesLayout = particleData.graphValuesLayout;
            taskData.uniformMapper.OverrideUniformsNamesWithOther(systemUniformMapper);

            // SRP Common Include
            srpCommonInclude = new AdditionalCommandDescriptor("VFXSRPCommonInclude", string.Format("#include \"{0}\"", srp.runtimePath + "/VFXCommon.hlsl"));

            // Load Attributes
            loadAttributeDescriptor = new AdditionalCommandDescriptor("VFXLoadAttribute", VFXCodeGenerator.GenerateLoadAttribute(".", context, taskData).ToString());

            // Graph Blocks
            var expressionToName = VFXCodeGenerator.BuildExpressionToName(context, taskData);
            VFXCodeGenerator.BuildContextBlocks(context, taskData, expressionToName,
                out var blockFunction,
                out var blockCallFunction,
                out var blockIncludes,
                out var blockDefines);

            perBlockDefines = new AdditionalCommandDescriptor("VFXPerBlockDefines", blockDefines.builder.ToString());
            perBlockIncludes = new AdditionalCommandDescriptor("VFXPerBlockIncludes", blockIncludes.builder.ToString());

            blockFunctionDescriptor = new AdditionalCommandDescriptor("VFXGeneratedBlockFunction", blockFunction.builder.ToString());
            blockCallFunctionDescriptor = new AdditionalCommandDescriptor("VFXProcessBlocks", blockCallFunction.builder.ToString());

            // Vertex Input
            VFXCodeGenerator.BuildVertexProperties(taskData, out var vertexPropertiesGeneration);
            vertexPropertiesGenerationDescriptor = new AdditionalCommandDescriptor("VFXVertexPropertiesGeneration", vertexPropertiesGeneration);

            // Interpolator
            VFXCodeGenerator.BuildInterpolatorBlocks(taskData, out var interpolatorsGeneration);
            interpolantsGenerationDescriptor = new AdditionalCommandDescriptor("VFXInterpolantsGeneration", interpolatorsGeneration);

            // Frag Inputs - Only VFX will know if frag inputs come from interpolator or the CBuffer.
            VFXCodeGenerator.BuildFragInputsGeneration(taskData, shaderGraphBinder.useFragInputs, out var buildFragInputsGeneration);
            buildVFXFragInputsDescriptor = new AdditionalCommandDescriptor("VFXSetFragInputs", buildFragInputsGeneration);

            VFXCodeGenerator.BuildFragInputsGenerationRayTracing(taskData, shaderGraphBinder.useFragInputs, out var buildFragInputsGenerationRT);
            buildFragInputsGenerationRTDescriptor = new AdditionalCommandDescriptor("VFXSetFragInputsRT", buildFragInputsGenerationRT);

            VFXCodeGenerator.BuildPixelPropertiesAssign(taskData, shaderGraphBinder.useFragInputs, out var pixelPropertiesAssign);
            pixelPropertiesAssignDescriptor = new AdditionalCommandDescriptor("VFXPixelPropertiesAssign", pixelPropertiesAssign);

            VFXCodeGenerator.BuildFillGraphValues(taskData, graphValuesLayout, systemUniformMapper, out var fillGraphValues);
            fillGraphValuesDescriptor = new AdditionalCommandDescriptor("VFXLoadGraphValues", fillGraphValues);

            VFXCodeGenerator.BuildLoadContextData(graphValuesLayout, out var loadContextData);
            loadContextDataDescriptor = new AdditionalCommandDescriptor("VFXLoadContextData", loadContextData);

            // Define coordinate space
            var defineSpaceDescriptorContent = string.Empty;
            if (context.GetData() is ISpaceable)
            {
                var spaceable = context.GetData() as ISpaceable;
                defineSpaceDescriptorContent =
                    $"#define {(spaceable.space == VFXSpace.World ? "VFX_WORLD_SPACE" : "VFX_LOCAL_SPACE")} 1";
            }
            defineSpaceDescriptor = new AdditionalCommandDescriptor("VFXDefineSpace", defineSpaceDescriptorContent);

            //Texture used as input of the shaderGraph will be declared by the shaderGraph generation
            //However, if we are sampling a texture (or a mesh), we have to declare them before the VFX code generation.
            //Thus, remove texture used in SG from VFX declaration and let the remainder.
            var shaderGraph = VFXShaderGraphHelpers.GetShaderGraph(context);
            var texureUsedInternallyInSG = shaderGraph.textureInfos.Select(o => o.name);

            var textureExposedFromSG = context.inputSlots.Where(o =>
            {
                return VFXExpression.IsTexture(o.property.type);
            }).Select(o => o.property.name);

            var filteredTextureInSG = texureUsedInternallyInSG.Concat(textureExposedFromSG).ToArray();

            // GraphValues + Buffers + Textures
            VFXCodeGenerator.BuildParameterBuffer(taskData, filteredTextureInSG, out var parameterBuffer, out var needsGraphValueStruct);
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
            foreach (string s in VFXCodeGenerator.GetInstancingAdditionalDefines(context, VFXTaskType.Output, particleData))
                additionalDefines.AppendLine(s);

            additionalDefinesDescriptor = new AdditionalCommandDescriptor("VFXDefines", additionalDefines.ToString());

            // Load Position Attribute
            loadPositionAttributeDescriptor = new AdditionalCommandDescriptor("VFXLoadPositionAttribute", VFXCodeGenerator.GenerateLoadAttribute("position", context, taskData).ToString());

            // Load Crop Factor Attribute
            var mainParameters = taskData.gpuMapper.CollectExpression(-1).ToArray();
            loadCropFactorAttributesDescriptor = new AdditionalCommandDescriptor("VFXLoadCropFactorParameter", VFXCodeGenerator.GenerateLoadParameter("cropFactor", mainParameters, expressionToName).ToString());
            loadTexcoordAttributesDescriptor = new AdditionalCommandDescriptor("VFXLoadTexcoordParameter", VFXCodeGenerator.GenerateLoadParameter("texCoord", mainParameters, expressionToName).ToString());
            loadCurrentFrameIndexParameterDescriptor = new AdditionalCommandDescriptor("VFXLoadCurrentFrameIndexParameter", VFXCodeGenerator.GenerateLoadParameter("currentFrameIndex", mainParameters, expressionToName).ToString());
            loadRayTracedScalingAttributesDescriptor = new AdditionalCommandDescriptor("VFXLoadRayTracedScaling", VFXCodeGenerator.GenerateLoadParameter("rayTracedScaling", mainParameters, expressionToName).ToString());

            //Set VFX Instancing indices
            setInstancingIndicesDescriptor = new AdditionalCommandDescriptor("VFXInitInstancing", VFXCodeGenerator.GenerateSetInstancingIndices().ToString());

            additionalFragInputs = GenerateFragInputs(context, taskData);
        }

        static AdditionalCommandDescriptor GenerateFragInputs(VFXContext context, VFXTaskCompiledData taskData)
        {
            var builder = new ShaderStringBuilder();

            if (taskData.SGInputs != null)
                foreach (var input in taskData.SGInputs.fragInputs)
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


        static StructCollection ApplyVaryingsStructModifier(StructCollection inStructs, VFXSRPBinder.ShaderGraphBinder shaderGraphSRPInfo, List<(string name, ShaderValueType type, string interpolation)> cachedVFXInterpolators)
        {
            // A key difference between Material Shader and VFX Shader generation is how surface properties are provided. Material Shaders
            // simply provide properties via UnityPerMaterial cbuffer. VFX expects these same properties to be computed in the vertex
            // stage (because we must evaluate them with the VFX blocks), and packed with the interpolators for the fragment stage.

            var outStructs = new StructCollection();
            outStructs.Add(shaderGraphSRPInfo.baseStructs);
            foreach (var inStruct in inStructs)
            {
                //Currently all the varyings structs contain the string "Varyings" in it, in URP and HDRP
                if (inStruct.descriptor.name.Contains("Varyings"))
                {
                    var modifiedVaryingsDescriptor = inStruct.descriptor;
                    var fieldList = inStruct.descriptor.fields.ToList();
                    var vfxInterpolators = cachedVFXInterpolators.Select(o =>
                        new FieldDescriptor(inStruct.descriptor.name, o.name, string.Empty, o.type, subscriptOptions: StructFieldOptions.Static, interpolation: o.interpolation));
                    fieldList.AddRange(vfxInterpolators);
                    fieldList.AddRange(shaderGraphSRPInfo.varyingsAdditionalFields);
                    modifiedVaryingsDescriptor.fields = fieldList.ToArray();
                    outStructs.Add(modifiedVaryingsDescriptor, inStruct.fieldConditions);
                }
            }
            return outStructs;
        }

        static readonly (KeywordDescriptor oldDesc, KeywordDescriptor newDesc)[] k_CommonKeywordReplacement =
        {
            (new KeywordDescriptor() {referenceName = Rendering.BuiltIn.ShaderGraph.BuiltInFields.VelocityPrecomputed.define}, VFXSRPBinder.ShaderGraphBinder.kKeywordDescriptorNone)
        };

        static readonly (KeywordDescriptor oldDesc, KeywordDescriptor newDesc)[] k_CommonDefineReplacement =
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

        static DefineCollection ApplyDefineModifier(DefineCollection inputDefines, VFXSRPBinder.ShaderGraphBinder shaderGraphSRPInfo, VFXTaskCompiledData data)
        {
            var defineReplacement = k_CommonDefineReplacement;
            //So far, no SRP custom define replacement

            var overridenDefines = new DefineCollection();
            bool empty = true;
            if (inputDefines != null)
            {
                foreach (var define in inputDefines)
                {
                    var currentDefine = define;

                    var replacement = defineReplacement.FirstOrDefault(o => o.oldDesc.referenceName == define.descriptor.referenceName);
                    if (replacement.newDesc.referenceName == VFXSRPBinder.ShaderGraphBinder.kPragmaDescriptorNone.value)
                        continue; //Skip this irrelevant pragmas, kPragmaDescriptorNone shouldn't be null/empty

                    if (!string.IsNullOrEmpty(replacement.newDesc.referenceName))
                        currentDefine = new DefineCollection.Item(replacement.newDesc, currentDefine.index, currentDefine.fieldConditions);

                    overridenDefines.Add(currentDefine.descriptor, currentDefine.index, currentDefine.fieldConditions);
                    empty = false;
                }
            }

            if (data.SGInputs != null)
            {
                foreach (var keyword in data.SGInputs.keywordsToDefine)
                {
                    if (!string.IsNullOrEmpty(keyword.Value))
                    {
                        //Even if the keyword behind the scene is an enum, we are short cutting the definition to boolean
                        //It will generate `#define _MY_ACTIVE_ENUM_OPTION 1`
                        overridenDefines.Add(new KeywordDescriptor() {referenceName = keyword.Value}, 1);
                        empty = false;
                    }
                }
            }

            return !empty ? overridenDefines : null;
        }

        internal static SubShaderDescriptor PostProcessSubShader(SubShaderDescriptor subShaderDescriptor, VFXContext context, VFXTaskCompiledData data)
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
                out var perBlockDefines,
                out var perBlockIncludes,
                out var loadAttributeDescriptor,
                out var blockFunctionDescriptor,
                out var blockCallFunctionDescriptor,
                out var interpolantsGenerationDescriptor,
                out var buildFragInputsGenerationRTDescriptor,
                out var buildVFXFragInputs,
                out var pixelPropertiesAssignDescriptor,
                out var defineSpaceDescriptor,
                out var parameterBufferDescriptor,
                out var additionalDefinesDescriptor,
                out var loadPositionAttributeDescriptor,
                out var loadCropFactorAttributesDescriptor,
                out var loadTexcoordAttributesDescriptor,
                out var loadCurrentFrameIndexParameterDescriptor,
                out var loadRayTracedScalingAttributesDescriptor,
                out var vertexPropertiesGenerationDescriptor,
                out var setInstancingIndicesDescriptor,
                out var fillGraphValuesDescriptor,
                out var loadContextDataDescriptor,
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

            // SPACEWARP DOES NOT SUPPORT VFX GRAPH FOR NOW, SO WE DISABLE IT HERE
            filteredPasses = filteredPasses.Where(o => o.descriptor.lightMode != "XRMotionVectors");

            var passes = filteredPasses.ToArray();

            var addPragmaRequireCubeArray = data.uniformMapper.textures.Any(o => o.valueType == VFXValueType.TextureCubeArray);

            PassCollection vfxPasses = new PassCollection();
            var cachedVFXInterpolators = GetVFXInterpolators(context, data).ToList();
            for (int i = 0; i < passes.Length; i++)
            {
                var passDescriptor = passes[i].descriptor;

                passDescriptor.pragmas = ApplyPragmaModifier(passDescriptor.pragmas, shaderGraphSRPInfo, addPragmaRequireCubeArray);
                passDescriptor.keywords = ApplyKeywordModifier(passDescriptor.keywords, shaderGraphSRPInfo);
                passDescriptor.defines = ApplyDefineModifier(passDescriptor.defines, shaderGraphSRPInfo, data);

                // Warning: We are replacing the struct provided in the regular pass. It is ok as for now the VFX editor don't support
                // tessellation or raytracing
                passDescriptor.structs = ApplyVaryingsStructModifier(passDescriptor.structs, shaderGraphSRPInfo, cachedVFXInterpolators);
                passDescriptor.structs.Add(attributesStruct);
                passDescriptor.structs.Add(sourceAttributesStruct);

                // Add additional VFX dependencies
                passDescriptor.fieldDependencies = passDescriptor.fieldDependencies == null ? new DependencyCollection() : new DependencyCollection { passDescriptor.fieldDependencies }; // Duplicate fieldDependencies to avoid side effects (static list modification)
                passDescriptor.fieldDependencies.Add(shaderGraphSRPInfo.fieldDependencies);

                passDescriptor.additionalCommands = new AdditionalCommandCollection
                {
                    srpCommonInclude,
                    perBlockDefines,
                    perBlockIncludes,
                    loadAttributeDescriptor,
                    blockFunctionDescriptor,
                    blockCallFunctionDescriptor,
                    interpolantsGenerationDescriptor,
                    buildFragInputsGenerationRTDescriptor,
                    buildVFXFragInputs,
                    pixelPropertiesAssignDescriptor,
                    defineSpaceDescriptor,
                    parameterBufferDescriptor,
                    additionalDefinesDescriptor,
                    loadPositionAttributeDescriptor,
                    loadCropFactorAttributesDescriptor,
                    loadTexcoordAttributesDescriptor,
                    loadCurrentFrameIndexParameterDescriptor,
                    loadRayTracedScalingAttributesDescriptor,
                    vertexPropertiesGenerationDescriptor,
                    setInstancingIndicesDescriptor,
                    fillGraphValuesDescriptor,
                    loadContextDataDescriptor,
                    fragInputsDescriptor
                };

                vfxPasses.Add(passDescriptor, passes[i].fieldConditions);
            }

            subShaderDescriptor.passes = vfxPasses;

            return subShaderDescriptor;
        }
    }
}
