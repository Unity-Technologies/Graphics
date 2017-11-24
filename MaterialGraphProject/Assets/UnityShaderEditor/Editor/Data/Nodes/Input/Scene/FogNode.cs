using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Controls;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    public enum FogMode
    {
        Linear,
        Exponential,
        ExponentialSquared
    };

    [Title("Input/Scene/Fog")]
    public class FogNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequirePosition
    {
        const string kOutputSlotName = "Color";
        const string kOutputSlot1Name = "Density";

        public const int OutputSlotId = 0;
        public const int OutputSlot1Id = 1;

        public FogNode()
        {
            name = "Fog";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector1MaterialSlot(OutputSlot1Id, kOutputSlot1Name, kOutputSlot1Name, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, OutputSlot1Id });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string uvValue = GetSlotValue(OutputSlotId, generationMode);
            string outputValue = GetSlotValue(OutputSlot1Id, generationMode);

            visitor.AddShaderChunk(string.Format("{0} {1} = unity_FogColor;",
                ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType),
                GetVariableNameForSlot(OutputSlotId)), true);

            visitor.AddShaderChunk(string.Format("{0} clipZ_01 = UNITY_Z_0_FAR_FROM_CLIPSPACE(UnityObjectToClipPos(IN.ObjectSpacePosition).z);", precision), true);
            visitor.AddShaderChunk("#if defined(FOG_LINEAR)", true);
            visitor.Indent();
            visitor.AddShaderChunk(string.Format("{0} fogFactor = saturate(clipZ_01 * unity_FogParams.z + unity_FogParams.w);", precision), true);
            visitor.AddShaderChunk(string.Format("{0} {1} = fogFactor;",
                ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlot1Id).concreteValueType),
                GetVariableNameForSlot(OutputSlot1Id)), true);
            visitor.Deindent();
            visitor.AddShaderChunk("#elif defined(FOG_EXP)", true);
            visitor.Indent();
            visitor.AddShaderChunk(string.Format("{0} fogFactor = unity_FogParams.y * clipZ_01;", precision), true);
            visitor.AddShaderChunk(string.Format("{0} {1} = {2}(saturate(exp2(-fogFactor)));",
                ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlot1Id).concreteValueType),
                GetVariableNameForSlot(OutputSlot1Id), precision), true);
            visitor.Deindent();
            visitor.AddShaderChunk("#elif defined(FOG_EXP2)", true);
            visitor.Indent();
            visitor.AddShaderChunk(string.Format("{0} fogFactor = unity_FogParams.x * clipZ_01;", precision), true);
            visitor.AddShaderChunk(string.Format("{0} {1} = {2}(saturate(exp2(-fogFactor*fogFactor)));",
                ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlot1Id).concreteValueType),
                GetVariableNameForSlot(OutputSlot1Id), precision), true);
            visitor.Deindent();
            visitor.AddShaderChunk("#else", true);
            visitor.Indent();
            visitor.AddShaderChunk(string.Format("{0} {1} = 0.0h;",
                ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlot1Id).concreteValueType),
                GetVariableNameForSlot(OutputSlot1Id)), true);
            visitor.Deindent();
            visitor.AddShaderChunk("#endif", true);
        }

        public NeededCoordinateSpace RequiresPosition()
        {
            return CoordinateSpace.Object.ToNeededCoordinateSpace();
        }
    }
}
