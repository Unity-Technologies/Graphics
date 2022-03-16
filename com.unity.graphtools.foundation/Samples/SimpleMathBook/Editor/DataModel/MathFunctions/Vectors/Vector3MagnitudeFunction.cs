using System;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Vectors/Vector3 Magnitude")]
    [SeacherHelp("Outputs the magnitude (also known as size, or distance) of the input vector 3.")]
    public class Vector3MagnitudeFunction : MathNode
    {
        public override string Title
        {
            get => "Vector3 Magnitude";
            set {}
        }

        public override TypeHandle[] ValueInputTypes => new[] { TypeHandle.Vector3 };

        public IPortModel Input { get; private set; }

        public override Value Evaluate()
        {
            return GetValue(Input).Vector3.magnitude;
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            Input = this.AddDataInputPort<Vector3>("Input");
            this.AddDataOutputPort<float>("Output");
        }

        public override string CompileToCSharp(MathBookGraphProcessor context)
        {
            var variable = context.GenerateCodeForPort(Input);
            var result = context.DeclareVariable(OutputsById.First().Value.DataTypeHandle, "");
            context.Statements.Add($"{result} = {variable}.magnitude");
            return result;
        }
    }
}
