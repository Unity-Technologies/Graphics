using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEditor.ShaderGraph
{
    abstract class NodeTypeState
    {
        public int id;
        public AbstractMaterialGraph owner;
        public NodeTypeDescriptor type;
        public bool typeCreated;
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

            foreach (var fieldInfo in typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (fieldInfo.FieldType != typeof(InputPort) && fieldInfo.FieldType != typeof(OutputPort))
                {
                    continue;
                }

                var portAttributes = fieldInfo.GetCustomAttributes<PortAttribute>().ToList();

                if (portAttributes.Count == 0)
                {
                    throw new InvalidOperationException($"{typeof(T).FullName}.{fieldInfo.Name} has type {fieldInfo.FieldType.Name}, but does not have a port attribute.");
                }

                if (portAttributes.Count > 1)
                {
                    throw new InvalidOperationException($"{typeof(T).FullName}.{fieldInfo.Name} has multiple port attributes.");
                }

                var portAttribute = portAttributes[0];

                var displayName = fieldInfo.Name;
                if (displayName.StartsWith("m_"))
                {
                    displayName = displayName.Substring(2);
                }
                if (displayName.EndsWith("Port"))
                {
                    displayName = displayName.Substring(0, displayName.Length - 4);
                }

                if (fieldInfo.FieldType == typeof(InputPort))
                {
                    inputPorts.Add(new InputPortDescriptor { id = fieldInfo.Name, displayName = displayName, value = portAttribute.value });
                    var portRef = new InputPort(inputPorts.Count);
                    fieldInfo.SetValue(nodeType, portRef);
                }
                else
                {
                    outputPorts.Add(new OutputPortDescriptor { id = fieldInfo.Name, displayName = displayName, type = portAttribute.value.type });
                    var portRef = new OutputPort(outputPorts.Count);
                    fieldInfo.SetValue(nodeType, portRef);
                }
            }

            var context = new NodeSetupContext(owner, id, this);
            nodeType.Setup(context);
            if (!typeCreated)
            {
                throw new InvalidOperationException($"{typeof(T).FullName}.{nameof(ShaderNodeType.Setup)} did not provide a type via {nameof(NodeSetupContext)}.{nameof(NodeSetupContext.CreateType)}({nameof(NodeTypeDescriptor)}).");
            }
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
