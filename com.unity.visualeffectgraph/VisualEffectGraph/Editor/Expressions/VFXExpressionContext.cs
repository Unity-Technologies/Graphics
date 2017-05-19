using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace UnityEditor.VFX
{
    public abstract partial class VFXExpression
    {
        public class Context
        {
            public enum ReductionOption
            {
                None =              0,
                CPUReduction =      1 << 0,
                CPUEvaluation =     1 << 1,
                ConstantFolding =   1 << 2,
            }

            public ReductionOption Option { get { return m_ReductionOption; } }

            public Context(ReductionOption reductionOption = ReductionOption.CPUReduction)
            {
                m_ReductionOption = reductionOption;
            }

            public void RegisterExpression(VFXExpression expression)
            {
                m_EndExpressions.Add(expression);
            }

            public void UnregisterExpression(VFXExpression expression)
            {
                Invalidate(expression);
                m_EndExpressions.Remove(expression);
            }

            public void Compile()
            {
                foreach (var exp in m_EndExpressions)
                    Compile(exp);
            }

            public void Recompile()
            {
                Invalidate();
                Compile();
            }

            private bool ShouldEvaluate(VFXExpression exp, VFXExpression[] reducedParents)
            {
                if (Option != ReductionOption.CPUEvaluation && Option != ReductionOption.ConstantFolding)
                    return false;

                if (!exp.Is(Flags.ValidOnCPU) || exp.Is(Flags.PerElement))
                    return false;

                Flags parentFlag = Flags.ValidOnCPU | Flags.Value;
                if (Option == ReductionOption.ConstantFolding)
                    parentFlag |= Flags.Constant;

                return reducedParents.All(e => e.Is(parentFlag));
            }

            public VFXExpression Compile(VFXExpression expression)
            {
                VFXExpression reduced;
                if (!m_ReducedCache.TryGetValue(expression, out reduced))
                {
                    var parents = expression.Parents.Select(e => Compile(e)).ToArray();
                    if (ShouldEvaluate(expression, parents))
                    {
                        reduced = expression.Evaluate(parents);
                    }
                    else if (Option != ReductionOption.None)
                    {
                        reduced = expression.Reduce(parents);
                    }
                    else
                    {
                        reduced = expression;
                    }

                    m_ReducedCache[expression] = reduced;
                }
                return reduced;
            }

            public void Invalidate()
            {
                m_ReducedCache.Clear();
            }

            public void Invalidate(VFXExpression expression)
            {
                m_ReducedCache.Remove(expression);
            }

            public VFXExpression GetReduced(VFXExpression expression)
            {
                VFXExpression reduced;
                m_ReducedCache.TryGetValue(expression, out reduced);
                return reduced != null ? reduced : expression;
            }

            private void AddReducedGraph(HashSet<VFXExpression> dst, VFXExpression exp)
            {
                if (!dst.Contains(exp))
                {
                    dst.Add(exp);
                    foreach (var parent in exp.Parents)
                        AddReducedGraph(dst, parent);
                }
            }

            public HashSet<VFXExpression> BuildAllReduced()
            {
                var reduced = new HashSet<VFXExpression>();
                foreach (var exp in m_EndExpressions)
                    if (m_ReducedCache.ContainsKey(exp))
                        AddReducedGraph(reduced, m_ReducedCache[exp]);
                return reduced;
            }

            public ReadOnlyCollection<VFXExpression> RegisteredExpressions { get { return m_EndExpressions.ToList().AsReadOnly(); } }

            private Dictionary<VFXExpression, VFXExpression> m_ReducedCache = new Dictionary<VFXExpression, VFXExpression>();
            private HashSet<VFXExpression> m_EndExpressions = new HashSet<VFXExpression>();

            private ReductionOption m_ReductionOption;
        }
    }
}
