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
        // TODO: Remove & Refactor when using Triggers
        public class SysInfo
        {
            public float SpawnRate;
            public uint AllocationCount;

            public SysInfo(float spawnRate, uint allocationCount)
            {
                SpawnRate = spawnRate;
                AllocationCount = allocationCount;
            }
        }
        // END TODO

        public string Name { get { return m_Name; } set { m_Name = value; } }
        public string Category { get { return m_Category; } set { m_Category = value; } }

        public SysInfo SystemInformation;

        public string Path { get { return m_Category + "/" + m_Name; } }

        private string m_Name = "";
        private string m_Category = "";

        internal Dictionary<string, ContextNodeInfo> ContextNodes { get { return m_ContextNodes; } }
        internal Dictionary<string, DataNodeInfo> DataNodes { get { return m_DataNodes; } }

        internal List<FlowConnection> FlowConnections { get { return m_FlowConnections; } }
        internal List<DataConnection> DataConnections { get { return m_DataConnections; } }

        [SerializeField]
        private Dictionary<string, ContextNodeInfo> m_ContextNodes;
        [SerializeField]
        private Dictionary<string, DataNodeInfo> m_DataNodes;

        [SerializeField]
        private List<FlowConnection> m_FlowConnections;
        [SerializeField]
        private List<DataConnection> m_DataConnections;
        public VFXEdSpawnTemplate()
        {
            m_ContextNodes = new Dictionary<string,ContextNodeInfo>();
            m_DataNodes = new Dictionary<string,DataNodeInfo>();

            m_FlowConnections = new List<FlowConnection>();
            m_DataConnections = new List<DataConnection>();
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

        public void AddDataNode(string nodename, bool bExposed)
        {
            m_DataNodes.Add(nodename, DataNodeInfo.Create(bExposed));
        }

        public void SetContextNodeParameter(string nodename, string paramName, VFXParamValue value)
        {
            m_ContextNodes[nodename].AddParameterOverride(paramName, value);
        }

        public void AddContextNodeBlock(string nodename, string blockname)
        {
            m_ContextNodes[nodename].nodeBlocks.Add(blockname, NodeBlockInfo.Create(blockname));
        }

        public void AddDataNodeBlock(string nodename, string blockname, string exposedName)
        {
            m_DataNodes[nodename].nodeBlocks.Add(blockname, DataNodeBlockInfo.Create(blockname, exposedName));
        }

        public void SetContextNodeBlockParameter(string nodename, string blockname, string paramName, VFXParamValue value)
        {
            m_ContextNodes[nodename].nodeBlocks[blockname].AddParameterOverride(paramName, value);
        }

        public void SetDataNodeBlockParameter(string nodename, string blockname, string paramName, VFXParamValue value)
        {
            m_DataNodes[nodename].nodeBlocks[blockname].AddParameterOverride(paramName, value);
        }

        public void AddFlowConnection(string nodeA, string nodeB)
        {
            m_FlowConnections.Add(FlowConnection.Create(m_ContextNodes[nodeA], m_ContextNodes[nodeB]));
        }

        internal void AddDataConnection(DataParamConnectorInfo input, ContextParamConnectorInfo output)
        {
            m_DataConnections.Add(DataConnection.Create(input, output));
        }

        internal void Spawn(VFXEdDataSource datasource, VFXEdCanvas canvas, Vector2 canvasPosition )
        {
            Dictionary<ContextNodeInfo, VFXEdNode> spawnedContextNodes = new Dictionary<ContextNodeInfo, VFXEdNode>();

            Vector2 CurrentPos = canvasPosition - new Vector2(VFXEditorMetrics.NodeDefaultWidth/2,80.0f);

            foreach(KeyValuePair<string,ContextNodeInfo> node_kvp in m_ContextNodes)
            {
                VFXEdContextNode node = null;
                string context = node_kvp.Value.Context;

                node = new VFXEdContextNode(CurrentPos, VFXEditor.ContextLibrary.GetContext(context), datasource);

                if(node != null)
                {
                    datasource.AddElement(node);
                    spawnedContextNodes.Add(node_kvp.Value, node);
                }

                foreach (KeyValuePair <string,VFXParamValue> param_kvp in node_kvp.Value.ParameterOverrides)
                {
                    node.SetContextParameterValue(param_kvp.Key, param_kvp.Value);
                }
                
                // TODO : Remove when using Triggers
                node.Model.GetOwner().MaxNb = SystemInformation.AllocationCount;
                node.Model.GetOwner().SpawnRate = SystemInformation.SpawnRate;
                // END TODO

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

            // Data Nodes
            CurrentPos = canvasPosition - new Vector2(VFXEditorMetrics.NodeDefaultWidth * 2 ,80.0f);

            Dictionary<DataNodeInfo, VFXEdNode> spawnedDataNodes = new Dictionary<DataNodeInfo, VFXEdNode>();

            foreach(KeyValuePair<string,DataNodeInfo> node_kvp in m_DataNodes)
            {
                VFXEdDataNode node = null;

                node = new VFXEdDataNode(CurrentPos, datasource);
                

                if(node != null)
                {
                    node.exposed = node_kvp.Value.Exposed;
                    datasource.AddElement(node);
                    spawnedDataNodes.Add(node_kvp.Value, node);
                }

                foreach(KeyValuePair<string,DataNodeBlockInfo> block_kvp in node_kvp.Value.nodeBlocks)
                {
                    VFXEdDataNodeBlock block = new VFXEdDataNodeBlock(VFXEditor.DataBlockLibrary.GetBlock(block_kvp.Value.BlockName), datasource,block_kvp.Value.ExposedName);
                    
                    foreach (KeyValuePair <string,VFXParamValue> param_kvp in block_kvp.Value.ParameterOverrides)
                    {
                        block.SetParametervalue(param_kvp.Key, param_kvp.Value);
                    }
                    node.NodeBlockContainer.AddNodeBlock(block);
                }

                node.Layout();
                CurrentPos.y += node.scale.y + 40.0f;
               
            }

            foreach(FlowConnection fc in m_FlowConnections)
            {
                datasource.ConnectFlow(spawnedContextNodes[fc.Previous].outputs[0], spawnedContextNodes[fc.Next].inputs[0]);
            }

            foreach(DataConnection c in m_DataConnections)
            {
                VFXEdDataAnchor input;
                VFXEdDataAnchor output;

                input = spawnedDataNodes[c.Previous.m_Node].NodeBlockContainer.nodeBlocks[c.Previous.m_NodeBlockIndex].GetField(c.Previous.m_ParameterName).Output;
                output = spawnedContextNodes[c.Next.m_Node].NodeBlockContainer.nodeBlocks[c.Next.m_NodeBlockIndex].GetField(c.Next.m_ParameterName).Input;

                datasource.ConnectData(input,output);
            }

            canvas.ReloadData();
        }


    }

}
