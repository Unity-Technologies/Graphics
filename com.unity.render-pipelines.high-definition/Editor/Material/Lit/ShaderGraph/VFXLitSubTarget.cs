using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.VFX;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed class VFXLitSubTarget : HDLitSubTarget, IVFXCompatibleTarget
    {
        private VFXContext m_Context;
        private VFXContextCompiledData m_ContextData;

        static readonly GUID kSubTargetSourceCodeGuid = new GUID("a4015296799c4bfd99499b48602f9e32");  // VFXLitSubTarget.cs
        protected override GUID subTargetAssetGuid => kSubTargetSourceCodeGuid;

        protected override string templatePath => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/ShaderGraph/Templates/ShaderPassVFX.template";

        protected override bool supportRaytracing => false;
        protected override bool supportPathtracing => false;

        protected override SubShaderDescriptor GetSubShaderDescriptor()
        {
            var baseSubShaderDescriptor = new SubShaderDescriptor
            {
                generatesPreview = false,
                passes = GetPasses()
            };

            PassCollection GetPasses()
            {
                // TODO: Use the VFX context to configure the passes
                var passes = new PassCollection
                {
                    HDShaderPasses.GenerateSceneSelection(supportLighting),
                    HDShaderPasses.GenerateLitDepthOnly(),
                    HDShaderPasses.GenerateGBuffer(),
                    HDShaderPasses.GenerateLitForward()
                };

                if (m_Context is VFXAbstractParticleOutput m_ContextHDRP)
                {
                    if (m_ContextHDRP.hasShadowCasting)
                    {
                        passes.Add(HDShaderPasses.GenerateShadowCaster(supportLighting));
                    }

                    if (m_ContextHDRP.hasMotionVector)
                    {
                        passes.Add(HDShaderPasses.GenerateMotionVectors(supportLighting, supportForward));
                    }
                }

                return passes;
            }

            // Use the current VFX context to configure the subshader.
            return PostProcessSubShaderVFX(baseSubShaderDescriptor, m_Context, m_ContextData);
        }

        static SubShaderDescriptor PostProcessSubShaderVFX(SubShaderDescriptor subShaderDescriptor, VFXContext context, VFXContextCompiledData contextData)
        {
            var attributesStruct = GenerateVFXAttributesStruct(context, VFXAttributeType.Current);
            var sourceAttributesStruct = GenerateVFXAttributesStruct(context, VFXAttributeType.Source);

            // Defer to VFX to generate various misc. code-gen that ShaderGraph currently can't handle.
            // We use the AdditionalCommand descriptors for ShaderGraph generation to splice these in.
            // ( i.e. VFX Graph Block Function declaration + calling, Property Mapping, etc. )
            GenerateVFXAdditionalCommands(
                context, contextData,
                out var loadAttributeDescriptor,
                out var blockFunctionDescriptor,
                out var blockCallFunctionDescriptor,
                out var interpolantsGenerationDescriptor,
                out var buildVFXFragInputs
            );

            var passes = subShaderDescriptor.passes.ToArray();
            PassCollection vfxPasses = new PassCollection();
            for (int i = 0; i < passes.Length; i++)
            {
                var passDescriptor = passes[i].descriptor;

                // Warning: Touching the structs field may require to manually append the default structs here.
                passDescriptor.structs = new StructCollection
                {
                    AttributesMeshVFX, // TODO: Could probably re-use the original HD Attributes Mesh and just ensure Instancing enabled.
                    AppendVFXInterpolator(HDStructs.VaryingsMeshToPS, context, contextData),
                    GenerateFragInputs(context, contextData),
                    Structs.SurfaceDescriptionInputs,
                    Structs.VertexDescriptionInputs,
                    attributesStruct,
                    sourceAttributesStruct
                };

                passDescriptor.pragmas = new PragmaCollection
                {
                    passDescriptor.pragmas,
                    // Pragma.DebugSymbolsD3D,
                    Pragma.MultiCompileInstancing
                };

                // passDescriptor.defines = new DefineCollection
                // {
                //     passDescriptor.defines,
                // };

                passDescriptor.additionalCommands = new AdditionalCommandCollection
                {
                    loadAttributeDescriptor,
                    blockFunctionDescriptor,
                    blockCallFunctionDescriptor,
                    interpolantsGenerationDescriptor,
                    buildVFXFragInputs
                };

                vfxPasses.Add(passDescriptor, passes[i].fieldConditions);
            }

            subShaderDescriptor.passes = vfxPasses;

            return subShaderDescriptor;
        }

        public static StructDescriptor AttributesMeshVFX = new StructDescriptor()
        {
            name = "AttributesMesh",
            packFields = false,
            fields = new FieldDescriptor[]
            {
                HDStructFields.AttributesMesh.positionOS,
                HDStructFields.AttributesMesh.normalOS,
                HDStructFields.AttributesMesh.tangentOS,
                HDStructFields.AttributesMesh.uv0,
                HDStructFields.AttributesMesh.uv1,
                HDStructFields.AttributesMesh.uv2,
                HDStructFields.AttributesMesh.uv3,
                HDStructFields.AttributesMesh.color,

                // AttributesMesh without the Preprocessor.
                new FieldDescriptor(HDStructFields.AttributesMesh.name, "instanceID", "", ShaderValueType.Uint, "INSTANCEID_SEMANTIC"),

                HDStructFields.AttributesMesh.weights,
                HDStructFields.AttributesMesh.indices,
                HDStructFields.AttributesMesh.vertexID,
            }
        };

        public static StructDescriptor GenerateFragInputs(VFXContext context, VFXContextCompiledData contextData)
        {
            var fields = new List<FieldDescriptor>();

            // Default
            // Note: These are all already defined in HDStructFields, but marked as "Optional".
            //       For now just be explicit here that we NEED everything to define the struct.
            fields.Add(new FieldDescriptor(HDStructFields.FragInputs.name, "positionSS", "", ShaderValueType.Float4));
            fields.Add(new FieldDescriptor(HDStructFields.FragInputs.name, "positionRWS", "", ShaderValueType.Float3));
            fields.Add(new FieldDescriptor(HDStructFields.FragInputs.name, "tangentToWorld", "", ShaderValueType.Matrix3));
            fields.Add(new FieldDescriptor(HDStructFields.FragInputs.name, "texCoord0", "", ShaderValueType.Float4));
            fields.Add(new FieldDescriptor(HDStructFields.FragInputs.name, "texCoord1", "", ShaderValueType.Float4));
            fields.Add(new FieldDescriptor(HDStructFields.FragInputs.name, "texCoord2", "", ShaderValueType.Float4));
            fields.Add(new FieldDescriptor(HDStructFields.FragInputs.name, "texCoord3", "", ShaderValueType.Float4));
            fields.Add(new FieldDescriptor(HDStructFields.FragInputs.name, "color", "", ShaderValueType.Float4));
            fields.Add(new FieldDescriptor(HDStructFields.FragInputs.name, "primitiveID", "", ShaderValueType.Uint));
            fields.Add(new FieldDescriptor(HDStructFields.FragInputs.name, "isFrontFace", "", ShaderValueType.Boolean));

            // VFX Material Properties
            // TODO: This can be merged with AppendVFXInterpolater. Lots of duplicated code just to query simple info.
            var expressionToName = context.GetData().GetAttributes().ToDictionary(o => new VFXAttributeExpression(o.attrib) as VFXExpression, o => (new VFXAttributeExpression(o.attrib)).GetCodeString(null));
            expressionToName = expressionToName.Union(contextData.uniformMapper.expressionToCode).ToDictionary(s => s.Key, s => s.Value);

            var mainParameters = contextData.gpuMapper.CollectExpression(-1).ToArray();

            foreach (string fragmentParameter in context.fragmentParameters)
            {
                var filteredNamedExpression = mainParameters.FirstOrDefault(o => fragmentParameter == o.name);

                if (filteredNamedExpression.exp != null)
                {
                    var type = VFXExpression.TypeToType(filteredNamedExpression.exp.valueType);

                    if (!kVFXShaderValueTypeyMap.TryGetValue(type, out var shaderValueType))
                        continue;

                    fields.Add(new FieldDescriptor(HDStructFields.FragInputs.name, filteredNamedExpression.name, "", shaderValueType));
                }
            }

            var fragInputs = new StructDescriptor
            {
                name = HDStructFields.FragInputs.name,
                fields = fields.ToArray()
            };

            return fragInputs;
        }

        // A key difference between Material Shader and VFX Shader generation is how surface properties are provided. Material Shaders
        // simply provide properties via UnityPerMaterial cbuffer. VFX expects these same properties to be computed in the vertex
        // stage (because we must evaluate them with the VFX blocks), and packed with the interpolators for the fragment stage.
        static StructDescriptor AppendVFXInterpolator(StructDescriptor interpolator, VFXContext context, VFXContextCompiledData contextData)
        {
            var fields = interpolator.fields.ToList();

            var expressionToName = context.GetData().GetAttributes().ToDictionary(o => new VFXAttributeExpression(o.attrib) as VFXExpression, o => (new VFXAttributeExpression(o.attrib)).GetCodeString(null));
            expressionToName = expressionToName.Union(contextData.uniformMapper.expressionToCode).ToDictionary(s => s.Key, s => s.Value);

            var mainParameters = contextData.gpuMapper.CollectExpression(-1).ToArray();

            int normalSemanticIndex = 0;

            // Warning/TODO: FragmentParameters are created from the ShaderGraphVfxAsset.
            // We may ultimately need to move this handling of VFX Interpolators + SurfaceDescriptionFunction function signature directly into the SG Generator (since it knows about the exposed properties).
            foreach (string fragmentParameter in context.fragmentParameters)
            {
                var filteredNamedExpression = mainParameters.FirstOrDefault(o => fragmentParameter == o.name &&
                    !(expressionToName.ContainsKey(o.exp) && expressionToName[o.exp] == o.name)); // if parameter already in the global scope, there's nothing to do

                if (filteredNamedExpression.exp != null)
                {
                    var type = VFXExpression.TypeToType(filteredNamedExpression.exp.valueType);

                    if (!kVFXShaderValueTypeyMap.TryGetValue(type, out var shaderValueType))
                        continue;

                    // TODO: NoInterpolation only for non-strips.
                    var interpolationModifier = InterpolationModifier.NoInterpolation;

                    fields.Add(new FieldDescriptor(HDStructFields.VaryingsMeshToPS.name, filteredNamedExpression.name, "", shaderValueType, $"NORMAL{normalSemanticIndex++}", "", StructFieldOptions.Static, interpolationModifier));
                }
            }

            interpolator.fields = fields.ToArray();
            return interpolator;
        }

        enum VFXAttributeType
        {
            Current,
            Source
        }

        private static string[] kVFXAttributeStructNames =
        {
            "Attributes",
            "SourceAttributes"
        };

        static void GenerateVFXAdditionalCommands(VFXContext context, VFXContextCompiledData contextData,
            out AdditionalCommandDescriptor loadAttributeDescriptor,
            out AdditionalCommandDescriptor blockFunctionDescriptor,
            out AdditionalCommandDescriptor blockCallFunctionDescriptor,
            out AdditionalCommandDescriptor interpolantsGenerationDescriptor,
            out AdditionalCommandDescriptor buildVFXFragInputs)
        {
            // Load Attributes
            loadAttributeDescriptor = new AdditionalCommandDescriptor("VFXLoadAttribute", VFXCodeGenerator.GenerateLoadAttribute(".", context).ToString());

            // Graph Blocks
            VFXCodeGenerator.BuildContextBlocks(context, contextData, out var blockFunction, out var blockCallFunction);

            blockFunctionDescriptor = new AdditionalCommandDescriptor("VFXGeneratedBlockFunction", blockFunction);
            blockCallFunctionDescriptor = new AdditionalCommandDescriptor("VFXProcessBlocks", blockCallFunction);

            // Interpolator
            VFXCodeGenerator.BuildInterpolatorBlocks(context, contextData, out var interpolatorsGeneration);

            interpolantsGenerationDescriptor = new AdditionalCommandDescriptor("VFXInterpolantsGeneration", interpolatorsGeneration);

            // Frag Inputs - Only VFX will know if frag inputs come from interpolator or the CBuffer.
            VFXCodeGenerator.BuildFragInputsGeneration(context, contextData, out var buildFragInputsGeneration);
            buildVFXFragInputs = new AdditionalCommandDescriptor("VFXSetFragInputs", buildFragInputsGeneration);
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
                var afd = VFXAttributeToFieldDescriptor(attribute);
                attributeFieldDescriptors.Add(afd);
            }

            return new StructDescriptor
            {
                name = kVFXAttributeStructNames[(int)attributeType],
                fields = attributeFieldDescriptors.ToArray()
            };
        }

        // See: VFXShaderWriter.TypeToUniformCode
        // TODO: Collapse these two maps into one
        static readonly Dictionary<Type, Type> kVFXShaderPropertyMap = new Dictionary<Type, Type>
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

        static readonly Dictionary<Type, ShaderValueType> kVFXShaderValueTypeyMap = new Dictionary<Type, ShaderValueType>
        {
            { typeof(float),     ShaderValueType.Float   },
            { typeof(Vector2),   ShaderValueType.Float2  },
            { typeof(Vector3),   ShaderValueType.Float3  },
            { typeof(Vector4),   ShaderValueType.Float4  },
            { typeof(int),       ShaderValueType.Integer },
            { typeof(uint),      ShaderValueType.Uint    },
            { typeof(Matrix4x4), ShaderValueType.Matrix4 },
            { typeof(bool),      ShaderValueType.Boolean },
        };

        static FieldDescriptor VFXAttributeToFieldDescriptor(VFXAttribute attribute)
        {
            var type = VFXExpression.TypeToType(attribute.type);

            if (!kVFXShaderValueTypeyMap.TryGetValue(type, out var shaderValueType))
                return null;

            return new FieldDescriptor("Attributes", attribute.name, "", shaderValueType);
        }

        static AbstractShaderProperty VFXExpressionToShaderProperty(VFXExpression expression, string name)
        {
            var type = VFXExpression.TypeToType(expression.valueType);

            if (!kVFXShaderPropertyMap.TryGetValue(type, out var shaderPropertyType))
                return null;

            // Must flag for non public here since all shader property constructors are internal.
            var property =  (AbstractShaderProperty)Activator.CreateInstance(shaderPropertyType, true);

            property.overrideReferenceName   = name;
            property.overrideHLSLDeclaration = true;
            property.hlslDeclarationOverride = HLSLDeclaration.VFX;

            return property;
        }

        static void CollectVFXShaderProperties(PropertyCollector collector, VFXContextCompiledData contextData)
        {
            // See: VFXShaderWriter.WriteCBuffer
            // TODO: It may just be better to replace this with another AdditionalCommand and let VFX do it, since it also handles padding.
            var mapper = contextData.uniformMapper;
            var uniformValues = mapper.uniforms
                .Where(e => !e.IsAny(VFXExpression.Flags.Constant | VFXExpression.Flags.InvalidOnCPU)) // Filter out constant expressions
                .OrderByDescending(e => VFXValue.TypeToSize(e.valueType));

            var uniformBlocks = new List<List<VFXExpression>>();
            foreach (var value in uniformValues)
            {
                var block = uniformBlocks.FirstOrDefault(b => b.Sum(e => VFXValue.TypeToSize(e.valueType)) + VFXValue.TypeToSize(value.valueType) <= 4);
                if (block != null)
                    block.Add(value);
                else
                    uniformBlocks.Add(new List<VFXExpression>() { value });
            }

            foreach (var block in uniformBlocks)
            {
                foreach (var value in block)
                {
                    string name = mapper.GetName(value);

                    //Reserved unity variable name (could be filled manually see : VFXCameraUpdate)
                    if (name.StartsWith("unity_"))
                        continue;

                    var property = VFXExpressionToShaderProperty(value, name);
                    collector.AddShaderProperty(property);
                }
            }
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            base.CollectShaderProperties(collector, generationMode);

            CollectVFXShaderProperties(collector, m_ContextData);
        }

        public bool TryConfigureVFX(VFXContext context, VFXContextCompiledData contextData)
        {
            m_Context = context as VFXAbstractParticleHDRPLitOutput;
            m_ContextData = contextData;
            return true;
        }
    }
}
