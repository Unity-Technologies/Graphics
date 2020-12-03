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
            set { _maskInput = value;  }

        }

        public bool ValidateMaskInput(int InputValueSize)
        {
            //mask(xyzw) 0< length <=4
            //mask cant be character other than "xyzw"
            //wont genetate shader code and give errors
            bool MaskInoutIsValid = true;
            char[] MaskChars = _maskInput.ToCharArray();
            char[] AllChars  = {'x', 'y' ,'z','w'};
            List<char> CurrentChars = new List<char>();
            for (int i = 0; i < InputValueSize; i++)
            {
                CurrentChars.Add(AllChars[i]);
            }

            foreach ( char c in MaskChars)
            {
                if (!CurrentChars.Contains(c))
                {
                    MaskInoutIsValid = false;
                }
            }
            if (MaskChars.Length == 0)
            {
                MaskInoutIsValid = false;
            }

            return MaskInoutIsValid;

        }

        //TODO:
        //1.Get input mask from textControl
        //2.Validate input mask, if not legit set back to xyzw
        //3.Check on length of input mask which decides output vector size
        //4.Do swizzle according to the validated mask
        //5.Output
        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector4.zero));
            //TODO: Not sure if the output size on UI will auto update
            AddSlot(new DynamicVectorMaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        //public override void ValidateNode()
        //{
        //    base.ValidateNode();

        //    if (!AreIndicesValid)
        //    {
        //        owner.AddValidationError(objectId, "Invalid index!", ShaderCompilerMessageSeverity.Error);
        //    }
        //}


        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            //Get origin vector input
            //Get mask
            //Swizzle: Remapping the vector according to mask's order

            Debug.Log(_maskInput);
            //_maskInput = 
            //ValidateChannelCount();
            var outputSlotType = FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString();
            var outputName = GetVariableNameForSlot(OutputSlotId);
            var inputValue = GetSlotValue(InputSlotId, generationMode);
            var inputValueType = FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType;

            var InputValueSize = SlotValueHelper.GetChannelCount(inputValueType);
            //TODO: edge case: input vect3, output xxxx.(Need to check valid mask input)
            if (!ValidateMaskInput(InputValueSize))
            {
                owner.AddValidationError(objectId, "Invalid mask!", ShaderCompilerMessageSeverity.Error);
                sb.AppendLine(string.Format("{0} {1} = float4 (0, 0, 0, 0);", outputSlotType, outputName));
            }
            else {
                if (_maskInput.Length == 4)
                {
                    sb.AppendLine("{0} {1} = {2}.{3};",
                        outputSlotType,
                        outputName,
                        inputValue, _maskInput);
                }
                else if (_maskInput.Length == 3)
                {
                    sb.AppendLine("{0} {1} = float4 ({2}.{3}, {2}.{4}, {2}.{5}, 0);",
                    outputSlotType,
                    outputName,
                    inputValue, _maskInput[0], _maskInput[1], _maskInput[2]);
                }
                else if (_maskInput.Length == 2)
                {
                    sb.AppendLine("{0} {1} = float4 ({2}.{3}, {2}.{4}, 0, 0);",
                    outputSlotType,
                    outputName,
                    inputValue, _maskInput[0], _maskInput[1]);
                }
                else if (_maskInput.Length == 1)
                {
                    sb.AppendLine("{0} {1} = float4 ({2}.{3}, 0, 0, 0);",
                    outputSlotType,
                    outputName,
                    inputValue, _maskInput[0]);
                }
            }





            //if (inputValueType == ConcreteSlotValueType.Vector1)
            //    sb.AppendLine(string.Format("{0} {1} = {2};", outputSlotType, outputName, inputValue));
            //else if (generationMode == GenerationMode.ForReals)
            //    sb.AppendLine("{0} {1} = {2}.{3}{4}{5}{6};",
            //        outputSlotType,
            //        outputName,
            //        inputValue,
            //        s_ComponentList[m_RedChannel].ToString(CultureInfo.InvariantCulture),
            //        s_ComponentList[m_GreenChannel].ToString(CultureInfo.InvariantCulture),
            //        s_ComponentList[m_BlueChannel].ToString(CultureInfo.InvariantCulture),
            //        s_ComponentList[m_AlphaChannel].ToString(CultureInfo.InvariantCulture));
            //else
            //    sb.AppendLine("{0} {1} = {0}({3}[((int){2} >> 0) & 3], {3}[((int){2} >> 2) & 3], {3}[((int){2} >> 4) & 3], {3}[((int){2} >> 6) & 3]);", outputSlotType, outputName, GetVariableNameForNode(), inputValue);
        }

        public override void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode)
        {
            base.CollectShaderProperties(properties, generationMode);
            if (generationMode != GenerationMode.Preview)
                return;
            properties.AddShaderProperty(new Vector1ShaderProperty
            {
                overrideReferenceName = GetVariableNameForNode(),
                generatePropertyBlock = false
            });
        }

        public override void CollectPreviewMaterialProperties(List<PreviewProperty> properties)
        {
            base.CollectPreviewMaterialProperties(properties);
            // Encode swizzle values into an integer
            //var value = ((int)redChannel) | ((int)greenChannel << 2) | ((int)blueChannel << 4) | ((int)alphaChannel << 6);
            //properties.Add(new PreviewProperty(PropertyType.Float)
            //{
            //    name = GetVariableNameForNode(),
            //    floatValue = value
            //});
        }
    }
}
