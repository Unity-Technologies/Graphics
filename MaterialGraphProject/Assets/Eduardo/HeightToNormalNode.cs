using UnityEngine.Graphing;
using System.Linq;
using System.Collections;

namespace UnityEngine.MaterialGraph
{
    [Title("HeightToNormal")]
    public class HeightToNormalNode : FunctionNInNOut, IGeneratesFunction
    {

        public HeightToNormalNode()
        {
            name = "HeightToNormal";
            AddSlot("HeightMap", "heightmap", Graphing.SlotType.Input, SlotValueType.sampler2D, Vector4.zero);
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

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype(), false);
            outputString.AddShaderChunk("{", false);
            outputString.AddShaderChunk("float2 offsetU = float2(texCoord.x + texOffset, texCoord.y);", false);
            outputString.AddShaderChunk("float2 offsetV = float2(texCoord.x, texCoord.y + texOffset);", false);

            outputString.AddShaderChunk("float normalSample = tex2D(heightmap, texCoord).r;", false);
            outputString.AddShaderChunk("float uSample = tex2D(heightmap, offsetU).r;", false);
            outputString.AddShaderChunk("float vSample = tex2D(heightmap, offsetV).r;", false);

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
