using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class ScreenPositionMaterialSlot : Vector4MaterialSlot, IMayRequireScreenPosition
    {
        public ScreenPositionMaterialSlot(int slotId, string displayName, string shaderOutputName,
                                          ShaderStage shaderStage = ShaderStage.Dynamic, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, Vector3.zero, shaderStage, hidden)
        {}

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return ShaderGeneratorNames.ScreenPosition;
        }

        public bool RequiresScreenPosition()
        {
            return !isConnected;
        }
    }
}
