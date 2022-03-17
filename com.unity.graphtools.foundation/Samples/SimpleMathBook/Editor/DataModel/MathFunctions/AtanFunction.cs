using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Functions/Atan")]
    [SeacherHelp("Outputs the Arc tangent of the float input.")]
    public class AtanFunction : MathFunction
    {
        public override string Title
        {
            get => "Atan";
            set {}
        }

        public AtanFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "f" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Atan(GetParameterValue(0));
        }

        public override string CompileToCSharp(MathBookGraphProcessor context)
        {
            var variable = context.GenerateCodeForPort(InputsByDisplayOrder[0]);
            var result = context.DeclareVariable(OutputsById.First().Value.DataTypeHandle, "");
            context.Statements.Add($"{result} = Mathf.Atan({variable})");
            return result;
        }
    }
}
