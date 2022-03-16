using System;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Vectors/Vector3 Dot Product")]
    [SeacherHelp("Outputs the dot product of the two input vector 3.")]
    public class Vector3DotProduct : MathNode
    {
        public override string Title
        {
            get => "Vector3 Dot Product";
            set {}
        }

        public override TypeHandle[] ValueInputTypes => new[] { TypeHandle.Vector3 };

        public IPortModel InputA { get; private set; }
        public IPortModel InputB { get; private set; }

        public override Value Evaluate()
        {
            return Vector3.Dot(GetValue(InputA).Vector3, GetValue(InputB).Vector3);
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            InputA = this.AddDataInputPort<Vector3>("A");
            InputB = this.AddDataInputPort<Vector3>("B");
            this.AddDataOutputPort<float>("Output");
        }

        public override string CompileToCSharp(MathBookGraphProcessor context)
        {
            var variable1 = context.GenerateCodeForPort(InputA);
            var variable2 = context.GenerateCodeForPort(InputB);
            var result = context.DeclareVariable(OutputsById.First().Value.DataTypeHandle, "");
            context.Statements.Add($"{result} = Vector3.Dot({variable1}, {variable2})");
            return result;
        }
    }
}
