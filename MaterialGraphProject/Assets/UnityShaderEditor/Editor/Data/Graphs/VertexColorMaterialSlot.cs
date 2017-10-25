using System;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class VertexColorMaterialSlot : Vector4MaterialSlot, IMayRequireScreenPosition
    {
        public VertexColorMaterialSlot(int slotId, string displayName, string shaderOutputName,
            ShaderStage shaderStage = ShaderStage.Dynamic, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, Vector3.zero, shaderStage, hidden)
        { }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return ShaderGeneratorNames.VertexColor;
        }

        public bool RequiresScreenPosition()
        {
            return !isConnected;
        }
    }
}