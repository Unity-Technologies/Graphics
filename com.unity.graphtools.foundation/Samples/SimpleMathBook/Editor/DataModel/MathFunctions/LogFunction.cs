using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Functions/Log")]
    [SeacherHelp("Outputs the logarithm of the f in the base of p.")]
    public class LogFunction : MathFunction
    {
        public override string Title
        {
            get => "Log";
            set {}
        }

        public LogFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "f", "p" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Log(GetParameterValue(0), GetParameterValue(1));
        }

        public override string CompileToCSharp(MathBookGraphProcessor context)
        {
            bool validInput = true;

            foreach (var portModel in InputsByDisplayOrder)
            {
                if (!portModel.IsConnected() && portModel.EmbeddedValue != null)
                {
                    var c = portModel.EmbeddedValue;
                    switch (c)
                    {
                        case Constant<bool> b:
                            validInput = b.Value;
                            break;
                        case Constant<int> b:
                            validInput = b.Value > 0;
                            break;
                        case Constant<float> b:
                            validInput = b.Value > 0f;
                            break;
                        case Constant<Vector2> b:
                            validInput = b.Value.x > 0f && b.Value.y > 0f;
                            break;
                        case Constant<Vector3> b:
                            validInput = b.Value.x > 0f && b.Value.y > 0f && b.Value.z > 0f;
                            break;
                    }
                }

                if (!validInput)
                {
                    context.ProcessingResult.AddWarning("Invalid input.", this);
                    break;
                }
            }

            var variable1 = context.GenerateCodeForPort(InputsByDisplayOrder[0]);
            var variable2 = context.GenerateCodeForPort(InputsByDisplayOrder[1]);
            var result = context.DeclareVariable(OutputsById.First().Value.DataTypeHandle, "");
            context.Statements.Add($"{result} = Mathf.Log({variable1}, {variable2})");
            return result;
        }
    }
}
