using System.Collections.Generic;
using UnityEditor.ShaderFoundry;
using BlockProperty = UnityEditor.ShaderFoundry.BlockVariable;

namespace UnityEditor.ShaderFoundry
{
    internal static class ShaderFunctionExtensions
    {
        // Get the input and output types for this function (assumed to be an entry point)
        internal static bool GetInOutTypes(this ShaderFunction function, out ShaderType inputType, out ShaderType outputType)
        {
            if (function.IsValid)
            {
                var parameters = function.Parameters.GetEnumerator();
                if (parameters.MoveNext())
                {
                    inputType = parameters.Current.Type;
                    outputType = function.ReturnType;
                    return true;
                }
            }
            inputType = outputType = ShaderType.Invalid;
            return false;
        }
    }

    static class VariableLinkInstanceExtensions
    {
        internal static void Declare(this VariableLinkInstance varInstance, ShaderBuilder builder)
        {
            if (varInstance.Parent != null)
            {
                const string dotToken = ".";
                Declare(varInstance.Parent, builder);
                builder.Add(dotToken);
            }
            builder.Add(varInstance.Name);
        }

        internal static string GetDeclarationString(this VariableLinkInstance varInstance)
        {
            ShaderBuilder builder = new ShaderBuilder();
            varInstance.Declare(builder);
            return builder.ToString();
        }
    }

    static class BlockExtensions
    {
        internal static IEnumerable<BlockVariable> Properties(this Block block)
        {
            foreach (var input in block.Inputs)
            {
                if (input.Attributes.FindFirst(CommonShaderAttributes.Property).IsValid)
                    yield return input;
            }
        }
    }

    static class BlockBuilderExtensions
    {
        internal static void MergeTypesAndFunctions(this Block.Builder builder, Block block)
        {
            // Make sure to visit referenced items before owned. Owned items may depend on referenced ones.
            foreach (var item in block.ReferencedTypes)
                builder.AddReferencedType(item);
            foreach (var item in block.Types)
                builder.AddReferencedType(item);
            foreach (var item in block.ReferencedFunctions)
                builder.AddReferencedFunction(item);
            foreach (var item in block.Functions)
                builder.AddReferencedFunction(item);
        }

        internal static void MergeDescriptors(this Block.Builder builder, Block block)
        {
            foreach (var item in block.Commands)
                builder.AddCommand(item);
            foreach (var item in block.Defines)
                builder.AddDefine(item);
            foreach (var item in block.Includes)
                builder.AddInclude(item);
            foreach (var item in block.Keywords)
                builder.AddKeyword(item);
            foreach (var item in block.Pragmas)
                builder.AddPragma(item);
        }

        internal static void MergeTypesFunctionsDescriptors(this Block.Builder builder, Block block)
        {
            builder.MergeTypesAndFunctions(block);
            builder.MergeDescriptors(block);
        }
    }

    internal static class SwizzleUtils
    {
        // Convert the string to 4 channels per element where each bit corresponds to the element
        internal static bool FromString(string swizzle, out int result)
        {
            result = 0;
            if (swizzle == null)
                return true;

            if (swizzle.Length > 4)
                return false;

            for (var i = 0; i < swizzle.Length; ++i)
            {
                var charValue = swizzle[i];
                int swizzleIndex = charValue - 'x';
                if (swizzleIndex < -1 || 3 <= swizzleIndex)
                    return false;

                if (swizzleIndex == -1)
                    swizzleIndex = 3;

                var elementMask = (1 << swizzleIndex);
                result |= elementMask << (i * 4);
            }
            return true;
        }

        internal static string ToString(int value)
        {
            if (value == 0)
                return null;

            string result = "";
            for (var i = 0; i < 4; ++i)
            {
                var mask = (value >> (i * 4)) & 0b1111;
                if (mask == 0)
                    break;
                else if (mask == 0b0001)
                    result += 'x';
                else if (mask == 0b0010)
                    result += 'y';
                else if (mask == 0b0100)
                    result += 'z';
                else if (mask == 0b1000)
                    result += 'w';
            }
            return result;
        }

        // Get the vector size of the swizzle
        internal static int GetCount(int value)
        {
            for (var i = 0; i < 4; ++i)
            {
                var mask = (value >> (i * 4)) & 0b1111;
                if (mask == 0)
                    return i;
            }
            return 4;
        }

        // Finds the largest size required to swizzle (e.g. vector.zx requires a size of 3)
        internal static int GetRequiredSize(int value)
        {
            int result = 0;
            for (var i = 0; i < 4; ++i)
            {
                var mask = (value >> (i * 4)) & 0b1111;
                if (mask == 0)
                    break;
                int elementSize = 0;
                if (mask == 0b0001)
                    elementSize = 1;
                else if (mask == 0b0010)
                    elementSize = 2;
                if (mask == 0b0100)
                    elementSize = 3;
                if (mask == 0b1000)
                    elementSize = 4;
                result = (elementSize > result) ? elementSize : result;
            }
            return result;
        }
    }
}
