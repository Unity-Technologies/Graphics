using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace UnityEditor.ShaderGraph
{
    abstract class NodeTypeState
    {
        public int id;
        public AbstractMaterialGraph owner;
        public NodeTypeDescriptor type;
        public List<InputPortDescriptor> inputPorts = new List<InputPortDescriptor>();
        public List<OutputPortDescriptor> outputPorts = new List<OutputPortDescriptor>();
        public List<HlslSource> hlslSources = new List<HlslSource>();
        public List<ControlState> controls = new List<ControlState>();
        public List<HlslValue> hlslValues = new List<HlslValue>();

        #region Change lists for consumption by IShaderNode implementation

        // TODO: Need to also store node ID versions somewhere
        public IndexSet addedNodes = new IndexSet();
        public IndexSet modifiedNodes = new IndexSet();

        #endregion

        public bool isDirty => addedNodes.Any() || modifiedNodes.Any();

        public void ClearChanges()
        {
            addedNodes.Clear();
            modifiedNodes.Clear();
            // TODO: Use IndexSet for modified controls
            for (var i = 0; i < controls.Count; i++)
            {
                var control = controls[i];
                control.wasModified = false;
                controls[i] = control;
            }
        }

        public ShaderNodeType nodeType { get; protected set; }

        public abstract void DispatchChanges(NodeChangeContext context);
    }
    
    static class Regexes
    {
        public static readonly Regex camelCaseWords = new Regex(@"(\B[A-Z]+?(?=[A-Z][^A-Z])|\B[A-Z]+?(?=[^A-Z]))");
    }

    // This construction allows us to move the virtual call to outside the loop. The calls to the ShaderNodeType in
    // DispatchChanges are to a generic type parameter, and thus will be devirtualized if T is a sealed class.
    sealed class NodeTypeState<T> : NodeTypeState where T : ShaderNodeType, new()
    {
        public NodeTypeState(AbstractMaterialGraph owner, int id)
        {
            this.owner = owner;
            this.id = id;
            nodeType = new T();
            base.nodeType = nodeType;
            
            type.inputs = new List<InputPort>();
            type.outputs = new List<OutputPort>();

            foreach (var fieldInfo in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (fieldInfo.FieldType != typeof(InputPort) && fieldInfo.FieldType != typeof(OutputPort))
                {
                    continue;
                }

                var portAttribute = fieldInfo.GetCustomAttribute<PortAttribute>();
                if (portAttribute == null)
                {
                    throw new InvalidOperationException($"{typeof(T).FullName}.{fieldInfo.Name} has type {fieldInfo.FieldType.Name}, but does not have a port attribute.");
                }

                var displayName = fieldInfo.Name;
                const string portSuffix = "Port";
                if (displayName.EndsWith(portSuffix) && displayName.Length > portSuffix.Length)
                {
                    displayName = displayName.Substring(0, displayName.Length - portSuffix.Length);
                }

                displayName = char.ToUpperInvariant(displayName[0]) + Regexes.camelCaseWords.Replace(displayName.Substring(1), " $1");

                if (fieldInfo.FieldType == typeof(InputPort))
                {
                    inputPorts.Add(new InputPortDescriptor { id = fieldInfo.Name, displayName = displayName, value = portAttribute.value });
                    var portRef = new InputPort(inputPorts.Count);
                    fieldInfo.SetValue(nodeType, portRef);
                    type.inputs.Add(portRef);
                }
                else
                {
                    outputPorts.Add(new OutputPortDescriptor { id = fieldInfo.Name, displayName = displayName, type = portAttribute.value.type });
                    var portRef = new OutputPort(outputPorts.Count);
                    fieldInfo.SetValue(nodeType, portRef);
                    type.outputs.Add(portRef);
                }
            }

            var nameAttribute = typeof(T).GetCustomAttribute<NameAttribute>();
            type.name = nameAttribute?.name;
            // Provide auto-generated name if one is not provided.
            if (string.IsNullOrWhiteSpace(type.name))
            {
                type.name = typeof(T).Name;

                // Strip "Node" from the end of the name. We also make sure that we don't strip it to an empty string,
                // in case someone decided that `Node` was a good name for a class.
                const string nodeSuffix = "Node";
                if (type.name.Length > nodeSuffix.Length && type.name.EndsWith(nodeSuffix))
                {
                    type.name = type.name.Substring(0, type.name.Length - nodeSuffix.Length);
                }

                type.name = char.ToUpperInvariant(type.name[0]) + Regexes.camelCaseWords.Replace(type.name.Substring(1), " $1");
            }
            
            var pathAttribute = typeof(T).GetCustomAttribute<PathAttribute>();
            type.path = pathAttribute?.path ?? "Uncategorized";

            var context = new NodeSetupContext(owner, id, this);
            nodeType.Setup(context);
        }

        public new T nodeType { get; }

        public override void DispatchChanges(NodeChangeContext context)
        {
            var castNodeType = nodeType;

            foreach (var node in addedNodes)
            {
                castNodeType.OnNodeAdded(context, new ShaderNode(owner, owner.currentStateId, (ProxyShaderNode)owner.m_Nodes[node]));
            }

            foreach (var node in modifiedNodes)
            {
                castNodeType.OnNodeModified(context, new ShaderNode(owner, owner.currentStateId, (ProxyShaderNode)owner.m_Nodes[node]));
            }
        }
    }
}
