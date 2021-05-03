using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;

public static class SandboxNodeUtils
{
    public static SandboxValueType DetermineDynamicVectorType(ISandboxNodeBuildContext context, ShaderFunction dynamicShaderFunc)
    {
        int vectorCount = 1;
        foreach (var p in dynamicShaderFunc.Parameters)
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

    internal static void ProvideFunctionToRegistry(ShaderFunction function, FunctionRegistry registry)
    {
        // hmm...  currently provide function doesn't ensure any dependency ordering between functions
        // it's just relying on them being provided in dependency order.  So we must provide dependencies first:
        if (function.FunctionsCalled != null)
        {
            foreach (var subsig in function.FunctionsCalled)
            {
                // some function calls can just be a signature, no declaration provided.
                // but if there is a declaration, provide it!
                if (subsig is ShaderFunction subfunction)
                {
                    ProvideFunctionToRegistry(subfunction, registry);
                }
            }
        }

        // then provide the main function last
        registry.ProvideFunction(function.Name, sb =>
        {
            function.AppendHLSLDeclarationString(sb);
        });
    }
};
