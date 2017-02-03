using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.VFX
{
    public abstract partial class VFXExpression
    {
        public class Context
        {
            public enum ReductionOption
            {
                CPUEvaluation = 0,
                ConstantFolding = 1,
            }

            public ReductionOption Option { get { return ReductionOption.ConstantFolding; } }

            public void RegisterExpression(VFXExpression expression)
            {
                m_EndExpressions.Add(expression);
            }

            public void UnregisterExpression(VFXExpression expression)
            {
                Invalidate(expression);
                m_EndExpressions.Remove(expression);
            }

            public VFXExpression Compile(VFXExpression expression)
            {
                VFXExpression reduced;
                if (!m_ReducedCache.TryGetValue(expression, out reduced))
                {
                    var parents = expression.Parents.Select(e => Compile(e)).ToArray();
                    if (Option == ReductionOption.ConstantFolding && parents.All(e => e.Is(Flags.Constant | Flags.ValidOnCPU)))
                    {
                        reduced = expression.Evaluate(parents);
                    }
                    else
                    {
                        reduced = expression.Reduce(parents);
                    }

                    m_ReducedCache[expression] = reduced;
                }
                return reduced;
            }

            public void Invalidate(VFXExpression expression)
            {
                m_ReducedCache.Remove(expression);
            }

            private Dictionary<VFXExpression, VFXExpression> m_ReducedCache = new Dictionary<VFXExpression, VFXExpression>();
            private HashSet<VFXExpression> m_EndExpressions = new HashSet<VFXExpression>();
        }
    }
}