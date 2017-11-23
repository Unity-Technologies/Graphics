using System;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    sealed class VFXBuiltInExpression : VFXExpression
    {
        public static readonly VFXExpression TotalTime = new VFXBuiltInExpression(VFXExpressionOp.kVFXTotalTimeOp);
        public static readonly VFXExpression DeltaTime = new VFXBuiltInExpression(VFXExpressionOp.kVFXDeltaTimeOp);
        public static readonly VFXExpression SystemSeed = new VFXBuiltInExpression(VFXExpressionOp.kVFXSystemSeedOp);
        public static readonly VFXExpression LocalToWorld = new VFXBuiltInExpression(VFXExpressionOp.kVFXLocalToWorldOp);
        public static readonly VFXExpression WorldToLocal = new VFXBuiltInExpression(VFXExpressionOp.kVFXWorldToLocalOp);

        private static readonly VFXExpression[] AllExpressions = VFXReflectionHelper.CollectStaticReadOnlyExpression<VFXExpression>(typeof(VFXBuiltInExpression));
        public static readonly VFXExpressionOp[] All = AllExpressions.Select(e => e.operation).ToArray();

        public static VFXExpression Find(VFXExpressionOp op)
        {
            var expression = AllExpressions.FirstOrDefault(e => e.operation == op);
            return expression;
        }

        private VFXExpressionOp m_Operation;

        private VFXBuiltInExpression(VFXExpressionOp op)
            : base(Flags.None)
        {
            m_Operation = op;
        }

        public sealed override VFXExpressionOp operation
        {
            get
            {
                return m_Operation;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is VFXBuiltInExpression))
                return false;

            var other = (VFXBuiltInExpression)obj;
            return valueType == other.valueType && operation == other.operation;
        }

        public override int GetHashCode()
        {
            return operation.GetHashCode();
        }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            return this;
        }
    }
}
