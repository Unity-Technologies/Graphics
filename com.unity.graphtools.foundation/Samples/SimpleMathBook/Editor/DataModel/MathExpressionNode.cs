using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class MathExpressionNode : MathNode, IRenamable
    {
        public override string Title => Expression;
        public override string DisplayTitle => Expression;

        public IPortModel MainOutputPort { get; private set; }

        readonly Dictionary<string, IPortModel> m_Variables = new Dictionary<string, IPortModel>();
        string m_ExpressionError;
        IExpression m_RootExpression;

        string Expression { get; set; } = "10 + 2";

        public void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            Expression = newName;
            DefineNode();
        }

        public MathExpressionNode()
        {
            m_Capabilities.Add(Overdrive.Capabilities.Renamable);
        }

        public override Value Evaluate()
        {
            if (m_ExpressionError != null)
                throw new InvalidDataException(m_ExpressionError);
            try
            {
                return ComputeResult(m_RootExpression);
            }
            catch (InvalidDataException e)
            {
                throw new InvalidDataException(e.Message);
            }
        }

        public override IPortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName, PortType portType, TypeHandle dataType, string portId, PortModelOptions options)
        {
            return new MathExpressionPortModel
            {
                Direction = direction,
                Orientation = orientation,
                PortType = portType,
                DataTypeHandle = dataType,
                Title = portName ?? "",
                UniqueName = portId,
                Options = options,
                NodeModel = this,
                AssetModel = AssetModel
            };
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            m_RootExpression = MathExpressionParser.Parse(Expression, out m_ExpressionError);
            m_Variables.Clear();

            if (m_ExpressionError == null)
            {
                try
                {
                    GetVariables(m_RootExpression);
                }
                catch (Exception e)
                {
                    m_ExpressionError = e.Message;
                }
            }

            MainOutputPort = this.AddDataOutputPort<float>(null, nameof(MainOutputPort));
        }

        void GetVariables(IExpression expression)
        {
            switch (expression)
            {
                case Variable variable:
                    if (!m_Variables.ContainsKey(variable.Id))
                        AddVariable(variable.Id);
                    break;
                case UnaryOperation unaryOperation:
                    GetVariables(unaryOperation.Operand);
                    break;
                case BinaryOperation binaryOperation:
                    GetVariables(binaryOperation.OperandA);
                    GetVariables(binaryOperation.OperandB);
                    break;
                case FunctionCall functionCall:
                    foreach (var argument in functionCall.Arguments)
                        GetVariables(argument);
                    break;
                case ExpressionValue _:
                    break;
                default:
                    throw new InvalidDataException($"Error when parsing the expression '{Expression}': Unknown expression '{expression}'");
            }
        }

        void AddVariable(string variableId)
        {
            var port = this.AddDataInputPort<float>(variableId);
            m_Variables.Add(variableId, port);
        }

        float ComputeResult(IExpression expression)
        {
            switch (expression)
            {
                case Variable variable:
                    m_Variables.TryGetValue(variable.Id, out var portModel);
                    return GetValue(portModel).Float;
                case UnaryOperation unaryOperation:
                    var operand = ComputeResult(unaryOperation.Operand);
                    return ComputeOperation(unaryOperation.Type, operand);
                case BinaryOperation binaryOperation:
                    var operandA = ComputeResult(binaryOperation.OperandA);
                    var operandB = ComputeResult(binaryOperation.OperandB);
                    return ComputeOperation(binaryOperation.Type, operandA, operandB);
                case FunctionCall functionCall:
                    var argumentValues = functionCall.Arguments.Select(ComputeResult).ToList();
                    return ComputeFunctionCall(functionCall.Id, argumentValues);
                case ExpressionValue value:
                    return value.Value;
                default:
                    throw new InvalidDataException($"Error when computing the result': Unknown expression '{expression}'");
            }
        }

        static float ComputeOperation(OperationType operationType, float operandA, float operandB = 0)
        {
            switch (operationType)
            {
                case OperationType.Add:
                    return operandA + operandB;
                case OperationType.Sub:
                    return operandA - operandB;
                case OperationType.Mul:
                    return operandA * operandB;
                case OperationType.Div:
                    return operandA / operandB;
                case OperationType.Mod:
                    return operandA % operandB;
                case OperationType.Minus:
                    return -operandA;
                case OperationType.Plus:
                    return +operandA;
                default:
                    throw new InvalidDataException($"Error when computing an operation: Unknown operation '{operationType}'");
            }
        }

        static float ComputeFunctionCall(string functionType, List<float> arguments)
        {
            switch (functionType)
            {
                case "asin":
                    if (arguments.Count == 1)
                        return Mathf.Asin(arguments[0]);
                    break;
                case "acos":
                    if (arguments.Count == 1)
                        return Mathf.Acos(arguments[0]);
                    break;
                case "atan":
                    if (arguments.Count == 1)
                        return Mathf.Atan(arguments[0]);
                    break;
                case "clamp":
                    if (arguments.Count == 3)
                        return Mathf.Clamp(arguments[0], arguments[1], arguments[2]);
                    break;
                case "cos":
                    if (arguments.Count == 1)
                        return Mathf.Cos(arguments[0]);
                    break;
                case "exp":
                    if (arguments.Count == 1)
                        return Mathf.Exp(arguments[0]);
                    break;
                case "log":
                    if (arguments.Count == 2)
                        return Mathf.Log(arguments[0], arguments[1]);
                    break;
                case "max":
                    return Mathf.Max(arguments.ToArray());
                case "min":
                    return Mathf.Min(arguments.ToArray());
                case "pow":
                    if (arguments.Count == 2)
                        return Mathf.Pow(arguments[0], arguments[1]);
                    break;
                case "round":
                    if (arguments.Count == 1)
                        return Mathf.Round(arguments[0]);
                    break;
                case "sin":
                    if (arguments.Count == 1)
                        return Mathf.Sin(arguments[0]);
                    break;
                case "sqrt":
                    if (arguments.Count == 1)
                        return Mathf.Sqrt(arguments[0]);
                    break;
                case "tan":
                    if (arguments.Count == 1)
                        return Mathf.Tan(arguments[0]);
                    break;
                default:
                    throw new InvalidDataException($"Error when computing a function: Unknown function '{functionType}'");
            }
            throw new InvalidDataException($"Error when computing a function: Invalid number of arguments for the function '{functionType}'");
        }
    }
}
