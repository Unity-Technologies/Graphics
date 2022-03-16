using System;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public abstract class CompareOperator : MathNode
    {
        public IPortModel InputA { get; private set; }
        public IPortModel InputB { get; private set; }

        public abstract bool Compare(float a, float b);

        public abstract string CSharpCompareOperator();

        public override Value Evaluate()
        {
            return Compare(GetValue(InputA).Float, GetValue(InputB).Float);
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            InputA = this.AddDataInputPort<float>("A");
            InputB = this.AddDataInputPort<float>("B");
            this.AddDataOutputPort<bool>("out");
        }

        public override string CompileToCSharp(MathBookGraphProcessor context)
        {
            var variable1 = context.GenerateCodeForPort(InputA);
            var variable2 = context.GenerateCodeForPort(InputB);
            var result = context.DeclareVariable(OutputsById.First().Value.DataTypeHandle, "");
            context.Statements.Add($"{result} = {variable1} {CSharpCompareOperator()} {variable2}");
            return result;
        }
    }
}
