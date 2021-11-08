using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Texture 2D Array Asset")]
    [HasDependencies(typeof(Minimal2dArrayTextureAssetNode))]
    class Texture2DArrayAssetNode : AbstractMaterialNode, IPropertyFromNode
    {
        public const int OutputSlotId = 0;

        const string kOutputSlotName = "Out";

        public Texture2DArrayAssetNode()
        {
            name = "Texture 2D Array Asset";
            synonyms = new string[] { "stack", "pile" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Texture2DArrayMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        [SerializeField]
        private SerializableTextureArray m_Texture = new SerializableTextureArray();

        [TextureArrayControl("")]
        public Texture2DArray texture
        {
            get { return m_Texture.textureArray; }
            set
            {
                if (m_Texture.textureArray == value)
                    return;
                m_Texture.textureArray = value;
                Dirty(ModificationScope.Node);
            }
        }

        string GetTexturePropertyName()
        {
            return base.GetVariableNameForSlot(OutputSlotId);
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return $"UnityBuildTexture2DArrayStruct({GetTexturePropertyName()})";
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new Texture2DArrayShaderProperty()
            {
                overrideReferenceName = GetTexturePropertyName(),
                generatePropertyBlock = true,
                value = m_Texture,
                modifiable = false
            });
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Texture2DArray)
            {
                name = GetTexturePropertyName(),
                textureValue = texture
            });
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            var prop = new Texture2DArrayShaderProperty { value = m_Texture };
            if (texture != null)
                prop.displayName = texture.name;
            return prop;
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }

    class Minimal2dArrayTextureAssetNode : IHasDependencies
    {
        [SerializeField]
        private SerializableTextureArray m_Texture = null;

        public void GetSourceAssetDependencies(AssetCollection assetCollection)
        {
            var guidString = m_Texture.guid;
            if (!string.IsNullOrEmpty(guidString) && GUID.TryParse(guidString, out var guid))
            {
                assetCollection.AddAssetDependency(guid, AssetCollection.Flags.IncludeInExportPackage);
            }
        }
    }
}
