using UnityEngine.MaterialGraph;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Logic/If")]
    public class IfNode : Function4Input, IGeneratesFunction
    {
        public enum ComparisonOperationType
        {
            Equal = 0,
            NotEqual,
            GreaterThan,
            GreaterThanOfEqual,
            LessThan,
            LessThanOfEqual
        }

        [SerializeField]
        private ComparisonOperationType m_comparisonOperation = ComparisonOperationType.Equal;

        public ComparisonOperationType ComparisonOperation
        {
            get { return m_comparisonOperation; }
            set
            {
                if (m_comparisonOperation == value)
                    return;

                m_comparisonOperation = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        public IfNode()
        {
            name = "If";
        }

        protected override string GetFunctionName()
        {
            return "unity_if_" + precision;
        }

        protected override string GetInputSlot1Name()
        {
            return "A";
        }

        protected override string GetInputSlot2Name()
        {
            return "B";
        }

        protected override string GetInputSlot3Name()
        {
            return "True Value";
        }

        protected override string GetInputSlot4Name()
        {
            return "False Value";
        }

        protected override MaterialSlot GetInputSlot1()
        {
            return new MaterialSlot(InputSlot1Id, GetInputSlot1Name(), kInputSlot1ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        protected override MaterialSlot GetInputSlot2()
        {
            return new MaterialSlot(InputSlot2Id, GetInputSlot2Name(), kInputSlot2ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        protected override MaterialSlot GetInputSlot3()
        {
            return new MaterialSlot(InputSlot3Id, GetInputSlot3Name(), kInputSlot3ShaderName, UnityEngine.Graphing.SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
        }

        protected override MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, UnityEngine.Graphing.SlotType.Output, SlotValueType.Dynamic, Vector4.zero);
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype("a", "b", "trueOutputValue", "falseOutputValue"), false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();


            if (m_comparisonOperation == ComparisonOperationType.Equal)
            {
                outputString.AddShaderChunk("if (a == b)", false);
            }
            else if (m_comparisonOperation == ComparisonOperationType.NotEqual)
            {
                outputString.AddShaderChunk("if (a != b)", false);
            }
            else if (m_comparisonOperation == ComparisonOperationType.GreaterThan)
            {
                outputString.AddShaderChunk("if (a > b)", false);
            }
            else if (m_comparisonOperation == ComparisonOperationType.GreaterThanOfEqual)
            {
                outputString.AddShaderChunk("if (a >= b)", false);
            }
            else if (m_comparisonOperation == ComparisonOperationType.LessThan)
            {
                outputString.AddShaderChunk("if (a < b)", false);
            }
            else if (m_comparisonOperation == ComparisonOperationType.LessThanOfEqual)
            {
                outputString.AddShaderChunk("if (a <= b)", false);
            }

            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return trueOutputValue;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);
            outputString.AddShaderChunk("else", false);
            outputString.AddShaderChunk("{", false);
            outputString.Indent();
            outputString.AddShaderChunk("return falseOutputValue;", false);
            outputString.Deindent();
            outputString.AddShaderChunk("}", false);


            outputString.Deindent();
            outputString.AddShaderChunk("}", false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }
    }
}
