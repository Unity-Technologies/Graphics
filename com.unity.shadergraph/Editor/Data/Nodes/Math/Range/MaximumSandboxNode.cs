using System;
using System.Reflection;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Range", "MaximumSandbox")]
    class MaximumSandboxNode : SandboxNode<MaximumNodeDefinition>
    {
    }

    class MaximumNodeDefinition : JsonObject, ISandboxNodeDefinition
    {
        public static SandboxValueType DetermineDynamicVectorType(ISandboxNodeBuildContext context, ShaderFunction dynamicShaderFunc)
        {
            int vectorCount = 1;
            foreach (var p in shaderFunc.Parameters)
            {
                if (p.Type == Types._dynamicVector)
                {
                    var pType = context.GetInputType(p.Name);
                    if (pType != null)
                        vectorCount = Math.Max(vectorCount, pType.VectorSize);
                }
            }
            return Types.Precision(vectorCount);
        }

        public void BuildRuntime(ISandboxNodeBuildContext context)
        {
            context.SetName("MaximumSandbox");

            // cached generic function
            if (shaderFunc == null)
                shaderFunc = BuildFunction();

            // TODO: move this to a utility function
            var vectorType = DetermineDynamicVectorType(context, shaderFunc);
            var specializedFunc = shaderFunc.SpecializeType(Types._dynamicVector, vectorType);

            context.SetMainFunction(specializedFunc, declareStaticPins: true);
            context.SetPreviewFunction(specializedFunc);
        }

        // statically cached function definition
        static GenericShaderFunction shaderFunc = null;
        static GenericShaderFunction BuildFunction()
        {
            var func = new ShaderFunction.Builder("Unity_Maximum");
            var dynamicVectorType = func.AddGenericTypeParameter(Types._dynamicVector);
            func.AddInput(Types._dynamicVector, "A");       // TODO: could call AddGenericTypeParameter automatically for any input or output placeholder type...
            func.AddInput(Types._dynamicVector, "B");
            func.AddOutput(Types._dynamicVector, "Out");
            func.AddLine("Out = max(A, B);");
            return func.BuildGeneric();
        }
    }
}
