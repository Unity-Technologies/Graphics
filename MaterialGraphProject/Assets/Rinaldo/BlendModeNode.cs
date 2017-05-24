using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Art/BlendMode")]
    public class BlendModeNode : Function2Input, IGeneratesFunction
    {
        public BlendModeNode()
        {
            name = "BlendMode";
        }
        
        protected override string GetFunctionName()
        {
            return "unity_blendmode_" + System.Enum.GetName(typeof(BlendModesEnum), m_BlendMode);
        }
        protected override string GetInputSlot1Name()
        {
            return "A|Blend/Src";
        }
        protected override string GetInputSlot2Name()
        {
            return "B|Base/Dest";
        }

        [SerializeField]
        private BlendModesEnum m_BlendMode;
        public BlendModesEnum blendMode
        {
            get { return m_BlendMode; }
            set
            {
                if (m_BlendMode == value)
                    return;

                m_BlendMode = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }
/*This is to overide input to be vector 3, not really necessary */
/*
        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, SlotType.Input, SlotValueType.Vector3, Vector4.zero);
        }
        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, SlotType.Input, SlotValueType.Vector3, Vector4.zero);
        }
        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector3, Vector4.zero);
        }
*/
        public override bool hasPreview { get { return true; } }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            switch (m_BlendMode)
            {
                case BlendModesEnum.AddSub:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk("return 2.0 * arg1 + arg2 - 1.0;", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.Burn:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk("return 1.0 - (1.0 - arg2)/arg1;", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.Darken:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk("return min(arg2,arg1);", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.Difference:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk("return abs(arg2-arg1);", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.Dodge:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk("return arg2 / (1.0 - arg1);", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.Divide:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk("return arg2 / (arg1 + 0.000000000001);", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.Exclusion:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
//                    outputString.AddShaderChunk("return arg2 + arg1 - (" + precision + "3(2.0,2.0,2.0)*arg2*arg1);", false);
                    outputString.AddShaderChunk("return arg2 + arg1 - (2.0 *arg2*arg1);", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.HardLight:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk(precision + outputDimension + " result1 = 1.0 - 2.0 * (1.0 - arg1) * (1.0 - arg2);", false);
                    outputString.AddShaderChunk(precision + outputDimension + " result2 = 2.0 * arg1 * arg2;", false);
                    outputString.AddShaderChunk(precision + outputDimension + " zeroOrOne = step(arg1, 0.5);", false);
                    outputString.AddShaderChunk("return result2 * zeroOrOne + (1 - zeroOrOne) * result1;", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.HardMix:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk("return step(1-arg1, arg2);", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.Lighten:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk("return max(arg2,arg1);", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.LinearBurn:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk("return arg1 + arg2 - 1.0;", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.LinearDodge:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk("return arg1 + arg2;", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.LinearLight:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk("return arg2 + 2.0 * arg1 - 1.0;", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.Multiply:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk("return arg1 * arg2;", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.Negation:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk("return 1.0 - abs(1.0 - arg2 - arg1);", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.Overlay:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk(precision + outputDimension + " result1 = 1.0 - 2.0 * (1.0 - arg1) * (1.0 - arg2);", false);
                    outputString.AddShaderChunk(precision + outputDimension + " result2 = 2.0 * arg1 * arg2;", false);
                    outputString.AddShaderChunk(precision + outputDimension + " zeroOrOne = step(arg2, 0.5);", false);
                    outputString.AddShaderChunk("return result2 * zeroOrOne + (1 - zeroOrOne) * result1;", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.PinLight:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk(precision + outputDimension + " check = step (0.5, arg1);", false);
                    outputString.AddShaderChunk(precision + outputDimension + " result = check * max(2.0*(arg1 - 0.5), arg2);", false);
                    outputString.AddShaderChunk("return result += (1.0 - check) * min(2.0 * arg1,arg2);", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.Screen:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk("return 1.0 - (1.0-arg2) * (1.0 - arg1);", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.SoftLight:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk(precision + outputDimension + " result1= 2.0 * arg2 * arg1 + arg2*arg2 - 2.0 * arg2*arg2*arg1;", false);
                    outputString.AddShaderChunk(precision + outputDimension + " result2= 2.0* sqrt(arg2) * arg1 - sqrt(arg2) + 2.0 * arg2 - 2.0 * arg2*arg1;", false);
                    outputString.AddShaderChunk(precision + outputDimension + " zeroOrOne = step(0.5, arg1);", false);
                    outputString.AddShaderChunk("return result2 * zeroOrOne + (1 - zeroOrOne) * result1;", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.Substract:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk("return arg2 - arg1;", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
                case BlendModesEnum.VividLight:
                    outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2"), false);
                    outputString.AddShaderChunk("{", false);
                    outputString.Indent();
                    outputString.AddShaderChunk(precision + outputDimension + " result1= 1.0-(1.0-arg2)/(2.0*arg1);", false);
                    outputString.AddShaderChunk(precision + outputDimension + " result2= arg2/(2.0*(1.0-arg1));", false);
                    outputString.AddShaderChunk(precision + outputDimension + " zeroOrOne = step(0.5, arg1);", false);
                    outputString.AddShaderChunk("return result2 * zeroOrOne + (1 - zeroOrOne) * result1;", false);
                    outputString.Deindent();
                    outputString.AddShaderChunk("}", false);
                    break;
            }
            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}

