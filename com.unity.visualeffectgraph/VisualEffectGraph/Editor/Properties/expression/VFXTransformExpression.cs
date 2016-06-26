namespace UnityEngine.Experimental.VFX
{
    public abstract class VFXTransformExpression : VFXExpression
    {
        protected VFXTransformExpression(VFXExpression expr, SpaceRef spaceRef)
        {
            m_Expr = expr;
            m_SpaceRef = spaceRef;
        }

        public override sealed VFXExpression Reduce() { return m_Expr.Reduce(); } // Just a pass through on C# side (same behavior as with an identity transform)
        public override sealed void Invalidate() { }
        public override sealed VFXExpression[] GetParents() { return new VFXExpression[] { m_Expr }; }

        public override bool Equals(object obj)
        {
            return base.Equals(obj) && m_SpaceRef == (obj as VFXTransformExpression).m_SpaceRef;
        }
        public override int GetHashCode() { return base.GetHashCode(); } // Just to remove a warning

        public SpaceRef GetSpaceRef() { return m_SpaceRef; }

        protected VFXExpression m_Expr;
        protected SpaceRef m_SpaceRef;
    }

    public class VFXExpressionTransformMatrix : VFXTransformExpression
    {
        public VFXExpressionTransformMatrix(VFXExpression mat, SpaceRef spaceRef) : base(mat, spaceRef) {}

        public override VFXValueType ValueType { get { return VFXValueType.kTransform; } }
        public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXTransformMatrixOp; } }        
    }

    public class VFXExpressionTransformPosition : VFXTransformExpression
    {
        public VFXExpressionTransformPosition(VFXExpression pos, SpaceRef spaceRef) : base(pos, spaceRef) { }

        public override VFXValueType ValueType { get { return VFXValueType.kFloat3; } }
        public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXTransformPosOp; } }
    }

    public class VFXExpressionTransformVector : VFXTransformExpression
    {
        public VFXExpressionTransformVector(VFXExpression vec, SpaceRef spaceRef) : base(vec, spaceRef) { }

        public override VFXValueType ValueType { get { return VFXValueType.kFloat3; } }
        public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXTransformVecOp; } }
    }
}