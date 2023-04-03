using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
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
            synonyms = new string[] { "swap", "reorder", "component mask" };
            UpdateNodeAfterDeserialization();
        }

        const int InputSlotId = 0;
        const int OutputSlotId = 1;
        const string kInputSlotName = "In";
        const string kOutputSlotName = "Out";

        public override bool hasPreview
        {
            get { return true; }
        }

        [SerializeField]
        string _maskInput = "xxxx";

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
                Dirty(ModificationScope.Topological);
            }
        }

        public string convertedMask;

        public bool ValidateMaskInput(int InputValueSize)
        {
            convertedMask = _maskInput.ToLower();

            Dictionary<char, char> mask_map = new Dictionary<char, char>
            {
                {'r', 'x' },
                {'g', 'y' },
                {'b', 'z' },
                {'a', 'w' },
            };
            bool MaskInputIsValid = true;
            char[] MaskChars = convertedMask.ToCharArray();
            char[] AllChars = { 'x', 'y', 'z', 'w', 'r', 'g', 'b', 'a' };
            List<char> CurrentChars = new List<char>();
            for (int i = 0; i < InputValueSize; i++)
            {
                CurrentChars.Add(AllChars[i]);
                CurrentChars.Add(AllChars[i + 4]);
            }

            foreach (char c in MaskChars)
            {
                if (!CurrentChars.Contains(c))
                {
                    MaskInputIsValid = false;
                }
            }
            if (MaskChars.Length <= 0 || MaskChars.Length > 4)
            {
                MaskInputIsValid = false;
            }
            //Convert "rgba" input to "xyzw" to avoid mismathcing
            if (MaskInputIsValid)
            {
                char[] rgba = { 'r', 'g', 'b', 'a' };

                for (int i = 0; i < MaskChars.Length; i++)
                {
                    if (rgba.Contains(MaskChars[i]))
                    {
                        MaskChars[i] = mask_map[MaskChars[i]];
                    }
                }
                convertedMask = new string(MaskChars);
            }
            return MaskInputIsValid;
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            if (_maskInput == null)
                _maskInput = "xxxx";            
            AddSlot(new DynamicVectorMaterialSlot(InputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector4.zero));
            switch (_maskInput.Length)
            {
                case 1:
                    AddSlot(new Vector1MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0));
                    break;
                case 2:
                    AddSlot(new Vector2MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector2.zero));
                    break;
                case 3:
                    AddSlot(new Vector3MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector3.zero));
                    break;
                default:
                    AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
                    break;
            }
            RemoveSlotsNameNotMatching(new[] { InputSlotId, OutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var outputSlotType = FindOutputSlot<MaterialSlot>(OutputSlotId).concreteValueType.ToShaderString();
            var outputName = GetVariableNameForSlot(OutputSlotId);
            var inputValue = GetSlotValue(InputSlotId, generationMode);
            var inputValueType = FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType;
            var InputValueSize = SlotValueHelper.GetChannelCount(inputValueType);

            if (!ValidateMaskInput(InputValueSize))
            {
                sb.AppendLine(string.Format("{0} {1} = 0;", outputSlotType, outputName));
            }
            else if(!FindInputSlot<MaterialSlot>(InputSlotId).isConnected)
            {
                // cannot swizzle off of a float literal, so if there is no upstream connection, it means we have defaulted to a float,
                // and the node's initial base case will generate invalid code.
                sb.AppendLine("{0} {1} = $precision({2}).{3};", outputSlotType, outputName, inputValue, convertedMask);
            }            
            else
            {
                sb.AppendLine("{0} {1} = {2}.{3};", outputSlotType, outputName, inputValue, convertedMask);
            }
        }

        public override void ValidateNode()
        {
            base.ValidateNode();

            var inputValueType = FindInputSlot<MaterialSlot>(InputSlotId).concreteValueType;
            var InputValueSize = SlotValueHelper.GetChannelCount(inputValueType);
            if (!ValidateMaskInput(InputValueSize))
            {
                owner.AddValidationError(objectId, "Invalid mask for a Vector" + InputValueSize + " input.", ShaderCompilerMessageSeverity.Error);
            }
        }

        public override int latestVersion => 1;

        public override void OnAfterMultiDeserialize(string json)
        {
            //collect texturechannel properties
            //get the value
            //pass it to maskInput
            if (sgVersion < 1)
            {
                LegacySwizzleChannelData.LegacySwizzleChannel(json, this);
                ChangeVersion(1);
                UpdateNodeAfterDeserialization();
            }
        }

        public override IEnumerable<int> allowedNodeVersions => new List<int> { 1 };

        class LegacySwizzleChannelData
        {
            //collect texturechannel properties
            [SerializeField]
            public TextureChannel m_RedChannel;
            [SerializeField]
            public TextureChannel m_GreenChannel;
            [SerializeField]
            public TextureChannel m_BlueChannel;
            [SerializeField]
            public TextureChannel m_AlphaChannel;


            public static void LegacySwizzleChannel(string json, SwizzleNode node)
            {
                Dictionary<TextureChannel, string> s_ComponentList = new Dictionary<TextureChannel, string>
                {
                    {TextureChannel.Red, "r" },
                    {TextureChannel.Green, "g" },
                    {TextureChannel.Blue, "b" },
                    {TextureChannel.Alpha, "a" },
                };
                var legacySwizzleChannelData = new LegacySwizzleChannelData();
                JsonUtility.FromJsonOverwrite(json, legacySwizzleChannelData);
                node._maskInput = s_ComponentList[legacySwizzleChannelData.m_RedChannel] + s_ComponentList[legacySwizzleChannelData.m_GreenChannel] + s_ComponentList[legacySwizzleChannelData.m_BlueChannel] + s_ComponentList[legacySwizzleChannelData.m_AlphaChannel];
            }
        }
    }
}
