using System;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Functions/Pow")]
    [SeacherHelp("Outputs f to the power of p.")]
    public class PowFunction : MathFunction
    {
        public override string Title
        {
            get => "Pow";
            set {}
        }

        public PowFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "f", "p" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Pow(GetParameterValue(0), GetParameterValue(1));
        }

        public override string CompileToCSharp(MathBookGraphProcessor context)
        {
            var variable1 = context.GenerateCodeForPort(InputsByDisplayOrder[0]);
            var variable2 = context.GenerateCodeForPort(InputsByDisplayOrder[1]);
            var result = context.DeclareVariable(OutputsById.First().Value.DataTypeHandle, "");
            context.Statements.Add($"{result} = Mathf.Pow({variable1}, {variable2})");
            return result;
        }
    }
}
