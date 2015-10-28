using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Input/Normal Node")]
    public class NormalNode : BaseMaterialNode
    {
        private const string kOutputSlotName = "Normal";

        public override bool hasPreview { get { return true; } }
        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; } }

        public override void OnCreate()
        {
            base.OnCreate();
            name = "Normal";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotName), SlotValueType.Vector3));
        }

        public override string GetOutputVariableNameForSlot(Slot slot, GenerationMode generationMode)
        {
            if (generationMode == GenerationMode.Preview2D)
                Debug.LogError("Trying to generate 2D preview on a node that does not support it!");

            return generationMode.Is2DPreview() ? "half4 (IN.Normal, 1)" : "half4 (o.Normal, 1)";
        }
    }
}
