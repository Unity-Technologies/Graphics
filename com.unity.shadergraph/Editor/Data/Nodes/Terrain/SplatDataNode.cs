using System;
using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Terrain", "Splat Data")]
    class SplatDataNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV // IGeneratesFunction
    {
        public SplatDataNode()
        {
            name = "Splat Data";
            UpdateNodeAfterDeserialization();
        }

        // TODO hide from searcher when terrain isn't supported by target. Owner is null by default- how to set it up?
        //internal override bool ExposeToSearcher => (owner != null) && (owner.hasTerrainTarget);

        const int InputUVId = 0;
        const int InputLayerId = 1;
        const int OutputAlbedoId = 2;
        const int OutputNormalId = 3;
        const int OutputMetallicId = 4;
        const int OutputSmoothnessId = 5;
        const int OutputOcclusionId = 6;
        const int OutputAlphaId = 7;
        const int OutputControlId = 8;
        const int InputDoBlendId = 9;
        const string kInputUVSlotName = "UV0";
        const string kInputLayerSlotName = "Layer Index";
        const string kOutputAlbedoSlotName = "Albedo";
        const string kOutputNormalSlotName = "Normal";
        const string kOutputMetallicSlotName = "Metallic";
        const string kOutputSmoothnessSlotName = "Smoothness";
        const string kOutputOcclusionSlotName = "Occlusion";
        const string kOutputAlphaSlotName = "Alpha";
        const string kOutputControlSlotName = "Control";
        const string kInputDoBlendName = "Blend Layers";

        public override bool hasPreview
        {
            // Todo: Add preview?
            get { return false; }
        }

        string GetFunctionName()
        {
            return "Unity_Terrain_SplatData_$precision";
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new UVMaterialSlot(InputUVId, kInputUVSlotName, kInputUVSlotName, UVChannel.UV0));
            AddSlot(new Vector1MaterialSlot(InputLayerId, kInputLayerSlotName, kInputLayerSlotName, SlotType.Input, 0)); // Todo hide/disable layer input if DoBlend is true
            AddSlot(new BooleanMaterialSlot(InputDoBlendId, kInputDoBlendName, kInputDoBlendName, SlotType.Input, false));
            AddSlot(new Vector3MaterialSlot(OutputAlbedoId, kOutputAlbedoSlotName, kOutputAlbedoSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputNormalId, kOutputNormalSlotName, kOutputNormalSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector1MaterialSlot(OutputMetallicId, kOutputMetallicSlotName, kOutputMetallicSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSmoothnessId, kOutputSmoothnessSlotName, kOutputSmoothnessSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputOcclusionId, kOutputOcclusionSlotName, kOutputOcclusionSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputAlphaId, kOutputAlphaSlotName, kOutputAlphaSlotName, SlotType.Output, 0));
            AddSlot(new Vector4MaterialSlot(OutputControlId, kOutputControlSlotName, kOutputControlSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { InputUVId, InputLayerId, InputDoBlendId, OutputAlbedoId, OutputNormalId, OutputMetallicId, OutputSmoothnessId, OutputOcclusionId, OutputAlphaId, OutputControlId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            // TODO: Generalize for URP and HDRP. Currently works with HDRP terrain.
            //var uvVal = GetSlotValue(InputUVId, generationMode);
            //var layerVal = GetSlotValue(InputLayerId, generationMode);
            //var albedoVal = GetSlotValue(OutputAlbedoId, generationMode);
            //var normalVal = GetSlotValue(OutputNormalId, generationMode);
            //var metallicVal = GetSlotValue(OutputMetallicId, generationMode);
            //var smoothnessVal = GetSlotValue(OutputSmoothnessId, generationMode);
            //var occlusionVal = GetSlotValue(OutputOcclusionId, generationMode);
            //var alphaVal = GetSlotValue(OutputAlphaId, generationMode);
            //var controlVal = GetSlotValue(OutputControlId, generationMode);

            // Handle define ifdefing here to avoid calling an undefined function
            // Declare outputs
            var albedoVal = GetVariableNameForSlot(OutputAlbedoId);
            var normalVal = GetVariableNameForSlot(OutputNormalId);
            var metallicVal = GetVariableNameForSlot(OutputMetallicId);
            var smoothnessVal = GetVariableNameForSlot(OutputSmoothnessId);
            var occlusionVal = GetVariableNameForSlot(OutputOcclusionId);
            var alphaVal = GetVariableNameForSlot(OutputAlphaId);
            var controlVal = GetVariableNameForSlot(OutputControlId);

            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputAlbedoId).concreteValueType.ToShaderString(), albedoVal);
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputNormalId).concreteValueType.ToShaderString(), normalVal);
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputMetallicId).concreteValueType.ToShaderString(), metallicVal);
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSmoothnessId).concreteValueType.ToShaderString(), smoothnessVal);
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputOcclusionId).concreteValueType.ToShaderString(), occlusionVal);
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputAlphaId).concreteValueType.ToShaderString(), alphaVal);
            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputControlId).concreteValueType.ToShaderString(), controlVal);
            sb.AppendLine("#if (defined(TERRAIN_ENABLED) || defined(_TERRAIN_BASEMAP_GEN) || defined(TERRAIN_SPLAT_ADDPASS) || defined(TERRAIN_SPLAT_BASEPASS))");
            sb.AppendLine("GetSplatData({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9});", GetSlotValue(InputUVId, generationMode), GetSlotValue(InputLayerId, generationMode), GetSlotValue(InputDoBlendId, generationMode),
                albedoVal, normalVal, metallicVal, smoothnessVal,
                occlusionVal, alphaVal, controlVal);
            sb.AppendLine("#else");
            sb.AppendLine("{0} = 0;", albedoVal);
            sb.AppendLine("{0} = 0;", normalVal);
            sb.AppendLine("{0} = 0;", metallicVal);
            sb.AppendLine("{0} = 0;", smoothnessVal);
            sb.AppendLine("{0} = 0;", occlusionVal);
            sb.AppendLine("{0} = 0;", alphaVal);
            sb.AppendLine("{0} = 0;", controlVal);
            sb.AppendLine("#endif");

            //sb.AppendLine("#if (defined(TERRAIN_ENABLED) || defined(_TERRAIN_BASEMAP_GEN))");
            //string uvVar = GetVariableNameForSlot(InputUVId);
            //sb.AppendLine("float2 splatBaseUV = {0};", uvVar);
            //sb.AppendLine("float2 dxuv = ddx(splatBaseUV);");
            //sb.AppendLine("float2 dyuv = ddy(splatBaseUV);");
            //sb.AppendLine("int i = (int)({0} % 5);", GetVariableNameForSlot(InputLayerId));
            //sb.AppendLine("#ifdef _TERRAIN_8_LAYERS");
            //sb.AppendLine("i += 4;");
            //sb.AppendLine("#endif");
            //string controlVar = GetVariableNameForSlot(OutputControlId);
            //sb.AppendLine("{0} = SampleControl(i)[i % 5];", controlVar);
            //sb.AppendLine("SampleResults(i, {0});", controlVar);
            //sb.AppendLine("{0} = albedo[i].xyz;", GetVariableNameForSlot(OutputAlbedoId));
            //sb.AppendLine("{0} = normal[i].xyz;", GetVariableNameForSlot(OutputNormalId));
            //sb.AppendLine("{0} = masks[i].x;", GetVariableNameForSlot(OutputMetallicId));
            //sb.AppendLine("{0} = masks[i].w;", GetVariableNameForSlot(OutputSmoothnessId));
            //sb.AppendLine("{0} = masks[i].y;", GetVariableNameForSlot(OutputOcclusionId));
            //sb.AppendLine("{0} = SAMPLE_TEXTURE2D(_TerrainHolesTexture, sampler_TerrainHolesTexture, {1}).r == 0.0f ? 0.0f : 1.0f;", GetVariableNameForSlot(OutputAlphaId), uvVar);
            //sb.AppendLine("#endif // TERRAIN_ENABLED, _TERRAIN_BASEMAP_GEN");
            //sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputAlbedoId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutputAlbedoId));
            //sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputNormalId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutputNormalId));
            //sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputMetallicId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutputMetallicId));
            //sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSmoothnessId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutputSmoothnessId));
            //sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputOcclusionId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutputOcclusionId));
            //sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputAlphaId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutputAlphaId));
            //sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputControlId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutputControlId));

            //sb.AppendLine("{0}({1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9});", GetFunctionName(), uvVal, layerVal, albedoVal, normalVal, metallicVal, smoothnessVal, occlusionVal, alphaVal, controlVal);
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            if (!generationMode.IsPreview())
                return;

            base.CollectShaderProperties(properties, generationMode);
        }

        //public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        //{
        //    registry.ProvideFunction(GetFunctionName(), s =>
        //    {
        //        s.AppendLine("void {0} ({1} Uv, {2} Layer, out {2} Albedo, out {2} Normal, out {3} Metallic, out {3} Smoothness, out {3} Occlusion, out {3} Alpha, out {3} Control)",
        //            GetFunctionName(),
        //            FindSlot<MaterialSlot>(InputUVId).concreteValueType.ToShaderString(),
        //            FindInputSlot<MaterialSlot>(InputLayerId).concreteValueType.ToShaderString(),
        //            FindOutputSlot<MaterialSlot>(OutputAlbedoId).concreteValueType.ToShaderString(),
        //            FindOutputSlot<MaterialSlot>(OutputSmoothnessId).concreteValueType.ToShaderString()
        //            );
        //        using (s.BlockScope())
        //        {
        //            s.AppendLine("float2 splatBaseUV = Uv;");
        //            s.AppendLine("float2 dxuv = ddx(splatBaseUV);");
        //            s.AppendLine("float2 dyuv = ddy(splatBaseUV);");
        //            s.AppendLine("int i = (int)(Layer % 5);");
        //            s.AppendLine("#ifdef _TERRAIN_8_LAYERS");
        //            s.AppendLine("i += 4;");
        //            s.AppendLine("#endif");
        //            s.AppendLine("SampleResults(i, mask);");
        //            s.AppendLine("Albedo = albedo[i].xyz;");
        //            s.AppendLine("Normal = normal[i].xyz;");
        //            s.AppendLine("Metallic = masks[i].x;");
        //            s.AppendLine("Smoothness = masks[i].w;");
        //            s.AppendLine("Occlusion = masks[i].y;");
        //            s.AppendLine("Alpha = SAMPLE_TEXTURE2D(_TerrainHolesTexture, sampler_TerrainHolesTexture, Uv).r == 0.0f ? 0.0f : 1.0f;");
        //            s.AppendLine("Control = SampleControl(i)[i % 5];");
        //        }
        //    });
        //}

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                foreach (var slot in tempSlots)
                {
                    if (slot.RequiresMeshUV(channel))
                        return true;
                }

                return false;
            }
        }
    }
}
