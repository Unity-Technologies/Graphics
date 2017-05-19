using UIElements.GraphView;
using UnityEngine;

namespace UnityEditor.VFX.UI
{
    class VFXBuiltInParameterPresenter : VFXNodePresenter, IVFXPresenter
    {
        public VFXBuiltInParameter builtInParameter { get { return node as VFXBuiltInParameter; } }

        protected override void Reset()
        {
            if (builtInParameter)
            {
                title = builtInParameter.expressionOp.ToString() + " " + node.m_OnEnabledCount;
            }
            base.Reset();
        }
    }
}
