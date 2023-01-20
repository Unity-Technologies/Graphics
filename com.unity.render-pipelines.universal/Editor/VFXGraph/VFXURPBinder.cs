#if HAS_VFX_GRAPH
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph;
using Unity.Rendering.Universal;
using UnityEditor.Rendering.Universal;
using UnityEditor.Rendering.Universal.ShaderGraph;

namespace UnityEditor.VFX.URP
{
    class VFXURPBinder : VFXSRPBinder
    {
        public override string templatePath { get { return "Packages/com.unity.render-pipelines.universal/Editor/VFXGraph/Shaders"; } }
        public override string runtimePath { get { return "Packages/com.unity.render-pipelines.universal/Runtime/VFXGraph/Shaders"; } }
        public override string SRPAssetTypeStr { get { return "UniversalRenderPipelineAsset"; } }
        public override Type SRPOutputDataType { get { return null; } } // null by now but use VFXURPSubOutput when there is a need to store URP specific data

        public override void SetupMaterial(Material material, bool hasMotionVector = false, bool hasShadowCasting = false, ShaderGraphVfxAsset shaderGraph = null)
        {
            ShaderUtils.UpdateMaterial(material, ShaderUtils.MaterialUpdateType.ModifiedShader, shaderGraph);
            material.SetShaderPassEnabled("MotionVectors", hasMotionVector);
            material.SetShaderPassEnabled("ShadowCaster", hasShadowCasting);
        }

        public override bool TryGetQueueOffset(ShaderGraphVfxAsset shaderGraph, VFXMaterialSerializedSettings materialSettings, out int queueOffset)
        {
            //N.B.: Queue offset is always overridable in URP
            queueOffset = 0;
            if (materialSettings.HasProperty(Rendering.Universal.Property.QueueOffset))
            {
                queueOffset = (int)materialSettings.GetFloat(Rendering.Universal.Property.QueueOffset);
                return true;
            }
            return false;
        }

        public override bool TryGetCastShadowFromMaterial(ShaderGraphVfxAsset shaderGraph, VFXMaterialSerializedSettings materialSettings, out bool castShadow)
        {
            castShadow = false;

            var path = AssetDatabase.GetAssetPath(shaderGraph);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader.TryGetMetadataOfType<UniversalMetadata>(out var metaData) && !metaData.allowMaterialOverride)
            {
                castShadow = metaData.castShadows;
                return true;
            }
            else
            {
                if (materialSettings.HasProperty(Property.CastShadows))
                {
                    castShadow = materialSettings.GetFloat(Property.CastShadows) != 0.0f;
                    return true;
                }
            }
            return false;
        }

        public override VFXAbstractRenderedOutput.BlendMode GetBlendModeFromMaterial(ShaderGraphVfxAsset shaderGraph, VFXMaterialSerializedSettings materialSettings)
        {
            //N.B: About BlendMode multiply, it isn't officially supported by the VFX
            //but when using generatesWithShaderGraph, the shaderGraph generates the appropriate blendState.

            var vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Opaque;
            var path = AssetDatabase.GetAssetPath(shaderGraph);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader.TryGetMetadataOfType<UniversalMetadata>(out var metaData) && !metaData.allowMaterialOverride)
            {
                if (metaData.surfaceType == SurfaceType.Transparent)
                {
                    switch (metaData.alphaMode)
                    {
                        case AlphaMode.Alpha: vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Alpha; break;
                        case AlphaMode.Premultiply: vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.AlphaPremultiplied; break;
                        case AlphaMode.Additive: vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Additive; break;
                        case AlphaMode.Multiply: vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Additive; break;
                    }
                }
            }
            else
            {
                if (materialSettings.HasProperty(Property.SurfaceType))
                {
                    var surfaceType = (BaseShaderGUI.SurfaceType)materialSettings.GetFloat(Property.SurfaceType);
                    if (surfaceType == BaseShaderGUI.SurfaceType.Transparent)
                    {
                        var blendMode = (BaseShaderGUI.BlendMode)materialSettings.GetFloat(Property.BlendMode);
                        switch (blendMode)
                        {
                            case BaseShaderGUI.BlendMode.Alpha: vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Alpha; break;
                            case BaseShaderGUI.BlendMode.Premultiply: vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.AlphaPremultiplied; break;
                            case BaseShaderGUI.BlendMode.Additive: vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Additive; break;
                            case BaseShaderGUI.BlendMode.Multiply: vfxBlendMode = VFXAbstractRenderedOutput.BlendMode.Additive; break;
                        }
                    }
                }
            }
            return vfxBlendMode;
        }

        public override string GetShaderName(ShaderGraphVfxAsset shaderGraph)
        {
            var path = AssetDatabase.GetAssetPath(shaderGraph);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader.TryGetMetadataOfType<UniversalMetadata>(out var metaData))
            {
                switch (metaData.shaderID)
                {
                    case ShaderUtils.ShaderID.SG_Unlit:
                    case ShaderUtils.ShaderID.SG_SpriteUnlit: return "Unlit";
                    case ShaderUtils.ShaderID.SG_Lit:
                    case ShaderUtils.ShaderID.SG_SpriteLit:
                    case ShaderUtils.ShaderID.SG_SpriteCustomLit: return "Lit";
                }
            }
            return string.Empty;
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
            new FieldDependency(Fields.ObjectToWorld, StructFields.SurfaceDescriptionInputs.elementToWorld),

            // NormalDropOffOS requires worldToElement (see _NORMAL_DROPOFF_OS condition calling TransformObjectToWorldNormal which uses world inverse transpose)
            new FieldDependency(UniversalFields.NormalDropOffOS, StructFields.SurfaceDescriptionInputs.worldToElement),
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
                new FieldDescriptor(StructFields.Attributes.name, "vertexID", "ATTRIBUTES_NEED_VERTEXID", ShaderValueType.Uint, "VERTEXID_SEMANTIC")
            }
        };

        static StructDescriptor AppendVFXInterpolator(StructDescriptor interpolator, VFXContext context, VFXTaskCompiledData contextData)
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

        static IEnumerable<FieldDescriptor> GenerateSurfaceDescriptionInput(VFXContext context, VFXTaskCompiledData contextData)
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

        public override ShaderGraphBinder GetShaderGraphDescriptor(VFXContext context, VFXTaskCompiledData data)
        {
            var surfaceDescriptionInputWithVFX = new StructDescriptor
            {
                name = StructFields.SurfaceDescriptionInputs.name,
                populateWithCustomInterpolators = true,
                fields = GenerateSurfaceDescriptionInput(context, data).ToArray()
            };

            return new ShaderGraphBinder()
            {
                structs = new StructCollection
                {
                    AttributesMeshVFX, // TODO: Could probably re-use the original HD Attributes Mesh and just ensure Instancing enabled.
                    AppendVFXInterpolator(UniversalStructs.Varyings, context, data),
                    surfaceDescriptionInputWithVFX, //N.B. FragInput is in SurfaceDescriptionInputs
                    Structs.VertexDescriptionInputs,
                },

                fieldDependencies = ElementSpaceDependencies,
                pragmasReplacement = new (PragmaDescriptor, PragmaDescriptor)[]
                {
                    ( Pragma.Vertex("vert"), Pragma.Vertex("VertVFX") ),

                    //Minimal target of VFX is always Target45 (2.0 is used with GLCore)
                    ( Pragma.Target(ShaderModel.Target20), Pragma.Target(ShaderModel.Target45) ),
                    ( Pragma.Target(ShaderModel.Target30), Pragma.Target(ShaderModel.Target45) ),
                    ( Pragma.Target(ShaderModel.Target35), Pragma.Target(ShaderModel.Target45) ),
                    ( Pragma.Target(ShaderModel.Target40), Pragma.Target(ShaderModel.Target45) ),

                    //Irrelevant general multicompile instancing (VFX will append them when needed)
                    ( Pragma.MultiCompileInstancing, ShaderGraphBinder.kPragmaDescriptorNone),
                    ( Pragma.InstancingOptions(InstancingOptions.RenderingLayer), ShaderGraphBinder.kPragmaDescriptorNone ),
                    ( Pragma.InstancingOptions(InstancingOptions.NoLightProbe), ShaderGraphBinder.kPragmaDescriptorNone ),
                    ( Pragma.InstancingOptions(InstancingOptions.NoLodFade), ShaderGraphBinder.kPragmaDescriptorNone ),
                },
                useFragInputs = false
            };
        }
    }

    class VFXLWRPBinder : VFXURPBinder
    {
        public override string SRPAssetTypeStr { get { return "LightweightRenderPipelineAsset"; } }
    }
}
#endif
