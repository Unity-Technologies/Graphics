using System;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class LaunchEventBehavior : IPushButtonBehavior
    {
        public void OnClicked(string value)
        {
            var allComponent = UnityEngine.VFX.VFXManager.GetComponents();
            foreach (var component in allComponent)
            {
                component.SendEvent(value);
            }
        }
    }

    [VFXInfo]
    class VFXBasicEvent : VFXContext
    {
        [VFXSetting, PushButton(typeof(LaunchEventBehavior), "Send"), Delayed]
        public string eventName = VisualEffectAsset.PlayEventName;

        public VFXBasicEvent() : base(VFXContextType.Event, VFXDataType.None, VFXDataType.SpawnEvent) {}
        public override string name { get { return "Event"; } }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            return null;
        }

        public override bool CanBeCompiled()
        {
            return outputContexts.Any(c => c.CanBeCompiled());
        }
    }
}
