using UIElements.GraphView;
using UnityEngine;

namespace UnityEditor.VFX.UI
{
    class VFXAttributeParameterPresenter : VFXSlotContainerPresenter
    {
        public VFXAttributeParameter attributeParameter { get { return model as VFXAttributeParameter; } }
        /*
        protected override void Reset()
        {
            if (attributeParameter)
            {
                title = attributeParameter.attributeName;
            }
            base.Reset();
        }*/
    }
}
