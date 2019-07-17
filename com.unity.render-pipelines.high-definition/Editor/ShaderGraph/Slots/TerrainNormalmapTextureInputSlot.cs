using System;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition
{
    [Serializable]
    class TerrainNormalmapTextureInputSlot : Texture2DInputMaterialSlot
    {
        public TerrainNormalmapTextureInputSlot()
        { }

        public TerrainNormalmapTextureInputSlot(
            int slotId,
            string displayName,
            string shaderOutputName,
            ShaderStageCapability stageCapability = ShaderStageCapability.All,
            bool hidden = false)
            : base(slotId, displayName, shaderOutputName, stageCapability, hidden)
        { }

        public override string GetDefaultValue(GenerationMode generationMode)
            => texture == null ? "_TerrainNormalmapTexture" : base.GetDefaultValue(generationMode);

        public override void AddDefaultProperty(PropertyCollector properties, GenerationMode generationMode)
        {
            if (texture != null)
            {
                base.AddDefaultProperty(properties, generationMode);
                return;
            }

            var prop = new TextureShaderProperty();
            prop.overrideReferenceName = "_TerrainNormalmapTexture";
            prop.modifiable = false;
            prop.generatePropertyBlock = false;
            prop.value.texture = texture;
            prop.defaultType = TextureShaderProperty.DefaultType.Bump;
            properties.AddShaderProperty(prop);
        }
    }
}
