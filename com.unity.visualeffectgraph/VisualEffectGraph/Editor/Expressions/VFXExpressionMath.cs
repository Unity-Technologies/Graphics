using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    class VFXExpressionCos : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionCos() : this(VFXValue<float>.Default) {}

        public VFXExpressionCos(VFXExpression parent) : base(parent, VFXExpressionOp.CosOp)
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

        public VFXExpressionSin(VFXExpression parent) : base(parent, VFXExpressionOp.SinOp)
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

    class VFXExpressionTan : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionTan() : this(VFXValue<float>.Default) {}

        public VFXExpressionTan(VFXExpression parent) : base(parent, VFXExpressionOp.TanOp)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("tan({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Tan(input);
        }
    }

    class VFXExpressionACos : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionACos() : this(VFXValue<float>.Default) {}

        public VFXExpressionACos(VFXExpression parent) : base(parent, VFXExpressionOp.ACosOp)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("acos({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Acos(input);
        }
    }

    class VFXExpressionASin : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionASin() : this(VFXValue<float>.Default) {}

        public VFXExpressionASin(VFXExpression parent) : base(parent, VFXExpressionOp.ASinOp)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("asin({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Asin(input);
        }
    }

    class VFXExpressionATan : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionATan() : this(VFXValue<float>.Default) {}

        public VFXExpressionATan(VFXExpression parent) : base(parent, VFXExpressionOp.ATanOp)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("atan({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Atan(input);
        }
    }

    class VFXExpressionLog2 : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionLog2() : this(VFXValue<float>.Default) {}

        public VFXExpressionLog2(VFXExpression parent) : base(parent, VFXExpressionOp.Log2Op)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("log2({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Log(input, 2.0f);
        }
    }

    class VFXExpressionAbs : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionAbs() : this(VFXValue<float>.Default) {}

        public VFXExpressionAbs(VFXExpression parent) : base(parent, VFXExpressionOp.AbsOp)
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

    class VFXExpressionSign : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionSign() : this(VFXValue<float>.Default) {}

        public VFXExpressionSign(VFXExpression parent) : base(parent, VFXExpressionOp.SignOp)
        {
        }

        sealed protected override string GetUnaryOperationCode(string x)
        {
            return string.Format("sign({0})", x);
        }

        sealed protected override float ProcessUnaryOperation(float input)
        {
            return Mathf.Sign(input);
        }
    }

    class VFXExpressionFloor : VFXExpressionUnaryFloatOperation
    {
        public VFXExpressionFloor() : this(VFXValue<float>.Default) {}

        public VFXExpressionFloor(VFXExpression parent) : base(parent, VFXExpressionOp.FloorOp)
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

        public VFXExpressionAdd(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.AddOp)
        {
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var zero = VFXOperatorUtility.ZeroExpression[TypeToSize(reducedParents[0].valueType)];
            if (zero.Equals(reducedParents[0]))
                return reducedParents[1];
            if (zero.Equals(reducedParents[1]))
                return reducedParents[0];

            return base.Reduce(reducedParents);
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

        public VFXExpressionMul(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.MulOp)
        {
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var zero  = VFXOperatorUtility.ZeroExpression[TypeToSize(reducedParents[0].valueType)];
            if (zero.Equals(reducedParents[0]) || zero.Equals(reducedParents[1]))
                return zero;

            var one = VFXOperatorUtility.OneExpression[TypeToSize(reducedParents[0].valueType)];
            if (one.Equals(reducedParents[0]))
                return reducedParents[1];
            if (one.Equals(reducedParents[1]))
                return reducedParents[0];

            return base.Reduce(reducedParents);
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

        public VFXExpressionDivide(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.DivideOp)
        {
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var zero = VFXOperatorUtility.ZeroExpression[TypeToSize(reducedParents[0].valueType)];
            if (zero.Equals(reducedParents[0]))
                return zero;

            var one = VFXOperatorUtility.OneExpression[TypeToSize(reducedParents[0].valueType)];
            if (one.Equals(reducedParents[1]))
                return reducedParents[0];

            return base.Reduce(reducedParents);
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

        public VFXExpressionSubtract(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.SubtractOp)
        {
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var zero = VFXOperatorUtility.ZeroExpression[TypeToSize(reducedParents[0].valueType)];
            if (zero.Equals(reducedParents[1]))
                return reducedParents[0];

            return base.Reduce(reducedParents);
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

        public VFXExpressionMin(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.MinOp)
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

        public VFXExpressionMax(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.MaxOp)
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

        public VFXExpressionPow(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.PowOp)
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

    class VFXExpressionATan2 : VFXExpressionBinaryFloatOperation
    {
        public VFXExpressionATan2() : this(VFXValue<float>.Default, VFXValue<float>.Default)
        {
        }

        public VFXExpressionATan2(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.ATan2Op)
        {
        }

        sealed protected override float ProcessBinaryOperation(float left, float right)
        {
            return Mathf.Atan2(left, right);
        }

        sealed protected override string GetBinaryOperationCode(string left, string right)
        {
            return string.Format("atan2({0}, {1})", left, right);
        }
    }

    class VFXExpressionBitwiseLeftShift : VFXExpressionBinaryUIntOperation
    {
        public VFXExpressionBitwiseLeftShift()
            : this(VFXValue<uint>.Default, VFXValue<uint>.Default)
        {
        }

        public VFXExpressionBitwiseLeftShift(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.BitwiseLeftShiftOp)
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

        public VFXExpressionBitwiseRightShift(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.BitwiseRightShiftOp)
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

        public VFXExpressionBitwiseOr(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.BitwiseOrOp)
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

        public VFXExpressionBitwiseAnd(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.BitwiseAndOp)
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

        public VFXExpressionBitwiseXor(VFXExpression parentLeft, VFXExpression parentRight) : base(parentLeft, parentRight, VFXExpressionOp.BitwiseXorOp)
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

        public VFXExpressionBitwiseComplement(VFXExpression parent) : base(parent, VFXExpressionOp.BitwiseComplementOp)
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
