namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicSpawner : VFXContext
    {
        public VFXBasicSpawner() : base(VFXContextType.kSpawner, VFXDataType.kNone, VFXDataType.kSpawnEvent) {}
        public override string name { get { return "Spawner"; } }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            if (target == VFXDeviceTarget.CPU)
                return VFXExpressionMapper.FromContext(this);

            return null;
        }
    }
}
