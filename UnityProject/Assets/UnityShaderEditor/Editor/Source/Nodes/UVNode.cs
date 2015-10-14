using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Title("Input/UV Node")]
    public class UVNode : BaseMaterialNode, IGeneratesVertexToFragmentBlock, IGeneratesVertexShaderBlock, IGeneratesBodyCode
    {
        private const string kOutputSlotName = "UV";

        public override bool hasPreview { get { return true; } }

        public override void Init()
        {
            base.Init();
            name = "UV";
            AddSlot(new Slot(SlotType.OutputSlot, kOutputSlotName));
        }

        public static void StaticGenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string temp = "half4 meshUV0";
            if (generationMode == GenerationMode.Preview2D)
                temp += " : TEXCOORD0";
            temp += ";";
            visitor.AddShaderChunk(temp, true);
        }

        public void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            StaticGenerateVertexToFragmentBlock(visitor, generationMode);
        }

        public static void GenerateVertexShaderBlock(ShaderGenerator visitor)
        {
            visitor.AddShaderChunk("o.meshUV0 = v.texcoord;", true);
        }

        public void GenerateVertexShaderBlock(ShaderGenerator visitor, GenerationMode generationMode)
        {
            GenerateVertexShaderBlock(visitor);
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputSlot = FindOutputSlot(kOutputSlotName);

            string uvValue = "IN.meshUV0";
            visitor.AddShaderChunk(precision + "4 " + GetOutputVariableNameForSlot(outputSlot, generationMode) + " = " + uvValue + ";", true);
        }
    }
}
