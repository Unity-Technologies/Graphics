using System;
using System.Linq;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Operators/Boolean Logic/Not")]
    [SeacherHelp("Outputs true only if the boolean input is false.")]
    public class NotOperator : MathNode
    {
        public override string Title
        {
            get => "Not";
            set {}
        }

        public override TypeHandle[] ValueInputTypes => new[] { TypeHandle.Bool };

        public IPortModel Input { get; private set; }

        public override Value Evaluate()
        {
            return !GetValue(Input).Bool;
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            Input = this.AddDataInputPort<bool>("input");
            this.AddDataOutputPort<bool>("output");
        }

        public override string CompileToCSharp(MathBookGraphProcessor context)
        {
            var variable = context.GenerateCodeForPort(Input);
            var result = context.DeclareVariable(OutputsById.First().Value.DataTypeHandle, "");
            context.Statements.Add($"{result} = !{variable}");
            return result;
        }
    }
}
