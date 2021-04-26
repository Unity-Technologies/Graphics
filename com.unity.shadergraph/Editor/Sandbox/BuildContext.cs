using System;
using System.Collections.Generic;
using UnityEngine;


interface ISandboxNodeBuildContext
{
    SandboxValueType AddType(SandboxValueTypeDefinition typeDef);
    SandboxValueType GetInputType(string pinName);
    // public System.Object GetInputStaticValue(string inputPin);       // TODO
    // InputPin AddInputPin(string pinName, SandboxValueType concreteType, SandboxValueType.Filter dynamicTypeFilter = null);
    // OutputPin AddOutputPin(string pinName, SandboxValueType concreteType);
    void SetMainFunction(ShaderFunction function, bool declareStaticPins = false);
    // void SetPreviewFunction(ShaderFunction function, PreviewType previewType);
    // void AddFunction(ShaderFunction function); // may not need this, if Functions have to declare dependent functions...
    void Error(string message);
}


public class BuildContext
{
    internal BuildContext()
    {
    }

    /*
    internal void BuildNode(NodeInstance node)
    {
        this.node = node;
        this.graph = node.graph;

        // create new runtime to fill out
        this.nodeRuntime = new NodeInstance.Runtime();
        nodeRuntime.definition = graph.nodeLibrary.GetNodeDefinitionByType(node.definition);

        // call the definition BuildRuntime function
        nodeRuntime.definition.BuildRuntime(this);

        // TODO: we can compare the new runtime to the existing values on the node to know what changed...
        this.node.runtime = this.nodeRuntime;
    }

    internal NodeInstance node;
    internal NodeInstance.Runtime nodeRuntime;
    internal GraphContainer graph;

    public SandboxValueType AddType(SandboxValueTypeDefinition typeDef)
    {
        return graph.types.AddType(typeDef);
    }

    public NodeSettings nodeSettings => node?.settings.value;
    public SandboxValueType GetInputType(string pinName)
    {
        SandboxValueType result = null;
        InputPin inputPin = null;
        OutputPin outputPin = null;
        if (node.inputs.TryGetValue(pinName, out inputPin))
        {
            if (inputPin.connection.node.value != null)
            {
                outputPin = inputPin.connection.node.value.GetOutputPin(inputPin.connection.pin);
                result = outputPin?.runtime.concreteType ?? null;
            }
        }
        return result;
    }
    public System.Object GetInputStaticValue(string inputPin) { return 3; }

    // TODO: need to specify the default value when there is no input attached, somehow
    public InputPin AddInputPin(string pinName, SandboxValueType concreteType, SandboxValueType.Filter dynamicTypeFilter = null)
    {
        InputPin inputPin;
        if (!node.inputs.TryGetValue(pinName, out inputPin))
        {
            inputPin = new InputPin();
            node.inputs.Add(pinName, inputPin);

            inputPin.runtime.name = pinName;
            inputPin.runtime.parentNode = node;
        }

        inputPin.runtime.concreteType = concreteType;
        inputPin.runtime.dynamicTypeFilter = dynamicTypeFilter;

        return inputPin;
    }

    public OutputPin AddOutputPin(string pinName, SandboxValueType concreteType)
    {
        OutputPin outputPin;
        if (!node.outputs.TryGetValue(pinName, out outputPin))
        {
            outputPin = new OutputPin();
            node.outputs.Add(pinName, outputPin);

            outputPin.runtime.name = pinName;
            outputPin.runtime.parentNode = node;
        }

        outputPin.runtime.concreteType = concreteType;
        outputPin.runtime.dynamicTypeFilter = null;

        return outputPin;
    }
    */

    public void SetMainFunction(ShaderFunction function, bool declareStaticPins = false)
    {
/*
        nodeRuntime.mainFunction = function;
        if (declareStaticPins)
        {
            foreach (var p in function.Parameters)
            {
                if (p.IsInput)
                    AddInputPin(p.Name, p.Type, p.defaultValue);
                if (p.IsOutput)
                    AddOutputPin(p.Name, p.Type);
            }
        }
*/
    }

    /*
    public void SetPreviewFunction(ShaderFunction function, PreviewType previewType)
    {
        // function MUST return float4
    }
    */

    //    public void AddSubGraph(SubGraphAsset subGraph) { }

    public void AddFunction(ShaderFunction function)
    {
    }

    /*
    public void AddProperty(Property property)
    {
    }
    */

    // public void Error(Pin pin, string message) { Debug.Log("ERROR:" + message); }
    public void Error(string message) { Debug.Log("ERROR:" + message); }
}


// ---------------- placeholders ------------------
/*
public class GenerateContext
{
    // public FuncGenerateContext AddFunction(string functionName) { return new FuncGenerateContext(); }
}

public class Property
{
}


[Serializable]
public class SubGraphAsset
{
    // TODO: hide data structures behind generic property getters
    public List<SubGraphInput> inputs;
    public List<SubGraphOutput> outputs;
    public List<Property> properties;
    public ShaderFunction function { get { return (new ShaderFunction.Builder("SubGraphFunctionName")).Build(); } }     // TODO
};


public class SubGraphInput
{
    public string name;
    public SandboxValueType type;
    public bool isPublic;           // used for loops to hide local state
    public string defaultValue;     // need a better way to specify dynamic defaults
}

public class SubGraphOutput
{
    public string name;
    public SandboxValueType type;
    public bool isPublic;           // used for loops to hide local state
}

public enum PreviewType
{
    Render2D,
    Render3D
};
*/
