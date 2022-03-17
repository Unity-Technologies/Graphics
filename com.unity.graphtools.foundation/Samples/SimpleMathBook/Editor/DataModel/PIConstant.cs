using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Values/Pi")]
    public class PIConstant : MathNode
    {
        public override string Title
        {
            get => "π (Pi)";
            set {}
        }

        public override Value Evaluate()
        {
            return Mathf.PI;
        }

        /// <inheritdoc />
        public override string CompileToCSharp(MathBookGraphProcessor context)
        {
            var variable = context.DeclareVariable(OutputsById.First().Value.DataTypeHandle, "");
            context.Statements.Add($"{variable} = Mathf.PI");
            return variable;
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            this.AddDataOutputPort<float>("");
        }
    }
}
