using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Functions/Exp")]
    [SeacherHelp("Outputs e to the power of the float input.")]
    public class ExpFunction : MathFunction
    {
        public override string Title
        {
            get => "Exp";
            set {}
        }

        public ExpFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "f" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Exp(GetParameterValue(0));
        }

        public override string CompileToCSharp(MathBookGraphProcessor context)
        {
            var variable = context.GenerateCodeForPort(InputsByDisplayOrder[0]);
            var result = context.DeclareVariable(OutputsById.First().Value.DataTypeHandle, "");
            context.Statements.Add($"{result} = Mathf.Exp({variable})");
            return result;
        }
    }
}
