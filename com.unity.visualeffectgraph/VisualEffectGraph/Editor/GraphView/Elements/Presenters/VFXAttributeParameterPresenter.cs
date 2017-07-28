using UIElements.GraphView;
using UnityEngine;

namespace UnityEditor.VFX.UI
{
    class VFXAttributeParameterPresenter : VFXParameterSlotContainerPresenter
    {
        public VFXAttributeParameter attributeParameter { get { return model as VFXAttributeParameter; } }

        public override void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            base.Init(model, viewPresenter);
            title = attributeParameter.location == VFXAttributeLocation.Current ? "Current" : "Source";
        }
    }
}
