namespace UnityEngine.Experimental.VFX
{
    public class VFXExpressionTransformMatrix : VFXExpression
    {
        public VFXExpressionTransformMatrix(VFXExpression mat)
        {
            m_Mat = mat;
        }

        public override VFXValueType ValueType { get { return VFXValueType.kTransform; } }
        public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXTransformMatrixOp; } }

        public override VFXExpression Reduce() { return m_Mat.Reduce(); } // Just a pass through on C# side
        public override void Invalidate() {}
        public override VFXExpression[] GetParents() { return new VFXExpression[] { m_Mat }; }

        private VFXExpression m_Mat;
    }

    public class VFXExpressionTransformPosition : VFXExpression
    {
        public VFXExpressionTransformPosition(VFXExpression pos)
        {
            m_Pos = pos;
        }

        public override VFXValueType ValueType { get { return VFXValueType.kFloat3; } }
        public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXTransformPosOp; } }

        public override VFXExpression Reduce() { return m_Pos.Reduce(); } // Just a pass through on C# side
        public override void Invalidate() {}
        public override VFXExpression[] GetParents() { return new VFXExpression[] { m_Pos }; }

        private VFXExpression m_Pos;
    }

    public class VFXExpressionTransformVector : VFXExpression
    {
        public VFXExpressionTransformVector(VFXExpression vec)
        {
            m_Vec = vec;
        }

        public override VFXValueType ValueType { get { return VFXValueType.kFloat3; } }
        public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXTransformVecOp; } }

        public override VFXExpression Reduce() { return m_Vec.Reduce(); } // Just a pass through on C# side
        public override void Invalidate() {}          
        public override VFXExpression[] GetParents() { return new VFXExpression[] { m_Vec }; }

        private VFXExpression m_Vec;
    }
}