using UIElements.GraphView;
using UnityEngine;

namespace UnityEditor.VFX.UI
{
    class VFXBuiltInParameterPresenter : VFXParameterSlotContainerPresenter
    {
        public VFXBuiltInParameter builtInParameter { get { return model as VFXBuiltInParameter; } }

        public override void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            base.Init(model, viewPresenter);

            title = builtInParameter.expressionOp.ToString() + " " + model.m_OnEnabledCount;
        }
    }
}
