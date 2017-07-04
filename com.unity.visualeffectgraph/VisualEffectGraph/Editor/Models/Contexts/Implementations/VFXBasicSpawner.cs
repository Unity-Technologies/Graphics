namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicSpawner : VFXContext
    {
        public VFXBasicSpawner() : base(VFXContextType.kSpawner, VFXDataType.kNone, VFXDataType.kSpawnEvent) {}
        public override string name { get { return "Spawner"; } }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            if (target != VFXDeviceTarget.CPU)
                return null;

            var mapper = new VFXExpressionMapper("");
            foreach (var block in children)
            {
                int blockId = GetIndex(block);
                for (int iSlot = 0; iSlot < block.GetNbInputSlots(); ++iSlot)
                {
                    var slot = block.GetInputSlot(iSlot);
                    mapper.AddExpression(slot.GetExpression(), slot.name, blockId);
                }
            }
            return mapper;
        }
    }
}
