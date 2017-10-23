using System;
using UnityEngine;

namespace UnityEditor.VFX
{
    class LaunchEventBehavior : IPushButtonBehavior
    {
        public void OnClicked(string value)
        {
            var allComponent = VFXComponent.GetAllActive();
            foreach (var component in allComponent)
            {
                component.SendEvent(value);
            }
        }
    }

    [VFXInfo]
    class VFXBasicEvent : VFXContext
    {
        [VFXSetting, PushButton(typeof(LaunchEventBehavior))]
        public string eventName = "OnStart";

        public VFXBasicEvent() : base(VFXContextType.kEvent, VFXDataType.kNone, VFXDataType.kEvent) {}
        public override string name { get { return "Event"; } }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            return null;
        }
    }
}
