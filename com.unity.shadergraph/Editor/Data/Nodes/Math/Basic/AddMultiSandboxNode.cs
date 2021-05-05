using System.Collections.Generic;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Add Multi Sandbox")]
    class AddMultiSandboxNode : SandboxNode<AddMultiNodeDefinition>
    {
    }

    class AddMultiNodeDefinition : JsonObject, ISandboxNodeDefinition
    {
        public void BuildRuntime(ISandboxNodeBuildContext context)
        {
            context.SetName("Add Multi Sandbox");

            // check what the max connection is
            int maxSlot = 0;
            for (int index = 0; index < 26; index++)
            {
                var slotName = GetSlotName(index);
                if (context.GetInputConnected(slotName.ToString()))
                    maxSlot = index;
            }
            maxSlot = maxSlot + 1;

            // always generate at least the binary version of this function
            var shaderFunc = BuildFunction(maxSlot < 2 ? 2 : maxSlot);

            var vectorType = SandboxNodeUtils.DetermineDynamicVectorType(context, shaderFunc);
            var specializedFunc = shaderFunc.SpecializeType(Types._dynamicVector, vectorType);

            context.SetMainFunction(specializedFunc);
            context.SetPreviewFunction(specializedFunc);

            // Add the prompt slot for the user to connect another input, if they want
            if ((maxSlot >= 2) && (maxSlot < 26))
                context.AddInputSlot(vectorType, GetSlotName(maxSlot));
        }

        // static slot name cache
        static List<string> k_slotNames = new List<string>();
        static string GetSlotName(int slotIndex)
        {
            // grow if necessary
            if (k_slotNames.Count <= slotIndex)
                k_slotNames.Insert(slotIndex, null);

            // lookup in cache, populate if necessary
            string result = k_slotNames[slotIndex];
            if (result == null)
                k_slotNames[slotIndex] = result = ((char)('A' + (slotIndex % 26))).ToString();

            return result;
        }

        static GenericShaderFunction BuildFunction(int connectionCount)
        {
            var func = new ShaderFunction.Builder("Unity_Add" + connectionCount);
            var dynamicVectorType = func.AddGenericTypeParameter(Types._dynamicVector);
            func.AddOutput(Types._dynamicVector, "Out");
            using (var line = func.LineScope())
            {
                line.Add("Out = ");
                for (int input = 0; input < connectionCount; input++)
                {
                    var slotName = GetSlotName(input);
                    if (input > 0)
                        line.Add(" + ");
                    func.AddInput(Types._dynamicVector, slotName);
                    line.Add(slotName);
                }
                line.Add(";");
            }
            return func.BuildGeneric();
        }
    }
}
