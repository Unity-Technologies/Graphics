using UnityEditor.Graphing;
using System.Linq;
using System.Collections;

namespace UnityEditor.ShaderGraph
{
    /*  [Title("Art", "ChannelBlend")]
      public class ChannelBlend : FunctionNInNOut, IGeneratesFunction
      {

          public ChannelBlend()
          {
              name = "ChannelBlend";
              AddSlot("Mask", "mask", Graphing.SlotType.Input, SlotValueType.Vector4, Vector4.zero);
              AddSlot("RColor", "rCol", Graphing.SlotType.Input, SlotValueType.Vector4, Vector4.zero);
              AddSlot("GColor", "gCol", Graphing.SlotType.Input, SlotValueType.Vector4, Vector4.zero);
              AddSlot("BColor", "bCol", Graphing.SlotType.Input, SlotValueType.Vector4, Vector4.zero);
              AddSlot("AColor", "aCol", Graphing.SlotType.Input, SlotValueType.Vector4, Vector4.zero);
              AddSlot("BGColor", "bgCol", Graphing.SlotType.Input, SlotValueType.Vector4, Vector4.zero);

              AddSlot("BlendedColor", "blendCol", Graphing.SlotType.Output, SlotValueType.Vector4, Vector4.zero);
          }

          protected override string GetFunctionName()
          {
              return "unity_ChannelBlend";
          }

          public override bool hasPreview
          {
              get { return true; }
          }

          public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
          {
              var outputString = new ShaderGenerator();
              outputString.AddShaderChunk(GetFunctionPrototype(), false);
              outputString.AddShaderChunk("{", false);
              outputString.AddShaderChunk("float4 background = step(max(mask.r,max(mask.g,mask.b)), 0.001) * bgCol;", false);
              outputString.AddShaderChunk("blendCol = mask.r * rCol + mask.g * gCol + mask.b * bCol + mask.a * aCol + background;", false);
              outputString.AddShaderChunk("}", false);

              visitor.AddShaderChunk(outputString.GetShaderString(0), true);
          }
      }*/
}
