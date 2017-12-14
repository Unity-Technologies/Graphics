using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Scene", "Fog")]
    public class FogNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequirePosition
    {
        public FogNode()
        {
            name = "Fog";
            UpdateNodeAfterDeserialization();
        }

        const int OutputSlotId = 0;
        const int OutputSlot1Id = 1;
        const string k_OutputSlotName = "Color";
        const string k_OutputSlot1Name = "Density";

        public override bool hasPreview
        {
            get { return false; }
        }

        string GetFunctionName()
        {
            return string.Format("Unity_Fog_{0}", precision);
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, k_OutputSlotName, k_OutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector1MaterialSlot(OutputSlot1Id, k_OutputSlot1Name, k_OutputSlot1Name, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, OutputSlot1Id });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var colorValue = GetSlotValue(OutputSlotId, generationMode);
            var densityValue = GetSlotValue(OutputSlot1Id, generationMode);
            visitor.AddShaderChunk(string.Format("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision), GetVariableNameForSlot(OutputSlotId)), false);
            visitor.AddShaderChunk(string.Format("{0} {1};", FindOutputSlot<MaterialSlot>(OutputSlot1Id).concreteValueType.ToString(precision), GetVariableNameForSlot(OutputSlot1Id)), false);
            visitor.AddShaderChunk(string.Format("{0}(IN.{1}, {2}, {3});", GetFunctionName(), CoordinateSpace.Object.ToVariableName(InterpolatorType.Position), colorValue, densityValue), false);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var sg = new ShaderStringBuilder();
            sg.AppendLine("void {0}({1}3 ObjectSpacePosition, out {2} Color, out {3} Density)",
                GetFunctionName(),
                precision,
                FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToString(precision),
                FindOutputSlot<MaterialSlot>(OutputSlot1Id).concreteValueType.ToString(precision));
            using (sg.BlockScope())
            {
                sg.AppendLine("Color = unity_FogColor;");

                sg.AppendLine("{0} clipZ_01 = UNITY_Z_0_FAR_FROM_CLIPSPACE(UnityObjectToClipPos(ObjectSpacePosition).z);", precision);
                sg.AppendLine("#if defined(FOG_LINEAR)");
                using (sg.IndentScope())
                {
                    sg.AppendLine("{0} fogFactor = saturate(clipZ_01 * unity_FogParams.z + unity_FogParams.w);", precision);
                    sg.AppendLine("Density = fogFactor;");
                }
                sg.AppendLine("#elif defined(FOG_EXP)");
                using (sg.IndentScope())
                {
                    sg.AppendLine("{0} fogFactor = unity_FogParams.y * clipZ_01;", precision);
                    sg.AppendLine("Density = saturate(exp2(-fogFactor));");
                }
                sg.AppendLine("#elif defined(FOG_EXP2)");
                using (sg.IndentScope())
                {
                    sg.AppendLine("{0} fogFactor = unity_FogParams.x * clipZ_01;", precision);
                    sg.AppendLine("Density = saturate(exp2(-fogFactor*fogFactor));");
                }
                sg.AppendLine("#else");
                using (sg.IndentScope())
                {
                    sg.AppendLine("Density = 0.0h;");
                }
                sg.AppendLine("#endif");
            }

            visitor.AddShaderChunk(sg.ToString(), true);
        }

        public NeededCoordinateSpace RequiresPosition()
        {
            return CoordinateSpace.Object.ToNeededCoordinateSpace();
        }
    }
}
