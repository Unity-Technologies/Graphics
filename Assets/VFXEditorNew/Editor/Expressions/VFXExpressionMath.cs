using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXExpressionCos : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionCos() : this(VFXValueFloat.Default) { }

        public VFXExpressionCos(VFXExpression parent) : base(parent, VFXExpressionOp.kVFXCosOp)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("cos({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Cos(input);
        }
    }

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

    class VFXExpressionAbs : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionAbs() : this(VFXValueFloat.Default) { }

        public VFXExpressionAbs(VFXExpression parent) : base(parent, VFXExpressionOp.kVFXAbsOp)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("abs({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Abs(input);
        }
    }

    class VFXExpressionFloor : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionFloor() : this(VFXValueFloat.Default) { }

        public VFXExpressionFloor(VFXExpression parent) : base(parent, VFXExpressionOp.kVFXFloorOp)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("floor({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Floor(input);
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

    class VFXExpressionDivide : VFXExpressionBinaryFloatOperation
    {
        public VFXExpressionDivide() : this(VFXValueFloat.Default, VFXValueFloat.Default)
        {
        }

        public VFXExpressionDivide(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.kVFXDivideOp)
        {
        }

        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return left / right;
        }
        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("{0} / {1}", left, right);
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

    class VFXExpressionMin : VFXExpressionBinaryFloatOperation
    {
        public VFXExpressionMin() : this(VFXValueFloat.Default, VFXValueFloat.Default)
        {
        }

        public VFXExpressionMin(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.kVFXMinOp)
        {
        }
        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return Mathf.Min(left, right);
        }
        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("min({0}, {1})", left, right);
        }
    }

    class VFXExpressionMax : VFXExpressionBinaryFloatOperation
    {
        public VFXExpressionMax() : this(VFXValueFloat.Default, VFXValueFloat.Default)
        {
        }

        public VFXExpressionMax(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.kVFXMaxOp)
        {
        }
        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return Mathf.Max(left, right);
        }
        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("max({0}, {1})", left, right);
        }
    }

    class VFXExpressionPow : VFXExpressionBinaryFloatOperation
    {
        public VFXExpressionPow() : this(VFXValueFloat.Default, VFXValueFloat.Default)
        {
        }

        public VFXExpressionPow(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.kVFXPowOp)
        {
        }
        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return Mathf.Pow(left, right);
        }
        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("pow({0}, {1})", left, right);
        }
    }
}