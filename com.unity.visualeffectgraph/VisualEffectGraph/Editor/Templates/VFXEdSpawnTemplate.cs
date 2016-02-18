using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    public class VFXEdSpawnTemplate
    {
        public string Name { get { return m_Name; } }
        public string Category { get { return m_Category; } } 
        
        public string Path { get { return m_Category + "/" + m_Name; } }

        private string m_Name;
        private string m_Category;

        private Dictionary<string, NodeInfo> m_Nodes;
        private List<FlowConnection> m_Connections;

        public VFXEdSpawnTemplate(string category, string name)
        {
            m_Name = name;
            m_Category = category;
            m_Nodes = new Dictionary<string,NodeInfo>();
            m_Connections = new List<FlowConnection>();
        }

        public void AddNode(string nodename, VFXEdContext context)
        {
            m_Nodes.Add(nodename, new NodeInfo(context));
        }

        public void AddNodeBlock(string nodename, string blockname)
        {
            m_Nodes[nodename].nodeBlocks.Add(new NodeBlockInfo(blockname));
        }

        public void AddConnection(string nodeA, string nodeB)
        {
            m_Connections.Add(new FlowConnection(m_Nodes[nodeA], m_Nodes[nodeB]));
        }

        internal void Spawn(VFXEdDataSource datasource, VFXEdCanvas canvas, Vector2 canvasPosition )
        {
            Dictionary<NodeInfo, VFXEdNode> spawnedNodes = new Dictionary<NodeInfo, VFXEdNode>();

            Vector2 CurrentPos = canvasPosition - new Vector2(VFXEditorMetrics.NodeDefaultWidth/2,80.0f);

            foreach(KeyValuePair<string,NodeInfo> kvp in m_Nodes)
            {
                VFXEdNode node = null;
                switch(kvp.Value.Context)
                {
                    case VFXEdContext.Trigger:
                        node = new VFXEdTriggerNode(CurrentPos, datasource);
                        break;
                    case VFXEdContext.Initialize:
                    case VFXEdContext.Update:
                    case VFXEdContext.Output:
                        node = new VFXEdContextNode(CurrentPos, kvp.Value.Context, datasource);
                        break;
                    default:
                        break;
                }

                if(node != null)
                {
                    datasource.AddElement(node);
                    spawnedNodes.Add(kvp.Value, node);
                }

                foreach(NodeBlockInfo nodeblock in kvp.Value.nodeBlocks)
                {
                    node.NodeBlockContainer.AddNodeBlock(new VFXEdProcessingNodeBlock(VFXEditor.BlockLibrary.GetBlock(nodeblock.BlockName), datasource));
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

        private class NodeInfo
        {
            public List<NodeBlockInfo> nodeBlocks;
            public VFXEdContext Context {get { return m_Context; } }
            private VFXEdContext m_Context;
            
            public NodeInfo(VFXEdContext context)
            {
                m_Context = context;
                nodeBlocks = new List<NodeBlockInfo>();
            }
        }
        private class NodeBlockInfo
        {
            public string BlockName { get { return m_BlockName; } }
            private string m_BlockName;
            public NodeBlockInfo(string blockname) {
                m_BlockName = blockname;
            }
        }
        private class FlowConnection
        {
            public readonly NodeInfo Previous;
            public readonly NodeInfo Next;
            public FlowConnection(NodeInfo input, NodeInfo output)
            {
                Previous = input;
                Next = output;
            }
        }
    }

    internal class VFXEdTemplateSpawner : VFXEdSpawner
    {
        private string m_Path;
        private VFXEdDataSource m_Datasource;
        private VFXEdCanvas m_Canvas;

        public VFXEdTemplateSpawner(string path, VFXEdDataSource datasource, VFXEdCanvas canvas, Vector2 canvasPosition ) : base(canvasPosition)
        {
            m_Path = path;
            m_Datasource = datasource;
            m_Canvas = canvas;
        }

        public override void Spawn()
        {
            VFXEdSpawnTemplate template = VFXEditor.SpawnTemplates.GetTemplate(m_Path);
            template.Spawn(m_Datasource, m_Canvas, m_canvasPosition);
        }
    }

    public class VFXEdSpawnTemplateLibrary
    {
        public List<VFXEdSpawnTemplate> Templates { get { return m_Templates; } }
        private List<VFXEdSpawnTemplate> m_Templates;

        public VFXEdSpawnTemplateLibrary()
        {
            m_Templates = new List<VFXEdSpawnTemplate>();
        }

        public VFXEdSpawnTemplate GetTemplate(string path)
        {
            return m_Templates.Find(t => t.Path.Equals(path));
        }

        public void SpawnFromMenu(object o)
        {
            VFXEdTemplateSpawner spawner = o as VFXEdTemplateSpawner;
            spawner.Spawn();
        }

        public void Load()
        {

            VFXEdSpawnTemplate fulltemplate = new VFXEdSpawnTemplate("Full", "Full Template");
            fulltemplate.AddNode("init", VFXEdContext.Initialize);
            fulltemplate.AddNode("update", VFXEdContext.Update);
            fulltemplate.AddNode("output", VFXEdContext.Output);

            fulltemplate.AddNodeBlock("init", "Set Lifetime (Constant)");
            fulltemplate.AddNodeBlock("init", "Set Color (Constant)");
            fulltemplate.AddNodeBlock("init", "Set Position (Point)");
            fulltemplate.AddNodeBlock("init", "Set Velocity (Constant)");
            fulltemplate.AddNodeBlock("init", "Set Size Constant (Square)");

            fulltemplate.AddNodeBlock("update", "Apply Force");
            fulltemplate.AddNodeBlock("update", "Apply Drag");
            fulltemplate.AddNodeBlock("update", "Age and Reap");
            fulltemplate.AddNodeBlock("update", "Apply Velocity to Positions");

            fulltemplate.AddConnection("init", "update");
            fulltemplate.AddConnection("update", "output");

            m_Templates.Add(fulltemplate);


            VFXEdSpawnTemplate init = new VFXEdSpawnTemplate("Simple", "Initialize");
            init.AddNode("init", VFXEdContext.Initialize);
            init.AddNodeBlock("init", "Set Lifetime (Constant)");
            init.AddNodeBlock("init", "Set Color (Constant)");
            init.AddNodeBlock("init", "Set Position (Point)");
            init.AddNodeBlock("init", "Set Velocity (Constant)");
            init.AddNodeBlock("init", "Set Size Constant (Square)");

            m_Templates.Add(init);


            VFXEdSpawnTemplate update = new VFXEdSpawnTemplate("Simple", "Update");
            update.AddNode("update", VFXEdContext.Update);
            update.AddNodeBlock("update", "Apply Force");
            update.AddNodeBlock("update", "Apply Drag");
            update.AddNodeBlock("update", "Age and Reap");
            update.AddNodeBlock("update", "Apply Velocity to Positions");
            m_Templates.Add(update);



            VFXEdSpawnTemplate output = new VFXEdSpawnTemplate("Simple", "Output");
            output.AddNode("output", VFXEdContext.Output);

            m_Templates.Add(output);
        }
    }


}
