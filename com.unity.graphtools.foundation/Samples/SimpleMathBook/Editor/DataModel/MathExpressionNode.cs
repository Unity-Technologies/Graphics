using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    [SearcherItem(typeof(MathBookStencil), SearcherContext.Graph, "Expression")]
    [SeacherHelp("Computes an arbitrary expression.")]
    public class MathExpressionNode : MathNode, IRenamable
    {
        public override string Title => m_Expression;
        public override string DisplayTitle => m_Expression;

        public IPortModel MainOutputPort { get; private set; }

        readonly Dictionary<string, IPortModel> m_VariablesToPort = new Dictionary<string, IPortModel>();
        string m_ExpressionError;
        IExpression m_RootExpression;

        [SerializeField]
        [InspectorUseSetterMethod(nameof(SetExpression))]
        string m_Expression = "10 + 2 * a";

        public void SetExpression(string expression,
            out IEnumerable<IGraphElementModel> newModels,
            out IEnumerable<IGraphElementModel> changedModels,
            out IEnumerable<IGraphElementModel> deletedModels)
        {
            var edgeDiff = new NodeEdgeDiff(this, PortDirection.Input);

            Rename(expression);

            newModels = null;
            changedModels = null;
            deletedModels = edgeDiff.GetDeletedEdges();
        }

        public void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            m_Expression = newName;
            DefineNode();
        }

        public MathExpressionNode()
        {
            m_Capabilities.Add(Overdrive.Capabilities.Renamable);
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_VariablesToPort.Clear();
            m_ExpressionError = null;
            m_RootExpression = null;
        }

        public override Value Evaluate()
        {
            if (m_ExpressionError != null)
                throw new InvalidDataException(m_ExpressionError);

            var variableValues = new Dictionary<string, float>();
            foreach (var kv in m_VariablesToPort)
            {
                variableValues[kv.Key] = GetValue(kv.Value).Float;
            }

            return ComputeResult(m_RootExpression, variableValues);
        }

        /// <inheritdoc />
        public override string CompileToCSharp(MathBookGraphProcessor context)
        {
            if (m_ExpressionError != null)
            {
                context.ProcessingResult.AddError("Error in expression: " + m_ExpressionError, this);
            }

            var variableIdToCSharpVariable = new Dictionary<string, string>();
            foreach (var kv in m_VariablesToPort)
            {
                var v = context.GenerateCodeForPort(kv.Value);
                variableIdToCSharpVariable[kv.Key] = v;
            }

            var rootExpression = MathExpressionParser.Parse(m_Expression, out _);
            var cSharpExpression = TranslateExpression(context, rootExpression, variableIdToCSharpVariable);

            cSharpExpression = string.IsNullOrEmpty(cSharpExpression) ? "default" : cSharpExpression;
            var result = context.DeclareVariable(OutputsById.First().Value.DataTypeHandle, "");
            context.Statements.Add($"{result} = {cSharpExpression}");
            return result;
        }

        protected override IPortModel CreatePort(PortDirection direction, PortOrientation orientation, string portName, PortType portType, TypeHandle dataType, string portId, PortModelOptions options)
        {
            // We use the base port models, not the MathBook ones.
            return new PortModel
            {
                Direction = direction,
                Orientation = orientation,
                PortType = portType,
                DataTypeHandle = dataType,
                Title = portName ?? "",
                UniqueName = portId,
                Options = options,
                NodeModel = this,
                GraphModel = GraphModel
            };
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            m_RootExpression = MathExpressionParser.Parse(m_Expression, out m_ExpressionError);
            m_VariablesToPort.Clear();

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

            // Keep the previous ports as missing ports.
            foreach (var previousInput in m_PreviousInputs)
            {
                var portModel = previousInput.Value;
                if (!m_InputsById.ContainsKey(portModel.UniqueName) && portModel.GetConnectedEdges().Any())
                {
                    var title = portModel is IHasTitle hasTitle ? hasTitle.DisplayTitle : null;
                    this.AddPlaceHolderPort(PortDirection.Input, portModel.UniqueName, portName: title);
                }
            }

            MainOutputPort = this.AddDataOutputPort<float>(null, nameof(MainOutputPort));
        }

        void GetVariables(IExpression expression)
        {
            switch (expression)
            {
                case Variable variable:
                    if (!m_VariablesToPort.ContainsKey(variable.Id))
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
                    throw new InvalidDataException($"Error when parsing the expression '{m_Expression}': Unknown expression '{expression}'");
            }
        }

        void AddVariable(string variableId)
        {
            var port = this.AddDataInputPort<float>(variableId);
            m_VariablesToPort.Add(variableId, port);
        }

        static float ComputeResult(IExpression expression, Dictionary<string, float> variableValues)
        {
            switch (expression)
            {
                case Variable variable:
                    return variableValues[variable.Id];
                case UnaryOperation unaryOperation:
                    var operand = ComputeResult(unaryOperation.Operand, variableValues);
                    return ComputeOperation(unaryOperation.Type, operand);
                case BinaryOperation binaryOperation:
                    var operandA = ComputeResult(binaryOperation.OperandA, variableValues);
                    var operandB = ComputeResult(binaryOperation.OperandB, variableValues);
                    return ComputeOperation(binaryOperation.Type, operandA, operandB);
                case FunctionCall functionCall:
                    var argumentValues = functionCall.Arguments.Select(e => ComputeResult(e, variableValues)).ToList();
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

                public string TranslateExpression(MathBookGraphProcessor context, IExpression expression,
            Dictionary<string, string> variableIdToCSharpVariable)
        {
            switch (expression)
            {
                case Variable variable:
                    return variableIdToCSharpVariable[variable.Id];
                case UnaryOperation unaryOperation:
                    var operand = TranslateExpression(context, unaryOperation.Operand, variableIdToCSharpVariable);
                    return TranslateOperation(context, unaryOperation.Type, operand);
                case BinaryOperation binaryOperation:
                    var operandA = TranslateExpression(context, binaryOperation.OperandA, variableIdToCSharpVariable);
                    var operandB = TranslateExpression(context, binaryOperation.OperandB, variableIdToCSharpVariable);
                    return TranslateOperation(context, binaryOperation.Type, operandA, operandB);
                case FunctionCall functionCall:
                    var argumentValues = functionCall.Arguments.Select(e => TranslateExpression(context, e, variableIdToCSharpVariable)).ToList();
                    return TranslateFunctionCall(context, functionCall.Id, argumentValues);
                case ExpressionValue value:
                    return value.Value.ToString();
                default:
                    throw new InvalidDataException($"Error when computing the result': Unknown expression '{expression}'");
            }
        }

        string TranslateOperation(MathBookGraphProcessor context, OperationType operationType, string operandA, string operandB = "0")
        {
            switch (operationType)
            {
                case OperationType.Add:
                    return $"({operandA} + {operandB})";
                case OperationType.Sub:
                    return $"({operandA} - {operandB})";
                case OperationType.Mul:
                    return $"({operandA} * {operandB})";
                case OperationType.Div:
                    return $"({operandA} / {operandB})";
                case OperationType.Mod:
                    return $"({operandA} % {operandB})";
                case OperationType.Minus:
                    return $"(-{operandA})";
                case OperationType.Plus:
                    return $"(+{operandA})";
                default:
                    context.ProcessingResult.AddError($"Error when computing an operation: Unknown operation '{operationType}'", this);
                    return "0";
            }
        }

        string TranslateFunctionCall(MathBookGraphProcessor context, string functionType, List<string> arguments)
        {
            switch (functionType)
            {
                case "asin":
                    if (arguments.Count == 1)
                        return $"Mathf.Asin({arguments[0]})";
                    break;
                case "acos":
                    if (arguments.Count == 1)
                        return $"Mathf.Acos({arguments[0]})";
                    break;
                case "atan":
                    if (arguments.Count == 1)
                        return $"Mathf.Atan({arguments[0]})";
                    break;
                case "clamp":
                    if (arguments.Count == 3)
                        return $"Mathf.Clamp({arguments[0]}, {arguments[1]}, {arguments[2]})";
                    break;
                case "cos":
                    if (arguments.Count == 1)
                        return $"Mathf.Cos({arguments[0]})";
                    break;
                case "exp":
                    if (arguments.Count == 1)
                        return $"Mathf.Exp({arguments[0]})";
                    break;
                case "log":
                    if (arguments.Count == 2)
                        return $"Mathf.Log({arguments[0]}, {arguments[1]})";
                    break;
                case "max":
                    return $"Mathf.Max(arguments.ToArray())";
                case "min":
                    return $"Mathf.Min(arguments.ToArray())";
                case "pow":
                    if (arguments.Count == 2)
                        return $"Mathf.Pow({arguments[0]}, {arguments[1]})";
                    break;
                case "round":
                    if (arguments.Count == 1)
                        return $"Mathf.Round({arguments[0]})";
                    break;
                case "sin":
                    if (arguments.Count == 1)
                        return $"Mathf.Sin({arguments[0]})";
                    break;
                case "sqrt":
                    if (arguments.Count == 1)
                        return $"Mathf.Sqrt({arguments[0]})";
                    break;
                case "tan":
                    if (arguments.Count == 1)
                        return $"Mathf.Tan({arguments[0]})";
                    break;
                default:
                    context.ProcessingResult.AddError($"Error when computing a function: Unknown function '{functionType}'", this);
                    return "0";
            }

            context.ProcessingResult.AddError($"Error when computing a function: Invalid number of arguments for the function '{functionType}'", this);
            return "0";
        }
    }
}
