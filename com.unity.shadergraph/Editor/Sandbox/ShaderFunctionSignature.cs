using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;


[Serializable]
public class ShaderFunctionSignature : JsonObject
{
    public override int latestVersion => 1;

    // public API
    public string Name { get { return name; } }

    // note: IReadOnlyList is not a guarantee of immutability..  :(
    public IReadOnlyList<Parameter> Parameters { get { return parameters ?? ListUtils.EmptyReadOnlyList<Parameter>(); } }

    [SerializeField]
    protected string name;

    [SerializeField]
    protected List<Parameter> parameters;

    [Serializable]
    public struct Parameter
    {
        [SerializeField]
        string name;
        public string Name => name;

        [SerializeField]
        SandboxValueType type;
        public SandboxValueType Type => type;

        // TODO: make this an enum
        [SerializeField]
        bool input;
        [SerializeField]
        bool output;
        public bool IsInput => input;
        public bool IsOutput => output;
        public bool IsInOut => (input && output);       // TODO: doesn't work in sandbox node

        //[SerializeField]
        //JsonData<JsonObject> defaultValue; ?
        System.Object defaultValue;     // not serialized (yet??)
        public System.Object DefaultValue => defaultValue;

        public Parameter(SandboxValueType type, string name, bool input, bool output, System.Object defaultValue = null)
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

        internal Parameter ReplaceType(SandboxValueType newType)
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
            // todo: verify name collision
            parameters.Add(param);
        }

        public void AddInput(SandboxValueType type, string name, object defaultValue = null)
        {
            AddParameter(new Parameter(type, name, true, false, defaultValue));
        }

        public void AddOutput(SandboxValueType type, string name)
        {
            AddParameter(new Parameter(type, name, false, true));
        }

        public void AddInOut(SandboxValueType type, string name, object defaultValue = null)
        {
            AddParameter(new Parameter(type, name, true, true, defaultValue));
        }

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
