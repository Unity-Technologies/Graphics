using UnityEngine.Events;

namespace UnityEngine.VFX.Utility
{
    [ExecuteAlways]
    [RequireComponent(typeof(VisualEffect))]
    public class VFXOutputEventUnityEvent : VFXOutputEventHandler
    {
        public override bool canExecuteInEditor => false;

        public UnityEvent onEvent;
        public override void OnVFXOutputEvent(VFXEventAttribute eventAttribute)
        {
            onEvent?.Invoke();
        }
    }

}
