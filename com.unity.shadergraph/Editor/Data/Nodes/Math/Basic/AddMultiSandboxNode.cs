using System;
using System.Reflection;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

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

            // TODO: move this to a utility function
            var vectorType = SandboxNodeUtils.DetermineDynamicVectorType(context, shaderFunc);
            var specializedFunc = shaderFunc.SpecializeType(Types._dynamicVector, vectorType);

            context.SetMainFunction(specializedFunc, declareSlots: true);
            context.SetPreviewFunction(specializedFunc);

            if ((maxSlot >= 2) && (maxSlot < 26))
                context.AddInputSlot(vectorType, GetSlotName(maxSlot).ToString());
        }

        static char GetSlotName(int slotIndex)
        {
            char d = (char)('A' + (slotIndex % 26));
            return d;
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
                    var slotName = GetSlotName(input).ToString();
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


    /*
        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Add", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Add(
            [Slot(0, Binding.None)] DynamicDimensionVector A,
            [Slot(1, Binding.None)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = A + B;
}
";
        }
    }
    */
}
