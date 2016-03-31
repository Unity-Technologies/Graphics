using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    public class VFXEdSpawnTemplateLibrary : ScriptableObject
    {
        public static string LibraryPath = "/VFXEditor/Editor/TemplateLibrary.txt";

        public List<VFXEdSpawnTemplate> Templates { get { return m_Templates; } }
        [SerializeField]
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

        public void DeleteTemplate (string Path)
        {
            VFXEdSpawnTemplate t = GetTemplate(Path);
            if (t != null)
            {
                m_Templates.Remove(t);
                WriteLibrary();
            }
        }

        public void AddTemplate(VFXEdSpawnTemplate template)
        {
            VFXEdSpawnTemplate todelete = m_Templates.Find(t => t.Path.Equals(template.Path));

            if (todelete != null)
                if (EditorUtility.DisplayDialog("Template Already Exists", "Template Already Exists, Overwrite?", "Overwrite", "Cancel"))
                {
                    m_Templates.Remove(todelete);
                }
                else
                    return;

            m_Templates.Add(template);
            WriteLibrary();

        }

        public static VFXEdSpawnTemplateLibrary Create()
        {
            VFXEdSpawnTemplateLibrary lib = CreateInstance<VFXEdSpawnTemplateLibrary>();
            lib.ReloadLibrary();
            return lib;
        }

        public VFXParamValue CreateParamValue(string ParamType, string XMLStringValue)
        {
            string[] vals;

            switch(ParamType)
            {
                case "kTypeFloat":
                    return VFXParamValue.Create(float.Parse(XMLStringValue));
                case "kTypeInt":
                    return VFXParamValue.Create(int.Parse(XMLStringValue));
                case "kTypeUint":
                    return VFXParamValue.Create(uint.Parse(XMLStringValue));
                case "kTypeFloat2":
                    vals = XMLStringValue.Split(',');
                    Vector2 v2 = new Vector2(float.Parse(vals[0]), float.Parse(vals[1]));
                    return VFXParamValue.Create(v2);
                case "kTypeFloat3":
                    vals = XMLStringValue.Split(',');
                    Vector3 v3 = new Vector3(float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[2]));
                    return VFXParamValue.Create(v3);
                case "kTypeFloat4":
                    vals = XMLStringValue.Split(',');
                    Vector4 v4 = new Vector4(float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[2]), float.Parse(vals[3]));
                    return VFXParamValue.Create(v4);
                case "kTypeTexture2D":
                    return VFXParamValue.Create(AssetDatabase.LoadAssetAtPath<Texture2D>(XMLStringValue));
                case "kTypeTexture3D":
                    return VFXParamValue.Create(AssetDatabase.LoadAssetAtPath<Texture3D>(XMLStringValue));
                default:
                    return null;
            }
        }

        public void ReloadLibrary()
        {
            Templates.Clear();
            string path = Application.dataPath + LibraryPath;
            XDocument doc;

            try
            {
                doc = XDocument.Load(path);
            }
            catch (System.IO.FileNotFoundException e)
            {
                Debug.LogWarning("Template Library File Not found : " + e.FileName);
                WriteLibrary();
                doc = XDocument.Load(path);
            }
           
            XElement lib = doc.Element("Library");


            var templates = lib.Elements("Template");
            foreach(XElement t in templates)
            {

                VFXEdSpawnTemplate template = VFXEdSpawnTemplate.Create(t.Attribute("Category").Value, t.Attribute("Name").Value);

                // TODO : Remove when using Triggers
                var system = t.Element("System");
                if(system != null)
                    template.SystemInformation = new VFXEdSpawnTemplate.SysInfo(float.Parse(system.Attribute("SpawnRate").Value), uint.Parse(system.Attribute("AllocationCount").Value));
                // END TODO
            
                var nodes = t.Element("Nodes").Elements("Node");
                var datanodes = t.Element("Nodes").Elements("DataNode");

                var flowconnections = t.Element("Connections").Elements("FlowConnection");
                var dataconnections = t.Element("Connections").Elements("DataConnection");

                foreach(XElement n in nodes)
                {
                    template.AddContextNode(n.Attribute("Name").Value, n.Attribute("Context").Value);

                    XElement contextParms = n.Element("Context");

                    foreach(XElement parm in contextParms.Elements("VFXParamValue"))
                    {
                        string nn = n.Attribute("Name").Value;
                        string pn = parm.Attribute("Name").Value;
                        string pt = parm.Attribute("Type").Value;

                        VFXParamValue value = CreateParamValue(pt, parm.Attribute("Value").Value);
                        template.SetContextNodeParameter(nn, pn, value);
                    }

                    foreach(XElement nb in n.Elements("NodeBlock"))
                    {
                        template.AddContextNodeBlock(n.Attribute("Name").Value, nb.Attribute("Name").Value, nb.Attribute("BlockName").Value);

                        foreach(XElement parm in nb.Elements("VFXParamValue"))
                        {
                            string nn = n.Attribute("Name").Value;
                            string nbn = nb.Attribute("Name").Value;
                            string pn = parm.Attribute("Name").Value;
                            string pt = parm.Attribute("Type").Value;

                            VFXParamValue value = CreateParamValue(pt, parm.Attribute("Value").Value);
                            template.SetContextNodeBlockParameter(nn, nbn, pn, value);
                        }
                    }

                }

                // Data Nodes
                foreach(XElement n in datanodes)
                {
                    template.AddDataNode(n.Attribute("Name").Value, bool.Parse(n.Attribute("Exposed").Value));
                    
                    foreach(XElement nb in n.Elements("DataNodeBlock"))
                    {
                        XElement dataContainer = nb.Element("DataContainer");
                        if (dataContainer == null)
                            template.AddDataNodeBlock(n.Attribute("Name").Value, nb.Attribute("Name").Value, nb.Attribute("ExposedName").Value, nb.Attribute("BlockName").Value);
                        else
                            template.AddDataNodeBlock(n.Attribute("Name").Value, nb.Attribute("Name").Value, nb.Attribute("ExposedName").Value, nb.Attribute("BlockName").Value, dataContainer);

                        foreach(XElement parm in nb.Elements("VFXParamValue"))
                        {
                            string nn = n.Attribute("Name").Value;
                            string nbn = nb.Attribute("Name").Value;
                            string pn = parm.Attribute("Name").Value;
                            string pt = parm.Attribute("Type").Value;

                            VFXParamValue value = CreateParamValue(pt, parm.Attribute("Value").Value);
                            template.SetDataNodeBlockParameter(nn, nbn, pn, value);
                        }
                    }

                }

                foreach(XElement fc in flowconnections)
                {
                    template.AddFlowConnection(fc.Attribute("Previous").Value, fc.Attribute("Next").Value);
                }

                foreach(XElement dc in dataconnections)
                {
                    XElement dcin = dc.Element("Input");
                    XElement dcout = dc.Element("Output");

                    DataNodeInfo dataNode = template.DataNodes[dcin.Attribute("DataNode").Value];
                    ContextNodeInfo contextNode = template.ContextNodes[dcout.Attribute("ContextNode").Value];

                    DataParamConnectorInfo inputInfo = new DataParamConnectorInfo(dataNode,int.Parse(dcin.Attribute("NodeBlockIndex").Value),dcin.Attribute("ParamName").Value);
                    ContextParamConnectorInfo outputInfo = new ContextParamConnectorInfo(contextNode, int.Parse(dcout.Attribute("NodeBlockIndex").Value), dcout.Attribute("ParamName").Value);

                    template.AddDataConnection(inputInfo,outputInfo);
                }

                AddTemplate(template);
            }

        }

        public void WriteParamValue(XmlWriter doc, string name, VFXParam.Type type, VFXParamValue paramValue)
        {
                        doc.WriteStartElement("VFXParamValue");
                        doc.WriteAttributeString("Name", name);

                        doc.WriteAttributeString("Type", type.ToString());

                        string value = "";
                        switch(type)
                        {
                            case VFXParam.Type.kTypeFloat: value = paramValue.GetValue<float>().ToString(); break;
                            case VFXParam.Type.kTypeInt: value = paramValue.GetValue<int>().ToString(); break;
                            case VFXParam.Type.kTypeUint: value = paramValue.GetValue<uint>().ToString(); break;
                            case VFXParam.Type.kTypeFloat2:
                                Vector2 v2 = paramValue.GetValue<Vector2>();
                                value = v2.x + "," + v2.y ;
                                break;
                            case VFXParam.Type.kTypeFloat3:
                                Vector3 v3 = paramValue.GetValue<Vector3>();
                                value = v3.x + "," + v3.y+ "," + v3.z ;
                                break;
                            case VFXParam.Type.kTypeFloat4:
                                Vector4 v4 = paramValue.GetValue<Vector4>();
                                value = v4.x + "," + v4.y + "," + v4.z+ "," + v4.w;
                                break;
                            case VFXParam.Type.kTypeTexture2D:
                                Texture2D t = paramValue.GetValue<Texture2D>();
                                value = AssetDatabase.GetAssetPath(t) ;
                                break;
                            case VFXParam.Type.kTypeTexture3D:
                                Texture3D t3 = paramValue.GetValue<Texture3D>();
                                value = AssetDatabase.GetAssetPath(t3) ;
                                break;
                            default:
                                break;
                        }
                        doc.WriteAttributeString("Value", value);
                        doc.WriteEndElement();
        }

        public void WriteLibrary()
        {
            string path = Application.dataPath + LibraryPath;

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };

            XmlWriter doc = XmlWriter.Create(path, settings);

            doc.WriteStartElement("Library");
            foreach(VFXEdSpawnTemplate template in Templates)
            {
                doc.WriteStartElement("Template");
                doc.WriteAttributeString("Category", template.Category);
                doc.WriteAttributeString("Name", template.Name);

                // TODO: Remove When using Triggers
                if(template.SystemInformation != null)
                {
                    doc.WriteStartElement("System");
                    doc.WriteAttributeString("SpawnRate", template.SystemInformation.SpawnRate.ToString());
                    doc.WriteAttributeString("AllocationCount", template.SystemInformation.AllocationCount.ToString());
                    doc.WriteEndElement();
                }
                // END TODO

                doc.WriteStartElement("Nodes");
                foreach(KeyValuePair<string, ContextNodeInfo> kvp_node in template.ContextNodes)
                {
                    doc.WriteStartElement("Node");
                    doc.WriteAttributeString("Name", kvp_node.Key);
                    doc.WriteAttributeString("Context", kvp_node.Value.Context.ToString());

                    // Context Parameters
                    doc.WriteStartElement("Context");
                    foreach(KeyValuePair<string, VFXParamValue> kvp_param in kvp_node.Value.ParameterOverrides)
                    {
                        // WRITE PARAM VALUE
                        WriteParamValue(doc, kvp_param.Key, kvp_param.Value.ValueType, kvp_param.Value);  
                    }
                    doc.WriteEndElement(); // End Context

                    foreach(KeyValuePair<string, NodeBlockInfo> kvp_nodeblock in kvp_node.Value.nodeBlocks)
                    {
                        doc.WriteStartElement("NodeBlock");
                        doc.WriteAttributeString("Name", kvp_nodeblock.Key);
                        doc.WriteAttributeString("BlockName", kvp_nodeblock.Value.BlockLibraryName);

                        foreach(KeyValuePair<string, VFXParamValue> kvp_param in kvp_nodeblock.Value.ParameterOverrides)
                        {
                            // WRITE PARAM VALUE
                            WriteParamValue(doc, kvp_param.Key, kvp_param.Value.ValueType, kvp_param.Value);
                        }
                        
                        doc.WriteEndElement(); // End NodeBlock
                    }

                    doc.WriteEndElement(); // End Node
                }

                // Data Nodes
                foreach(KeyValuePair<string, DataNodeInfo> kvp_node in template.DataNodes)
                {
                    doc.WriteStartElement("DataNode");
                    doc.WriteAttributeString("Name", kvp_node.Key);
                    doc.WriteAttributeString("Exposed", kvp_node.Value.Exposed.ToString());

                    foreach(KeyValuePair<string, DataNodeBlockInfo> kvp_nodeblock in kvp_node.Value.nodeBlocks)
                    {
                        doc.WriteStartElement("DataNodeBlock");
                        doc.WriteAttributeString("Name", kvp_nodeblock.Key);
                        doc.WriteAttributeString("BlockName", kvp_nodeblock.Value.BlockLibraryName);
                        doc.WriteAttributeString("ExposedName", kvp_nodeblock.Value.ExposedName);

                        if(kvp_nodeblock.Value.dataContainer != null)
                        {
                            kvp_nodeblock.Value.dataContainer.Serialize(doc);
                        }

                        foreach(KeyValuePair<string, VFXParamValue> kvp_param in kvp_nodeblock.Value.ParameterOverrides)
                        {
                            // WRITE PARAM VALUE
                            WriteParamValue(doc, kvp_param.Key, kvp_param.Value.ValueType, kvp_param.Value);
                        }
                        
                        doc.WriteEndElement(); // End NodeBlock
                    }

                    doc.WriteEndElement(); // End DataNode
                }
                doc.WriteEndElement(); // End Nodes

                doc.WriteStartElement("Connections");

                foreach(FlowConnection c in template.FlowConnections)
                {
                    doc.WriteStartElement("FlowConnection");
                    foreach(KeyValuePair<string,ContextNodeInfo> kvp_node in template.ContextNodes )
                    {
                        if(kvp_node.Value == c.Previous) doc.WriteAttributeString("Previous", kvp_node.Key);
                        if(kvp_node.Value == c.Next) doc.WriteAttributeString("Next", kvp_node.Key);
                    }
                    
                    doc.WriteEndElement();
                }

                foreach(DataConnection c in template.DataConnections)
                {
                    doc.WriteStartElement("DataConnection");

                    doc.WriteStartElement("Input");
                    doc.WriteAttributeString("DataNode", c.Previous.m_Node.m_UniqueName);
                    doc.WriteAttributeString("NodeBlockIndex", c.Previous.m_NodeBlockIndex.ToString());
                    doc.WriteAttributeString("ParamName", c.Previous.m_ParameterName);
                    doc.WriteEndElement();

                    doc.WriteStartElement("Output");
                    doc.WriteAttributeString("ContextNode", c.Next.m_Node.m_UniqueName);
                    doc.WriteAttributeString("NodeBlockIndex", c.Next.m_NodeBlockIndex.ToString());
                    doc.WriteAttributeString("ParamName", c.Next.m_ParameterName);
                    doc.WriteEndElement();

                    doc.WriteEndElement();
                }
                doc.WriteEndElement(); // End Connections

                doc.WriteEndElement(); // End Template
            }

            doc.WriteEndElement(); // End Library
            doc.Close();
        }

        public void CreateDefaultAsset()
        {
            WriteLibrary();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        internal static VFXEdSpawnTemplate CreateTemplateFromSelection(VFXEdCanvas canvas, string category, string name)
        {
            VFXEdSpawnTemplate t = VFXEdSpawnTemplate.Create(category, name);
            if(canvas.selection.Count == 0)
            {
                if (EditorUtility.DisplayDialog("Warning", "Selection is Empty, Are you sure you want to continue?", "Break", "Continue"))
                {
                    return null;
                }
            }

            foreach(CanvasElement e in canvas.selection)
            {
                if(e is VFXEdContextNode)
                {
                    VFXEdContextNode node = (e as VFXEdContextNode);
                    t.AddContextNode(node.UniqueName, node.Model.Desc.Name);

                    // TODO: Remove & Refactor When Using Triggers
                    VFXSystemModel sysmodel = node.Model.GetOwner();
                    t.SystemInformation = new VFXEdSpawnTemplate.SysInfo(sysmodel.SpawnRate, sysmodel.MaxNb);
                    // END TODO

                    // Context Node Parameters
                    for(int i = 0; i < node.Model.GetNbParamValues(); i++)
                    {
                        t.SetContextNodeParameter(node.UniqueName, node.Model.Desc.m_Params[i].m_Name, node.Model.GetParamValue(i).Clone());
                    }

                    // Context Node Blocks
                    foreach(VFXEdProcessingNodeBlock block in node.NodeBlockContainer.nodeBlocks)
                    {
                        t.AddContextNodeBlock(node.UniqueName, block.UniqueName, block.LibraryName);
                        for (int i = 0 ;  i < block.Params.Length; i++)
                        {
                            t.SetContextNodeBlockParameter(node.UniqueName, block.UniqueName, block.Params[i].m_Name, block.ParamValues[i].Clone());
                        }
                    }
                }
                else if (e is VFXEdDataNode)
                {
                    VFXEdDataNode node = (e as VFXEdDataNode);
                    t.AddDataNode(node.UniqueName, node.exposed);
                    
                    // Data Node Blocks
                    foreach(VFXEdDataNodeBlock block in node.NodeBlockContainer.nodeBlocks)
                    {
                        if(block.editingDataContainer != null)
                            t.AddDataNodeBlock(node.UniqueName, block.UniqueName,block.m_exposedName, block.LibraryName ,block.editingDataContainer);
                        else
                            t.AddDataNodeBlock(node.UniqueName, block.UniqueName,block.m_exposedName, block.LibraryName);
                        for (int i = 0 ;  i < block.Params.Count; i++)
                        {
                            t.SetDataNodeBlockParameter(node.UniqueName, block.UniqueName, block.Params[i].m_Name, block.ParamValues[i].Clone());
                        }
                    }
                }
            }

            foreach(CanvasElement e in canvas.selection)
            {
                if(e is FlowEdge)
                {
                    FlowEdge edge = (e as FlowEdge);
                    if((edge.Left as VFXEdFlowAnchor).parent is VFXEdContextNode && (edge.Right as VFXEdFlowAnchor).parent is VFXEdContextNode)
                    {
                        string left = ((edge.Left as VFXEdFlowAnchor).parent as VFXEdNode).UniqueName;
                        string right = ((edge.Right as VFXEdFlowAnchor).parent as VFXEdNode).UniqueName;

                        if(t.ContextNodes.ContainsKey(left) && t.ContextNodes.ContainsKey(right))
                        {
                            t.AddFlowConnection(left, right);
                        }
                        
                    }

                }
                else if(e is DataEdge)
                {
                    DataEdge edge = (e as DataEdge);
                    VFXEdNodeBlockParameterField input = (edge.Left as VFXEdDataAnchor).GetAnchorField();
                    VFXEdNodeBlockParameterField output = (edge.Right as VFXEdDataAnchor).GetAnchorField();
                    VFXEdNodeBlockDraggable inputBlock = input.parent as VFXEdNodeBlockDraggable;
                    VFXEdNodeBlockDraggable outputBlock = output.parent as VFXEdNodeBlockDraggable;
                    VFXEdNode inputNode = inputBlock.parent.parent as VFXEdNode;
                    VFXEdNode outputNode = outputBlock.parent.parent as VFXEdNode;

                    t.AddDataConnection(
                        new DataParamConnectorInfo(t.DataNodes[inputNode.UniqueName], inputNode.NodeBlockContainer.nodeBlocks.IndexOf(inputBlock), input.Name),
                        new ContextParamConnectorInfo(t.ContextNodes[outputNode.UniqueName], outputNode.NodeBlockContainer.nodeBlocks.IndexOf(outputBlock), output.Name)
                        );
                }

            }
            return t;
        }
        
    }
}
