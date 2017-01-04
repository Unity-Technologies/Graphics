using System;
using System.Collections.Generic;

namespace UnityEditor.VFX
{
    class VFXExpressionContext
    {
        public void RegisterExpression(VFXExpression expression)
        {
            m_EndExpressions.Add(expression);
        }

        public void UnregisterExpression(VFXExpression expression)
        {
            Invalidate(expression);
            m_EndExpressions.Remove(expression);
        }

        public VFXExpression GetReduced(VFXExpression expression)
        {
            VFXExpression reduced = m_ReducedCache[expression];
            if (reduced == null)
            {
                reduced = expression.Reduce(this);
                m_ReducedCache[expression] = reduced;
            }
            return reduced;
        }

        public void Invalidate(VFXExpression expression)
        {
            m_ReducedCache.Remove(expression);
        }

        private Dictionary<VFXExpression, VFXExpression> m_ReducedCache;
        private HashSet<VFXExpression> m_EndExpressions;
    }
}