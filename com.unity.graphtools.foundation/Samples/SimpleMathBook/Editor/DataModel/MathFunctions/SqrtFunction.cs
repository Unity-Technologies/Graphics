using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Functions/Square Root")]
    [SeacherHelp("Outputs the square root of the float input.")]
    public class SqrtFunction : MathFunction
    {
        public override string Title
        {
            get => "Square Root";
            set {}
        }

        public SqrtFunction()
        {
            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new[] { "f" };
            }
        }

        public override float EvaluateFloat()
        {
            return Mathf.Sqrt(GetParameterValue(0));
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
                            validInput = b.Value >= 0;
                            break;
                        case Constant<float> b:
                            validInput = b.Value >= 0f;
                            break;
                        case Constant<Vector2> b:
                            validInput = b.Value.x >= 0f && b.Value.y >= 0f;
                            break;
                        case Constant<Vector3> b:
                            validInput = b.Value.x >= 0f && b.Value.y >= 0f && b.Value.z >= 0f;
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
            context.Statements.Add($"{result} = Mathf.Sqrt({variable})");
            return result;
        }
    }
}
