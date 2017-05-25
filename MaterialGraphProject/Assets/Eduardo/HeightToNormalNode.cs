using UnityEngine.Graphing;
using System.Linq;
using System.Collections;

namespace UnityEngine.MaterialGraph
{
    [Title("Utility/Heightmap To Normalmap")]
    public class HeightToNormalNode : FunctionNInNOut, IGeneratesFunction, IGenerateProperties
    {

        public HeightToNormalNode()
        {
            name = "HeightToNormal";
            AddSlot("HeightMap", "heightmap", Graphing.SlotType.Input, SlotValueType.Texture2D, Vector4.zero);
            AddSlot("UV", "texCoord", Graphing.SlotType.Input, SlotValueType.Vector2, Vector4.zero);
            AddSlot("Offset", "texOffset", Graphing.SlotType.Input, SlotValueType.Vector1, new Vector4(0.005f, 0,0,0));
            AddSlot("Strength", "strength", Graphing.SlotType.Input, SlotValueType.Vector1, new Vector4(8,0,0,0));

            AddSlot("Normal", "normalRes", Graphing.SlotType.Output, SlotValueType.Vector3, Vector4.zero);

            UpdateNodeAfterDeserialization();
        }

        protected override string GetFunctionName()
        {
            return "unity_HeightToNormal";
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            base.GeneratePropertyUsages(visitor, generationMode);

            visitor.AddShaderChunk("#ifdef UNITY_COMPILER_HLSL", false);
            visitor.AddShaderChunk("SamplerState my_linear_repeat_sampler;", false);
            visitor.AddShaderChunk("#endif", false);
        }
        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype(), false);
            outputString.AddShaderChunk("{", false);
            outputString.AddShaderChunk("float2 offsetU = float2(texCoord.x + texOffset, texCoord.y);", false);
            outputString.AddShaderChunk("float2 offsetV = float2(texCoord.x, texCoord.y + texOffset);", false);

            outputString.AddShaderChunk("float normalSample = 0;", false);
            outputString.AddShaderChunk("float uSample = 0;", false);
            outputString.AddShaderChunk("float vSample = 0;", false);

            visitor.AddShaderChunk("#ifdef UNITY_COMPILER_HLSL", false);

            outputString.AddShaderChunk("normalSample = heightmap.Sample(my_linear_repeat_sampler, texCoord).r;", false);
            outputString.AddShaderChunk("uSample = heightmap.Sample(my_linear_repeat_sampler, offsetU).r;", false);
            outputString.AddShaderChunk("vSample = heightmap.Sample(my_linear_repeat_sampler, offsetV).r;", false);

            visitor.AddShaderChunk("#endif", false);

            outputString.AddShaderChunk("float uMinusNormal = uSample - normalSample;", false);
            outputString.AddShaderChunk("float vMinusNormal = vSample - normalSample;", false);

            outputString.AddShaderChunk("uMinusNormal = uMinusNormal * strength;", false);
            outputString.AddShaderChunk("vMinusNormal = vMinusNormal * strength;", false);

            outputString.AddShaderChunk("float3 va = float3(1, 0, uMinusNormal);", false);
            outputString.AddShaderChunk("float3 vb = float3(0, 1, vMinusNormal);", false);

            outputString.AddShaderChunk("normalRes = cross(va, vb);", false);


            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
