using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;


[Serializable]
public class GenericShaderFunction : ShaderFunction
{
    public override bool isGeneric => true;

    List<SandboxValueType> genericTypeParameters;

    // constructor is internal, public must use the Builder class instead
    internal GenericShaderFunction(string name, List<Parameter> parameters, string body, List<JsonData<ShaderFunctionSignature>> functionsCalled, List<string> includePaths, List<SandboxValueType> genericTypeParameters)
        : base(name, parameters, body, functionsCalled, includePaths)
    {
        this.genericTypeParameters = genericTypeParameters;
    }

    public ShaderFunction SpecializeType(SandboxValueType genericTypeParameter, SandboxValueType specializedType)
    {
        // TODO: we could have a specialization cache to avoid re-specializing over and over...

        var specializedName = Name + "_" + specializedType.Name;

        // TODO: should figure out if the function is still generic or not...  maybe we should combine the function builders into one that can do either?
        var builder = new ShaderFunction.Builder(specializedName);

        // copy parameters, replacing types
        for (int pIndex = 0; pIndex < Parameters.Count; pIndex++)
        {
            var p = Parameters[pIndex];
            if (p.Type == genericTypeParameter)
                p = p.ReplaceType(specializedType);
            builder.AddParameter(p);
        }

        // TODO: this replacement needs to be a bit smarter to avoid falsely replacing only part of an identifier...
        // (unless we ensure the generic names are always uniquely identified, like $name$)
        var newBody = Body.Replace(genericTypeParameter.Name, specializedType.Name);
        builder.AddLine(newBody);

        // TODO: should also replace the generic type parameter in any shader function signatures
        // and maybe any dependent generic functions as well?  :D

        // TODO: copy over functions, includePaths, remaining generic type parameters

        return builder.Build();
    }
}
