using UnityEditor.Graphing;
using System.Linq;
using System.Collections;
using System;

namespace UnityEditor.ShaderGraph
{
    /* public abstract class FunctionNInNOut : AbstractMaterialNode, IGeneratesBodyCode
     {

         public FunctionNInNOut()
         {
             name = "FunctionNInNOut";
             UpdateNodeAfterDeserialization();
         }

         public override void UpdateNodeAfterDeserialization()
         {
             base.UpdateNodeAfterDeserialization();
             foreach (var slot in GetSlots<MaterialSlot>())
             {
                 slot.showValue = true;
             }
         }

         public int AddSlot(string displayName, string nameInShader, SlotType slotType, SlotValueType valueType, Vector4 defaultValue)
         {
             int nextSlotId;
             if (slotType == SlotType.Output)
                 nextSlotId = -( GetOutputSlots<MaterialSlot>().Count() + 1 );
             else
                 nextSlotId = GetInputSlots<MaterialSlot>().Count() + 1;

             bool useDefaultValue = (valueType != SlotValueType.Texture2D && valueType != SlotValueType.Sampler2D);
             AddSlot(new MaterialSlot(nextSlotId, displayName, nameInShader, slotType, valueType, defaultValue, useDefaultValue));
             return nextSlotId;
         }

         public void RemoveOutputSlot()
         {
             var lastSlotId = -GetOutputSlots<ISlot>().Count();
             RemoveSlotById(lastSlotId);
         }

         public void RemoveInputSlot()
         {
             var lastSlotId = GetInputSlots<ISlot>().Count();
             RemoveSlotById(lastSlotId);
         }

         private void RemoveSlotById(int slotID)
         {
             if (slotID == 0)
                 return;
             RemoveSlot(slotID);
         }

         public string GetSlotTypeName(int slotId)
         {
             return ConvertConcreteSlotValueTypeToString(FindSlot<MaterialSlot>(slotId).concreteValueType);
         }

         public ConcreteSlotValueType GetSlotValueType(int slotId)
         {
             return FindSlot<MaterialSlot>(slotId).concreteValueType;
         }

         protected string GetShaderOutputName(int slotId)
         {
             return FindSlot<MaterialSlot>(slotId).shaderOutputName;
         }

         // Must override this
         protected abstract string GetFunctionName();

         private string GetFunctionParameters()
         {
             string param = "";
             int remainingParams = GetSlots<ISlot>().Count();
             foreach (ISlot inSlot in GetSlots<ISlot>())
             {
                 if (inSlot.isOutputSlot)
                     param += "out ";

                 if (FindSlot<MaterialSlot>(inSlot.id).concreteValueType != ConcreteSlotValueType.Texture2D
                     && FindSlot<MaterialSlot>(inSlot.id).concreteValueType != ConcreteSlotValueType.SamplerState
                     && FindSlot<MaterialSlot>(inSlot.id).concreteValueType != ConcreteSlotValueType.Sampler2D
                     )
                     param += precision;
                 param += GetSlotTypeName(inSlot.id) + " ";
                 param += GetShaderOutputName(inSlot.id);

                 if (remainingParams > 1)
                     param += ", ";
                 --remainingParams;
             }

             return param;
         }

         private string GetFunctionCallParameters(GenerationMode generationMode)
         {
             string param = "";
             int remainingParams = GetSlots<ISlot>().Count();
             foreach (ISlot inSlot in GetSlots<ISlot>())
             {

                 if (FindSlot<MaterialSlot>(inSlot.id).concreteValueType == ConcreteSlotValueType.SamplerState)
                     param += GetSamplerInput(inSlot.id);
                 else
                     param += GetSlotValue(inSlot.id, generationMode);

                 if (remainingParams > 1)
                     param += ", ";
                 --remainingParams;
             }

             return param;
         }

         protected string GetFunctionPrototype()
         {
             return "inline " + "void" + " " + GetFunctionName() +
                 " (" +
                 GetFunctionParameters() +
                 ")";
         }


         protected virtual string GetFunctionCall(GenerationMode generationMode)
         {
             string prefix = "";
             string sufix = "";
             foreach (ISlot slot in GetInputSlots<MaterialSlot>())
             {
                 if (GetSlotValueType(slot.id) == ConcreteSlotValueType.Texture2D || GetSlotValueType(slot.id) == ConcreteSlotValueType.SamplerState)
                 {
                     prefix = "#ifdef UNITY_COMPILER_HLSL \n";
                     sufix = "\n #endif";
                 }
             }
             return prefix + GetFunctionName() +
                " (" +
                GetFunctionCallParameters(generationMode) +
                ");" + sufix;
         }

         private string GetOutputDeclaration()
         {
             string outDeclaration = "";
             foreach (MaterialSlot outSlot in GetOutputSlots<MaterialSlot>())
             {
                 outDeclaration += "\n " + precision + GetSlotTypeName(outSlot.id) + " " + GetVariableNameForSlot(outSlot.id) + ";\n";
             }

             return outDeclaration;
         }

         public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
         {
             var outputString = new ShaderGenerator();
             outputString.AddShaderChunk(GetOutputDeclaration(), false);
             outputString.AddShaderChunk(GetFunctionCall(generationMode), false);

             visitor.AddShaderChunk(outputString.GetShaderString(0), true);
         }

         public string GetSamplerInput(int slotID)
         {
             //default sampler if no input is provided
             var samplerName = "my_linear_repeat_sampler";

             //Sampler input slot
             var samplerSlot = FindInputSlot<MaterialSlot>(slotID);

             if (samplerSlot != null)
             {
                 var edgesSampler = owner.GetEdges(samplerSlot.slotReference).ToList();

                 if (edgesSampler.Count > 0)
                 {
                     var edge = edgesSampler[0];
                     var fromNode = owner.GetNodeFromGuid<AbstractMaterialNode>(edge.outputSlot.nodeGuid);
                     samplerName = ShaderGenerator.AdaptNodeOutput(fromNode, edge.outputSlot.slotId, ConcreteSlotValueType.SamplerState, true);
                 }
             }

             return samplerName;
         }
     }*/
}
