


namespace UnityEditor.Experimental.VFX
{
    public static class ModelSerializer
    {
       /* public static VFXAssetSerializedData SerializeModel(VFXAssetModel model)
        {
            VFXAssetSerializedData serializedAsset = new VFXAssetSerializedData();
            // Get systems
            for (int i = 0; i < model.GetNbChildren(); ++i)
            {
                VFXSystemModel system = model.GetChild(i);
                var serializedSystem = new VFXSystemSerializedData();
                serializedSystem.MaxNb = system.MaxNb;
                serializedSystem.SpawnRate = system.SpawnRate;
                serializedSystem.BlendingMode = system.BlendingMode;
                serializedSystem.OrderPriority = system.OrderPriority;
                serializedSystem.ID = system.Id;
                serializedAsset.Systems.Add(serializedSystem);

                for (int j = 0; j < system.GetNbChildren(); ++j)
                {
                    VFXContextModel context = system.GetChild(j);
                    var serializedContext = new VFXContextSerializedData();
                    serializedContext.DescId = context.Desc.Name; // TODO
                    serializedContext.UIPosition = context.UIPosition;
                    serializedContext.UICollapsed = context.UICollapsed;
                    serializedSystem.Contexts.Add(serializedContext);

                    for (int k = 0; k < context.GetNbChildren(); ++k)
                    {
                        VFXBlockModel block = context.GetChild(k);
                        var serializedBlock = new VFXBlockSerializedData();
                        serializedBlock.DescId = block.Desc.ID;
                        serializedBlock.UICollapsed = block.UICollapsed;
                        serializedAsset.Systems.Add(serializedSystem);
                        serializedContext.Blocks.Add(serializedBlock);
                    }
                }
            }

            return serializedAsset;
        }

        public static VFXAssetModel DeserializeModel(VFXAssetSerializedData modelData)
        {
            var model = new VFXAssetModel();
            return null;
        }*/
    }
}