//#if HAS_VFX_GRAPH
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.VFX;
using UnityEditor.Rendering.Universal.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    //TODOPAUL : This is a copy/past from VFXHDRPSubTarget
    [InitializeOnLoad]
    static class VFXUniversalSubTarget
    {
        internal const string Inspector = "Rendering.HighDefinition.VFXShaderGraphGUI";

        public static readonly DependencyCollection ElementSpaceDependencies = new DependencyCollection
        {
            // Interpolator dependency.
            new FieldDependency(StructFields.SurfaceDescriptionInputs.worldToElement, StructFields.Varyings.worldToElement0),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.worldToElement, StructFields.Varyings.worldToElement1),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.worldToElement, StructFields.Varyings.worldToElement2),

            new FieldDependency(StructFields.SurfaceDescriptionInputs.elementToWorld, StructFields.Varyings.elementToWorld0),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.elementToWorld, StructFields.Varyings.elementToWorld1),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.elementToWorld, StructFields.Varyings.elementToWorld2),

            // Note: Normal is dependent on elementToWorld for inverse transpose multiplication.
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceNormal,             StructFields.SurfaceDescriptionInputs.elementToWorld),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceTangent,            StructFields.SurfaceDescriptionInputs.worldToElement),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceBiTangent,          StructFields.SurfaceDescriptionInputs.worldToElement),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpacePosition,           StructFields.SurfaceDescriptionInputs.worldToElement),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceViewDirection,      StructFields.SurfaceDescriptionInputs.worldToElement),

            new FieldDependency(Fields.WorldToObject, StructFields.SurfaceDescriptionInputs.worldToElement),
            new FieldDependency(Fields.ObjectToWorld, StructFields.SurfaceDescriptionInputs.elementToWorld)
        };

        static VFXUniversalSubTarget()
        {
            VFXSubTarget.OnPostProcessSubShader += PostProcessSubShader;
        }

        // Configures an HDRP Subshader with a VFX context.
        private static SubShaderDescriptor PostProcessSubShader(SubShaderDescriptor subShaderDescriptor, VFXContext context, VFXContextCompiledData data)
        {
            var attributesStruct = GenerateVFXAttributesStruct(context, VFXAttributeType.Current);
            var sourceAttributesStruct = GenerateVFXAttributesStruct(context, VFXAttributeType.Source);

            // Defer to VFX to generate various misc. code-gen that ShaderGraph currently can't handle.
            // We use the AdditionalCommand descriptors for ShaderGraph generation to splice these in.
            // ( i.e. VFX Graph Block Function declaration + calling, Property Mapping, etc. )
            GenerateVFXAdditionalCommands(
                context, data,
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
                out var vertexPropertiesGenerationDescriptor,
                out var vertexPropertiesAssignDescriptor
            );

            //TODOPAUL Check this naming
            // Omit MV or Shadow Pass if disabled on the context.
            var filteredPasses = subShaderDescriptor.passes.AsEnumerable();

            var outputContext = (VFXAbstractParticleOutput)context;
            if (!outputContext.hasMotionVector)
                filteredPasses = filteredPasses.Where(o => o.descriptor.lightMode != "MotionVectors");

            if (!outputContext.hasShadowCasting)
                filteredPasses = filteredPasses.Where(o => o.descriptor.lightMode != "ShadowCaster");

            var passes = filteredPasses.ToArray();

            PassCollection vfxPasses = new PassCollection();
            for (int i = 0; i < passes.Length; i++)
            {
                var passDescriptor = passes[i].descriptor;

                var surfaceField = new List<FieldDescriptor>();

                var surfaceDescriptionInputWithVFX = new StructDescriptor
                {
                    name = StructFields.SurfaceDescriptionInputs.name,
                    fields = GenerateFragInputs_Special_URP(context, data).ToArray()
                };

                // Warning: Touching the structs field may require to manually append the default structs here.
                passDescriptor.structs = new StructCollection
                {
                    AttributesMeshVFX, // TODO: Could probably re-use the original HD Attributes Mesh and just ensure Instancing enabled.
                    AppendVFXInterpolator(UniversalStructs.Varyings, context, data),
                    surfaceDescriptionInputWithVFX, //TODOPAUL : URP specific FragInput == SurfaceDescriptionInputs
                    Structs.VertexDescriptionInputs,
                    attributesStruct,
                    sourceAttributesStruct
                };

                passDescriptor.pragmas = new PragmaCollection
                {
                    ModifyVertexEntry(passDescriptor.pragmas),
                    Pragma.MultiCompileInstancing
                };

                passDescriptor.additionalCommands = new AdditionalCommandCollection
                {
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
                    vertexPropertiesGenerationDescriptor,
                    vertexPropertiesAssignDescriptor,
                    GenerateFragInputs(context, data)
                };

                //URP Specific : There isn't FragInput we are forced to use customInterpolators : Doesn't work as I though
                /*passDescriptor.customInterpolators = new CustomInterpSubGen.Collection()
                {
                    new CustomInterpSubGen.Collection()
                    {
                        CustomInterpSubGen.Descriptor.MakeStruct("FragInputsVFXTOTO", "vfx")
                    }
                };*/
                vfxPasses.Add(passDescriptor, passes[i].fieldConditions);
            }

            subShaderDescriptor.passes = vfxPasses;

            return subShaderDescriptor;
        }

        private static readonly GraphicsDeviceType[] s_RequiresModifiedVertexEntry = new GraphicsDeviceType[]
        {
            GraphicsDeviceType.Direct3D12,
            GraphicsDeviceType.Vulkan
        };

        static PragmaCollection ModifyVertexEntry(PragmaCollection pragmas)
        {
            // Only modify the entry for the required APIs.
            var graphicsAPI = PlayerSettings.GetGraphicsAPIs(EditorUserBuildSettings.activeBuildTarget)[0];
            if (s_RequiresModifiedVertexEntry.All(o => o != graphicsAPI))
                return pragmas;

            // Replace the default vertex shader entry with one defined by VFX.
            // NOTE: Assumes they are named "Vert" for all shader passes, which they are.
            const string k_CoreBasicVertex = "#pragma vertex Vert";

            var pragmaVFX = new PragmaCollection();

            foreach (var pragma in pragmas)
            {
                if (pragma.value != k_CoreBasicVertex)
                    pragmaVFX.Add(pragma.descriptor);
                else
                    pragmaVFX.Add(Pragma.Vertex("VertVFX"));
            }

            return pragmaVFX;
        }

        static StructDescriptor AttributesMeshVFX = new StructDescriptor()
        {
            name = StructFields.Attributes.name,
            packFields = false,
            fields = new FieldDescriptor[]
            {
                StructFields.Attributes.positionOS,
                StructFields.Attributes.normalOS,
                StructFields.Attributes.tangentOS,
                StructFields.Attributes.uv0,
                StructFields.Attributes.uv1,
                StructFields.Attributes.uv2,
                StructFields.Attributes.uv3,
                StructFields.Attributes.color,

                // InstanceID without the Preprocessor.
                new FieldDescriptor(StructFields.Attributes.name, "instanceID", "", ShaderValueType.Uint, "INSTANCEID_SEMANTIC"),

                StructFields.Attributes.weights,
                StructFields.Attributes.indices,

                // VertexID without the Preprocessor.
                new FieldDescriptor(StructFields.Attributes.name, "vertexID", "ATTRIBUTES_NEED_VERTEXID", ShaderValueType.Uint, "SV_VertexID")
            }
        };

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

                    if (!kVFXShaderValueTypeMap.TryGetValue(type, out var shaderValueType))
                        continue;

                    // TODO: NoInterpolation only for non-strips.
                    fields.Add(new FieldDescriptor(UniversalStructs.Varyings.name, filteredNamedExpression.name, "", shaderValueType, $"NORMAL{normalSemanticIndex++}", "", StructFieldOptions.Static, "nointerpolation"));
                }
            }

            fields.Add(StructFields.Varyings.worldToElement0);
            fields.Add(StructFields.Varyings.worldToElement1);
            fields.Add(StructFields.Varyings.worldToElement2);

            fields.Add(StructFields.Varyings.elementToWorld0);
            fields.Add(StructFields.Varyings.elementToWorld1);
            fields.Add(StructFields.Varyings.elementToWorld2);

            interpolator.fields = fields.ToArray();
            return interpolator;
        }

        enum VFXAttributeType
        {
            Current,
            Source
        }

        static string[] kVFXAttributeStructNames =
        {
            "InternalAttributesElement",
            "InternalSourceAttributesElement"
        };

        static void GenerateVFXAdditionalCommands(VFXContext context, VFXContextCompiledData contextData,
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
            out AdditionalCommandDescriptor vertexPropertiesGenerationDescriptor,
            out AdditionalCommandDescriptor vertexPropertiesAssignDescriptor)
        {
            // TODO: Clean all of this up. Currently just an adapter between VFX Code Gen + SG Code Gen and *everything* has been stuffed here.

            // Load Attributes
            loadAttributeDescriptor = new AdditionalCommandDescriptor("VFXLoadAttribute", VFXCodeGenerator.GenerateLoadAttribute(".", context).ToString());

            // Graph Blocks
            VFXCodeGenerator.BuildContextBlocks(context, contextData, out var blockFunction, out var blockCallFunction);

            blockFunctionDescriptor = new AdditionalCommandDescriptor("VFXGeneratedBlockFunction", blockFunction);
            blockCallFunctionDescriptor = new AdditionalCommandDescriptor("VFXProcessBlocks", blockCallFunction);

            // Vertex Input
            VFXCodeGenerator.BuildVertexProperties(context, contextData, out var vertexPropertiesGeneration);
            vertexPropertiesGenerationDescriptor = new AdditionalCommandDescriptor("VFXVertexPropertiesGeneration", vertexPropertiesGeneration);

            VFXCodeGenerator.BuildVertexPropertiesAssign(context, contextData, out var vertexPropertiesAssign);
            vertexPropertiesAssignDescriptor = new AdditionalCommandDescriptor("VFXVertexPropertiesAssign", vertexPropertiesAssign);

            // Interpolator
            VFXCodeGenerator.BuildInterpolatorBlocks(context, contextData, out var interpolatorsGeneration);
            interpolantsGenerationDescriptor = new AdditionalCommandDescriptor("VFXInterpolantsGeneration", interpolatorsGeneration);

            // Frag Inputs - Only VFX will know if frag inputs come from interpolator or the CBuffer.
            VFXCodeGenerator.BuildFragInputsGeneration(context, contextData, out var buildFragInputsGeneration);
            buildVFXFragInputsDescriptor = new AdditionalCommandDescriptor("VFXSetFragInputs", buildFragInputsGeneration);

            VFXCodeGenerator.BuildPixelPropertiesAssign(context, contextData, out var pixelPropertiesAssign);
            pixelPropertiesAssignDescriptor = new AdditionalCommandDescriptor("VFXPixelPropertiesAssign", pixelPropertiesAssign);

            // Define coordinate space
            var defineSpaceDescriptorContent = string.Empty;
            if (context.GetData() is ISpaceable)
            {
                var spaceable = context.GetData() as ISpaceable;
                defineSpaceDescriptorContent =
                    $"#define {(spaceable.space == VFXCoordinateSpace.World ? "VFX_WORLD_SPACE" : "VFX_LOCAL_SPACE")} 1";
            }
            defineSpaceDescriptor = new AdditionalCommandDescriptor("VFXDefineSpace", defineSpaceDescriptorContent);

            // Parameter Cbuffer
            VFXCodeGenerator.BuildParameterBuffer(contextData, out var parameterBuffer);
            parameterBufferDescriptor = new AdditionalCommandDescriptor("VFXParameterBuffer", parameterBuffer);

            // Defines & Headers - Not all are necessary, however some important ones are mixed in like indirect draw, strips, flipbook, particle strip info...
            ShaderStringBuilder additionalDefines = new ShaderStringBuilder();
            // TODO: Need to add defines for current/source usage (i.e. scale).

            var allCurrentAttributes = context.GetData().GetAttributes().Where(a =>
                (context.GetData().IsCurrentAttributeUsed(a.attrib, context)) ||
                (context.contextType == VFXContextType.Init && context.GetData().IsAttributeStored(a.attrib))); // In init, needs to declare all stored attributes for intialization

            var allSourceAttributes = context.GetData().GetAttributes().Where(a => (context.GetData().IsSourceAttributeUsed(a.attrib, context)));

            foreach (var attribute in allCurrentAttributes)
                additionalDefines.AppendLine("#define VFX_USE_{0}_{1} 1", attribute.attrib.name.ToUpper(CultureInfo.InvariantCulture), "CURRENT");
            foreach (var attribute in allSourceAttributes)
                additionalDefines.AppendLine("#define VFX_USE_{0}_{1} 1", attribute.attrib.name.ToUpper(CultureInfo.InvariantCulture), "SOURCE");
            foreach (var header in context.additionalDataHeaders)
                additionalDefines.AppendLine(header);
            foreach (var define in context.additionalDefines)
                additionalDefines.AppendLine($"#define {define} 1");
            additionalDefinesDescriptor = new AdditionalCommandDescriptor("VFXDefines", additionalDefines.ToString());

            // Load Position Attribute
            loadPositionAttributeDescriptor = new AdditionalCommandDescriptor("VFXLoadPositionAttribute", VFXCodeGenerator.GenerateLoadAttribute("position", context).ToString().ToString());

            // Load Crop Factor Attribute
            var mainParameters = contextData.gpuMapper.CollectExpression(-1).ToArray();
            var expressionToName = context.GetData().GetAttributes().ToDictionary(o => new VFXAttributeExpression(o.attrib) as VFXExpression, o => (new VFXAttributeExpression(o.attrib)).GetCodeString(null));
            expressionToName = expressionToName.Union(contextData.uniformMapper.expressionToCode).ToDictionary(s => s.Key, s => s.Value);
            loadCropFactorAttributesDescriptor = new AdditionalCommandDescriptor("VFXLoadCropFactorParameter", VFXCodeGenerator.GenerateLoadParameter("cropFactor", mainParameters, expressionToName).ToString().ToString());
            loadTexcoordAttributesDescriptor = new AdditionalCommandDescriptor("VFXLoadTexcoordParameter", VFXCodeGenerator.GenerateLoadParameter("texCoord", mainParameters, expressionToName).ToString().ToString());
        }

        static AdditionalCommandDescriptor GenerateFragInputs(VFXContext context, VFXContextCompiledData contextData)
        {
            var builder = new ShaderStringBuilder();

            // VFX Material Properties
            var expressionToName = context.GetData().GetAttributes().ToDictionary(o => new VFXAttributeExpression(o.attrib) as VFXExpression, o => (new VFXAttributeExpression(o.attrib)).GetCodeString(null));
            expressionToName = expressionToName.Union(contextData.uniformMapper.expressionToCode).ToDictionary(s => s.Key, s => s.Value);

            var mainParameters = contextData.gpuMapper.CollectExpression(-1).ToArray();

            foreach (string fragmentParameter in context.fragmentParameters)
            {
                var filteredNamedExpression = mainParameters.FirstOrDefault(o => fragmentParameter == o.name);

                if (filteredNamedExpression.exp != null)
                {
                    var type = VFXExpression.TypeToType(filteredNamedExpression.exp.valueType);

                    if (!kVFXShaderValueTypeMap.TryGetValue(type, out var shaderValueType))
                        continue;

                    builder.AppendLine($"{shaderValueType.ToShaderString("float")} {filteredNamedExpression.name};");
                }
            }

            return new AdditionalCommandDescriptor("FragInputsVFX", builder.ToString());
        }

        //Special Implementation for URP TODOPAUL (clean)
        static IEnumerable<FieldDescriptor> GenerateFragInputs_Special_URP(VFXContext context, VFXContextCompiledData contextData)
        {
            // VFX Material Properties
            var expressionToName = context.GetData().GetAttributes().ToDictionary(o => new VFXAttributeExpression(o.attrib) as VFXExpression, o => (new VFXAttributeExpression(o.attrib)).GetCodeString(null));
            expressionToName = expressionToName.Union(contextData.uniformMapper.expressionToCode).ToDictionary(s => s.Key, s => s.Value);

            var mainParameters = contextData.gpuMapper.CollectExpression(-1).ToArray();

            var alreadyAddedField = new HashSet<string>();
            foreach (string fragmentParameter in context.fragmentParameters)
            {
                var filteredNamedExpression = mainParameters.FirstOrDefault(o => fragmentParameter == o.name);

                if (filteredNamedExpression.exp != null)
                {
                    var type = VFXExpression.TypeToType(filteredNamedExpression.exp.valueType);

                    if (!kVFXShaderValueTypeMap.TryGetValue(type, out var shaderValueType))
                        continue;

                    alreadyAddedField.Add(filteredNamedExpression.name);
                    yield return new FieldDescriptor(StructFields.SurfaceDescriptionInputs.name, filteredNamedExpression.name, "", shaderValueType);
                }
            }

            //Append everything from common SurfaceDescriptionInputs
            foreach (var field in Structs.SurfaceDescriptionInputs.fields)
            {
                if (!alreadyAddedField.Contains(field.name))
                    yield return field;
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

        static readonly Dictionary<Type, ShaderValueType> kVFXShaderValueTypeMap = new Dictionary<Type, ShaderValueType>
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

        static FieldDescriptor VFXAttributeToFieldDescriptor(VFXAttribute attribute)
        {
            var type = VFXExpression.TypeToType(attribute.type);

            if (!kVFXShaderValueTypeMap.TryGetValue(type, out var shaderValueType))
                return null;

            return new FieldDescriptor("Attributes", attribute.name, "", shaderValueType);
        }
    }
}
//#endif
