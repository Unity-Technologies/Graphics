using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    class NodeTypeState
    {
        public int id;
        public AbstractMaterialGraph owner;
        public IShaderNodeType shaderNodeType;
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
    }
}
