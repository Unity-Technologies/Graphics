using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    abstract class VFXExpressionUIntOperation : VFXExpression
    {
        protected VFXExpressionUIntOperation(VFXExpression[] parents)
            : base(Flags.None, parents)
        {
        }

        sealed public override VFXExpressionOperation operation { get { return m_Operation; } }
        sealed protected override int[] additionnalOperands { get { return new int[1] { 1 }; } } // TODO only handle size of 1 atm

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var newExpression = (VFXExpressionUIntOperation)CreateNewInstance();
            newExpression.Initialize(Flags.None, reducedParents);
            newExpression.m_Operation = m_Operation;
            return newExpression;
        }

        protected VFXExpressionOperation m_Operation;
    }

    abstract class VFXExpressionUnaryUIntOperation : VFXExpressionUIntOperation
    {
        protected VFXExpressionUnaryUIntOperation(VFXExpression parent, VFXExpressionOperation operation) : base(new VFXExpression[1] { parent })
        {
            if (!IsUIntValueType(parent.valueType))
            {
                throw new ArgumentException("Incorrect VFXExpressionUnaryUIntOperation");
            }

            m_Operation = operation;
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            return VFXValue.Constant(ProcessUnaryOperation(reducedParents[0].Get<uint>()));
        }

        sealed public override string GetCodeString(string[] parents)
        {
            return GetUnaryOperationCode(parents[0]);
        }

        abstract protected uint ProcessUnaryOperation(uint input);
        abstract protected string GetUnaryOperationCode(string x);
    }

    abstract class VFXExpressionBinaryUIntOperation : VFXExpressionUIntOperation
    {
        protected VFXExpressionBinaryUIntOperation(VFXExpression parentLeft, VFXExpression parentRight, VFXExpressionOperation operation) : base(new VFXExpression[2] { parentLeft, parentRight })
        {
            if (!IsUIntValueType(parentLeft.valueType) || !IsUIntValueType(parentRight.valueType))
            {
                throw new ArgumentException("Incorrect VFXExpressionBinaryUIntOperation (not uint type)");
            }

            m_Operation = operation;
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] reducedParents)
        {
            return VFXValue.Constant(ProcessBinaryOperation(reducedParents[0].Get<uint>(), reducedParents[1].Get<uint>()));
        }

        sealed public override string GetCodeString(string[] parents)
        {
            return GetBinaryOperationCode(parents[0], parents[1]);
        }

        protected abstract uint ProcessBinaryOperation(uint left, uint right);
        protected abstract string GetBinaryOperationCode(string a, string b);
    }
}
