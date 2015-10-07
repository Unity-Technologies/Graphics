using UnityEngine;

namespace UnityEditor.Graphs.Material
{
    [Title("Time/Sin Time Node")]
    public class SinTimeNode : BaseMaterialNode, IRequiresTime
    {
        private const string kOutputSlotName = "SinTime";

        public override void Init()
        {
            base.Init();
            name = "Sin Time";
            AddSlot(new Slot(SlotType.OutputSlot, kOutputSlotName));
        }

        public override bool hasPreview
        {
            get { return true; }
        }

        public override string GetOutputVariableNameForSlot(Slot s, GenerationMode generationMode)
        {
            return generationMode.IsPreview() ? "EDITOR_SIN_TIME" : "_SinTime";
        }

        public override void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode)
        {
            base.GeneratePropertyBlock(visitor, generationMode);

            if (!generationMode.IsPreview())
                return;

            visitor.AddShaderProperty(new VectorPropertyChunk("EDITOR_SIN_TIME", "EDITOR_SIN_TIME", Vector4.one, true));
        }

        public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
        {
            base.GeneratePropertyUsages(visitor, generationMode);

            if (!generationMode.IsPreview())
                return;

            visitor.AddShaderChunk(precision + "4 " + GetPropertyName() + ";", true);
        }

        public string GetPropertyName() {return "EDITOR_SIN_TIME"; }
    }
}
