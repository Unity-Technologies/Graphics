using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering.HighDefinition;
using UnityEditor.Rendering.HighDefinition.ShaderGraph;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.VFX;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using BlendMode = UnityEditor.Rendering.HighDefinition.BlendMode;

using static UnityEngine.Rendering.HighDefinition.HDMaterial;

namespace UnityEditor.VFX.HDRP
{
    class VFXHDRPBinder : VFXSRPBinder
    {
        public override string templatePath { get { return "Packages/com.unity.render-pipelines.high-definition/Editor/VFXGraph/Shaders"; } }
        public override string runtimePath { get { return "Packages/com.unity.render-pipelines.high-definition/Runtime/VFXGraph/Shaders"; } }

        public override string SRPAssetTypeStr { get { return typeof(HDRenderPipelineAsset).Name; } }
        public override Type SRPOutputDataType { get { return typeof(VFXHDRPSubOutput); } }

        public override bool IsShaderVFXCompatible(Shader shader) => shader.TryGetMetadataOfType<HDMetadata>(out var metadata) && metadata.isVFXCompatible;

        public override void SetupMaterial(Material mat, bool hasMotionVector = false, bool hasShadowCasting = false, ShaderGraphVfxAsset shaderGraph = null)
        {
            try
            {
                if (shaderGraph != null)
                {
                    // The following will throw an exception if the given shaderGraph object actually doesn't contain an HDMetaData object.
                    // It thus bypasses the check to see if the shader assigned to the material is a shadergraph: this is necessary because this later check
                    // uses GraphUtil's IsShaderGraphAsset(shader) which check for a shadergraph importer (cf IsShaderGraph(material) which check for a material
                    // tag "ShaderGraphShader").
                    // In our context, IsShaderGraphAsset() will fail even though the ShaderGraphVfxAsset does have an HDMetaData object so we need to bypass the check:
                    HDShaderUtils.ResetMaterialKeywords(mat, assetWithHDMetaData: shaderGraph);

                    // Configure HDRP Shadow + MV
                    mat.SetShaderPassEnabled(HDShaderPassNames.s_MotionVectorsStr, hasMotionVector);
                    mat.SetShaderPassEnabled(HDShaderPassNames.s_ShadowCasterStr, hasShadowCasting);
                }
                else
                    HDShaderUtils.ResetMaterialKeywords(mat);
            }
            catch (ArgumentException) // Silently catch the 'Unknown shader' in case of non HDRP shaders
            { }
        }

        public override bool TryGetQueueOffset(ShaderGraphVfxAsset shaderGraph, VFXMaterialSerializedSettings materialSettings, out int queueOffset)
        {
            queueOffset = 0;
            if (materialSettings.HasProperty(HDMaterialProperties.kTransparentSortPriority))
            {
                queueOffset = (int)materialSettings.GetFloat(HDMaterialProperties.kTransparentSortPriority);
                return true;
            }
            return false;
        }

        public override VFXAbstractRenderedOutput.BlendMode GetBlendModeFromMaterial(ShaderGraphVfxAsset shader, VFXMaterialSerializedSettings materialSettings)
        {
            var blendMode = VFXAbstractRenderedOutput.BlendMode.Opaque;

            if (!materialSettings.HasProperty(HDMaterialProperties.kSurfaceType) ||
                !materialSettings.HasProperty(HDMaterialProperties.kBlendMode))
            {
                return blendMode;
            }

            var surfaceType = materialSettings.GetFloat(HDMaterialProperties.kSurfaceType);
            if (surfaceType == (int)SurfaceType.Transparent)
            {
                switch (materialSettings.GetFloat(HDMaterialProperties.kBlendMode))
                {
                    case (int)BlendMode.Additive:
                        blendMode = VFXAbstractRenderedOutput.BlendMode.Additive;
                        break;
                    case (int)BlendMode.Alpha:
                        blendMode = VFXAbstractRenderedOutput.BlendMode.Alpha;
                        break;
                    case (int)BlendMode.Premultiply:
                        blendMode = VFXAbstractRenderedOutput.BlendMode.AlphaPremultiplied;
                        break;
                }
            }

            return blendMode;
        }

        public override bool GetSupportsMotionVectorPerVertex(ShaderGraphVfxAsset shaderGraph, VFXMaterialSerializedSettings materialSettings)
        {
            var path = AssetDatabase.GetAssetPath(shaderGraph);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader.TryGetMetadataOfType<HDMetadata>(out var metaData))
            {
                if (metaData.hasVertexModificationInMotionVector)
                    return false;
            }
            return true;
        }

        public override bool TransparentMotionVectorEnabled(Material material)
        {
            if (!material.HasProperty(HDMaterialProperties.kSurfaceType) ||
                !material.HasProperty(HDMaterialProperties.kTransparentWritingMotionVec))
            {
                return false;
            }

            var surfaceType = material.GetFloat(HDMaterialProperties.kSurfaceType);

            if (surfaceType == (int)SurfaceType.Transparent)
                return material.GetFloat(HDMaterialProperties.kTransparentWritingMotionVec) == 1f;

            return false;
        }

        public override string GetShaderName(ShaderGraphVfxAsset shaderGraph)
        {
            // Recover the HDRP Shader ids from the VFX Shader Graph.
            (ShaderID shaderID, GUID subTargetGUID) = HDShaderUtils.GetShaderIDsFromHDMetadata(shaderGraph);
            return HDShaderUtils.GetMaterialSubTargetDisplayName(subTargetGUID);
        }

        // List of shader properties that currently are not supported for exposure in VFX shaders (for HDRP).
        private static readonly Dictionary<Type, string> s_UnsupportedHDRPShaderPropertyTypes = new Dictionary<Type, string>()
        {
            { typeof(DiffusionProfileShaderProperty), "Diffusion Profile" },
        };

        public override IEnumerable<KeyValuePair<Type, string>> GetUnsupportedShaderPropertyType()
        {
            return base.GetUnsupportedShaderPropertyType().Concat(s_UnsupportedHDRPShaderPropertyTypes);
        }

        static readonly StructDescriptor AttributesMeshVFX = new StructDescriptor()
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

                // InstanceID without the Preprocessor.
                new FieldDescriptor(HDStructFields.AttributesMesh.name, "instanceID", "", ShaderValueType.Uint, "INSTANCEID_SEMANTIC"),

                HDStructFields.AttributesMesh.weights,
                HDStructFields.AttributesMesh.indices,

                // VertexID without the Preprocessor.
                new FieldDescriptor(HDStructFields.AttributesMesh.name, "vertexID", "ATTRIBUTES_NEED_VERTEXID", ShaderValueType.Uint, "VERTEXID_SEMANTIC")
            }
        };

        // A key difference between Material Shader and VFX Shader generation is how surface properties are provided. Material Shaders
        // simply provide properties via UnityPerMaterial cbuffer. VFX expects these same properties to be computed in the vertex
        // stage (because we must evaluate them with the VFX blocks), and packed with the interpolators for the fragment stage.
        static StructDescriptor AppendVFXInterpolator(StructDescriptor interpolator, VFXContext context, VFXContextCompiledData contextData)
        {
            var fields = interpolator.fields.ToList();

            fields.AddRange(VFXSubTarget.GetVFXInterpolators(HDStructFields.VaryingsMeshToPS.name, context, contextData));

            // VFX Object Space Interpolators
            fields.Add(HDStructFields.VaryingsMeshToPS.worldToElement0);
            fields.Add(HDStructFields.VaryingsMeshToPS.worldToElement1);
            fields.Add(HDStructFields.VaryingsMeshToPS.worldToElement2);

            fields.Add(HDStructFields.VaryingsMeshToPS.elementToWorld0);
            fields.Add(HDStructFields.VaryingsMeshToPS.elementToWorld1);
            fields.Add(HDStructFields.VaryingsMeshToPS.elementToWorld2);

            interpolator.fields = fields.ToArray();
            return interpolator;
        }

        static readonly DependencyCollection ElementSpaceDependencies = new DependencyCollection
        {
            // Interpolator dependency.
            new FieldDependency(HDStructFields.FragInputs.worldToElement, HDStructFields.VaryingsMeshToPS.worldToElement0),
            new FieldDependency(HDStructFields.FragInputs.worldToElement, HDStructFields.VaryingsMeshToPS.worldToElement1),
            new FieldDependency(HDStructFields.FragInputs.worldToElement, HDStructFields.VaryingsMeshToPS.worldToElement2),

            new FieldDependency(HDStructFields.FragInputs.elementToWorld, HDStructFields.VaryingsMeshToPS.elementToWorld0),
            new FieldDependency(HDStructFields.FragInputs.elementToWorld, HDStructFields.VaryingsMeshToPS.elementToWorld1),
            new FieldDependency(HDStructFields.FragInputs.elementToWorld, HDStructFields.VaryingsMeshToPS.elementToWorld2),

            // Note: Normal is dependent on elementToWorld for inverse transpose multiplication.
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceNormal,             HDStructFields.FragInputs.elementToWorld),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceTangent,            HDStructFields.FragInputs.worldToElement),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceBiTangent,          HDStructFields.FragInputs.worldToElement),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpacePosition,           HDStructFields.FragInputs.worldToElement),
            new FieldDependency(StructFields.SurfaceDescriptionInputs.ObjectSpaceViewDirection,      HDStructFields.FragInputs.worldToElement),

            new FieldDependency(Fields.WorldToObject, HDStructFields.FragInputs.worldToElement),
            new FieldDependency(Fields.ObjectToWorld, HDStructFields.FragInputs.elementToWorld),

            // Normal in object space requires worldToElement (see GetNormalWS_SrcOS calling TransformObjectToWorldNormal which uses world inverse transpose)
            new FieldDependency(HDBlockFields.SurfaceDescription.IrisNormalOS, HDStructFields.FragInputs.worldToElement),
            new FieldDependency(HDBlockFields.SurfaceDescription.CoatNormalOS, HDStructFields.FragInputs.worldToElement),
            new FieldDependency(BlockFields.SurfaceDescription.NormalOS, HDStructFields.FragInputs.worldToElement),
        };


        public override ShaderGraphBinder GetShaderGraphDescriptor(VFXContext context, VFXContextCompiledData data)
        {
            return new ShaderGraphBinder
            {
                structs = new StructCollection
                {
                    AttributesMeshVFX, // TODO: Could probably re-use the original HD Attributes Mesh and just ensure Instancing enabled.
                    Structs.VertexDescriptionInputs,
                    Structs.SurfaceDescriptionInputs,
                    AppendVFXInterpolator(HDStructs.VaryingsMeshToPS, context, data),
                },
                
                fieldDependencies = ElementSpaceDependencies,
                useFragInputs = true
            };
        }

        public override IEnumerable<GraphicsDeviceType> GetSupportedGraphicDevices()
        {
            foreach (var device in base.GetSupportedGraphicDevices())
            {
                if (HDUtils.IsSupportedGraphicDevice(device))
                    yield return device;
            }
        }
    }
}
