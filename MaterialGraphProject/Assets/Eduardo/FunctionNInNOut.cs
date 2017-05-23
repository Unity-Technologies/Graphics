using UnityEngine.Graphing;
using System.Linq;
using System.Collections;
using System;

namespace UnityEngine.MaterialGraph
{
    public abstract class FunctionNInNOut : AbstractMaterialNode, IGeneratesBodyCode
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

        public int AddSlot(string displayName, string nameInShader, SlotType slotType, SlotValueType valueType)
        {
            int nextSlotId;
            if (slotType == SlotType.Output)
                nextSlotId = -( GetOutputSlots<MaterialSlot>().Count() + 1 );
            else
                nextSlotId = GetInputSlots<MaterialSlot>().Count() + 1;

            AddSlot(new MaterialSlot(nextSlotId, displayName, nameInShader, slotType, valueType, Vector4.zero, true));
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

                param += precision + GetSlotTypeName(inSlot.id) + " ";
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

        private string GetFunctionCall(GenerationMode generationMode)
        {
            return GetFunctionName() +
               " (" +
               GetFunctionCallParameters(generationMode) +
               ");";
        }

        private string GetOutputDeclaration()
        {
            string outDeclaration = "";
            foreach (ISlot outSlot in GetOutputSlots<ISlot>())
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
    }
}
