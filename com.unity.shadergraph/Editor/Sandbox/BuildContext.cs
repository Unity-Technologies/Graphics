using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine;


public interface ISandboxNodeBuildContext
{
    void SetName(string name);
    SandboxType AddType(SandboxTypeDefinition typeDef);

    SandboxType GetInputType(string pinName);           // returns the type of the given input (_before_ it is cast to the local input type), or null if it is not connected
    bool GetInputConnected(string pinName);             // returns true if the given input is connected (something is overriding the default value) -- TODO: there could be better ways to do user prompt slots...
    // public System.Object GetInputStaticValue(string inputPin);       // TODO: provide a way to access compile-time static input values

    void AddInputSlot(SandboxType type, string name, System.Object defaultValue = null);        // TODO: slot type filters, hidden inputs
    void AddOutputSlot(SandboxType type, string name);
    void HideSlot(string name);

    void SetMainFunction(ShaderFunction function, bool declareSlots = true);
    void SetPreviewFunction(ShaderFunction function, PreviewMode defaultPreviewMode = PreviewMode.Inherit);

    // provide feedback about the node -- i.e. the node is configured incorrectly, or an input is not correct
    void Error(string message);
//     void Warning(string message);
//     void Message(string message);
}
