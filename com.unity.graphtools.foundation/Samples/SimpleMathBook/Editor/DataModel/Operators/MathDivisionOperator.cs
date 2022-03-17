using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Operators/Divide", "operator-div")]
    public class MathDivisionOperator : MathFloatOperator
    {
        public override string Title
        {
            get => "Divide";
            set {}
        }

        public override float EvaluateFloat()
        {
            return FloatValues.Skip(1).Aggregate(FloatValues.FirstOrDefault(), (current, value) => current / value);
        }

        protected override void AddInputPorts()
        {
            for (var i = 0; i < InputPortCount; ++i)
                this.AddDataInputPort<float>(i == 0 ? "Dividend" : "Divisor " + i);
        }

        /// <inheritdoc />
        protected override (string op, bool isInfix) GetCSharpOperator()
        {
            return ("/", true);
        }

        public override string CompileToCSharp(MathBookGraphProcessor context)
        {
            bool divideByZero = false;

            foreach (var portModel in InputsByDisplayOrder.Skip(1))
            {
                if (!portModel.IsConnected() && portModel.EmbeddedValue != null)
                {
                    var c = portModel.EmbeddedValue;
                    switch (c)
                    {
                        case Constant<bool> b:
                            divideByZero = !b.Value;
                            break;
                        case Constant<int> b:
                            divideByZero = b.Value == 0;
                            break;
                        case Constant<float> b:
                            divideByZero = b.Value == 0f;
                            break;
                        case Constant<Vector2> b:
                            divideByZero = b.Value.x == 0f || b.Value.y == 0f;
                            break;
                        case Constant<Vector3> b:
                            divideByZero = b.Value.x == 0f || b.Value.y == 0f || b.Value.z == 0f;
                            break;
                    }
                }

                if (divideByZero)
                {
                    context.ProcessingResult.AddError("Division by zero.", this);
                    break;
                }
            }

            return base.CompileToCSharp(context);
        }
    }
}
