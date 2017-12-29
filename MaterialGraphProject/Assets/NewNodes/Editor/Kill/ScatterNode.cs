using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    /*  [Title("Procedural", "Scatter")]
      public class ScatterNode : FunctionNInNOut, IGeneratesFunction
      {

          [SerializeField]
          private int m_num = 1 ;


          public int num
          {
              get { return m_num; }
              set
              {

                  if (m_num == value)
                  {
                      return;
                  }

                  m_num = value;
                  Dirty(ModificationScope.Graph);
              }
          }

          public ScatterNode()
          {
              name = "Scatter";
              AddSlot("TextureAsset", "inputTex", Graphing.SlotType.Input, SlotValueType.Texture2D, Vector4.zero);
              AddSlot("SamplerState", "inputSampler", Graphing.SlotType.Input, SlotValueType.SamplerState, Vector4.zero);
              AddSlot("UV", "inputUV", Graphing.SlotType.Input, SlotValueType.Vector2, Vector2.one);
              AddSlot("Seed", "seed", Graphing.SlotType.Input, SlotValueType.Vector2, Vector2.one);
              AddSlot("PositionRange", "p_range", Graphing.SlotType.Input, SlotValueType.Vector2, Vector2.zero);
              AddSlot("RotationRange", "r_range", Graphing.SlotType.Input, SlotValueType.Vector2, Vector2.zero);
              AddSlot("ScaleRange", "s_range", Graphing.SlotType.Input, SlotValueType.Vector2, Vector2.zero);
              AddSlot("RGBA", "finalColor", Graphing.SlotType.Output, SlotValueType.Vector4, Vector4.zero);
              UpdateNodeAfterDeserialization();
          }

          protected override string GetFunctionName()
          {
              return "unity_scatter_" + precision;
          }

          public override bool hasPreview
          {
              get { return true; }
          }

          public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
          {

              //Sampler input slot

              base.GeneratePropertyUsages(visitor, generationMode);
              var samplerSlot = FindInputSlot<MaterialSlot>(2);

              if (samplerSlot != null)
              {
                  var samplerName = GetSamplerInput(2);

                  visitor.AddShaderChunk("#ifdef UNITY_COMPILER_HLSL", false);
                  visitor.AddShaderChunk("SamplerState " + samplerName + ";", true);
                  visitor.AddShaderChunk("#endif", false);
              }
          }

          public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
          {
              var outputString = new ShaderGenerator();

              //RotateUVFunction ===================================================================
              outputString.AddShaderChunk("inline float2 rotateUV( float2 arg1, float arg2 )", false);
              outputString.AddShaderChunk("{", false);
              outputString.Indent();
              //center texture's pivot
              outputString.AddShaderChunk("arg1.xy -= 0.5;", false);
              //rotation matrix
              outputString.AddShaderChunk(precision + " s = sin(arg2);", false);
              outputString.AddShaderChunk(precision + " c = cos(arg2);", false);
              outputString.AddShaderChunk(precision + "2x2 rMatrix = float2x2(c, -s, s, c);", false);
              //center rotation matrix
              outputString.AddShaderChunk("rMatrix *= 0.5;", false);
              outputString.AddShaderChunk("rMatrix += 0.5;", false);
              outputString.AddShaderChunk("rMatrix = rMatrix*2 - 1;", false);
              //multiply the UVs by the rotation matrix
              outputString.AddShaderChunk("arg1.xy = mul(arg1.xy, rMatrix);", false);
              outputString.AddShaderChunk("arg1.xy += 0.5;", false);
              outputString.AddShaderChunk("return " + "arg1;", false);
              outputString.Deindent();
              outputString.AddShaderChunk("}", false);

              //RamdomFunction ===================================================================
              outputString.AddShaderChunk("inline float randomrange(float2 randomseed, float min, float max)", false);
              outputString.AddShaderChunk("{", false);
              outputString.Indent();
              outputString.AddShaderChunk("float randomno =  frac(sin(dot(randomseed, float2(12.9898, 78.233)))*43758.5453);", false);
              outputString.AddShaderChunk("return lerp(min,max,abs(frac(randomno)));", false);
              //outputString.AddShaderChunk("return floor(randomno * (max - min + 1)) + min;", false);
              outputString.Deindent();
              outputString.AddShaderChunk("}", false);

              //ScatterFunction ===================================================================
              outputString.AddShaderChunk(GetFunctionPrototype(), false);
              outputString.AddShaderChunk("{", false);
              outputString.Indent();

              outputString.AddShaderChunk("finalColor = float4(0,0,0,0);", false);
              outputString.AddShaderChunk("float2 newuv;", false);
              outputString.AddShaderChunk("float4 tex = finalColor;", false);

              outputString.AddShaderChunk("float scale;", false);
              outputString.AddShaderChunk("float rotation;", false);
              outputString.AddShaderChunk("float2 position;", false);
              outputString.AddShaderChunk("float j;", false);


              for (int i=0; i<m_num; i++)
              {
                  //random UV

                  outputString.AddShaderChunk("newuv = inputUV;", false);
                  outputString.AddShaderChunk("j = "+i+"+0.001f;", false);

                  outputString.AddShaderChunk("scale = randomrange(seed+j,s_range.x,s_range.y);", false); //Random Scale
                  outputString.AddShaderChunk("newuv *= scale;", false); //Scale
                  outputString.AddShaderChunk("newuv -= (scale-1)*0.5f;", false); //Move to Center After Scale

                  outputString.AddShaderChunk("rotation = randomrange(seed+j,r_range.x,r_range.y);", false); //Random rotation
                  outputString.AddShaderChunk("newuv = rotateUV(newuv,rotation);", false); //Rotate

                  outputString.AddShaderChunk("position.x = randomrange(seed.x+j,p_range.x,p_range.y);", false); //Random position
                  outputString.AddShaderChunk("position.y = randomrange(seed.y+j,p_range.x,p_range.y);", false); //Random position
                  outputString.AddShaderChunk("newuv += position*scale;", false); //Position

                  //sample
                  outputString.AddShaderChunk("#ifdef UNITY_COMPILER_HLSL", false);
                  outputString.AddShaderChunk("tex = inputTex.Sample(inputSampler, newuv);", false);
                  outputString.AddShaderChunk("#endif",false);

                  //blend together
                  outputString.AddShaderChunk("finalColor = lerp(finalColor,tex, tex.a);", false);

                  //seamless
                  outputString.AddShaderChunk("newuv = 1-newuv;", false);
                  outputString.AddShaderChunk("#ifdef UNITY_COMPILER_HLSL", false);
                  outputString.AddShaderChunk("tex = inputTex.Sample(inputSampler, newuv);", false);
                  outputString.AddShaderChunk("#endif", false);
                  //blend seamless
                  outputString.AddShaderChunk("finalColor = lerp(finalColor,tex, tex.a);", false);

              }

              outputString.Deindent();
              outputString.AddShaderChunk("}", false);

              visitor.AddShaderChunk(outputString.GetShaderString(0), true);
          }
      }*/
}
