using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

namespace UnityEditor.VFX.UI
{
    class VFXBuiltInParameterPresenter : VFXParameterSlotContainerPresenter
    {
        public VFXBuiltInParameter builtInParameter { get { return model as VFXBuiltInParameter; } }

        public override void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            base.Init(model, viewPresenter);
        }

        public override void UpdateTitle()
        {
            title = builtInParameter.expressionOp.ToString();
        }
    }
}
