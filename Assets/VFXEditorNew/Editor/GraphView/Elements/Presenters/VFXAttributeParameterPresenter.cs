using UIElements.GraphView;
using UnityEngine;

namespace UnityEditor.VFX.UI
{
    class VFXAttributeParameterPresenter : VFXNodePresenter, IVFXPresenter
    {
        public VFXAttributeParameter attributeParameter { get { return node as VFXAttributeParameter; } }

        protected override void Reset()
        {
            if (attributeParameter)
            {
                title = attributeParameter.attributeName;
            }
            base.Reset();
        }
    }
}