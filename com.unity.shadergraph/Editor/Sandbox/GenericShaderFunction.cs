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
    internal GenericShaderFunction(string name, List<Parameter> parameters, string body, List<JsonData<ShaderFunctionSignature>> functions, List<string> includePaths, List<SandboxValueType> genericTypeParameters)
        : base(name, parameters, body, functions, includePaths)
    {
        this.genericTypeParameters = genericTypeParameters;
    }

    public ShaderFunction SpecializeType(SandboxValueType genericTypeParameter, SandboxValueType specializedType)
    {
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

        // TODO: should also replace the generic type parameter in any shader function signatures..

        // TODO: copy over functions, includePaths, remaining generic type parameters

        return builder.Build();
    }

    // "new" here means hide the inherited ShaderFunction.Builder, and replace it with this declaration
    public new class Builder : ShaderFunction.Builder
    {
        List<SandboxValueType> genericTypeParameters;
        // TODO: generic function parameters..  ;)

        public Builder(string name) : base(name)
        {
        }

        public SandboxValueType AddGenericTypeParameter(string name)
        {
            // create a local placeholder type with the given name
            var type = new SandboxValueType(name, SandboxValueType.Flags.Placeholder);
            return AddGenericTypeParameter(type);
        }

        public SandboxValueType AddGenericTypeParameter(SandboxValueType placeholderType)
        {
            if (!placeholderType.IsPlaceholder)
                return null;        // TODO: error?  can only use placeholder types as generic type parameters

            if (genericTypeParameters == null)
                genericTypeParameters = new List<SandboxValueType>();
            genericTypeParameters.Add(placeholderType);

            return placeholderType;
        }

        public new GenericShaderFunction Build()
        {
            var func = new GenericShaderFunction(name, parameters, body.ConvertToString(), functions, includePaths, genericTypeParameters);

            // clear data so we can't accidentally re-use it
            this.name = null;
            this.parameters = null;
            this.body = null;
            this.functions = null;
            this.includePaths = null;
            this.genericTypeParameters = null;

            return func;
        }
    }
}
