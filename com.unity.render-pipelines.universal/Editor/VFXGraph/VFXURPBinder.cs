#if HAS_VFX_GRAPH
using System;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph;
using System.Linq;
using UnityEditor.Rendering.Universal.ShaderGraph;
using System.Collections.Generic;

namespace UnityEditor.VFX.URP
{
    class VFXURPBinder : VFXSRPBinder
    {
        public override string templatePath { get { return "Packages/com.unity.render-pipelines.universal/Editor/VFXGraph/Shaders"; } }
        public override string runtimePath { get { return "Packages/com.unity.render-pipelines.universal/Runtime/VFXGraph/Shaders"; } }
        public override string SRPAssetTypeStr { get { return "UniversalRenderPipelineAsset"; } }
        public override Type SRPOutputDataType { get { return null; } } // null by now but use VFXURPSubOutput when there is a need to store URP specific data

        public override bool IsGraphDataValid(ShaderGraph.GraphData graph)
        {
            //TODOPAUL : Probably filter todo
            return true;
        }

        public override void SetupMaterial(Material mat, bool hasMotionVector = false, bool hasShadowCasting = false, ShaderGraphVfxAsset shaderGraph = null)
        {
            //TODOPAUL (N.B. conflict on this function definition incoming ^)
            try
            {
                if (shaderGraph != null)
                {
                    //TODOPAUL : retrieve id & switch among target here
                    Rendering.Universal.ShaderGUI.UnlitShader.SetMaterialKeywords(mat);

                    // Configure HDRP Shadow + MV
                    //mat.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, hasMotionVector);
                    //mat.SetShaderPassEnabled(HDShaderPassNames.s_ShadowCasterStr, hasShadowCasting);
                }
                else
                {
                    //TODOPAUL : Something todo ?
                    //HDShaderUtils.ResetMaterialKeywords(mat);
                }
            }
            catch (ArgumentException) // TODOPAUL : is it something expected ?
            {}
        }

        public override VFXAbstractRenderedOutput.BlendMode GetBlendModeFromMaterial(VFXMaterialSerializedSettings materialSettings)
        {
            var vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Opaque;
            if (materialSettings.HasProperty("_Surface"))
            {
                var surfaceType = (BaseShaderGUI.SurfaceType)materialSettings.GetFloat("_Surface");
                if (surfaceType == BaseShaderGUI.SurfaceType.Transparent)
                {
                    BaseShaderGUI.BlendMode blendMode = (BaseShaderGUI.BlendMode)materialSettings.GetFloat("_Blend");
                    switch (blendMode)
                    {
                        case BaseShaderGUI.BlendMode.Alpha: vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Alpha; break;
                        case BaseShaderGUI.BlendMode.Premultiply: vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.AlphaPremultiplied; break;
                        case BaseShaderGUI.BlendMode.Additive: vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Additive; break;
                        case BaseShaderGUI.BlendMode.Multiply: vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Additive; break; //TODOPAUL : Should we add it ?
                    }
                }
            }
            return vfxBlendMode;
        }

        static readonly DependencyCollection ElementSpaceDependencies = new DependencyCollection
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

        static readonly StructDescriptor AttributesMeshVFX = new StructDescriptor()
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

        static StructDescriptor AppendVFXInterpolator(StructDescriptor interpolator, VFXContext context, VFXContextCompiledData contextData)
        {
            var fields = interpolator.fields.ToList();

            var expressionToName = context.GetData().GetAttributes().ToDictionary(o => new VFXAttributeExpression(o.attrib) as VFXExpression, o => (new VFXAttributeExpression(o.attrib)).GetCodeString(null));
            expressionToName = expressionToName.Union(contextData.uniformMapper.expressionToCode).ToDictionary(s => s.Key, s => s.Value);

            var mainParameters = contextData.gpuMapper.CollectExpression(-1).ToArray();

            // Warning/TODO: FragmentParameters are created from the ShaderGraphVfxAsset.
            // We may ultimately need to move this handling of VFX Interpolators + SurfaceDescriptionFunction function signature directly into the SG Generator (since it knows about the exposed properties).
            foreach (string fragmentParameter in context.fragmentParameters)
            {
                var filteredNamedExpression = mainParameters.FirstOrDefault(o => fragmentParameter == o.name &&
                    !(expressionToName.ContainsKey(o.exp) && expressionToName[o.exp] == o.name)); // if parameter already in the global scope, there's nothing to do

                if (filteredNamedExpression.exp != null)
                {
                    var type = VFXExpression.TypeToType(filteredNamedExpression.exp.valueType);

                    if (!VFXSubTarget.kVFXShaderValueTypeMap.TryGetValue(type, out var shaderValueType))
                        continue;

                    // TODO: NoInterpolation only for non-strips.
                    fields.Add(new FieldDescriptor(UniversalStructs.Varyings.name, filteredNamedExpression.name, "", shaderValueType, subscriptOptions: StructFieldOptions.Static, interpolation: "nointerpolation"));
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

                    if (!VFXSubTarget.kVFXShaderValueTypeMap.TryGetValue(type, out var shaderValueType))
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

        public override ShaderGraphBinder GetShaderGraphDescriptor(VFXContext context, VFXContextCompiledData data)
        {
            var surfaceDescriptionInputWithVFX = new StructDescriptor
            {
                name = StructFields.SurfaceDescriptionInputs.name,
                populateWithCustomInterpolators = true,
                fields = GenerateFragInputs_Special_URP(context, data).ToArray()
            };

            return new ShaderGraphBinder()
            {
                structs = new StructCollection
                {
                    AttributesMeshVFX, // TODO: Could probably re-use the original HD Attributes Mesh and just ensure Instancing enabled.
                    AppendVFXInterpolator(UniversalStructs.Varyings, context, data),
                    surfaceDescriptionInputWithVFX, //TODOPAUL : URP specific FragInput == SurfaceDescriptionInputs
                    Structs.VertexDescriptionInputs,
                },

                fieldDependencies = ElementSpaceDependencies
            };
        }
    }

    class VFXLWRPBinder : VFXURPBinder
    {
        public override string SRPAssetTypeStr { get { return "LightweightRenderPipelineAsset"; } }
    }
}
#endif
