namespace UnityEngine.Experimental.VFX
{
    class VFXExpressionBuiltInValue : VFXExpression
    {
        public VFXExpressionBuiltInValue(VFXExpressionOp op)
        {
            m_operation = op;
        }

        private VFXExpressionOp m_operation = VFXExpressionOp.kVFXDeltaTimeOp;
        public override VFXExpressionOp Operation { get { return m_operation; } }

        private static VFXValueType GetTypeFromExpression(VFXExpressionOp op)
        {
            switch (op)
            {
                case VFXExpressionOp.kVFXDeltaTimeOp:
                    return VFXValueType.kFloat;
            }
            return VFXValueType.kNone;
        }

        public override VFXValueType ValueType { get { return GetTypeFromExpression(m_operation); } }
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