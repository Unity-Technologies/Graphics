namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicSpawner : VFXContext
    {
        public VFXBasicSpawner() : base(VFXContextType.kSpawner, VFXDataType.kEvent, VFXDataType.kSpawnEvent) {}
        public override string name { get { return "Spawner"; } }

        protected override int inputFlowCount
        {
            get
            {
                return 2;
            }
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            if (target == VFXDeviceTarget.CPU)
                return VFXExpressionMapper.FromContext(this);

            return null;
        }
    }
}
