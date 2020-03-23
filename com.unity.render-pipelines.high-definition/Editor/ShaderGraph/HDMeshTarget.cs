using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
#region Enumerations
    enum MaterialType
    {
        Unlit,
        Lit,
        Eye,
        Fabric,
        Hair,
        StackLit,
    }

    enum AlphaMode
    {
        Alpha,
        Premultiply,
        Additive,
    }

    enum DistortionMode
    {
        Add,
        Multiply,
        Replace
    }

    enum DoubleSidedMode
    {
        Disabled,
        Enabled,
        FlippedNormals,
        MirroredNormals,
    }

    enum EmissionGIMode
    {
        Disabled,
        Realtime,
        Baked,
    }

    enum SpecularOcclusionMode
    {
        Off,
        FromAO,
        FromAOAndBentNormal,
        Custom
    }
#endregion

    class HDMeshTarget : ITargetImplementation
    {
#region Serialized Fields
        [SerializeField]
        MaterialType m_MaterialType;

        [SerializeField]
        SurfaceType m_SurfaceType;

        [SerializeField]
        AlphaMode m_AlphaMode;

        [SerializeField]
        HDRenderQueue.RenderQueueType m_RenderingPass = HDRenderQueue.RenderQueueType.Opaque;

        [SerializeField]
        bool m_TransparencyFog = true;

        [SerializeField]
        bool m_Distortion;

        [SerializeField]
        DistortionMode m_DistortionMode;

        [SerializeField]
        bool m_DistortionOnly = true;

        [SerializeField]
        bool m_DistortionDepthTest = true;

        [SerializeField]
        bool m_AlphaTest;

        [SerializeField]
        int m_SortPriority;

        [SerializeField]
        bool m_DoubleSided;

        [SerializeField]
        bool m_ZWrite = true;

        [SerializeField]
        TransparentCullMode m_TransparentCullMode = TransparentCullMode.Back;

        [SerializeField]
        CompareFunction m_ZTest = CompareFunction.LessEqual;

        [SerializeField]
        bool m_AddPrecomputedVelocity = false;

        [SerializeField]
        bool m_EnableShadowMatte = false;

        [SerializeField]
        bool m_DOTSInstancing = false;
#endregion

#region ITargetImplementation Properties
        public Type targetType => typeof(MeshTarget);
        public string displayName => "HDRP";
        public string passTemplatePath => string.Empty;
        public string sharedTemplateDirectory => $"{HDUtils.GetHDRenderPipelinePath()}Editor/ShaderGraph/Templates";
        public string renderTypeTag => GetRenderTypeTag();
        public string renderQueueTag => GetRenderQueueTag();
#endregion

#region Data Properties
        public MaterialType materialType
        {
            get => m_MaterialType;
            set => m_MaterialType = value;
        }

        public SurfaceType surfaceType
        {
            get => m_SurfaceType;
            set => m_SurfaceType = value;
        }

        public AlphaMode alphaMode
        {
            get => m_AlphaMode;
            set => m_AlphaMode = value;
        }

        public HDRenderQueue.RenderQueueType renderingPass
        {
            get => m_RenderingPass;
            set => m_RenderingPass = value;
        }

        public bool transparencyFog
        {
            get => m_TransparencyFog;
            set => m_TransparencyFog = value;
        }

        public bool distortion
        {
            get => m_Distortion;
            set => m_Distortion = value;
        }

        public DistortionMode distortionMode
        {
            get => m_DistortionMode;
            set => m_DistortionMode = value;
        }

        public bool distortionOnly
        {
            get => m_DistortionOnly;
            set => m_DistortionOnly = value;
        }

        public bool distortionDepthTest
        {
            get => m_DistortionDepthTest;
            set => m_DistortionDepthTest = value;
        }

        public bool alphaTest
        {
            get => m_AlphaTest;
            set => m_AlphaTest = value;
        }

        public int sortPriority
        {
            get => m_SortPriority;
            set => m_SortPriority = value;
        }

        public bool doubleSided
        {
            get => m_DoubleSided;
            set => m_DoubleSided = value;
        }

        public bool zWrite
        {
            get => m_ZWrite;
            set => m_ZWrite = value;
        }

        public TransparentCullMode transparentCullMode
        {
            get => m_TransparentCullMode;
            set => m_TransparentCullMode = value;
        }

        public CompareFunction zTest
        {
            get => m_ZTest;
            set => m_ZTest = value;
        }

        public bool addPrecomputedVelocity
        {
            get => m_AddPrecomputedVelocity;
            set => m_AddPrecomputedVelocity = value;
        }

        public bool enableShadowMatte
        {
            get => m_EnableShadowMatte;
            set => m_EnableShadowMatte = value;
        }

        public bool dotsInstancing
        {
            get => m_DOTSInstancing;
            set => m_DOTSInstancing = value;
        }
#endregion

#region Helper Properties
        bool activeDistortion => m_SurfaceType == SurfaceType.Transparent && m_Distortion;
        bool activeAlpha => m_SurfaceType == SurfaceType.Transparent || m_AlphaTest;
        bool activeAlphaTest => m_AlphaTest;
        bool activeShadowTint => m_EnableShadowMatte;
#endregion

#region Setup
        public void SetupTarget(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("7395c9320da217b42b9059744ceb1de6")); // MeshTarget
            context.AddAssetDependencyPath(AssetDatabase.GUIDToAssetPath("326a52113ee5a7d46bf9145976dcb7f6")); // HDRPMeshTarget

            switch(m_MaterialType)
            {
                case MaterialType.Unlit:
                    context.AddSubShader(HDSubShaders.HDUnlit);
                    break;
                case MaterialType.Lit:
                    context.AddSubShader(HDSubShaders.HDLit);
                    break;
                case MaterialType.Eye:
                    context.AddSubShader(HDSubShaders.Eye);
                    break;
                case MaterialType.Fabric:
                    context.AddSubShader(HDSubShaders.Fabric);
                    break;
                case MaterialType.Hair:
                    context.AddSubShader(HDSubShaders.Hair);
                    break;
                case MaterialType.StackLit:
                    context.AddSubShader(HDSubShaders.StackLit);
                    break;
            }
        }
#endregion

#region Active Blocks
        public void SetActiveBlocks(ref List<BlockFieldDescriptor> activeBlocks)
        {
            // Always supported Blocks
            activeBlocks.Add(BlockFields.VertexDescription.Position);
            activeBlocks.Add(BlockFields.VertexDescription.Normal);
            activeBlocks.Add(BlockFields.VertexDescription.Tangent);
            activeBlocks.Add(BlockFields.SurfaceDescription.BaseColor);
            activeBlocks.Add(BlockFields.SurfaceDescription.Emission);

            if(activeAlpha)
                activeBlocks.Add(BlockFields.SurfaceDescription.Alpha);

            if(activeAlphaTest)
                activeBlocks.Add(BlockFields.SurfaceDescription.AlphaClipThreshold);
            
            if(activeShadowTint)
                activeBlocks.Add(HDBlockFields.SurfaceDescription.ShadowTint);

            if(activeDistortion)
            {
                activeBlocks.Add(HDBlockFields.SurfaceDescription.Distortion);
                activeBlocks.Add(HDBlockFields.SurfaceDescription.DistortionBlur);
            }
        }
#endregion

#region Conditional Fields
        public ConditionalField[] GetConditionalFields(PassDescriptor pass, List<BlockFieldDescriptor> blocks)
        {
            return new ConditionalField[]
            {
                // Features
                new ConditionalField(Fields.GraphVertex,                    blocks.Contains(BlockFields.VertexDescription.Position) ||
                                                                                blocks.Contains(BlockFields.VertexDescription.Normal) ||
                                                                                blocks.Contains(BlockFields.VertexDescription.Tangent)),
                new ConditionalField(Fields.GraphPixel,                     true),

                // Distortion
                new ConditionalField(HDFields.DistortionDepthTest,          m_DistortionDepthTest),
                new ConditionalField(HDFields.DistortionAdd,                m_DistortionMode == DistortionMode.Add),
                new ConditionalField(HDFields.DistortionMultiply,           m_DistortionMode == DistortionMode.Multiply),
                new ConditionalField(HDFields.DistortionReplace,            m_DistortionMode == DistortionMode.Replace),
                new ConditionalField(HDFields.TransparentDistortion,        activeDistortion),
                
                // Misc
                new ConditionalField(Fields.AlphaTest,                      m_AlphaTest && pass.pixelBlocks.Contains(BlockFields.SurfaceDescription.AlphaClipThreshold)),
                new ConditionalField(HDFields.AlphaFog,                     m_SurfaceType != SurfaceType.Opaque && m_TransparencyFog),
                new ConditionalField(Fields.VelocityPrecomputed,            m_AddPrecomputedVelocity),
                new ConditionalField(HDFields.EnableShadowMatte,            m_EnableShadowMatte),
            };
        }
#endregion

#region Shader Properties
        public void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            // Trunk currently relies on checking material property "_EmissionColor" to allow emissive GI. If it doesn't find that property, or it is black, GI is forced off.
            // ShaderGraph doesn't use this property, so currently it inserts a dummy color (white). This dummy color may be removed entirely once the following PR has been merged in trunk: Pull request #74105
            // The user will then need to explicitly disable emissive GI if it is not needed.
            // To be able to automatically disable emission based on the ShaderGraph config when emission is black,
            // we will need a more general way to communicate this to the engine (not directly tied to a material property).
            collector.AddShaderProperty(new ColorShaderProperty()
            {
                overrideReferenceName = "_EmissionColor",
                hidden = true,
                value = new Color(1.0f, 1.0f, 1.0f, 1.0f)
            });

            // ShaderGraph only property used to send the RenderQueueType to the material
            collector.AddShaderProperty(new Vector1ShaderProperty
            {
                overrideReferenceName = "_RenderQueueType",
                hidden = true,
                value = (int)m_RenderingPass,
            });

            // See SG-ADDITIONALVELOCITY-NOTE
            if (m_AddPrecomputedVelocity)
            {
                collector.AddShaderProperty(new BooleanShaderProperty
                {
                    value  = true,
                    hidden = true,
                    overrideReferenceName = kAddPrecomputedVelocity,
                });
            }

            if (m_EnableShadowMatte)
            {
                uint mantissa = ((uint)LightFeatureFlags.Punctual | (uint)LightFeatureFlags.Directional | (uint)LightFeatureFlags.Area) & 0x007FFFFFu;
                uint exponent = 0b10000000u; // 0 as exponent
                collector.AddShaderProperty(new Vector1ShaderProperty
                {
                    hidden = true,
                    value = HDShadowUtils.Asfloat((exponent << 23) | mantissa),
                    overrideReferenceName = HDMaterialProperties.kShadowMatteFilter
                });
            }

            // Add all shader properties required by the inspector
            HDSubShaderUtilities.AddStencilShaderProperties(collector, false, false);
            HDSubShaderUtilities.AddBlendingStatesShaderProperties(
                collector,
                m_SurfaceType,
                HDSubShaderUtilities.ConvertAlphaModeToBlendMode(m_AlphaMode),
                m_SortPriority,
                m_ZWrite,
                m_TransparentCullMode,
                m_ZTest,
                false,
                m_TransparencyFog
            );
            HDSubShaderUtilities.AddAlphaCutoffShaderProperties(collector, m_AlphaTest, false);
            HDSubShaderUtilities.AddDoubleSidedProperty(collector, m_DoubleSided ? DoubleSidedMode.Enabled : DoubleSidedMode.Disabled);
        }
#endregion

#region Preview Material
        public void ProcessPreviewMaterial(Material material)
        {
            // Fixup the material settings:
            material.SetFloat(kSurfaceType, (int)m_SurfaceType);
            material.SetFloat(kDoubleSidedEnable, m_DoubleSided ? 1.0f : 0.0f);
            material.SetFloat(kAlphaCutoffEnabled, m_AlphaTest ? 1 : 0);
            material.SetFloat(kBlendMode, (int)HDSubShaderUtilities.ConvertAlphaModeToBlendMode(m_AlphaMode));
            material.SetFloat(kEnableFogOnTransparent, m_TransparencyFog ? 1.0f : 0.0f);
            material.SetFloat(kZTestTransparent, (int)m_ZTest);
            material.SetFloat(kTransparentCullMode, (int)m_TransparentCullMode);
            material.SetFloat(kZWrite, m_ZWrite ? 1.0f : 0.0f);

            // No sorting priority for shader graph preview
            material.renderQueue = (int)HDRenderQueue.ChangeType(m_RenderingPass, offset: 0, alphaTest: m_AlphaTest);

            HDUnlitGUI.SetupMaterialKeywordsAndPass(material);
        }
#endregion

#region Settings View
        public VisualElement GetSettings(Action onChange)
        {
            return new HDMeshTargetSettingsView(this, onChange);
        }
#endregion

#region Helpers
        string GetRenderTypeTag()
        {
            return HDRenderTypeTags.HDUnlitShader.ToString();
        }

        string GetRenderQueueTag()
        {
            int queue = HDRenderQueue.ChangeType(m_RenderingPass, m_SortPriority, m_AlphaTest);
            return HDRenderQueue.GetShaderTagValue(queue);
        }
#endregion
    }
}
