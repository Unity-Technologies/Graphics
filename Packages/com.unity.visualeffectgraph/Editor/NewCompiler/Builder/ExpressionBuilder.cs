using System.Collections.Generic;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class ExpressionBuilder : SystemBuilder
    {
        List<VFXContext> m_CompilableContexts = new();

        public override bool ProcessContext(VFXContext context)
        {
            base.ProcessContext(context);
            m_CompilableContexts.Add(context);
            return true;
        }

        public override void Build()
        {
            ExpressionGraph.CompileExpressions(m_CompilableContexts,
                VfxExpressionReducer.GetReductionOptions(CompilationMode));
        }

        public override void EndBuilding()
        {
            m_CompilableContexts.Clear();
            base.EndBuilding();
        }
    }
}
