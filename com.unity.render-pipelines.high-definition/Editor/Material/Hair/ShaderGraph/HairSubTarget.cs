using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.Rendering.HighDefinition.ShaderGraph.Legacy;
using static UnityEngine.Rendering.HighDefinition.HDMaterialProperties;
using static UnityEditor.Rendering.HighDefinition.HDShaderUtils;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    sealed partial class HairSubTarget : LightingSubTarget, ILegacyTarget, IRequiresData<HairData>
    {
        public HairSubTarget() => displayName = "Hair";

        protected override string templateMaterialDirectory => $"{HDUtils.GetHDRenderPipelinePath()}Editor/Material/Hair/ShaderGraph/";
        protected override string customInspector => "Rendering.HighDefinition.HairGUI";
        protected override string subTargetAssetGuid => "7e681cc79dd8e6c46ba1e8412d519e26"; // HairSubTarget.cs
        protected override ShaderID shaderID => HDShaderUtils.ShaderID.SG_Hair;
        protected override string subShaderInclude => CoreIncludes.kHair;
        protected override FieldDescriptor subShaderField => HDFields.SubShader.Hair;
        protected override bool requireSplitLighting => false;


        HairData m_HairData;

        HairData IRequiresData<HairData>.data
        {
            get => m_HairData;
            set => m_HairData = value;
        }

        public HairData hairData
        {
            get => m_HairData;
            set => m_HairData = value;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
            base.GetFields(ref context);

            var descs = context.blocks.Select(x => x.descriptor);
            // Hair specific properties:
            context.AddField(HDFields.KajiyaKay,                            hairData.materialType == HairData.MaterialType.KajiyaKay);
            context.AddField(HDFields.HairStrandDirection,                  descs.Contains(HDBlockFields.SurfaceDescription.HairStrandDirection) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.HairStrandDirection));
            context.AddField(HDFields.RimTransmissionIntensity,             descs.Contains(HDBlockFields.SurfaceDescription.RimTransmissionIntensity) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.RimTransmissionIntensity));
            context.AddField(HDFields.UseLightFacingNormal,                 hairData.useLightFacingNormal);
            context.AddField(HDFields.Transmittance,                        descs.Contains(HDBlockFields.SurfaceDescription.Transmittance) && context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.Transmittance));

            // Misc
            context.AddField(HDFields.SpecularAA,                           lightingData.specularAA &&
                                                                                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAThreshold) &&
                                                                                context.pass.validPixelBlocks.Contains(HDBlockFields.SurfaceDescription.SpecularAAScreenSpaceVariance));
        }

        public override void GetActiveBlocks(ref TargetActiveBlockContext context)
        {
            base.GetActiveBlocks(ref context);

            // Hair specific blocks
            context.AddBlock(HDBlockFields.SurfaceDescription.Transmittance);
            context.AddBlock(HDBlockFields.SurfaceDescription.RimTransmissionIntensity);
            context.AddBlock(HDBlockFields.SurfaceDescription.HairStrandDirection);
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularTint);
            context.AddBlock(HDBlockFields.SurfaceDescription.SpecularShift);
            context.AddBlock(HDBlockFields.SurfaceDescription.SecondarySpecularTint);
            context.AddBlock(HDBlockFields.SurfaceDescription.SecondarySmoothness);
            context.AddBlock(HDBlockFields.SurfaceDescription.SecondarySpecularShift);
        }

        protected override void AddInspectorPropertyBlocks(SubTargetPropertiesGUI blockList)
        {
            blockList.AddPropertyBlock(new SurfaceOptionPropertyBlock(SurfaceOptionPropertyBlock.Features.Lit));
            blockList.AddPropertyBlock(new HairAdvancedOptionsPropertyBlock(hairData));
        }
    }
}
