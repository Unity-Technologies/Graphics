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
            vtProperty.overrideReferenceName = "MyPVT";
            vtProperty.value.procedural = true;
            vtProperty.value.shaderDeclaration = HLSLDeclaration.UnityPerMaterial;

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
            SetLayerCount(layers);
            vtProperty.generatePropertyBlock = true;
            vtProperty.hidden = true;
        }

        public override int latestVersion => 1;
        public override void OnAfterMultiDeserialize(string json)
        {
            if (sgVersion == 0)
            {
                // version 0 was implicitly declaring PVT stacks as Global shader properties
                shaderDeclaration = HLSLDeclaration.Global;
                ChangeVersion(1);
            }
        }

        [SerializeField]
        private VirtualTextureShaderProperty vtProperty = new VirtualTextureShaderProperty();

        void SetLayerCount(int layers)
        {
            var uniqueName = objectId;
            vtProperty.value.layers.Clear();
            layers = System.Math.Max(System.Math.Min(layers, SampleVirtualTextureNode.kMaxLayers), SampleVirtualTextureNode.kMinLayers);
            for (int x = 0; x < layers; x++)
            {
                vtProperty.value.layers.Add(new SerializableVirtualTextureLayer("Layer" + x + "_" + uniqueName, "Layer" + x + "_" + uniqueName, null));
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

                SetLayerCount(value);
                Dirty(ModificationScope.Topological);
                //Hack to handle downstream SampleVirtualTextureNodes
                owner.ValidateGraph();
            }
        }

        internal HLSLDeclaration shaderDeclaration
        {
            get { return vtProperty.value.shaderDeclaration; }
            set
            {
                if (vtProperty.value.shaderDeclaration == value)
                    return;

                vtProperty.value.shaderDeclaration = value;
                Dirty(ModificationScope.Graph);
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

        // to show Shader Declaration in the node settings, as if this node was itself a real AbstractShaderProperty
        internal bool AllowHLSLDeclaration(HLSLDeclaration decl) =>
            (decl == HLSLDeclaration.Global || decl == HLSLDeclaration.UnityPerMaterial);
    }
#endif // PROCEDURAL_VT_IN_GRAPH
}
