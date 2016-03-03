using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    public class VFXEdSpawnTemplate : ScriptableObject
    {
        public string Name { get { return m_Name; } set { m_Name = value; } }
        public string Category { get { return m_Category; } set { m_Category = value; } } 
        
        public string Path { get { return m_Category + "/" + m_Name; } }

        private string m_Name = "";
        private string m_Category = "";

        internal Dictionary<string, NodeInfo> Nodes { get { return m_Nodes; } }
        internal List<FlowConnection> Connections { get { return m_Connections; } }

        [SerializeField]
        private Dictionary<string, NodeInfo> m_Nodes;
        [SerializeField]
        private List<FlowConnection> m_Connections;
        
        public VFXEdSpawnTemplate()
        {
            m_Nodes = new Dictionary<string,NodeInfo>();
            m_Connections = new List<FlowConnection>();
        }

        internal static VFXEdSpawnTemplate Create(string category, string name)
        {
            VFXEdSpawnTemplate t = ScriptableObject.CreateInstance<VFXEdSpawnTemplate>();
            t.Name = name;
            t.Category = category;
            return t;
        }


        public void AddNode(string nodename, VFXEdContext context)
        {
            m_Nodes.Add(nodename, NodeInfo.Create(context));
        }

        public void AddNodeBlock(string nodename, string blockname)
        {
            m_Nodes[nodename].nodeBlocks.Add(blockname, NodeBlockInfo.Create(blockname));
        }

        public void SetNodeBlockParameter(string nodename, string blockname, string paramName, VFXParamValue value)
        {
            m_Nodes[nodename].nodeBlocks[blockname].AddParameterOverride(paramName, value);
        }

        public void AddConnection(string nodeA, string nodeB)
        {
            m_Connections.Add(FlowConnection.Create(m_Nodes[nodeA], m_Nodes[nodeB]));
        }

        internal void Spawn(VFXEdDataSource datasource, VFXEdCanvas canvas, Vector2 canvasPosition )
        {
            Dictionary<NodeInfo, VFXEdNode> spawnedNodes = new Dictionary<NodeInfo, VFXEdNode>();

            Vector2 CurrentPos = canvasPosition - new Vector2(VFXEditorMetrics.NodeDefaultWidth/2,80.0f);

            foreach(KeyValuePair<string,NodeInfo> node_kvp in m_Nodes)
            {
                VFXEdNode node = null;
                switch(node_kvp.Value.Context)
                {
                    case VFXEdContext.Trigger:
                        node = new VFXEdTriggerNode(CurrentPos, datasource);
                        break;
                    case VFXEdContext.Initialize:
                    case VFXEdContext.Update:
                    case VFXEdContext.Output:
                        node = new VFXEdContextNode(CurrentPos, node_kvp.Value.Context, datasource);
                        break;
                    default:
                        break;
                }

                if(node != null)
                {
                    datasource.AddElement(node);
                    spawnedNodes.Add(node_kvp.Value, node);
                }

                foreach(KeyValuePair<string,NodeBlockInfo> block_kvp in node_kvp.Value.nodeBlocks)
                {
                    VFXEdProcessingNodeBlock block = new VFXEdProcessingNodeBlock(VFXEditor.BlockLibrary.GetBlock(block_kvp.Value.BlockName), datasource);
                    
                    foreach (KeyValuePair <string,VFXParamValue> param_kvp in block_kvp.Value.ParameterOverrides)
                    {
                        block.SetParameterValue(param_kvp.Key, param_kvp.Value);
                    }
                    node.NodeBlockContainer.AddNodeBlock(block);
                }

                node.Layout();
                CurrentPos.y += node.scale.y + 40.0f;
               
            }

            foreach(FlowConnection c in m_Connections)
            {
                datasource.ConnectFlow(spawnedNodes[c.Previous].outputs[0], spawnedNodes[c.Next].inputs[0]);
            }

            canvas.ReloadData();
        }


    }

}
