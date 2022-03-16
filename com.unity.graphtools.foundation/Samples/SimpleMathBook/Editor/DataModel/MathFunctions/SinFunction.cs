using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Functions/Sin")]
    [SeacherHelp("Outputs the Sine of the float input.")]
    public class SinFunction : MathFunction
    {
        public override string Title
        {
            get => "Sin";
            set {}
        }

        public SinFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "f" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Sin(GetParameterValue(0));
        }

        public override string CompileToCSharp(MathBookGraphProcessor context)
        {
            var variable = context.GenerateCodeForPort(InputsByDisplayOrder[0]);
            var result = context.DeclareVariable(OutputsById.First().Value.DataTypeHandle, "");
            context.Statements.Add($"{result} = Mathf.Sin({variable})");
            return result;
        }
    }
}
