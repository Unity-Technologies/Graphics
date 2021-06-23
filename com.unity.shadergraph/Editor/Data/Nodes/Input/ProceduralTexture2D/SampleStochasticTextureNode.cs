using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;


namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Sample Stochastic Texture")]
    class SampleStochasticTextureNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        public const int OutputSlotRGBAId = 0;
        public const int OutputSlotRId = 1;
        public const int OutputSlotGId = 2;
        public const int OutputSlotBId = 3;
        public const int OutputSlotAId = 4;
        public const int StochasticTextureId = 5;
        public const int SamplerInputId = 6;
        public const int UVInputId = 7;
        public const int BlendId = 8;

        const string kOutputSlotRGBAName = "RGBA";
        const string kOutputSlotRName = "R";
        const string kOutputSlotGName = "G";
        const string kOutputSlotBName = "B";
        const string kOutputSlotAName = "A";
        const string kStochasticTextureName = "StochasticTexture";
        const string kSamplerInputName = "Sampler";
        const string kUVInputName = "UV";
        const string kBlendIdName = "Blend";

        public override bool hasPreview { get { return true; } }

        public SampleStochasticTextureNode()
        {
            name = "Sample Stochastic Texture";
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
            AddSlot(new StochasticTextureInputMaterialSlot(StochasticTextureId, kStochasticTextureName, kStochasticTextureName, ShaderStageCapability.Fragment, false));     // TODO: input slot
            AddSlot(new SamplerStateMaterialSlot(SamplerInputId, kSamplerInputName, kSamplerInputName, SlotType.Input));
            AddSlot(new UVMaterialSlot(UVInputId, kUVInputName, kUVInputName, UVChannel.UV0));
            AddSlot(new Vector1MaterialSlot(BlendId, kBlendIdName, kBlendIdName, SlotType.Input, 0, ShaderStageCapability.Fragment));

            RemoveSlotsNameNotMatching(new[]
              { OutputSlotRGBAId, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId,
                StochasticTextureId, SamplerInputId, UVInputId, BlendId });
        }

        public override void ValidateNode()
        {
            base.ValidateNode();
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            StochasticTextureMaterialSlot slot = FindInputSlot<StochasticTextureMaterialSlot>(StochasticTextureId);

            string code =
@"
	float4 {o} = float4(0, 0, 0, 0);
	{
        UnityStochasticTexture2D stex = {st};
        float2 uv = {uv};
		float2 uvScaled = uv * 3.464; // 2 * sqrt(3)

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

		float2 uv1 = uv + frac(sin(mul(float2x2(127.1, 311.7, 269.5, 183.3), (float2)vertex1)) * 43758.5453);
		float2 uv2 = uv + frac(sin(mul(float2x2(127.1, 311.7, 269.5, 183.3), (float2)vertex2)) * 43758.5453);
		float2 uv3 = uv + frac(sin(mul(float2x2(127.1, 311.7, 269.5, 183.3), (float2)vertex3)) * 43758.5453);

		float2 duvdx = ddx(uv);
		float2 duvdy = ddy(uv);

        SamplerState ss = {ss};
		float4 G1 = stex.tex.SampleGrad(ss, uv1, duvdx, duvdy);
		float4 G2 = stex.tex.SampleGrad(ss, uv2, duvdx, duvdy);
		float4 G3 = stex.tex.SampleGrad(ss, uv3, duvdx, duvdy);

		float exponent = 1.0 + {blend} * 15.0;
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
		G = G * stex.compressionScalers;
		G = G + 0.5;

		duvdx *= stex.tex.texelSize.zw;
		duvdy *= stex.tex.texelSize.zw;
		float delta_max_sqr = max(dot(duvdx, duvdx), dot(duvdy, duvdy));
		float mml = 0.5 * log2(delta_max_sqr);
		float LOD = max(0, mml) * stex.invT.texelSize.y;

		{o}.r = stex.invT.SampleLevel(stex.invT.samplerstate, float2(G.r, LOD), 0).r;
		{o}.g = stex.invT.SampleLevel(stex.invT.samplerstate, float2(G.g, LOD), 0).g;
		{o}.b = stex.invT.SampleLevel(stex.invT.samplerstate, float2(G.b, LOD), 0).b;
		{o}.a = stex.invT.SampleLevel(stex.invT.samplerstate, float2(G.a, LOD), 0).a;

//        if (stex.type != 2)           // TODO
//        {
            {o}.rgb = stex.colorSpaceOrigin + stex.colorSpaceVector1 * {o}.r + stex.colorSpaceVector2 * {o}.g + stex.colorSpaceVector3 * {o}.b;
//        }
//        if (stex.type == 1)
//        {
//            {o}.rgb = UnpackNormalmapRGorAG({o});
//        }
	}
";

            code = code.Replace("{st}", GetSlotValue(StochasticTextureId, generationMode));
            code = code.Replace("{uv}", GetSlotValue(UVInputId, generationMode));
            code = code.Replace("{o}", GetVariableNameForSlot(OutputSlotRGBAId));

            var edgesSampler = owner.GetEdges(FindInputSlot<MaterialSlot>(SamplerInputId).slotReference);
            code = code.Replace("{ss}", edgesSampler.Any() ? GetSlotValue(SamplerInputId, generationMode) : "stex.tex.samplerstate");

            code = code.Replace("{blend}", GetSlotValue(BlendId, generationMode));

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
