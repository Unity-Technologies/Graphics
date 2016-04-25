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

        private VFXExpression m_X;
        private VFXExpression m_Y;
        private VFXExpression m_Z;
        private VFXExpression m_W;

        private VFXValueFloat4 m_Cached;
        private bool m_CacheValid;
    }

}