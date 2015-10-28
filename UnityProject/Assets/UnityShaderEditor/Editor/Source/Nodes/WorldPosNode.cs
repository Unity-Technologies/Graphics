using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Input/World Pos Node")]
    public class WorldPosNode : BaseMaterialNode, IGeneratesVertexToFragmentBlock
    {
        private const string kOutputSlotName = "WorldPos";

        public override bool hasPreview { get { return true; } }

        public override void OnCreate()
        {
            base.OnCreate();
            name = "WorldPos";
        }

        public override void OnEnable()
        {
            base.OnEnable();
            AddSlot(new MaterialGraphSlot(new Slot(SlotType.OutputSlot, kOutputSlotName), null));
        }

        public override string GetOutputVariableNameForSlot(Slot slot, GenerationMode generationMode)
        {
            // For now add () around whole expressions so that things like .xy work
            // A better solution would be to provide information on the context in which this is used so that we avoid expressions like (half4(p, 1, 1)).xy
            // and can directly output p.
            return "(" + precision + "4 (IN.worldPos, 1))";
        }

        public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            visitor.AddShaderChunk(precision + "3 worldPos;", true);
        }
    }
}
