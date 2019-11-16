using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{

    [Title("Utility", "SpeedTree", "HueVariation")]
    class HueVariationNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequirePosition, IMayRequireNormal, IGeneratesFunction
    {

        public const int OutputColorSlotId = 0;
        private const string OutputColorSlotName = "OutColor";

        public const int PositionSlotId = 1;
        public const string PositionSlotName = "Position";

        public const int NormalSlotId = 2;
        public const string NormalSlotName = "Normal";

        public const int BaseColorSlotId = 3;
        public const string BaseColorSlotName = "Base Color";

        public const int VariationColorSlotId = 4;
        public const string VariationColorSlotName = "Hue Variation Color";

        public override bool hasPreview { get { return false; } }

        public HueVariationNode()
        {
            name = "SpeedTree HueVariation";
            UpdateNodeAfterDeserialization();
        }

        string GetFunctionName()
        {
            return $"SpeedTree_DefaultHueVariation_{concretePrecision.ToShaderString()}";
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new ColorRGBMaterialSlot(OutputColorSlotId, OutputColorSlotName, OutputColorSlotName, SlotType.Output, Color.white, ColorMode.Default));

            AddSlot(new PositionMaterialSlot(PositionSlotId, PositionSlotName, PositionSlotName, CoordinateSpace.Object));
            AddSlot(new NormalMaterialSlot(NormalSlotId, NormalSlotName, NormalSlotName, CoordinateSpace.Object));
            AddSlot(new ColorRGBMaterialSlot(BaseColorSlotId, BaseColorSlotName, BaseColorSlotName, SlotType.Input, Color.white, ColorMode.Default));
            AddSlot(new ColorRGBAMaterialSlot(VariationColorSlotId, VariationColorSlotName, VariationColorSlotName, SlotType.Input, Color.white));

            RemoveSlotsNameNotMatching(new[]
            {
                OutputColorSlotId, PositionSlotId, NormalSlotId,
                BaseColorSlotId, VariationColorSlotId
            });
        }

        public NeededCoordinateSpace RequiresPosition(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.Object;
        }

        public NeededCoordinateSpace RequiresNormal(ShaderStageCapability stageCapability)
        {
            return NeededCoordinateSpace.Object;
        }

        public void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode)
        {
            // This node is fortunately, one of those which is the same for both SpeedTree7 and SpeedTree8
            registry.ProvideFunction(GetFunctionName(), s =>
            {
                s.AppendLine("void {0}({1} ObjPos, {2} ObjNorm, {3} BaseColor, {4} HueColor, out {5} ResultColor)", GetFunctionName(),
                    FindInputSlot<MaterialSlot>(PositionSlotId).concreteValueType.ToShaderString(),
                    FindInputSlot<MaterialSlot>(NormalSlotId).concreteValueType.ToShaderString(),
                    FindInputSlot<MaterialSlot>(BaseColorSlotId).concreteValueType.ToShaderString(),
                    FindInputSlot<MaterialSlot>(VariationColorSlotId).concreteValueType.ToShaderString(),
                    FindOutputSlot<MaterialSlot>(OutputColorSlotId).concreteValueType.ToShaderString());
                using (s.BlockScope())
                {
                    // Computing the hue variation amount
                    s.AppendLine("$precision4x4 objToWorld = UNITY_MATRIX_M;");
                    s.AppendLine("$precision3 objWorldPos = $precision3(objToWorld[0].w, objToWorld[1].w, objToWorld[2].w);");
                    s.AppendLine("#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)");
                    s.AppendLine("objWorldPos += _WorldSpaceCameraPos;");
                    s.AppendLine("#endif");
                    s.AppendLine("$precision hueVariationAmount = frac(dot(objWorldPos, 1));");
                    s.AppendLine("hueVariationAmount += frac(ObjPos.x + ObjNorm.y + ObjNorm.x) * 0.5 - 0.3;");
                    s.AppendLine("hueVariationAmount = saturate(hueVariationAmount * HueColor.a);");

                    // Applying variation blend to the base color
                    s.AppendLine("$precision3 shiftedColor = lerp(BaseColor, HueColor.rgb, hueVariationAmount);");
                    s.AppendLine("$precision maxBase = max(BaseColor.r, max(BaseColor.g, BaseColor.b));");
                    s.AppendLine("$precision maxShifted = max(shiftedColor.r, max(shiftedColor.g, shiftedColor.b));");
                    s.AppendLine("maxBase = (maxBase / maxShifted) * 0.5 + 0.5;");

                    s.AppendLine("ResultColor = saturate(shiftedColor.rgb * maxBase);");
                }
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var positionValue = GetSlotValue(PositionSlotId, generationMode);
            var normalValue = GetSlotValue(NormalSlotId, generationMode);
            var colorValue = GetSlotValue(BaseColorSlotId, generationMode);
            var hueVariationValue = GetSlotValue(VariationColorSlotId, generationMode);
            var outputValue = GetSlotValue(OutputColorSlotId, generationMode);

            sb.AppendLine("{0} {1};", FindOutputSlot<MaterialSlot>(OutputColorSlotId).concreteValueType.ToShaderString(), GetVariableNameForSlot(OutputColorSlotId));
            sb.AppendLine("{0}({1}, {2}, {3}, {4}, {5});", GetFunctionName(), positionValue, normalValue, colorValue, hueVariationValue, outputValue);
        }
    }
}
