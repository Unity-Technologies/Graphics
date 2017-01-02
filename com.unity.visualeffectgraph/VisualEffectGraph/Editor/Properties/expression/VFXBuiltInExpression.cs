using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UnityEngine.Experimental.VFX
{
    public static class CommonBuiltIn
    {
        public class VFXBuiltInExpressionDesc
        {
            public VFXBuiltInExpressionDesc(VFXBlockDesc.Flag flag, VFXExpressionOp op, VFXValueType type, string name)
            {
                Flag = flag;
                Expression = new VFXExpressionBuiltInValue(op, type);
                Name = name;
                DeclarationName = VFXValue.TypeToName(type) + " " + name;
            }
            public VFXBlockDesc.Flag Flag { get; private set; }
            public VFXExpression Expression { get; private set; }
            public string Name { get; private set; }
            public string DeclarationName { get; private set; }
        }

        public static readonly ReadOnlyCollection<VFXBuiltInExpressionDesc> Expressions = new List<VFXBuiltInExpressionDesc>()
        {
            new VFXBuiltInExpressionDesc(VFXBlockDesc.Flag.kNeedsDeltaTime, VFXExpressionOp.kVFXDeltaTimeOp, VFXValueType.kFloat, "deltaTime"),
            new VFXBuiltInExpressionDesc(VFXBlockDesc.Flag.kNeedsTotalTime, VFXExpressionOp.kVFXTotalTimeOp, VFXValueType.kFloat, "totalTime"),
            new VFXBuiltInExpressionDesc(VFXBlockDesc.Flag.kNeedsSystemSeed, VFXExpressionOp.kVFXSystemSeedOp, VFXValueType.kUint, "systemSeed")
        }.AsReadOnly();

        public static readonly VFXExpression DeltaTime = Expressions.First(o => o.Expression.Operation == VFXExpressionOp.kVFXDeltaTimeOp).Expression;
        public static readonly VFXExpression SystemSeed = Expressions.First(o => o.Expression.Operation == VFXExpressionOp.kVFXSystemSeedOp).Expression;
        public static readonly Dictionary<VFXExpression, VFXBuiltInExpressionDesc> DictionnaryExpression = Expressions.ToDictionary(e => e.Expression, e => e);
    }

    class VFXExpressionBuiltInValue : VFXExpression
    {
        private VFXExpressionOp m_operation = VFXExpressionOp.kVFXDeltaTimeOp;
        private VFXValueType m_type = VFXValueType.kNone;

        public VFXExpressionBuiltInValue(VFXExpressionOp op, VFXValueType type)
        {
            m_operation = op;
            m_type = type;
        }

        public override VFXExpressionOp Operation { get { return m_operation; } }
        public override VFXValueType ValueType { get { return m_type; } }
        public override VFXExpression Reduce() { return this; }
        public override void Invalidate() { }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj.GetType() != GetType())
                return false;

            var other = obj as VFXExpressionBuiltInValue;
            if (other.Operation != m_operation)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode() * 31 + m_operation.GetHashCode();
        }
    }
}