using System;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    // Must match enum in C++
    enum VFXCondition
    {
        Equal,
        NotEqual,
        Less,
        LessOrEqual,
        Greater,
        GreaterOrEqual,
    }

    class VFXExpressionCondition : VFXExpression
    {
        public VFXExpressionCondition()
            : this(VFXValueType.Float, VFXCondition.Equal, VFXValue.Constant(0.0f), VFXValue.Constant(0.0f))
        {}

        public VFXExpressionCondition(VFXValueType type, VFXCondition cond, VFXExpression left, VFXExpression right) : base(VFXExpression.Flags.None, new VFXExpression[] { left, right })
        {
            if (type != left.valueType || type != right.valueType)
                throw new InvalidOperationException(string.Format("Unexpected value type in condition expression : {0}/{1} (expected {2})", left.valueType, right.valueType, type));

            if (type != VFXValueType.Float && type != VFXValueType.Uint32 && type != VFXValueType.Int32)
                throw new NotImplementedException("This type is not handled by condition expression: " + type);

            condition = cond;
            this.type = type;
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.Condition;
            }
        }

        private VFXValue<bool> Evaluate<T>(VFXExpression[] constParents) where T : IComparable<T>
        {
            T left = constParents[0].Get<T>();
            T right = constParents[1].Get<T>();
            int comp = left.CompareTo(right);

            bool res = false;
            switch (condition)
            {
                case VFXCondition.Equal:            res = comp == 0; break;
                case VFXCondition.NotEqual:         res = comp != 0; break;
                case VFXCondition.Less:             res = comp < 0; break;
                case VFXCondition.LessOrEqual:      res = comp <= 0; break;
                case VFXCondition.Greater:          res = comp > 0; break;
                case VFXCondition.GreaterOrEqual:   res = comp >= 0; break;
                default: throw new NotImplementedException("Invalid VFXCondition: " + condition);
            }

            return VFXValue.Constant<bool>(res);
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            switch(type)
            {
                case VFXValueType.Float:    return Evaluate<float>(constParents);
                case VFXValueType.Int32:    return Evaluate<int>(constParents);
                case VFXValueType.Uint32:   return Evaluate<uint>(constParents);
                default: throw new NotImplementedException("This type is not handled by condition expression: " + type);
            }
        }

        public override string GetCodeString(string[] parents)
        {
            string comparator = null;
            switch (condition)
            {
                case VFXCondition.Equal:            comparator = "==";  break;
                case VFXCondition.NotEqual:         comparator = "!=";  break;
                case VFXCondition.Less:             comparator = "<";   break;
                case VFXCondition.LessOrEqual:      comparator = "<=";  break;
                case VFXCondition.Greater:          comparator = ">";   break;
                case VFXCondition.GreaterOrEqual:   comparator = ">=";  break;
            }

            return string.Format("{0} {1} {2}", parents[0], comparator, parents[1]);
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var newExpression = (VFXExpressionCondition)base.Reduce(reducedParents);
            newExpression.condition = condition;
            newExpression.type = type;
            return newExpression;
        }

        protected override int[] additionnalOperands { get { return new int[] { (int)type, (int)condition }; } }
        private VFXValueType type;
        private VFXCondition condition;
    }

    class VFXExpressionBranch : VFXExpression
    {
        public VFXExpressionBranch()
            : this(VFXValue.Constant(true), VFXValue.Constant(0.0f), VFXValue.Constant(0.0f))
        {}

        public VFXExpressionBranch(VFXExpression pred, VFXExpression trueExp, VFXExpression falseExp)
            : base(VFXExpression.Flags.None, new VFXExpression[] { pred, trueExp, falseExp })
        {
            if (parents[1].valueType != parents[2].valueType)
                throw new ArgumentException("both branch expressions must be of the same types");
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.Branch;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            bool pred = constParents[0].Get<bool>();
            return pred ? constParents[1] : constParents[2];
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("{0} ? {1} : {2}", parents);
        }

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            if (reducedParents[0].Is(VFXExpression.Flags.Constant)) // detect static branching
                return Evaluate(reducedParents);
            return base.Reduce(reducedParents);
        }

        protected override int[] additionnalOperands { get { return new int[] { (int)parents[1].valueType }; } }
    }
}
