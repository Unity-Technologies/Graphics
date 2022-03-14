using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
   //[SearcherItem(typeof(ShaderGraphStencil), SearcherContext.Graph, "Customizable Node")]
    [Serializable]
    public class CustomizableNodeModel : NodeModel
    {
        [Serializable]
        public class PortDescription
        {
            public string Name, Id;
            public TypeHandle Type;
        }

        [SerializeField]
        List<PortDescription> m_Inputs;

        [SerializeField]
        List<PortDescription> m_Outputs;

        string[] m_inputIds => m_Inputs.Select(p => p.Id).ToArray();
        string[] m_outputIds => m_Outputs.Select(p => p.Id).ToArray();

        public CustomizableNodeModel()
        {
            m_Inputs = new List<PortDescription>();
            m_Outputs = new List<PortDescription>();
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            foreach (var portDescription in m_Inputs)
            {
                this.AddDataInputPort(portDescription.Name, portDescription.Type, portDescription.Id);
            }

            foreach (var portDescription in m_Outputs)
            {
                this.AddDataOutputPort(portDescription.Name, portDescription.Type, portDescription.Id);
            }
        }

        public void AddCustomDataInputPort(string name, TypeHandle type)
        {
            var uniqueName = ObjectNames.GetUniqueName(m_inputIds, name);
            m_Inputs.Add(new PortDescription { Name = uniqueName, Type = type, Id = uniqueName });
            this.AddDataInputPort(uniqueName, type, uniqueName);
        }

        public void AddCustomDataOutputPort(string name, TypeHandle type)
        {
            var uniqueName = ObjectNames.GetUniqueName(m_outputIds, name);
            m_Outputs.Add(new PortDescription { Name = uniqueName, Type = type, Id = uniqueName });
            this.AddDataOutputPort(uniqueName, type, uniqueName);
        }

        public void RemovePortByName(string name, bool output)
        {
            var ports = output ? m_Outputs : m_Inputs;
            ports.Remove(ports.Find(p => p.Name == name));
            DefineNode();
        }
    }
}
