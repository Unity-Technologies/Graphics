using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The type of a parsed operation.
    /// </summary>
    public enum OperationType
    {
        Add,
        Sub,
        Mul,
        Div,
        LeftParens,
        Plus,
        Minus,
        Coma,
        Mod
    }

    /// <summary>
    /// The parsed expression.
    /// </summary>
    public interface IExpression
    {
    }

    interface IOperation : IExpression
    {
    }

    interface IValue : IExpression
    {
    }

    enum Associativity
    {
        None,
        Left,
        Right
    }

    /// <summary>
    /// Tokens for parsing the mathematical expression.
    /// </summary>
    [Flags]
    public enum Token
    {
        None = 0,
        Op = 1,
        Number = 2,
        Identifier = 4,
        LeftParens = 8,
        RightParens = 16,
        Coma = 32,
    }

    /// <summary>
    /// Utility class to parse a mathematical expression.
    /// </summary>
    public static class MathExpressionParser
    {
        /// <summary>
        /// Parses a mathematical expression.
        /// </summary>
        /// <param name="expressionStr">The expression in string to be parsed.</param>
        /// <param name="error">The error message if an exception occured.</param>
        /// <returns>An IExpression that contains the parsed expression.</returns>
        public static IExpression Parse(string expressionStr, out string error)
        {
            var output = new Stack<IExpression>();
            var opStack = new Stack<Operator>();

            var r = new MathExpressionReader(expressionStr);

            try
            {
                r.ReadToken();
                error = null;
                return ParseUntil(r, opStack, output, Token.None, 0);
            }
            catch (Exception e)
            {
                error = e.Message;
                return null;
            }
        }

        internal static readonly Dictionary<OperationType, Operator> Ops = new Dictionary<OperationType, Operator>
        {
            {OperationType.Add, new Operator(OperationType.Add, "+", 2, Associativity.Left)},
            {OperationType.Sub, new Operator(OperationType.Sub, "-", 2, Associativity.Left)},

            {OperationType.Mul, new Operator(OperationType.Mul, "*", 3, Associativity.Left)},
            {OperationType.Div, new Operator(OperationType.Div, "/", 3, Associativity.Left)},
            {OperationType.Mod, new Operator(OperationType.Mod, "%", 3, Associativity.Left)},

            {OperationType.LeftParens, new Operator(OperationType.LeftParens, "(", 5)},
            {OperationType.Minus, new Operator(OperationType.Minus, "-", 2000, Associativity.Right, unary: true)},
        };

        static Operator ReadOperator(string input, bool unary)
        {
            return Ops.Single(o => o.Value.Str == input && o.Value.Unary == unary).Value;
        }

#if !UNITY_2022_1_OR_NEWER
        static bool TryPeek<T>(this Stack<T> stack, out T t)
        {
            if (stack.Count != 0)
            {
                t = stack.Peek();
                return true;
            }

            t = default;
            return false;
        }
#endif

        static IExpression ParseUntil(MathExpressionReader r, Stack<Operator> opStack, Stack<IExpression> output, Token readUntilToken,
            int startOpStackSize)
        {
            do
            {
                switch (r.CurrentTokenType)
                {
                    case Token.LeftParens:
                    {
                        opStack.Push(Ops[OperationType.LeftParens]);
                        r.ReadToken();
                        var arg = ParseUntil(r, opStack, output, Token.Coma | Token.RightParens,
                            opStack.Count);
                        if (r.CurrentTokenType == Token.Coma)
                            throw new InvalidDataException("Tuples not supported");
                        if (r.CurrentTokenType != Token.RightParens)
                            throw new InvalidDataException("Mismatched parens, missing a closing parens");
                        output.Push(arg);

                        while (opStack.TryPeek(out var stackOp) && stackOp.Type != OperationType.LeftParens)
                        {
                            opStack.Pop();
                            PopOpOperandsAndPushNode(stackOp);
                        }

                        if (opStack.TryPeek(out var leftParens) && leftParens.Type == OperationType.LeftParens)
                            opStack.Pop();
                        else
                            throw new InvalidDataException("Mismatched parens");
                        r.ReadToken();
                        break;
                    }
                    case Token.RightParens:
                        throw new InvalidDataException("Mismatched parens");
                    case Token.Op:
                    {
                        var unary = r.PrevTokenType == Token.Op ||
                            r.PrevTokenType == Token.LeftParens ||
                            r.PrevTokenType == Token.None;
                        var readBinOp = ReadOperator(r.CurrentToken, unary);

                        while (opStack.TryPeek(out var stackOp) &&
                               // the operator at the top of the operator stack is not a left parenthesis or coma
                               stackOp.Type != OperationType.LeftParens && stackOp.Type != OperationType.Coma &&
                               // there is an operator at the top of the operator stack with greater precedence
                               (stackOp.Precedence > readBinOp.Precedence ||
                                // or the operator at the top of the operator stack has equal precedence and the token is left associative
                                stackOp.Precedence == readBinOp.Precedence &&
                                readBinOp.Associativity == Associativity.Left))
                        {
                            opStack.Pop();
                            PopOpOperandsAndPushNode(stackOp);
                        }

                        opStack.Push(readBinOp);
                        r.ReadToken();
                        break;
                    }
                    case Token.Number:
                        output.Push(new ExpressionValue(float.Parse(r.CurrentToken, CultureInfo.InvariantCulture)));
                        r.ReadToken();
                        break;
                    case Token.Identifier:
                        var id = r.CurrentToken;
                        r.ReadToken();
                        if (r.CurrentTokenType != Token.LeftParens) // variable
                        {
                            output.Push(new Variable(id));
                            break;
                        }
                        else // function call
                        {
                            r.ReadToken(); // skip (
                            var args = new List<IExpression>();

                            while (true)
                            {
                                var arg = ParseUntil(r, opStack, output, Token.Coma | Token.RightParens,
                                    opStack.Count);
                                args.Add(arg);
                                if (r.CurrentTokenType == Token.RightParens)
                                {
                                    break;
                                }
                                r.ReadToken();
                            }

                            r.ReadToken(); // skip )

                            output.Push(new FunctionCall(id.ToLower(), args));
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException(r.CurrentTokenType.ToString());
                }
            }
            while (!readUntilToken.HasFlag(r.CurrentTokenType));

            while (opStack.Count > startOpStackSize)
            {
                var readBinOp = opStack.Pop();
                if (readBinOp.Type == OperationType.LeftParens)
                    break;
                PopOpOperandsAndPushNode(readBinOp);
            }

            return output.Pop();

            void PopOpOperandsAndPushNode(Operator readBinOp)
            {
                var b = output.Pop();
                if (readBinOp.Unary)
                {
                    output.Push(new UnaryOperation(readBinOp.Type, b));
                }
                else
                {
                    if (output.Count == 0)
                        throw new InvalidDataException($"Missing operand for the {readBinOp.Str} operator in the expression");
                    var a = output.Pop();
                    output.Push(new BinaryOperation(readBinOp.Type, a, b));
                }
            }
        }
    }
}
