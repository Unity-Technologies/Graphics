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
        public VFXURPBinder()
        {
            BaseShaderGUI.ShadowCasterPassEnabledChanged += OnShadowCasterPassEnabledChanged;
            BaseShaderGUI.MotionVectorPassEnabledChanged += OnMotionVectorPassEnabledChanged;
        }

        ~VFXURPBinder()
        {
            BaseShaderGUI.ShadowCasterPassEnabledChanged -= OnShadowCasterPassEnabledChanged;
            BaseShaderGUI.MotionVectorPassEnabledChanged -= OnMotionVectorPassEnabledChanged;
        }

        public override string templatePath { get { return "Packages/com.unity.render-pipelines.universal/Editor/VFXGraph/Shaders"; } }
        public override string runtimePath { get { return "Packages/com.unity.render-pipelines.universal/Runtime/VFXGraph/Shaders"; } }
        public override string SRPAssetTypeStr { get { return "UniversalRenderPipelineAsset"; } }
        public override Type SRPOutputDataType { get { return typeof(VFXURPSubOutput); } }

        public override bool IsShaderVFXCompatible(Shader shader)
        {
            return shader.TryGetMetadataOfType<UniversalMetadata>(out var metadata) && metadata.isVFXCompatible;
        }

        public override bool GetSupportsMotionVectorPerVertex(ShaderGraphVfxAsset shaderGraph, VFXMaterialSerializedSettings materialSettings)
        {
            var path = AssetDatabase.GetAssetPath(shaderGraph);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader.TryGetMetadataOfType<UniversalMetadata>(out var metaData))
            {
                if (metaData.hasVertexModificationInMotionVector)
                    return false;
            }
            return true;
        }

        public override void SetupMaterial(Material material, bool hasMotionVector = false, bool hasShadowCasting = false, ShaderGraphVfxAsset shaderGraph = null)
        {
            if (!hasMotionVector)
                material.shaderKeywords = material.shaderKeywords.Append(disableMotionVectorKeyword).ToArray();
            if (!hasShadowCasting)
                material.shaderKeywords = material.shaderKeywords.Append(disableShadowCasterKeyword).ToArray();

            material.SetShaderPassEnabled("ShadowCaster", hasShadowCasting);
            material.SetShaderPassEnabled("MotionVectors", hasMotionVector);

            ShaderUtils.UpdateMaterial(material, ShaderUtils.MaterialUpdateType.ModifiedShader, shaderGraph);
        }

        public override bool AllowMaterialOverride(ShaderGraphVfxAsset shaderGraph)
        {
            var path = AssetDatabase.GetAssetPath(shaderGraph);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader.TryGetMetadataOfType<UniversalMetadata>(out var metaData))
            {
                return metaData.allowMaterialOverride;
            }

            return base.AllowMaterialOverride(shaderGraph);
        }

        public override bool TryGetQueueOffset(ShaderGraphVfxAsset shaderGraph, VFXMaterialSerializedSettings materialSettings, out int queueOffset)
        {
            //N.B.: Queue offset is always overridable in URP
            queueOffset = 0;

            var path = AssetDatabase.GetAssetPath(shaderGraph);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!materialSettings.TryGetFloat(Rendering.Universal.Property.QueueOffset, material, out float queueOffsetFloat))
                return false;
            queueOffset = (int)queueOffsetFloat;
            return true;
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
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (materialSettings.TryGetFloat(Property.CastShadows, material, out float castShadowFloat))
                {
                    castShadow = castShadowFloat != 0.0f;
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
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (materialSettings.TryGetFloat(Property.SurfaceType, material, out var surfaceTypeFloat))
                {
                    var surfaceType = (BaseShaderGUI.SurfaceType)surfaceTypeFloat;
                    if (surfaceType == BaseShaderGUI.SurfaceType.Transparent && materialSettings.TryGetFloat(Property.BlendMode, material, out var blendModeTypeFloat))
                    {
                        var blendMode = (BaseShaderGUI.BlendMode)blendModeTypeFloat;
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
                    case ShaderUtils.ShaderID.SG_Unlit: return "Unlit";
                    case ShaderUtils.ShaderID.SG_SpriteUnlit: return "Sprite Unlit";
                    case ShaderUtils.ShaderID.SG_Lit: return "Lit";
                    case ShaderUtils.ShaderID.SG_SpriteLit: return "Sprite Lit";
                    case ShaderUtils.ShaderID.SG_SpriteCustomLit: return "Sprite Custom Lit";
                    case ShaderUtils.ShaderID.SG_SixWaySmokeLit: return "Six-way Lit";
                }
            }
            return string.Empty;
        }

        private const string disableShadowCasterKeyword = "__VFX_DISABLE_SHADOW_CASTER";
        public static bool DoesVFXControlShadowCaster(Material material, out bool vfxShadowCasterEnabled)
        {
            vfxShadowCasterEnabled = false;
            // Currently only controls shadow caster pass to disable it.
            if (material.shaderKeywords.Contains(disableShadowCasterKeyword))
            {
                vfxShadowCasterEnabled = false;
                return true;
            }
            return false;
        }

        static void OnShadowCasterPassEnabledChanged(Material material)
        {
            if (DoesVFXControlShadowCaster(material, out bool vfxShadowCasterEnabled))
                material.SetShaderPassEnabled("ShadowCaster", vfxShadowCasterEnabled);
        }

        private const string disableMotionVectorKeyword = "__VFX_DISABLE_MOTION_VECTOR";
        public static bool DoesVFXControlMotionVector(Material material, out bool vfxMotionVectorEnabled)
        {
            vfxMotionVectorEnabled = false;
            // Currently only controls motion vector pass to disable it.
            if (material.shaderKeywords.Contains(disableMotionVectorKeyword))
            {
                vfxMotionVectorEnabled = false;
                return true;
            }
            return false;
        }

        static void OnMotionVectorPassEnabledChanged(Material material)
        {
            if (DoesVFXControlMotionVector(material, out bool vfxMotionVectorEnabled))
                material.SetShaderPassEnabled("MotionVectors", vfxMotionVectorEnabled);
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

        static readonly FieldDescriptor[] VaryingsAdditionalFields = {
            StructFields.Varyings.worldToElement0,
            StructFields.Varyings.worldToElement1,
            StructFields.Varyings.worldToElement2,
            StructFields.Varyings.elementToWorld0,
            StructFields.Varyings.elementToWorld1,
            StructFields.Varyings.elementToWorld2,
        };

        static IEnumerable<FieldDescriptor> GenerateSurfaceDescriptionInput(VFXContext context, VFXTaskCompiledData contextData)
        {
            var alreadyAddedField = new HashSet<string>();

            //Everything from common SurfaceDescriptionInputs
            foreach (var field in Structs.SurfaceDescriptionInputs.fields)
            {
                alreadyAddedField.Add(field.name);
                yield return field;
            }

			// VFX Material Properties
			if (contextData.SGInputs != null)
            {
                foreach (var input in contextData.SGInputs.fragInputs)
                {
                    var (name, exp) = (input.Key,input.Value);

                    if (!VFXSubTarget.kVFXShaderValueTypeMap.TryGetValue(VFXExpression.TypeToType(exp.valueType), out var shaderValueType))
                        throw new Exception($"Unsupported property type for {name}: {exp.valueType}");

					if (alreadyAddedField.Contains(name))
						throw new Exception($"Name conflict detected in SurfaceDescriptionInputs: {name}");

                    yield return new FieldDescriptor(StructFields.SurfaceDescriptionInputs.name, name, "", shaderValueType);
                }
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
                baseStructs = new StructCollection
                {
                    AttributesMeshVFX, // TODO: Could probably re-use the original HD Attributes Mesh and just ensure Instancing enabled.
                    Structs.VertexDescriptionInputs,
                    surfaceDescriptionInputWithVFX
                },
                varyingsAdditionalFields = VaryingsAdditionalFields,

                fieldDependencies = ElementSpaceDependencies,
                pragmasReplacement = new []
                {
                    ( Pragma.Vertex("vert"), Pragma.Vertex("VertVFX") ),

                    //Minimal target of VFX is always Target45 (2.0 is used with GLCore)
                    ( Pragma.Target(ShaderModel.Target20), Pragma.Target(ShaderModel.Target45) ),
                    ( Pragma.Target(ShaderModel.Target30), Pragma.Target(ShaderModel.Target45) ),
                    ( Pragma.Target(ShaderModel.Target35), Pragma.Target(ShaderModel.Target45) ),
                    ( Pragma.Target(ShaderModel.Target40), Pragma.Target(ShaderModel.Target45) ),
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
