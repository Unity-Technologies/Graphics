using UnityEngine;
using UnityEditor.Graphing;

/*namespace UnityEditor.ShaderGraph
{
    [Title("OLD", "Parallax")]
    public class ParallaxNode : AbstractMaterialNode, IGeneratesBodyCode, IGeneratesFunction, IMayRequireMeshUV, IMayRequireViewDirection
    {
        protected const string kInputSlot1ShaderName = "Depth";
        protected const string kOutputSlotShaderName = "UV";

        public const int InputSlot1Id = 0;
        public const int OutputSlotId = 1;

        public override bool hasPreview
        {
            get { return true; }
        }

        public override PreviewMode previewMode
        {
            get
            {
                return PreviewMode.Preview3D;
            }
        }

        public ParallaxNode()
        {
            name = "Parallax";
            UpdateNodeAfterDeserialization();
        }

        public string GetFunctionName()
        {
            return "unity_parallax_" + precision;
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(GetInputSlot1());
            AddSlot(GetOutputSlot());
            RemoveSlotsNameNotMatching(validSlots);
        }

        protected int[] validSlots
        {
            get { return new[] { InputSlot1Id, OutputSlotId }; }
        }

        protected virtual MaterialSlot GetInputSlot1()
        {
            return new Vector1MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, SlotType.Input, 1);
        }

        protected virtual MaterialSlot GetOutputSlot()
        {
            return new Vector2MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, Vector2.zero);
        }

        protected virtual string GetInputSlot1Name()
        {
            return kInputSlot1ShaderName;
        }

        protected virtual string GetOutputSlotName()
        {
            return kOutputSlotShaderName;
        }

        protected virtual string GetFunctionPrototype(string arg1Name, string arg2Name, string arg3Name)
        {
            return "inline " + precision + "2 " + GetFunctionName() + " (" +
                precision + " " + arg1Name + ", " +
                precision + "2 " + arg2Name + ", " +
                precision + "3 " + arg3Name + ")";
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            NodeUtils.SlotConfigurationExceptionIfBadConfiguration(this, new[] { InputSlot1Id }, new[] { OutputSlotId });
            string input1Value = GetSlotValue(InputSlot1Id, generationMode);

            visitor.AddShaderChunk(precision + "2 " + GetVariableNameForSlot(OutputSlotId) + " = " + GetFunctionCallBody(input1Value) + ";", true);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("depth", "UVs", "viewTangentSpace"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return UVs + depth * viewTangentSpace.xy / viewTangentSpace.z;", false);

            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        protected virtual string GetFunctionCallBody(string inputValue1)
        {
            var channel = UVChannel.uv0;

            return GetFunctionName() + " (" +
                inputValue1 + ", " +
                channel.GetUVName() + ", " +
                CoordinateSpace.View.ToVariableName(InterpolatorType.Tangent) + ")";
        }

        public bool RequiresMeshUV(UVChannel channel)
        {
            return channel == UVChannel.uv0;
        }

        public NeededCoordinateSpace RequiresViewDirection()
        {
            return NeededCoordinateSpace.Tangent;
        }
    }
}*/
