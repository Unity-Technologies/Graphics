using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Functions/Clamp")]
    [SeacherHelp("Outputs the clamped value of the float input.")]
    public class ClampFunction : MathFunction
    {
        public override string Title
        {
            get => "Clamp";
            set {}
        }

        public ClampFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "val", "min", "max" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Clamp(GetParameterValue(0), GetParameterValue(1), GetParameterValue(2));
        }

        public override string CompileToCSharp(MathBookGraphProcessor context)
        {
            var variable1 = context.GenerateCodeForPort(InputsByDisplayOrder[0]);
            var variable2 = context.GenerateCodeForPort(InputsByDisplayOrder[1]);
            var variable3 = context.GenerateCodeForPort(InputsByDisplayOrder[2]);
            var result = context.DeclareVariable(OutputsById.First().Value.DataTypeHandle, "");
            context.Statements.Add($"{result} = Mathf.Clamp({variable1}, {variable2}, {variable3})");
            return result;
        }
    }
}
