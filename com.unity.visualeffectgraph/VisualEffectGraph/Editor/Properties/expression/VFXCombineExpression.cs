using System.Collections.Generic;

namespace UnityEngine.Experimental.VFX
{
    class VFXExpressionCombineFloat2 : VFXExpression
    {
        public VFXExpressionCombineFloat2(VFXExpression x, VFXExpression y)
        {
            m_X = x;
            m_Y = y;
            m_Cached = new VFXValueFloat2();
        }

        public override VFXValueType ValueType { get { return VFXValueType.kFloat2; } }
        public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXCombine2fOp; } }

        // Reduce the expression and potentially cache the result before returning it
        public override VFXExpression Reduce()
        {
            if (m_CacheValid)
                return m_Cached;

            if (m_X.IsValue() && m_Y.IsValue())
            {
                m_Cached.Set(new Vector2(m_X.Get<float>(), m_Y.Get<float>()));
                m_CacheValid = true;
                return m_Cached;
            }

            return this;
        }

        // Invalidate the reduction to impose a recomputation
        public override void Invalidate()
        {
            m_CacheValid = false;
        }

        public override VFXExpression[] GetParents()
        {
            return new VFXExpression[] { m_X, m_Y };
        }

        private VFXExpression m_X;
        private VFXExpression m_Y;

        private VFXValueFloat2 m_Cached;
        private bool m_CacheValid;
    }

    class VFXExpressionCombineFloat3 : VFXExpression
    {
        public VFXExpressionCombineFloat3(VFXExpression x, VFXExpression y, VFXExpression z)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
            m_Cached = new VFXValueFloat3();
        }

        public override VFXValueType ValueType { get { return VFXValueType.kFloat3; } }
        public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXCombine3fOp; } }

        // Reduce the expression and potentially cache the result before returning it
        public override VFXExpression Reduce()
        {
            if (m_CacheValid)
                return m_Cached;

            if (m_X.IsValue() && m_Y.IsValue() && m_Z.IsValue())
            {
                m_Cached.Set(new Vector3(m_X.Get<float>(),m_Y.Get<float>(),m_Z.Get<float>()));
                m_CacheValid = true;
                return m_Cached;
            }

            return this; 
        }

        // Invalidate the reduction to impose a recomputation
        public override void Invalidate()
        {
            m_CacheValid = false;
        }

        public override VFXExpression[] GetParents()
        {
            return new VFXExpression[] { m_X, m_Y, m_Z };
        }

        private VFXExpression m_X;
        private VFXExpression m_Y;
        private VFXExpression m_Z;

        private VFXValueFloat3 m_Cached;
        private bool m_CacheValid;
    }

    class VFXExpressionCombineFloat4 : VFXExpression
    {
        public VFXExpressionCombineFloat4(VFXExpression x, VFXExpression y, VFXExpression z, VFXExpression w)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
            m_W = w;
            m_Cached = new VFXValueFloat4();
        }

        public override VFXValueType ValueType { get { return VFXValueType.kFloat4; } }
        public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXCombine4fOp; } }

        // Reduce the expression and potentially cache the result before returning it
        public override VFXExpression Reduce()
        {
            if (m_CacheValid)
                return m_Cached;

            if (m_X.IsValue() && m_Y.IsValue() && m_Z.IsValue() && m_W.IsValue())
            {
                m_Cached.Set(new Vector4(m_X.Get<float>(), m_Y.Get<float>(), m_Z.Get<float>(), m_W.Get<float>()));
                m_CacheValid = true;
                return m_Cached;
            }

            return this;
        }

        // Invalidate the reduction to impose a recomputation
        public override void Invalidate()
        {
            m_CacheValid = false;
        }

        public override VFXExpression[] GetParents()
        {
            return new VFXExpression[] { m_X, m_Y, m_Z, m_W };
        }

        private VFXExpression m_X;
        private VFXExpression m_Y;
        private VFXExpression m_Z;
        private VFXExpression m_W;

        private VFXValueFloat4 m_Cached;
        private bool m_CacheValid;
    }

    // TODO Move that in another file
    class VFXExpressionTRSToMatrix : VFXExpression
    {
        public VFXExpressionTRSToMatrix(VFXExpression t, VFXExpression r, VFXExpression s)
        {
            m_Position = t;
            m_Rotation = r;
            m_Scale = s;

            m_Cached = new VFXValueTransform();
        }

        public override VFXValueType ValueType { get { return VFXValueType.kTransform; } }
        public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXTRSToMatrixOp; } }

        // Reduce the expression and potentially cache the result before returning it
        public override VFXExpression Reduce()
        {
            if (m_CacheValid)
                return m_Cached;

            if (m_Position.IsValue() && m_Rotation.IsValue() && m_Scale.IsValue())
            {
                Vector3 position = m_Position.Get<Vector3>();
                Quaternion rotation = Quaternion.Euler(m_Rotation.Get<Vector3>());
                Vector3 scale = m_Scale.Get<Vector3>();

                m_Cached.Set(Matrix4x4.TRS(position, rotation, scale));
                m_CacheValid = true;
                return m_Cached;
            }

            return this;
        }

        // Invalidate the reduction to impose a recomputation
        public override void Invalidate()
        {
            m_CacheValid = false;
        }

        public override VFXExpression[] GetParents()
        {
            return new VFXExpression[] { m_Position, m_Rotation, m_Scale };
        }

        private VFXExpression m_Position;
        private VFXExpression m_Rotation;
        private VFXExpression m_Scale;

        private VFXValueTransform m_Cached;
        private bool m_CacheValid;
    }

    class VFXExpressionInverseTRS : VFXExpression
    {
        public VFXExpressionInverseTRS(VFXExpression trs)
        {
            m_Trs = trs;
            m_Cached = new VFXValueTransform();
        }

        public override VFXValueType ValueType { get { return VFXValueType.kTransform; } }
        public override VFXExpressionOp Operation { get { return VFXExpressionOp.kVFXInverseTRSOp; } }

        // Reduce the expression and potentially cache the result before returning it
        public override VFXExpression Reduce()
        {
            if (m_CacheValid)
                return m_Cached;

            if (m_Trs.IsValue())
            {
                Matrix4x4 trs = m_Trs.Get<Matrix4x4>();
                m_Cached.Set(trs.inverse);
                m_CacheValid = true;
                return m_Cached;
            }

            return this;
        }

        // Invalidate the reduction to impose a recomputation
        public override void Invalidate()
        {
            m_CacheValid = false;
        }

        public override VFXExpression[] GetParents()
        {
            return new VFXExpression[] { m_Trs };
        }

        private VFXExpression m_Trs;

        private VFXValueTransform m_Cached;
        private bool m_CacheValid;
    }

}