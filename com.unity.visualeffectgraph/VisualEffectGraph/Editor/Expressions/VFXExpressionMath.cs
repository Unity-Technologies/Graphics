using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.VFX
{
    class VFXExpressionCos : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionCos() : this(VFXValue<float>.Default) {}

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
        public VFXExpressionSin() : this(VFXValue<float>.Default) {}

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
        public VFXExpressionAbs() : this(VFXValue<float>.Default) {}

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
        public VFXExpressionFloor() : this(VFXValue<float>.Default) {}

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
        public VFXExpressionAdd() : this(VFXValue<float>.Default, VFXValue<float>.Default)
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
        public VFXExpressionMul() : this(VFXValue<float>.Default, VFXValue<float>.Default)
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
        public VFXExpressionDivide() : this(VFXValue<float>.Default, VFXValue<float>.Default)
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
        public VFXExpressionSubtract() : this(VFXValue<float>.Default, VFXValue<float>.Default)
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
        public VFXExpressionMin() : this(VFXValue<float>.Default, VFXValue<float>.Default)
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
        public VFXExpressionMax() : this(VFXValue<float>.Default, VFXValue<float>.Default)
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
        public VFXExpressionPow() : this(VFXValue<float>.Default, VFXValue<float>.Default)
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

    class VFXExpressionBitwiseLeftShift : VFXExpressionBinaryUIntOperation
    {
        public VFXExpressionBitwiseLeftShift()
            : this(VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionBitwiseLeftShift(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.kVFXBitwiseLeftShiftOp)
        {
        }

        sealed protected override uint ProcessBinaryOperation(uint left, uint right)
        {
            return left << (int)right;
        }

        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("{0} << {1}", left, right);
        }
    }

    class VFXExpressionBitwiseRightShift : VFXExpressionBinaryUIntOperation
    {
        public VFXExpressionBitwiseRightShift() : this(VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionBitwiseRightShift(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.kVFXBitwiseRightShiftOp)
        {
        }

        sealed protected override uint ProcessBinaryOperation(uint left, uint right)
        {
            return left >> (int)right;
        }

        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("{0} >> {1}", left, right);
        }
    }

    class VFXExpressionBitwiseOr : VFXExpressionBinaryUIntOperation
    {
        public VFXExpressionBitwiseOr() : this(VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionBitwiseOr(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.kVFXBitwiseOrOp)
        {
        }

        sealed protected override uint ProcessBinaryOperation(uint left, uint right)
        {
            return left | right;
        }

        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("{0} | {1}", left, right);
        }
    }

    class VFXExpressionBitwiseAnd : VFXExpressionBinaryUIntOperation
    {
        public VFXExpressionBitwiseAnd() : this(VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionBitwiseAnd(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.kVFXBitwiseAndOp)
        {
        }

        sealed protected override uint ProcessBinaryOperation(uint left, uint right)
        {
            return left & right;
        }

        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("{0} & {1}", left, right);
        }
    }

    class VFXExpressionBitwiseXor : VFXExpressionBinaryUIntOperation
    {
        public VFXExpressionBitwiseXor() : this(VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionBitwiseXor(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.kVFXBitwiseXorOp)
        {
        }

        sealed protected override uint ProcessBinaryOperation(uint left, uint right)
        {
            return left ^ right;
        }

        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("{0} ^ {1}", left, right);
        }
    }

    class VFXExpressionBitwiseComplement : VFXExpressionUnaryUIntOperation
    {
        public VFXExpressionBitwiseComplement() : this(VFXValue<uint>.Default)
        {
        }

        public VFXExpressionBitwiseComplement(VFXExpression parent) : base(parent, VFXExpressionOp.kVFXBitwiseComplementOp)
        {
        }

        sealed protected override uint ProcessUnaryOperation(uint input)
        {
            return ~input;
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("~{0}", x);
        }
    }
}
