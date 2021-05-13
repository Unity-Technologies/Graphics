using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;


[Serializable]
public class ShaderFunctionSignature : JsonObject
{
    public override int latestVersion => 1;

    // public API
    public string Name => name;

    public IReadOnlyList<Parameter> Parameters => parameters?.AsReadOnly() ?? ListUtils.EmptyReadOnlyList<Parameter>();

    [SerializeField]
    protected string name;

    // TODO: can we store ReadOnlyCollection<> instead of List<> ?   is it serializable?
    [SerializeField]
    protected List<Parameter> parameters;

    [Serializable]
    public struct Parameter
    {
        public string Name => name;
        public SandboxType Type => type;
        public bool IsInput => input;
        public bool IsOutput => output;
        public bool IsInOut => (input && output);       // TODO: doesn't work in sandbox node
        public System.Object DefaultValue => defaultValue;


        [SerializeField]
        string name;

        [SerializeField]
        SandboxType type;

        // TODO: make this an enum
        [SerializeField]
        bool input;
        [SerializeField]
        bool output;

        //[SerializeField]
        //JsonData<JsonObject> defaultValue; ?
        System.Object defaultValue;     // not serialized (yet??)

        internal Parameter(SandboxType type, string name, bool input, bool output, System.Object defaultValue = null)
        {
            this.type = type;
            this.name = name;
            this.input = input;
            this.output = output;
            this.defaultValue = defaultValue;
        }

        public bool Equals(Parameter p)
        {
            return
                Type == p.Type &&
                name == p.name &&
                input == p.input &&
                output == p.output;
        }

        internal Parameter ReplaceType(SandboxType newType)
        {
            return new Parameter(newType, name, input, output, defaultValue);
        }
    }

    public class Builder
    {
        protected string name;
        protected List<Parameter> parameters;

        public Builder(string name)
        {
            this.name = name;
        }

        public void AddParameter(Parameter param)
        {
            if (parameters == null)
                parameters = new List<Parameter>();

            var existing = parameters.FindIndex(x => x.Name == param.Name);
            if (existing < 0)
            {
                parameters.Add(param);
            }
            else
            {
                // error : name collision
            }
        }

        public void AddInput(SandboxType type, string name, object defaultValue = null)
        {
            AddParameter(new Parameter(type, name, true, false, defaultValue));
        }

        public void AddOutput(SandboxType type, string name)
        {
            AddParameter(new Parameter(type, name, false, true));
        }

//         public void AddInOut(SandboxType type, string name, object defaultValue = null)
//         {
//             AddParameter(new Parameter(type, name, true, true, defaultValue));
//         }

        public ShaderFunctionSignature Build()
        {
            var result = new ShaderFunctionSignature(name, parameters);
            return result;
        }
    }

    // constructor is internal, public must use the Builder class instead
    internal ShaderFunctionSignature(string name, List<Parameter> parameters)
    {
        this.name = name;
        this.parameters = parameters;
    }

    public virtual bool ValueEquals(ShaderFunctionSignature other)
    {
        // same reference means they are trivially equal
        // this is important to check first to reduce the work involved
        if (other == this)
            return true;

        if (other == null)
            return false;

        if (name != other.name)
            return false;

        if (parameters != other.parameters)
        {
            int inputCount = parameters?.Count ?? 0;
            int otherInputCount = other.parameters?.Count ?? 0;
            if (inputCount != otherInputCount)
                return false;

            // not obvious, but if either is null, count will be zero here
            for (int i = 0; i < inputCount; i++)
                if (!parameters[i].Equals(other.parameters[i]))
                    return false;
        }

        return true;
    }
}
