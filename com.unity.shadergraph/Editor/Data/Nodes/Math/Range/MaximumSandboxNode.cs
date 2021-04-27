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

        // TODO: make a more general treatment of "generic" functions and specialization...
        internal static ShaderFunction SpecializeDynamicVectorFunction(ShaderFunction shaderFunc, SandboxValueType vectorType)
        {
            var specializedName = shaderFunc.Name.Replace(Types._dynamicVector.Name, vectorType.Name);
            var builder = new ShaderFunction.Builder(specializedName);

            // copy parameters, replacing types
            for (int pIndex = 0; pIndex < shaderFunc.Parameters.Count; pIndex++)
            {
                var p = shaderFunc.Parameters[pIndex];
                if (p.Type == Types._dynamicVector)
                    p = p.ReplaceType(vectorType);
                builder.AddParameter(p);
            }

            var newBody = shaderFunc.Body.Replace(Types._dynamicVector.Name, vectorType.Name);
            builder.AddLine(newBody);

            // TODO: functions, includePaths

            return builder.Build();
        }

        public void BuildRuntime(ISandboxNodeBuildContext context)
        {
            context.SetName("MaximumSandbox");

            // cached generic function
            if (shaderFunc == null)
                shaderFunc = BuildFunction();

            var vectorType = DetermineDynamicVectorType(context, shaderFunc);

            // TODO: cache the specialization?
            var specializedFunc = SpecializeDynamicVectorFunction(shaderFunc, vectorType);

            context.SetMainFunction(specializedFunc, declareStaticPins: true);
            context.SetPreviewFunction(specializedFunc);
        }

        // statically cached function definition
        static ShaderFunction shaderFunc = null;
        static ShaderFunction BuildFunction()
        {
            // TODO: this should be GenericShaderFunction.Builder...
            var func = new ShaderFunction.Builder("Unity_Maximum_$dynamicVector");
            func.AddInput(Types._dynamicVector, "A");
            func.AddInput(Types._dynamicVector, "B");
            func.AddOutput(Types._dynamicVector, "Out");
            func.AddLine("Out = max(A, B);");

            return func.Build();
        }
    }
}
