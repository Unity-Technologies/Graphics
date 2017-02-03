using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXExpressionSin : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionSin() : this(VFXValueFloat.Default) { }

        public VFXExpressionSin(VFXExpression parent) : base(parent, VFXExpressionOp.kVFXSinOp)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("sin({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Sin(input);
        }
    }

    class VFXExpressionAdd : VFXExpressionBinaryFloatOperation
    {
        public VFXExpressionAdd() : this(VFXValueFloat.Default, VFXValueFloat.Default)
        {
        }

        public VFXExpressionAdd(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.kVFXAddOp)
        {
        }

        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("{0} + {1}", left, right);
        }

        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return left + right;
        }
    }

    class VFXExpressionMul : VFXExpressionBinaryFloatOperation
    {
        public VFXExpressionMul() : this(VFXValueFloat.Default, VFXValueFloat.Default)
        {
        }

        public VFXExpressionMul(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.kVFXMulOp)
        {
        }

        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return left * right;
        }
        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("{0} * {1}", left, right);
        }
    }

    class VFXExpressionSubtract : VFXExpressionBinaryFloatOperation
    {
        public VFXExpressionSubtract() : this(VFXValueFloat.Default, VFXValueFloat.Default)
        {
        }

        public VFXExpressionSubtract(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.kVFXSubtractOp)
        {
        }
        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return left - right;
        }
        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("{0} - {1}", left, right);
        }
    }

    class VFXExpressionLerp : VFXExpressionTernaryFloatOperation
    {
        public VFXExpressionLerp() : this(VFXValueFloat.Default, VFXValueFloat.Default, VFXValueFloat.Default)
        {
        }

        public VFXExpressionLerp(VFXExpression x, VFXExpression y, VFXExpression s) : base(x, y, s, VFXExpressionOp.kVFXLerpOp)
        {
        }

        sealed protected override float ProcessTernaryOperation(float x, float y, float s)
        {
            return Mathf.Lerp(x, y, s);
        }

        sealed protected override string GetTernaryOperationCode(string x, string y, string s)
        {
            return string.Format("lerp({0}, {1}, {2})", x, y, s);
        }
    }
}