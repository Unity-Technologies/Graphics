using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    public class VFXDataEdgePresenter : EdgePresenter
    {
        public VFXDataEdgePresenter()
        {
        }

        public override UnityEngine.Object[] GetObjectsToWatch()
        {
            VFXDataAnchorPresenter anchorInput = input as VFXDataAnchorPresenter;
            VFXDataAnchorPresenter anchorOutput = output as VFXDataAnchorPresenter;

            return new UnityEngine.Object[] { this, anchorInput != null ? anchorInput.model : null, anchorOutput != null ? anchorOutput.model : null };
        }

        public override void OnRemoveFromGraph()
        {
            base.OnRemoveFromGraph();

            VFXDataAnchorPresenter anchorInput = input as VFXDataAnchorPresenter;
            VFXDataAnchorPresenter anchorOutput = output as VFXDataAnchorPresenter;

            if (anchorInput != null)
                anchorInput.Disconnect(this);
            if (anchorOutput != null)
                anchorOutput.Disconnect(this);
        }
    }
}
