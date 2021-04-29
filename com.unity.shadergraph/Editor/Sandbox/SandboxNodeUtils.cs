using System;
using System.Collections;
using System.Collections.Generic;


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
};
