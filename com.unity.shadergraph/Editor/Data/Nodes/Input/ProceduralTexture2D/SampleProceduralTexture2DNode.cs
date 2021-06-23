using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Sample Procedural Texture 2D")]
    class SampleProceduralTexture2DNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        public const int OutputSlotRGBAId = 0;
        public const int OutputSlotRId = 1;
        public const int OutputSlotGId = 2;
        public const int OutputSlotBId = 3;
        public const int OutputSlotAId = 4;
        public const int ProceduralTexture2DId = 5;
        public const int SamplerInput = 6;
        public const int UVInput = 7;
        public const int BlendId = 8;
        public const int TinputId = 9;
        public const int InvTinputId = 10;
        public const int CompressionScalersId = 11;
        public const int ColorSpaceOriginId = 12;
        public const int ColorSpaceVector1Id = 13;
        public const int ColorSpaceVector2Id = 14;
        public const int ColorSpaceVector3Id = 15;
        public const int InputSizeId = 16;

        const string kOutputSlotRGBAName = "RGBA";
        const string kOutputSlotRName = "R";
        const string kOutputSlotGName = "G";
        const string kOutputSlotBName = "B";
        const string kOutputSlotAName = "A";
        const string kProceduralTexture2DName = "ProceduralTex2D";
        const string kSamplerInputName = "Sampler";
        const string kUVInputName = "UV";
        const string kBlendIdName = "Blend";
        const string kTinputName = "Tinput";
        const string kInvTinputName = "invT";
        const string kCompressionScalersId = "compressionScalers";
        const string kColorSpaceOriginName = "colorSpaceOrigin";
        const string kColorSpaceVector1Name = "colorSpaceVector1";
        const string kColorSpaceVector2Name = "colorSpaceVector2";
        const string kColorSpaceVector3Name = "colorSpaceVector3";
        const string kInputSizeName = "inputSize";

        public override bool hasPreview { get { return true; } }

        public SampleProceduralTexture2DNode()
        {
            name = "Sample Procedural Texture 2D";
            UpdateNodeAfterDeserialization();
        }

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Sample-Texture-2D-Node"; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            // Output slots
            AddSlot(new Vector4MaterialSlot(OutputSlotRGBAId, kOutputSlotRGBAName, kOutputSlotRGBAName, SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(OutputSlotRId, kOutputSlotRName, kOutputSlotRName, SlotType.Output, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(OutputSlotGId, kOutputSlotGName, kOutputSlotGName, SlotType.Output, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(OutputSlotBId, kOutputSlotBName, kOutputSlotBName, SlotType.Output, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(OutputSlotAId, kOutputSlotAName, kOutputSlotAName, SlotType.Output, 0, ShaderStageCapability.Fragment));

            // Input slots
            AddSlot(new ProceduralTexture2DInputMaterialSlot(ProceduralTexture2DId, kProceduralTexture2DName, kProceduralTexture2DName, ShaderStageCapability.Fragment, false));
            AddSlot(new SamplerStateMaterialSlot(SamplerInput, kSamplerInputName, kSamplerInputName, SlotType.Input));
            AddSlot(new UVMaterialSlot(UVInput, kUVInputName, kUVInputName, UVChannel.UV0));
            AddSlot(new Vector1MaterialSlot(BlendId, kBlendIdName, kBlendIdName, SlotType.Input, 0, ShaderStageCapability.Fragment));

            // Hidden slots
            AddSlot(new Texture2DInputMaterialSlot(TinputId, kTinputName, kTinputName, ShaderStageCapability.Fragment, true));
            AddSlot(new Texture2DInputMaterialSlot(InvTinputId, kInvTinputName, kInvTinputName, ShaderStageCapability.Fragment, true));
            AddSlot(new Vector4MaterialSlot(CompressionScalersId, kCompressionScalersId, kCompressionScalersId, SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment, "X", "Y", "Z", "W", true));
            AddSlot(new Vector3MaterialSlot(ColorSpaceOriginId, kColorSpaceOriginName, kColorSpaceOriginName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment, "X", "Y", "Z", true));
            AddSlot(new Vector3MaterialSlot(ColorSpaceVector1Id, kColorSpaceVector1Name, kColorSpaceVector1Name, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment, "X", "Y", "Z", true));
            AddSlot(new Vector3MaterialSlot(ColorSpaceVector2Id, kColorSpaceVector2Name, kColorSpaceVector2Name, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment, "X", "Y", "Z", true));
            AddSlot(new Vector3MaterialSlot(ColorSpaceVector3Id, kColorSpaceVector3Name, kColorSpaceVector3Name, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment, "X", "Y", "Z", true));
            AddSlot(new Vector3MaterialSlot(InputSizeId, kInputSizeName, kInputSizeName, SlotType.Input, Vector3.zero, ShaderStageCapability.Fragment, "X", "Y", "Z", true));

            RemoveSlotsNameNotMatching(new[] { OutputSlotRGBAId, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId,
                ProceduralTexture2DId, SamplerInput, UVInput, BlendId, TinputId,
                InvTinputId, CompressionScalersId, ColorSpaceOriginId, ColorSpaceVector1Id, ColorSpaceVector2Id, ColorSpaceVector3Id, InputSizeId });
        }

        public override void ValidateNode()
        {
            base.ValidateNode();
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            ProceduralTexture2DInputMaterialSlot slot = FindInputSlot<ProceduralTexture2DInputMaterialSlot>(ProceduralTexture2DId);

            // Find Procedural Texture 2D Asset
            ProceduralTexture2D proceduralTexture2D = slot.proceduralTexture2D;
            var edges = owner.GetEdges(slot.slotReference).ToArray();
            if (edges.Any())
            {
                var fromSocketRef = edges.First().outputSlot;
                var fromNode = fromSocketRef.node;
                if (fromNode != null)
                {
                    if (fromNode is ProceduralTexture2DNode proceduralNode)
                    {
                        proceduralTexture2D = proceduralNode.proceduralTexture2D;
                    }
                }
            }

            // No Procedural Texture 2D Asset found, break and initialize output values to default white
            if (proceduralTexture2D == null || proceduralTexture2D.Tinput == null || proceduralTexture2D.invT == null)
            {
                sb.AppendLine(string.Format("$precision4 {0} = float4(1, 1, 1, 1);", GetVariableNameForSlot(OutputSlotRGBAId)));
                sb.AppendLine(string.Format("$precision {0} = {1}.r;", GetVariableNameForSlot(OutputSlotRId), GetVariableNameForSlot(OutputSlotRGBAId)));
                sb.AppendLine(string.Format("$precision {0} = {1}.g;", GetVariableNameForSlot(OutputSlotGId), GetVariableNameForSlot(OutputSlotRGBAId)));
                sb.AppendLine(string.Format("$precision {0} = {1}.b;", GetVariableNameForSlot(OutputSlotBId), GetVariableNameForSlot(OutputSlotRGBAId)));
                sb.AppendLine(string.Format("$precision {0} = {1}.a;", GetVariableNameForSlot(OutputSlotAId), GetVariableNameForSlot(OutputSlotRGBAId)));
                return;
            }

            // Apply hidden inputs stored in Procedural Texture 2D Asset to shader
            FindInputSlot<Texture2DInputMaterialSlot>(TinputId).texture = proceduralTexture2D.Tinput;
            FindInputSlot<Texture2DInputMaterialSlot>(InvTinputId).texture = proceduralTexture2D.invT;
            FindInputSlot<Vector4MaterialSlot>(CompressionScalersId).value = proceduralTexture2D.compressionScalers;
            FindInputSlot<Vector3MaterialSlot>(ColorSpaceOriginId).value = proceduralTexture2D.colorSpaceOrigin;
            FindInputSlot<Vector3MaterialSlot>(ColorSpaceVector1Id).value = proceduralTexture2D.colorSpaceVector1;
            FindInputSlot<Vector3MaterialSlot>(ColorSpaceVector2Id).value = proceduralTexture2D.colorSpaceVector2;
            FindInputSlot<Vector3MaterialSlot>(ColorSpaceVector3Id).value = proceduralTexture2D.colorSpaceVector3;
            FindInputSlot<Vector3MaterialSlot>(InputSizeId).value = new Vector3(
                proceduralTexture2D.Tinput.width, proceduralTexture2D.Tinput.height, proceduralTexture2D.invT.height);

            string code =
            @"
				float4 {9} = float4(0, 0, 0, 0);
				{
					float2 uvScaled = {0} * 3.464; // 2 * sqrt(3)

					const float2x2 gridToSkewedGrid = float2x2(1.0, 0.0, -0.57735027, 1.15470054);
					float2 skewedCoord = mul(gridToSkewedGrid, uvScaled);

					int2 baseId = int2(floor(skewedCoord));
					float3 temp = float3(frac(skewedCoord), 0);
					temp.z = 1.0 - temp.x - temp.y;

					float w1, w2, w3;
					int2 vertex1, vertex2, vertex3;
					if (temp.z > 0.0)
					{
						w1 = temp.z;
						w2 = temp.y;
						w3 = temp.x;
						vertex1 = baseId;
						vertex2 = baseId + int2(0, 1);
						vertex3 = baseId + int2(1, 0);
					}
					else
					{
						w1 = -temp.z;
						w2 = 1.0 - temp.y;
						w3 = 1.0 - temp.x;
						vertex1 = baseId + int2(1, 1);
						vertex2 = baseId + int2(1, 0);
						vertex3 = baseId + int2(0, 1);
					}

					float2 uv1 = {0} + frac(sin(mul(float2x2(127.1, 311.7, 269.5, 183.3), (float2)vertex1)) * 43758.5453);
					float2 uv2 = {0} + frac(sin(mul(float2x2(127.1, 311.7, 269.5, 183.3), (float2)vertex2)) * 43758.5453);
					float2 uv3 = {0} + frac(sin(mul(float2x2(127.1, 311.7, 269.5, 183.3), (float2)vertex3)) * 43758.5453);

					float2 duvdx = ddx({0});
					float2 duvdy = ddy({0});

					float4 G1 = {1}.SampleGrad({10}, uv1, duvdx, duvdy);
					float4 G2 = {1}.SampleGrad({10}, uv2, duvdx, duvdy);
					float4 G3 = {1}.SampleGrad({10}, uv3, duvdx, duvdy);

					float exponent = 1.0 + {11} * 15.0;
					w1 = pow(w1, exponent);
					w2 = pow(w2, exponent);
					w3 = pow(w3, exponent);
					float sum = w1 + w2 + w3;
					w1 = w1 / sum;
					w2 = w2 / sum;
					w3 = w3 / sum;

					float4 G = w1 * G1 + w2 * G2 + w3 * G3;
					G = G - 0.5;
					G = G * rsqrt(w1 * w1 + w2 * w2 + w3 * w3);
					G = G * {3};
					G = G + 0.5;

					duvdx *= {8}.xy;
					duvdy *= {8}.xy;
					float delta_max_sqr = max(dot(duvdx, duvdx), dot(duvdy, duvdy));
					float mml = 0.5 * log2(delta_max_sqr);
					float LOD = max(0, mml) / {8}.z;

					{9}.r = {2}.SampleLevel({2}.samplerstate, float2(G.r, LOD), 0).r;
					{9}.g = {2}.SampleLevel({2}.samplerstate, float2(G.g, LOD), 0).g;
					{9}.b = {2}.SampleLevel({2}.samplerstate, float2(G.b, LOD), 0).b;
					{9}.a = {2}.SampleLevel({2}.samplerstate, float2(G.a, LOD), 0).a;
				}
			";
            
            if (proceduralTexture2D != null && proceduralTexture2D.type != ProceduralTexture2D.TextureType.Other)
                code += "{9}.rgb = {4} + {5} * {9}.r + {6} * {9}.g + {7} * {9}.b;";
            if (proceduralTexture2D != null && proceduralTexture2D.type == ProceduralTexture2D.TextureType.Normal)
                code += "{9}.rgb = UnpackNormalmapRGorAG({9});";

            code = code.Replace("{0}", GetSlotValue(UVInput, generationMode));
            code = code.Replace("{1}", GetSlotValue(TinputId, generationMode));
            code = code.Replace("{2}", GetSlotValue(InvTinputId, generationMode));
            code = code.Replace("{3}", GetSlotValue(CompressionScalersId, generationMode));
            code = code.Replace("{4}", GetSlotValue(ColorSpaceOriginId, generationMode));
            code = code.Replace("{5}", GetSlotValue(ColorSpaceVector1Id, generationMode));
            code = code.Replace("{6}", GetSlotValue(ColorSpaceVector2Id, generationMode));
            code = code.Replace("{7}", GetSlotValue(ColorSpaceVector3Id, generationMode));
            code = code.Replace("{8}", GetSlotValue(InputSizeId, generationMode));
            code = code.Replace("{9}", GetVariableNameForSlot(OutputSlotRGBAId));

            var edgesSampler = owner.GetEdges(FindInputSlot<MaterialSlot>(SamplerInput).slotReference);
            code = code.Replace("{10}", edgesSampler.Any() ? GetSlotValue(SamplerInput, generationMode) : GetSlotValue(TinputId, generationMode) + ".samplerstate");

            code = code.Replace("{11}", GetSlotValue(BlendId, generationMode));

            sb.AppendLine(code);

            sb.AppendLine(string.Format("$precision {0} = {1}.r;", GetVariableNameForSlot(OutputSlotRId), GetVariableNameForSlot(OutputSlotRGBAId)));
            sb.AppendLine(string.Format("$precision {0} = {1}.g;", GetVariableNameForSlot(OutputSlotGId), GetVariableNameForSlot(OutputSlotRGBAId)));
            sb.AppendLine(string.Format("$precision {0} = {1}.b;", GetVariableNameForSlot(OutputSlotBId), GetVariableNameForSlot(OutputSlotRGBAId)));
            sb.AppendLine(string.Format("$precision {0} = {1}.a;", GetVariableNameForSlot(OutputSlotAId), GetVariableNameForSlot(OutputSlotRGBAId)));
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                var result = false;
                foreach (var slot in tempSlots)
                {
                    if (slot.RequiresMeshUV(channel))
                    {
                        result = true;
                        break;
                    }
                }

                tempSlots.Clear();
                return result;
            }
        }
    }
}
