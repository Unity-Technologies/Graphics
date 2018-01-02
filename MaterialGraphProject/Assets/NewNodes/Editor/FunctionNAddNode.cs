using UnityEditor.Graphing;
using System.Linq;
using System.Collections;

namespace UnityEditor.ShaderGraph
{
    /*  [Title("Math", "Advanced", "Adder")]
      public class AddManyNode : FunctionNInNOut, IGeneratesFunction
      {
          int m_nodeInputCount = 2;

          public void AddInputSlot()
          {
              string inputName = "Input" + GetInputSlots<MaterialSlot>().Count().ToString();
              AddSlot(inputName, inputName, Graphing.SlotType.Input, SlotValueType.Dynamic, Vector4.zero);
          }

          public AddManyNode()
          {
              name = "Adder";
              for(int i = 0; i < m_nodeInputCount; ++i)
              {
                  AddInputSlot();
              }

              AddSlot("Sum", "finalSum", Graphing.SlotType.Output, SlotValueType.Dynamic, Vector4.zero);
              UpdateNodeAfterDeserialization();
          }

          public void OnModified()
          {
              Dirty(ModificationScope.Node);
          }

          protected override string GetFunctionName()
          {
              return "unity_Adder";
          }

          string GetSumOfAllInputs()
          {
              string sumString = "";
              int inputsLeft = GetInputSlots<ISlot>().Count();

              foreach (ISlot slot in GetInputSlots<ISlot>())
              {
                  sumString += GetShaderOutputName(slot.id);
                  if (inputsLeft > 1)
                      sumString += " + ";
                  --inputsLeft;
              }

              return sumString;
          }

          public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
          {
              var outputString = new ShaderGenerator();
              outputString.AddShaderChunk(GetFunctionPrototype(), false);
              outputString.AddShaderChunk("{", false);
              outputString.Indent();
              outputString.AddShaderChunk("finalSum = " + GetSumOfAllInputs() + ";", false);
              outputString.Deindent();
              outputString.AddShaderChunk("}", false);

              visitor.AddShaderChunk(outputString.GetShaderString(0), true);
          }
      }*/
}
