using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "UGUI", "RectTransform Size")]
    class RectTransformSizeNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        [SerializeField] Vector2 previewSize = Vector2.one * 100;
        public Vector2 PreviewSize { get => previewSize; set => previewSize = value; }

        [SerializeField] float previewScaleFactor = 1.0f;
        public float PreviewScaleFactor { get => previewScaleFactor; set => previewScaleFactor = value; }

        [SerializeField] float previewPixelsPerUnit = 100f;
        public float PreviewPixelsPerUnit { get => previewPixelsPerUnit; set => previewPixelsPerUnit = value; }

        public Vector4 PreviewValue => new Vector4(previewSize.x, previewSize.y, previewScaleFactor, previewPixelsPerUnit);

        public RectTransformSizeNode()
        {
            name = "RectTransform Size";
            UpdateNodeAfterDeserialization();
        }

        //public override string documentationURL => NodeUtils.GetDocumentationString("RectTransformSizeNode");

        public override bool hasPreview => false;

        public sealed override void UpdateNodeAfterDeserialization()
        {
            var slots = new List<int>();
            MaterialSlot slot = new Vector2MaterialSlot(0, "Size", "_RectTransformSize", SlotType.Output, PreviewSize);
            AddSlot(slot);
            slots.Add(0);
            MaterialSlot slot1 = new Vector1MaterialSlot(1, "Scale Factor", "_CanvasScaleFactor", SlotType.Output, PreviewScaleFactor);
            AddSlot(slot1);
            slots.Add(1);
            MaterialSlot slot2 = new Vector1MaterialSlot(2, "Pixel Per Unit", "_CanvasPixelPerUnit", SlotType.Output, PreviewPixelsPerUnit);
            AddSlot(slot2);
            slots.Add(2);
            RemoveSlotsNameNotMatching(slots, true);
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            properties.AddShaderProperty(new Vector4ShaderProperty
            {
                hidden = true,
                overrideHLSLDeclaration = true,
                hlslDeclarationOverride = HLSLDeclaration.UnityPerMaterial,
                value = PreviewValue,
                overrideReferenceName = "_RectTransformInfo"
            });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            sb.AppendLine($"$precision2 {GetVariableNameForSlot(0)} = _RectTransformInfo.xy;");
            sb.AppendLine($"$precision {GetVariableNameForSlot(1)} = _RectTransformInfo.z;");
            sb.AppendLine($"$precision {GetVariableNameForSlot(2)} = _RectTransformInfo.w;");
        }
    }
}
