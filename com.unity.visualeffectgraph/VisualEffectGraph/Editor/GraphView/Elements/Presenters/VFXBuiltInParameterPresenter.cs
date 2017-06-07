using UIElements.GraphView;
using UnityEngine;

namespace UnityEditor.VFX.UI
{
    class VFXBuiltInParameterPresenter : VFXSlotContainerPresenter
    {
        public VFXBuiltInParameter builtInParameter { get { return model as VFXBuiltInParameter; } }
        /*
        protected override void Reset()
        {
            if (builtInParameter)
            {
                title = builtInParameter.expressionOp.ToString() + " " + node.m_OnEnabledCount;
            }
            base.Reset();
        }
        */
    }
}
