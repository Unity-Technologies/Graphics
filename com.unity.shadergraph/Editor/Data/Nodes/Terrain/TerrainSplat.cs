using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Terrain", "Terrain Splat")]
    class TerrainSplat : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV // IGeneratesFunction
    {
        const int InputUVId = 0;
        const int InputSplatId = 1;
        const int OutputControlRId = 2;
        const int OutputControlGId = 3;
        const int OutputControlBId = 4;
        const int OutputControlAId = 5;

        const string kInputUVSlotName = "UV";
        const string kInputSplatSlotName = "Splat Index";
        const string kOutputControlRSlotName = "Control(r)";
        const string kOutputControlGSlotName = "Control(g)";
        const string kOutputControlBSlotName = "Control(b)";
        const string kOutputControlASlotName = "Control(a)";

        public TerrainSplat()
        {
            name = "Terrain Splat";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new UVMaterialSlot(InputUVId, kInputUVSlotName, kInputUVSlotName, UVChannel.UV0));
            AddSlot(new Vector1MaterialSlot(InputSplatId, kInputSplatSlotName, kInputSplatSlotName, SlotType.Input, 0));
            AddSlot(new Vector1MaterialSlot(OutputControlRId, kOutputControlRSlotName, kOutputControlRSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputControlGId, kOutputControlGSlotName, kOutputControlGSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputControlBId, kOutputControlBSlotName, kOutputControlBSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputControlAId, kOutputControlASlotName, kOutputControlASlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[] { InputUVId, InputSplatId, OutputControlRId, OutputControlGId, OutputControlBId, OutputControlAId, });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var inputSplatIndexValue = GetSlotValue(InputSplatId, GenerationMode.ForReals);
            var inputSplatIndex = int.Parse(inputSplatIndexValue);

            var controlRType = FindOutputSlot<MaterialSlot>(OutputControlRId).concreteValueType.ToShaderString();
            var controlGType = FindOutputSlot<MaterialSlot>(OutputControlGId).concreteValueType.ToShaderString();
            var controlBType = FindOutputSlot<MaterialSlot>(OutputControlBId).concreteValueType.ToShaderString();
            var controlAType = FindOutputSlot<MaterialSlot>(OutputControlAId).concreteValueType.ToShaderString();

            var controlRValue = GetVariableNameForSlot(OutputControlRId);
            var controlGValue = GetVariableNameForSlot(OutputControlGId);
            var controlBValue = GetVariableNameForSlot(OutputControlBId);
            var controlAValue = GetVariableNameForSlot(OutputControlAId);

            sb.AppendLine("");
            if (inputSplatIndex == 0)
                sb.AppendLine("#if defined(UNIVERSAL_TERRAIN_ENABLED)");
            else
                sb.AppendLine("#if defined(UNIVERSAL_TERRAIN_ENABLED) && defined(_TERRAIN_8_LAYERS)");
            sb.AppendLine("    #ifndef SPLAT_CONTROL");
            sb.AppendLine("    #define SPLAT_CONTROL");
            sb.AppendLine("float2 splatUV = (IN.uv0.xy * (_Control_TexelSize.zw - 1.0) + 0.5) * _Control_TexelSize.xy;");
            sb.AppendLine("half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);");
            sb.AppendLine("    #endif // SPLAT_CONTROL");
            sb.AppendLine("{0} {1} = splatControl.r;", controlRType, controlRValue);
            sb.AppendLine("{0} {1} = splatControl.g;", controlGType, controlGValue);
            sb.AppendLine("{0} {1} = splatControl.b;", controlBType, controlBValue);
            sb.AppendLine("{0} {1} = splatControl.a;", controlAType, controlAValue);
            if (inputSplatIndex == 0)
                sb.AppendLine("#elif defined(HD_TERRAIN_ENABLED)");
            else
                sb.AppendLine("#elif defined(HD_TERRAIN_ENABLED) && defined(_TERRAIN_8_LAYERS)");
            sb.AppendLine("    #ifndef SPLAT_CONTROL{0}", inputSplatIndexValue);
            sb.AppendLine("    #define SPLAT_CONTROL{0}", inputSplatIndexValue);
            sb.AppendLine("float2 blendUV{0} = (IN.uv0.xy * (_Control{0}_TexelSize.zw - 1.0) + 0.5) * _Control{0}_TexelSize.xy;", inputSplatIndexValue);
            sb.AppendLine("float4 splatControl{0} = SAMPLE_TEXTURE2D(_Control{0}, sampler_Control0, blendUV{0});", inputSplatIndexValue);
            sb.AppendLine("    #endif // SPLAT_CONTROL{0}", inputSplatIndexValue);
            sb.AppendLine("{0} {1} = splatControl{2}.r;", controlRType, controlRValue, inputSplatIndexValue);
            sb.AppendLine("{0} {1} = splatControl{2}.g;", controlGType, controlGValue, inputSplatIndexValue);
            sb.AppendLine("{0} {1} = splatControl{2}.b;", controlBType, controlBValue, inputSplatIndexValue);
            sb.AppendLine("{0} {1} = splatControl{2}.a;", controlAType, controlAValue, inputSplatIndexValue);
            sb.AppendLine("#else");
            sb.AppendLine("{0} {1} = 0.0;", controlRType, controlRValue);
            sb.AppendLine("{0} {1} = 0.0;", controlGType, controlGValue);
            sb.AppendLine("{0} {1} = 0.0;", controlBType, controlBValue);
            sb.AppendLine("{0} {1} = 0.0;", controlAType, controlAValue);
            sb.AppendLine("#endif // UNIVERSAL_TERRAIN_ENABLED / HD_TERRAIN_ENABLED");
        }

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
