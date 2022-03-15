using System;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Vectors/Vector3 Cross Product")]
    [SeacherHelp("Outputs the cross product of all vector 3 inputs.")]
    public class Vector3CrossProduct : MathOperator
    {
        public override string Title
        {
            get => "Vector3 Cross Product";
            set {}
        }

        public override TypeHandle[] ValueInputTypes => new[] { TypeHandle.Vector3 };

        public override Value Evaluate()
        {
            return Values.Select(v => v.Vector3).Skip(1).Aggregate(Values.FirstOrDefault().Vector3, Vector3.Cross);
        }

        protected override void AddInputPorts()
        {
            for (var i = 0; i < InputPortCount; ++i)
                this.AddDataInputPort<Vector3>("Port " + i);
        }

        protected override void AddOutputPorts()
        {
            this.AddDataOutputPort<Vector3>("Output");
        }

        /// <inheritdoc />
        protected override (string op, bool isInfix) GetCSharpOperator()
        {
            return ("Vector3.Cross", false);
        }
    }
}
