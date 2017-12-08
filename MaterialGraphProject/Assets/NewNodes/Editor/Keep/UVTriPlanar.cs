using UnityEditor.Graphing;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    /* [Title("UV", "Tri-Planar Mapping")]
     public class UVTriPlanar : FunctionNInNOut, IGeneratesFunction, IMayRequireNormal, IMayRequireWorldPosition
     {
         private int slot0, slot1, slot2, slot3, slot4, slot5, slot6 = 0;

         private string slot0Name = "texRef";
         private string slot1Name = "tileFactor";
         private string slot2Name = "blendFactor";
         private string slot4Name = "normalRef";
         private string slot5Name = "posRef";
         private string slot6Name = "outputRef";

         protected override string GetFunctionName()
         {
             return "unity_triplanar_" + precision;
         }

         public UVTriPlanar()
         {
             name = "UVTriPlanar";

             slot0 = AddSlot("Texture", slot0Name, Graphing.SlotType.Input, SlotValueType.Sampler2D, Vector4.zero);
             slot1 = AddSlot("Tile", slot1Name, Graphing.SlotType.Input, SlotValueType.Vector1, Vector4.zero);
             slot2 = AddSlot("Blend", slot2Name, Graphing.SlotType.Input, SlotValueType.Vector1, Vector4.zero);
             slot4 = AddSlot("Normals", slot4Name, Graphing.SlotType.Input, SlotValueType.Vector3, Vector3.one);
             slot5 = AddSlot("Position", slot5Name, Graphing.SlotType.Input, SlotValueType.Vector3, Vector3.one);
             slot6 = AddSlot("RGBA ", slot6Name, Graphing.SlotType.Output, SlotValueType.Vector4, Vector4.zero);

             UpdateNodeAfterDeserialization();
         }

         public override bool hasPreview
         {
             get { return true; }
         }

         public override PreviewMode previewMode
         {
             get
             {
                 return PreviewMode.Preview3D;
             }
         }



         //TODO:Externalize
         //Reference code from:http://www.chilliant.com/rgb2hsv.html
         public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
         {
             var outputString = new ShaderGenerator();

             // outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2", "arg3", samplerName), false);
             outputString.AddShaderChunk(GetFunctionPrototype(), false);
             outputString.Indent();
             outputString.AddShaderChunk("{", false);
             // create UVs from position
             outputString.AddShaderChunk("float3 uvs = " + slot5Name + "*" + slot1Name + ";", false);

             // use absolute value of normal as texture weights
             outputString.AddShaderChunk("half3 blend = pow(abs(" + slot4Name + ")," + slot2Name + ");", false);

             // make sure the weights sum up to 1 (divide by sum of x+y+z)
             outputString.AddShaderChunk("blend /= dot(blend, 1.0);", false);

             // read the three texture projections, for x,y,z axes
             outputString.AddShaderChunk("float4 cx = " + "tex2D(" + slot0Name + ", uvs.yz);", false);
             outputString.AddShaderChunk("float4 cy = " + "tex2D(" + slot0Name + ", uvs.xz);", false);
             outputString.AddShaderChunk("float4 cz = " + "tex2D(" + slot0Name + ", uvs.xy);", false);

             // blend the textures based on weights
             outputString.AddShaderChunk(slot6Name + " = cx * blend.x + cy * blend.y + cz * blend.z;", false);

             outputString.Deindent();
             outputString.AddShaderChunk("}", false);
             visitor.AddShaderChunk(outputString.GetShaderString(0), true);
         }




         //prevent validation errors when a sampler2D input is missing
         //use on any input requiring a Texture2DNode
         public override void ValidateNode()
         {
             base.ValidateNode();
             var slot = FindInputSlot<MaterialSlot>(0);
             if (slot == null)
                 return;

             var edges = owner.GetEdges(slot.slotReference).ToList();
             hasError |= edges.Count == 0;
         }

         public bool RequiresNormal()
         {
             return true;
         }

         public bool RequiresWorldPosition()
         {
             return true;
         }
     }
 }

 ///////////////// TEXTURE2D version below. Works fine, but the master nodes don't jive with it //////////////////////////


 /*

 namespace UnityEditor.ShaderGraph
 {
     [Title("UV", "Tri-Planar Mapping")]
     public class UVTriPlanar : FunctionNInNOut, IGeneratesFunction, IMayRequireNormal, IMayRequireWorldPosition
     {
         private int slot0, slot1, slot2, slot3, slot4, slot5, slot6 = 0;

         private string slot0Name = "texRef";
         private string slot1Name = "tileFactor";
         private string slot2Name = "blendFactor";
         private string slot3Name = "samplerRef";
         private string slot4Name = "normalRef";
         private string slot5Name = "posRef";
         private string slot6Name = "outputRef";

         protected override string GetFunctionName()
         {
             return "unity_triplanar_" + precision;
         }

         public UVTriPlanar()
         {
             name = "UVTriPlanar";

             slot0 = AddSlot("Texture", slot0Name, Graphing.SlotType.Input, SlotValueType.Texture2D, Vector4.zero);
             slot1 = AddSlot("Tile", slot1Name, Graphing.SlotType.Input, SlotValueType.Vector1, Vector4.zero);
             slot2 = AddSlot("Blend", slot2Name, Graphing.SlotType.Input, SlotValueType.Vector1, Vector4.zero);
             slot3 = AddSlot("Sampler", slot3Name, Graphing.SlotType.Input, SlotValueType.SamplerState, Vector4.zero);
             slot4 = AddSlot("Normals", slot4Name, Graphing.SlotType.Input, SlotValueType.Vector3, Vector3.one);
             slot5 = AddSlot("Position", slot5Name, Graphing.SlotType.Input, SlotValueType.Vector3, Vector3.one);
             slot6 = AddSlot("RGBA ", slot6Name, Graphing.SlotType.Output, SlotValueType.Vector4, Vector4.zero);

             UpdateNodeAfterDeserialization();
         }

         public override bool hasPreview
         {
             get { return true; }
         }

         public override PreviewMode previewMode
         {
             get
             {
                 return PreviewMode.Preview3D;
             }
         }



         //TODO:Externalize
         //Reference code from:http://www.chilliant.com/rgb2hsv.html
         public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
         {
             var outputString = new ShaderGenerator();
             //Sampler input
             var samplerName = GetSamplerInput(3);
             outputString.AddShaderChunk("#ifdef UNITY_COMPILER_HLSL", false);

            // outputString.AddShaderChunk(GetFunctionPrototype("arg1", "arg2", "arg3", samplerName), false);
             outputString.AddShaderChunk(GetFunctionPrototype(), false);
             outputString.Indent();
             outputString.AddShaderChunk("{", false);
             // create UVs from position
             outputString.AddShaderChunk("float3 uvs = " + slot5Name + "*" + slot1Name + ";", false);

             // use absolute value of normal as texture weights
             outputString.AddShaderChunk("half3 blend = pow(abs(" + slot4Name + ")," + slot2Name + ");", false);

             // make sure the weights sum up to 1 (divide by sum of x+y+z)
             outputString.AddShaderChunk("blend /= dot(blend, 1.0);", false);

             // read the three texture projections, for x,y,z axes
             outputString.AddShaderChunk("float4 cx = " + slot0Name + ".Sample(" + slot3Name + ", uvs.yz);", false);
             outputString.AddShaderChunk("float4 cy = " + slot0Name + ".Sample(" + slot3Name + ", uvs.xz);", false);
             outputString.AddShaderChunk("float4 cz = " + slot0Name + ".Sample(" + slot3Name + ", uvs.xy);", false);


             // blend the textures based on weights
             outputString.AddShaderChunk(slot6Name + " = cx * blend.x + cy * blend.y + cz * blend.z;", false);

             //outputString.AddShaderChunk("return " + "c;", false);
             outputString.Deindent();
             outputString.AddShaderChunk("}", false);
             outputString.AddShaderChunk("#endif", true);
             visitor.AddShaderChunk(outputString.GetShaderString(0), true);
         }

         public override void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode)
         {

     //Sampler input slot

             base.GeneratePropertyUsages(visitor, generationMode);
             var samplerSlot = FindInputSlot<MaterialSlot>(slot3);

             if (samplerSlot != null)
             {
                 var samplerName = GetSamplerInput(slot3);

                 visitor.AddShaderChunk("#ifdef UNITY_COMPILER_HLSL", false);
                 visitor.AddShaderChunk("SamplerState " + samplerName + ";", true);
                 visitor.AddShaderChunk("#endif", false);
             }
         }


         //prevent validation errors when a sampler2D input is missing
         //use on any input requiring a Texture2DNode
         public override void ValidateNode()
         {
             base.ValidateNode();
             var slot = FindInputSlot<MaterialSlot>(0);
             if (slot == null)
                 return;

             var edges = owner.GetEdges(slot.slotReference).ToList();
             hasError |= edges.Count == 0;
         }

         public bool RequiresNormal()
         {
             return true;
         }

         public bool RequiresWorldPosition()
         {
             return true;
         }
     }*/
}
