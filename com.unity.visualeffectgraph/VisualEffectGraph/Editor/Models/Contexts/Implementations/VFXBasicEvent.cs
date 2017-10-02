namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicEvent : VFXContext
    {
        [VFXSetting]
        public string eventName = "OnStart";

        public VFXBasicEvent() : base(VFXContextType.kEvent, VFXDataType.kNone, VFXDataType.kEvent) {}
        public override string name { get { return "Event"; } }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            return null;
        }
    }
}
