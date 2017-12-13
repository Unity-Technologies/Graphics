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
        const string kOutputSlotName = "Color";
        const string kOutputSlot1Name = "Density";

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
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            AddSlot(new Vector1MaterialSlot(OutputSlot1Id, kOutputSlot1Name, kOutputSlot1Name, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, OutputSlot1Id });
        }

        string GetFunctionPrototype(string argIn, string argOut, string argOut2)
        {
            return string.Format("void {0} ({1}3 {2}, out {3} {4}, out {5} {6})", GetFunctionName(), precision, argIn,
                ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), argOut,
                ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlot1Id).concreteValueType), argOut2);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string colorValue = GetSlotValue(OutputSlotId, generationMode);
            string densityValue = GetSlotValue(OutputSlot1Id, generationMode);
            visitor.AddShaderChunk(string.Format("{0} {1};", ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType), GetVariableNameForSlot(OutputSlotId)), true);
            visitor.AddShaderChunk(string.Format("{0} {1};", ConvertConcreteSlotValueTypeToString(precision, FindOutputSlot<MaterialSlot>(OutputSlot1Id).concreteValueType), GetVariableNameForSlot(OutputSlot1Id)), true);
            string objectSpacePosition = string.Format("IN.{0}", CoordinateSpace.Object.ToVariableName(InterpolatorType.Position));
            visitor.AddShaderChunk(GetFunctionCallBody(objectSpacePosition, colorValue, densityValue), true);
        }

        string GetFunctionCallBody(string objectSpaceValue, string outputValue, string output1Value)
        {
            return GetFunctionName() + " (" + objectSpaceValue + ", " + outputValue + ", " + output1Value + ");";
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var sg = new ShaderGenerator();
            sg.AddShaderChunk(GetFunctionPrototype("ObjectSpacePosition", "Color", "Density"), false);
            sg.AddShaderChunk("{", false);
            sg.Indent();

            sg.AddShaderChunk("Color = unity_FogColor;", false);

            sg.AddShaderChunk(string.Format("{0} clipZ_01 = UNITY_Z_0_FAR_FROM_CLIPSPACE(UnityObjectToClipPos(ObjectSpacePosition).z);", precision), false);
            sg.AddShaderChunk("#if defined(FOG_LINEAR)", false);
            sg.Indent();
            sg.AddShaderChunk(string.Format("{0} fogFactor = saturate(clipZ_01 * unity_FogParams.z + unity_FogParams.w);", precision), false);
            sg.AddShaderChunk("Density = fogFactor;", false);
            sg.Deindent();
            sg.AddShaderChunk("#elif defined(FOG_EXP)", false);
            sg.Indent();
            sg.AddShaderChunk(string.Format("{0} fogFactor = unity_FogParams.y * clipZ_01;", precision), false);
            sg.AddShaderChunk("Density = {2}(saturate(exp2(-fogFactor)));", false);
            sg.Deindent();
            sg.AddShaderChunk("#elif defined(FOG_EXP2)", false);
            sg.Indent();
            sg.AddShaderChunk(string.Format("{0} fogFactor = unity_FogParams.x * clipZ_01;", precision), false);
            sg.AddShaderChunk("Density = {2}(saturate(exp2(-fogFactor*fogFactor)));", false);
            sg.Deindent();
            sg.AddShaderChunk("#else", false);
            sg.Indent();
            sg.AddShaderChunk("Density = 0.0h;", false);
            sg.Deindent();
            sg.AddShaderChunk("#endif", false);

            sg.Deindent();
            sg.AddShaderChunk("}", false);

            visitor.AddShaderChunk(sg.GetShaderString(0), true);
        }

        public NeededCoordinateSpace RequiresPosition()
        {
            return CoordinateSpace.Object.ToNeededCoordinateSpace();
        }
    }
}
