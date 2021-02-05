using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace _2D.ShaderGraph
{
    enum LightTextureId
    {
        LightTexture0,
        LightTexture1,
        LightTexture2,
        LightTexture3,
    }

    [Title("Input", "2D", "Light Texture")]
    class LightTextureNode :  AbstractMaterialNode
    {
        private const int OutputSlotId = 0;
        private const string kOutputSlotName = "Out";

        [SerializeField] private LightTextureId m_LightTextureId = LightTextureId.LightTexture0;

        [EnumControl("")]
        public LightTextureId matrixType
        {
            get { return m_LightTextureId; }
            set
            {
                if (m_LightTextureId == value)
                    return;

                m_LightTextureId = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public LightTextureNode()
        {
            name = "2D Light Texture";
            UpdateNodeAfterDeserialization();
        }
        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Texture2DMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        string GetVariableName()
        {
            return $"_ShapeLightTexture{(int)m_LightTextureId}";
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return $"UnityBuildTexture2DStructNoScale({GetVariableName()})";
        }

        // public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        // {
        //     properties.AddShaderProperty(new Texture2DShaderProperty()
        //     {
        //         overrideReferenceName = GetVariableName(),
        //         generatePropertyBlock = true,
        //         // value = m_Texture,
        //         modifiable = false
        //     });
        // }

        public int outputSlotId => OutputSlotId;
    }
}
