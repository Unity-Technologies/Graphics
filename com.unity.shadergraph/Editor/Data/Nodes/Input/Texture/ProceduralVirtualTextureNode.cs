using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
#if PROCEDURAL_VT_IN_GRAPH
    [Title("Input", "Texture", "Procedural Virtual Texture")]
    class ProceduralVirtualTextureNode : AbstractMaterialNode
    {
        public const int OutputSlotId = 0;

        const string kOutputSlotName = "Out";

        public ProceduralVirtualTextureNode()
        {
            UpdateNodeAfterDeserialization();
            SetLayerCount(2);

            vtProperty.displayName = "ProceduralVirtualTexture";
            vtProperty.overrideReferenceName = "MyPVT";             // TODO : make unique
            vtProperty.generatePropertyBlock = false;
            vtProperty.value.procedural = true;

            UpdateName();
        }

        void UpdateName()
        {
            name = "Procedural Virtual Texture: " + vtProperty.overrideReferenceName;
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new VirtualTextureMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        [SerializeField]
        private VirtualTextureShaderProperty vtProperty = new VirtualTextureShaderProperty();

        void SetLayerCount(int layers)
        {
            vtProperty.value.layers.Clear();
            layers = System.Math.Max(System.Math.Min(layers, SampleVirtualTextureNode.kMaxLayers), SampleVirtualTextureNode.kMinLayers);
            for (int x = 0; x < layers; x++)
            {
                // TODO: keep layer names around?  Do we want to allow custom layer names for PVT?
                vtProperty.value.layers.Add(new SerializableVirtualTextureLayer("Layer" + x, "Layer" + x, null));
            }
        }

        [IdentifierControl("Name")]
        public string vtName
        {
            get { return vtProperty.overrideReferenceName; }
            set
            {
                if (vtProperty.overrideReferenceName == value)
                    return;
                vtProperty.overrideReferenceName = value;
                UpdateName();
                Dirty(ModificationScope.Graph);
            }
        }

        [IntegerControl("Layers")]
        public int layers
        {
            get { return vtProperty.value.layers.Count; }
            set
            {
                if (vtProperty.value.layers.Count == value)
                    return;
                // TODO: is there an easier way to do this ?
                SetLayerCount(value);
                Dirty(ModificationScope.Topological);       // TODO: need some way to re-run node setup on dependent nodes, so sampleVT nodes can update their output count
            }
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(vtProperty);
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            properties.Add(new PreviewProperty(PropertyType.VirtualTexture)
            {
                name = GetVariableNameForSlot(OutputSlotId),
                vtProperty = vtProperty
            });
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            return vtProperty;
        }
    }
#endif // PROCEDURAL_VT_IN_GRAPH
}

