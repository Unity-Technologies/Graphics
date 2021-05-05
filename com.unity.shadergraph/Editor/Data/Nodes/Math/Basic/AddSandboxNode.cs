using System;
using System.Reflection;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "AddSandbox")]
    class AddSandboxNode : SandboxNode<AddNodeDefinition>
    {
    }

    class AddNodeDefinition : JsonObject, ISandboxNodeDefinition
    {
        public void BuildRuntime(ISandboxNodeBuildContext context)
        {
            context.SetName("AddSandbox");

            // cached generic function
            if (shaderFunc == null)
                shaderFunc = BuildFunction();

            var vectorType = SandboxNodeUtils.DetermineDynamicVectorType(context, shaderFunc);
            var specializedFunc = shaderFunc.SpecializeType(Types._dynamicVector, vectorType);

            context.SetMainFunction(specializedFunc);
            context.SetPreviewFunction(specializedFunc);
        }

        // statically cached function definition
        static GenericShaderFunction shaderFunc = null;
        static GenericShaderFunction BuildFunction()
        {
            var func = new ShaderFunction.Builder("Unity_AddSB");
            var dynamicVectorType = func.AddGenericTypeParameter(Types._dynamicVector);
            func.AddInput(Types._dynamicVector, "A");       // TODO: could call AddGenericTypeParameter automatically for any input or output placeholder type...
            func.AddInput(Types._dynamicVector, "B");
            func.AddOutput(Types._dynamicVector, "Out");
            func.AddLine("Out = A + B;");
            return func.BuildGeneric();
        }
    }
}
