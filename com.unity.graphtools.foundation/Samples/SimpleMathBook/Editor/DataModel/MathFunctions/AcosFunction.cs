using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Functions/Acos")]
    [SeacherHelp("Outputs the Arc cosine of the float input.")]
    public class AcosFunction : MathFunction
    {
        public override string Title
        {
            get => "Acos";
            set {}
        }

        public AcosFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "f" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Acos(GetParameterValue(0));
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
                        case Constant<bool> _:
                            validInput = false;
                            break;
                        case Constant<int> b:
                            validInput = b.Value == 0 || b.Value == 1 || b.Value == -1;
                            break;
                        case Constant<float> b:
                            validInput = b.Value >= -1f && b.Value <= 1f;
                            break;
                        case Constant<Vector2> b:
                            validInput = b.Value.x >= -1f && b.Value.x <= 1f && b.Value.y >= -1f && b.Value.y <= 1f;
                            break;
                        case Constant<Vector3> b:
                            validInput = b.Value.x >= -1f && b.Value.x <= 1f && b.Value.y >= -1f && b.Value.y <= 1f && b.Value.z >= -1f && b.Value.z <= 1f;
                            break;
                    }
                }

                if (!validInput)
                {
                    context.ProcessingResult.AddWarning("Invalid input.", this);
                    break;
                }
            }

            var variable = context.GenerateCodeForPort(InputsByDisplayOrder[0]);
            var result = context.DeclareVariable(OutputsById.First().Value.DataTypeHandle, "");
            context.Statements.Add($"{result} = Mathf.Acos({variable})");
            return result;
        }
    }
}
