using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Vector/TangentToWorld")]
    public class TangentToWorldNode : Function1Input, IGeneratesFunction, IMayRequireNormal, IMayRequireTangent, IMayRequireBitangent
    {
        public TangentToWorldNode()
        {
            name = "TangentToWorld";
        }

        protected override string GetFunctionName()
        {
            return "unity_tangenttoworld_" + precision;
        }

        protected override MaterialSlot GetInputSlot()
        {
            return new MaterialSlot(InputSlotId, GetInputSlotName(), kInputSlotShaderName, SlotType.Input, SlotValueType.Vector3, Vector4.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector3, Vector4.zero);
        }

        protected override string GetFunctionPrototype(string argName)
        {
            return "inline " + precision + outputDimension + " " + GetFunctionName() + " ("
                   + precision + inputDimension + " " + argName + ", float3 tangent, float3 bitangent, float3 normal )";
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("arg1"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            //outputString.AddShaderChunk("float3x3 tangentToWorld = transpose(float3x3(" + ShaderGeneratorNames.WorldSpaceTangent + ", " + ShaderGeneratorNames.WorldSpaceBitangent + ", " + ShaderGeneratorNames.WorldSpaceNormal + "));", false);
            outputString.AddShaderChunk("float3x3 tangentToWorld = transpose(float3x3(tangent, bitangent, normal));", false);
            outputString.AddShaderChunk("return saturate(mul(tangentToWorld, normalize(arg1)));", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        protected override string GetFunctionCallBody(string inputValue)
        {
            return GetFunctionName() + " (" + inputValue + ", "+ShaderGeneratorNames.WorldSpaceTangent + ", " + ShaderGeneratorNames.WorldSpaceBitangent + ", " + ShaderGeneratorNames.WorldSpaceNormal +")";
        }

        public bool RequiresNormal()
        {
            return true;
        }

        public bool RequiresTangent()
        {
            return true;
        }

        public bool RequiresBitangent()
        {
            return true;
        }
    }
}
