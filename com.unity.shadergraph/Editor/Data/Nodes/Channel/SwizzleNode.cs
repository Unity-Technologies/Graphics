using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEditor.Rendering;

namespace UnityEditor.ShaderGraph
{
    [Title("Channel", "Swizzle")]
    class SwizzleNode : AbstractMaterialNode, IGeneratesBodyCode
    {
        public SwizzleNode()
        {
            name = "Swizzle";
            UpdateNodeAfterDeserialization();
        }

        const int InputSlotId = 0;
        const int OutputSlotId = 1;
        const string kInputSlotName = "In";
        const string kOutputSlotName = "Out";

        [SerializeField]
        string _maskInput = "xyzw";

        [TextControl("Mask:")]
        public string maskInput
        {
            get { return _maskInput; }
            set
            {
                if (_maskInput.Equals(value))
                    return;
                _maskInput = value;
                UpdateNodeAfterDeserialization();
                owner.ValidateGraph();
                Dirty(ModificationScope.Topological);
            }
        }

        //1.mask(xyzw) 0< length <=4
        //2.mask cant be character other than "xyzwrgba"
        //3.If the input is not valid, wont genetate shader code and give errors
        public bool ValidateMaskInput(int InputValueSize)
        {
            bool MaskInputIsValid = true;
            _maskInput = _maskInput.ToLower();
            char[] MaskChars = _maskInput.ToCharArray();
            char[] AllChars  = {'x', 'y' , 'z', 'w', 'r','g', 'b', 'a'};
            List<char> CurrentChars = new List<char>();
            for (int i = 0; i < InputValueSize; i++)
            {
                CurrentChars.Add(AllChars[i]);
                CurrentChars.Add(AllChars[i+4]);
            }

            foreach (char c in MaskChars)
            {
                if (!CurrentChars.Contains(c))
                {
                    MaskInputIsValid = false;
                }
            }
            if (MaskChars.Length == 0 || MaskChars.Length > 4)
            {
                MaskInputIsValid = false;
            }
            return MaskInputIsValid;
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector4.zero));
            switch (_maskInput.Length)
            {
                default:
                    AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
                    break;
                case 3:
                    AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
                    break;
                case 2:
                    AddSlot(new Vector2MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector2.zero));
                    break;
                case 1:
                    AddSlot(new Vector1MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0));
                    break;
            }
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        //1.Get vector input value
        //2.Get validated mask input
        //3.Swizzle: Remap the vector according to mask
        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var outputSlotType = FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString();
            var outputName = GetVariableNameForSlot(OutputSlotId);
            var inputValue = GetSlotValue(InputSlotId, generationMode);
            var inputValueType = FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType;
            var InputValueSize = SlotValueHelper.GetChannelCount(inputValueType);

            if (!ValidateMaskInput(InputValueSize))
            {
                owner.AddValidationError(objectId, "Invalid mask!", ShaderCompilerMessageSeverity.Error);
                sb.AppendLine(string.Format("{0} {1} = float4 (0, 0, 0, 0);", outputSlotType, outputName));
            }
            else
            {
                string outputValue = "";
                for (int i = 0; i < 4; i++)
                {
                    if (i != 0)
                        outputValue += ",";

                    if (i < _maskInput.Length)
                        outputValue += inputValue + "." + _maskInput[i];

                    if (i >= _maskInput.Length)
                        outputValue += "0";
                }
                sb.AppendLine("{0} {1} = float4 ({2});", outputSlotType, outputName, outputValue);
            }
        }
    }
}
