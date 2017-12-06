using System;
using UnityEditor.Graphing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace UnityEditor.ShaderGraph
{
/*    [Title("Logic", "CustomCode")]
    public class CustomCodeNode : FunctionNInNOut, IGeneratesFunction
    {
        [SerializeField] private string m_code;

        public class FunctionArgument
        {
            public string TypeName;
            public string Name;

            public FunctionArgument(string typeName, string name)
            {
                this.TypeName = typeName;
                this.Name = name;
            }
        }

        List<int> inputSlotIds = new List<int>();
        List<int> outputSlotIds = new List<int>();

        public string Code
        {
            get { return m_code; }
            set
            {
                if (string.Equals(m_code, value))
                    return;

                m_code = value;
            }
        }

        public CustomCodeNode()
        {
            name = "CustomCode";
        }

        protected override string GetFunctionName()
        {
            string guid = System.Guid.NewGuid().ToString();
            guid = guid.Replace("-", "_");
            return "unity_CustomCode_" + GetFunctionName(GetFunctionSignature()); // + "_" + guid;
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            var outputString = new ShaderGenerator();
            outputString.AddShaderChunk(GetFunctionPrototype(), false);

            outputString.AddShaderChunk(GetFunctionBody(), false);

            visitor.AddShaderChunk(outputString.GetShaderString(0), true);
        }

        public void UpdateInputAndOuputSlots()
        {
            if (string.IsNullOrEmpty(m_code))
            {
                return;
            }

            onModified(this, ModificationScope.Graph);

            string functionSignature = GetFunctionSignature();
            List<FunctionArgument> inputArguments = GetInputArguments(functionSignature);
            List<FunctionArgument> outputArguments = GetOutputArguments(functionSignature);
            UpdateInputSlots(inputArguments);
            UpdateOutputSlots(outputArguments);

        }

        void UpdateInputSlots(List<FunctionArgument> inputArguments)
        {
            if (inputArguments == null || inputArguments.Count <= 0)
            {
                return;
            }

            int slotIndex = 0;
            if (inputSlotIds.Count > 0)
            {
                for (slotIndex = 0; slotIndex < inputSlotIds.Count; ++slotIndex)
                {
                    RemoveSlot(inputSlotIds[slotIndex]);
                }
                inputSlotIds.Clear();
            }

            List<int> slotIds = new List<int>();
            foreach (ISlot inSlot in GetSlots<ISlot>())
            {
                slotIds.Add(inSlot.id);
            }

            // TODO: This is a bit of a hack.  We need to find a better way
            // to handle the copying of this node.
            for (slotIndex = 0; slotIndex < slotIds.Count; ++slotIndex)
            {
                RemoveSlot(slotIds[slotIndex]);
            }

            for (slotIndex = 0; slotIndex < inputArguments.Count; ++slotIndex)
            {
                string nativeType = inputArguments[slotIndex].TypeName;
                SlotValueType valueType = SlotValueType.Vector1;
                if (string.Equals(nativeType, "float2"))
                {
                    valueType = SlotValueType.Vector2;
                }
                else if (string.Equals(nativeType, "float3"))
                {
                    valueType = SlotValueType.Vector3;
                }
                else if (string.Equals(nativeType, "float4"))
                {
                    valueType = SlotValueType.Vector4;
                }

                inputSlotIds.Add(AddSlot(inputArguments[slotIndex].Name, inputArguments[slotIndex].Name, SlotType.Input, valueType, Vector4.zero));
            }
        }

        void UpdateOutputSlots(List<FunctionArgument> outputArguments)
        {
            if (outputArguments == null || outputArguments.Count <= 0)
            {
                return;
            }

            int slotIndex = 0;
            if (outputSlotIds.Count > 0)
            {
                for (slotIndex = 0; slotIndex < outputSlotIds.Count; ++slotIndex)
                {
                    RemoveSlot(outputSlotIds[slotIndex]);
                }
                outputSlotIds.Clear();
            }

            for (slotIndex = 0; slotIndex < outputArguments.Count; ++slotIndex)
            {
                string nativeType = outputArguments[slotIndex].TypeName;
                SlotValueType valueType = SlotValueType.Vector1;
                if (string.Equals(nativeType, "float2"))
                {
                    valueType = SlotValueType.Vector2;
                }
                else if (string.Equals(nativeType, "float3"))
                {
                    valueType = SlotValueType.Vector3;
                }
                else if (string.Equals(nativeType, "float4"))
                {
                    valueType = SlotValueType.Vector4;
                }

                inputSlotIds.Add(AddSlot(outputArguments[slotIndex].Name, outputArguments[slotIndex].Name, SlotType.Output, valueType, Vector4.zero));
            }
        }

        string GetFunctionWithoutComments()
        {
            List<string> codeWithoutComments = new List<string>();
            string[] lines = m_code.Split(new string[] {System.Environment.NewLine}, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; ++i)
            {
                string line = lines[i].Trim();
                if (!line.StartsWith("//"))
                {
                    codeWithoutComments.Add(lines[i]);
                    continue;
                }
            }

            return string.Join(System.Environment.NewLine, codeWithoutComments.ToArray());
        }

        string GetFunctionSignature()
        {
            // Find First { character so we can get the entire function signature;
            string codeWithoutComments = GetFunctionWithoutComments();
            int braceIndex = codeWithoutComments.IndexOf('{');
            string functionSignature = codeWithoutComments.Substring(0, braceIndex);
            functionSignature = functionSignature.Trim();

            functionSignature = Regex.Replace(functionSignature, @"\s+", " ");

            return functionSignature;
        }

        string GetFunctionBody()
        {
            // Find First { character so we can get the entire function signature;
            string codeWithoutComments = GetFunctionWithoutComments();
            int firstBraceIndex = codeWithoutComments.IndexOf('{');
            int lastBraceIndex = codeWithoutComments.LastIndexOf('}')+1;
            string functionBody = codeWithoutComments.Substring(firstBraceIndex, lastBraceIndex - firstBraceIndex);

            return functionBody;
        }

        string GetFunctionName(string functionSignature)
        {
            string codeWithoutComments = GetFunctionWithoutComments();
            int firstSpace = functionSignature.IndexOf(' ') + 1;
            int firstParenthesee = codeWithoutComments.IndexOf('(');

            string functionName = codeWithoutComments.Substring(firstSpace, firstParenthesee - firstSpace);
            return functionName;
        }

        List<FunctionArgument> GetInputArguments(string functionSignature)
        {
            string codeWithoutComments = GetFunctionWithoutComments();
            int firstCharacter = codeWithoutComments.IndexOf('(') + 1;
            int lastParenthesee = codeWithoutComments.IndexOf(')');

            string argumentString = codeWithoutComments.Substring(firstCharacter, lastParenthesee - firstCharacter);
            string[] arguments = argumentString.Split(new char[] {','});
            if (arguments == null || arguments.Length <= 0)
            {
                return null;
            }

            List<FunctionArgument> inputArguments = new List<FunctionArgument>();
            foreach (string arg in arguments)
            {
                string trimmedArg = arg.Trim();
                string[] components = trimmedArg.Split(new char[] {' '});
                if (string.Equals(components[0].ToLower(), "out"))
                {
                    break;
                }

                inputArguments.Add(new FunctionArgument(components[0], components[1]));
            }

            return inputArguments;
        }

        List<FunctionArgument> GetOutputArguments(string functionSignature)
        {
            string codeWithoutComments = GetFunctionWithoutComments();
            int firstCharacter = codeWithoutComments.IndexOf('(') + 1;
            int lastParenthesee = codeWithoutComments.IndexOf(')');

            string argumentString = codeWithoutComments.Substring(firstCharacter, lastParenthesee - firstCharacter);
            string[] arguments = argumentString.Split(new char[] { ',' });
            if (arguments == null || arguments.Length <= 0)
            {
                return null;
            }

            List<FunctionArgument> outputArguments = new List<FunctionArgument>();
            foreach (string arg in arguments)
            {
                string trimmedArg = arg.Trim();
                string[] components = trimmedArg.Split(new char[] { ' ' });
                // Skip non output nodes
                if (!string.Equals(components[0].ToLower(), "out"))
                {
                    continue;
                }

                outputArguments.Add(new FunctionArgument(components[1], components[2]));
            }

            return outputArguments;
        }
    }*/
}
