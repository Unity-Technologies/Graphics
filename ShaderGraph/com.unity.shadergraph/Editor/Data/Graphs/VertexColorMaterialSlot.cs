using System;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Slots;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class VertexColorMaterialSlot : Vector4MaterialSlot, IMayRequireScreenPosition
    {
        public VertexColorMaterialSlot(int slotId, string displayName, string shaderOutputName,
                                       ShaderStage shaderStage = ShaderStage.Dynamic, bool hidden = false)
            : base(slotId, displayName, shaderOutputName, SlotType.Input, Vector3.zero, shaderStage, hidden: hidden)
        {}

        public override VisualElement InstantiateControl()
        {
            return new LabelSlotControlView("Vertex Color");
        }

        public override string GetDefaultValue(GenerationMode generationMode)
        {
            return string.Format("IN.{0}", ShaderGeneratorNames.VertexColor);
        }

        public bool RequiresScreenPosition()
        {
            return !isConnected;
        }
    }
}
