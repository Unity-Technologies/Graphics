using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

namespace UnityEditor.VFX.UI
{
    internal class VFXFlowEdgeController : VFXEdgeController<VFXFlowAnchorController>
    {
        public VFXFlowEdgeController(VFXFlowAnchorController input, VFXFlowAnchorController output) : base(input, output)
        {
        }

        public override void ApplyChanges()
        {
            NotifyChange(AnyThing);
        }
    }
}
