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

        internal Dictionary<string, ContextNodeInfo> ContextNodes { get { return m_ContextNodes; } }
        internal List<FlowConnection> Connections { get { return m_Connections; } }

        [SerializeField]
        private Dictionary<string, ContextNodeInfo> m_ContextNodes;
        [SerializeField]
        private List<FlowConnection> m_Connections;
        
        public VFXEdSpawnTemplate()
        {
            m_ContextNodes = new Dictionary<string,ContextNodeInfo>();
            m_Connections = new List<FlowConnection>();
        }

        internal static VFXEdSpawnTemplate Create(string category, string name)
        {
            VFXEdSpawnTemplate t = ScriptableObject.CreateInstance<VFXEdSpawnTemplate>();
            t.Name = name;
            t.Category = category;
            return t;
        }

        public void AddContextNode(string nodename, string contextName)
        {
            m_ContextNodes.Add(nodename, ContextNodeInfo.Create(contextName));
        }

        public void SetContextNodeParameter(string nodename, string paramName, VFXParamValue value)
        {
            m_ContextNodes[nodename].AddParameterOverride(paramName, value);
        }

        public void AddContextNodeBlock(string nodename, string blockname)
        {
            m_ContextNodes[nodename].nodeBlocks.Add(blockname, NodeBlockInfo.Create(blockname));
        }

        public void SetContextNodeBlockParameter(string nodename, string blockname, string paramName, VFXParamValue value)
        {
            m_ContextNodes[nodename].nodeBlocks[blockname].AddParameterOverride(paramName, value);
        }

        public void AddConnection(string nodeA, string nodeB)
        {
            m_Connections.Add(FlowConnection.Create(m_ContextNodes[nodeA], m_ContextNodes[nodeB]));
        }

        internal void Spawn(VFXEdDataSource datasource, VFXEdCanvas canvas, Vector2 canvasPosition )
        {
            Dictionary<ContextNodeInfo, VFXEdNode> spawnedNodes = new Dictionary<ContextNodeInfo, VFXEdNode>();

            Vector2 CurrentPos = canvasPosition - new Vector2(VFXEditorMetrics.NodeDefaultWidth/2,80.0f);

            foreach(KeyValuePair<string,ContextNodeInfo> node_kvp in m_ContextNodes)
            {
                VFXEdNode node = null;
                string context = node_kvp.Value.Context;

                node = new VFXEdContextNode(CurrentPos, VFXEditor.ContextLibrary.GetContext(context), datasource);

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
