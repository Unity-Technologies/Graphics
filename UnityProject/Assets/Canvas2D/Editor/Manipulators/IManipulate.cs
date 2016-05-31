using UnityEngine;

namespace UnityEditor.Experimental
{
    internal delegate bool ManipulateDelegate(Event e, Canvas2D parent, Object customData);

    public interface IManipulate
    {
        bool GetCaps(ManipulatorCapability cap);
        void AttachTo(CanvasElement e);
    }
}
