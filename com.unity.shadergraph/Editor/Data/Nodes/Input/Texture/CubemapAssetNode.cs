using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Texture", "Cubemap Asset")]
    [HasDependencies(typeof(MinimalCubemapAssetNode))]
    class CubemapAssetNode : AbstractMaterialNode, IPropertyFromNode
    {
        public const int OutputSlotId = 0;

        const string kOutputSlotName = "Out";

        public CubemapAssetNode()
        {
            name = "Cubemap Asset";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new CubemapMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        [SerializeField]
        private SerializableCubemap m_Cubemap = new SerializableCubemap();

        [CubemapControl("")]
        public Cubemap cubemap
        {
            get { return m_Cubemap.cubemap; }
            set
            {
                if (m_Cubemap.cubemap == value)
                    return;
                m_Cubemap.cubemap = value;
                Dirty(ModificationScope.Node);
            }
        }

        string GetTexturePropertyName()
        {
            return base.GetVariableNameForSlot(OutputSlotId);
        }

        public override string GetVariableNameForSlot(int slotId)
        {
            return $"UnityBuildTextureCubeStruct({GetTexturePropertyName()})";
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new CubemapShaderProperty()
            {
                overrideReferenceName = GetTexturePropertyName(),
                generatePropertyBlock = true,
                value = m_Cubemap,
                modifiable = false
            });
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.Cubemap)
            {
                name = GetTexturePropertyName(),
                cubemapValue = cubemap
            });
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            var prop = new CubemapShaderProperty { value = m_Cubemap };
            if (cubemap != null)
                prop.displayName = cubemap.name;
            return prop;
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }

    class MinimalCubemapAssetNode : IHasDependencies
    {
        [SerializeField]
        private SerializableCubemap m_Cubemap = null;

        public void GetSourceAssetDependencies(AssetCollection assetCollection)
        {
            var guidString = m_Cubemap.guid;
            if (!string.IsNullOrEmpty(guidString) && GUID.TryParse(guidString, out var guid))
            {
                assetCollection.AddAssetDependency(guid, AssetCollection.Flags.IncludeInExportPackage);
            }
        }
    }
}
