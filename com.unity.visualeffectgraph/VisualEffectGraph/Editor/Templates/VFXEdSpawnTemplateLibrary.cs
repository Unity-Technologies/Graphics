using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    // TODO Refactor Make work again
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

        public VFXValue CreateParamValue(string ParamType, string XMLStringValue)
        {
            string[] vals;

            switch(ParamType)
            {
                // Test both old and you string (kType* is deprecated !)
                case "kTypeFloat":
                case "kFloat":
                    return VFXValue.Create(float.Parse(XMLStringValue));
                case "kTypeInt":
                case "kInt":
                    return VFXValue.Create(int.Parse(XMLStringValue));
                case "kTypeUint":
                case "kUint":
                    return VFXValue.Create(uint.Parse(XMLStringValue));
                case "kTypeFloat2":
                case "kFloat2":
                    vals = XMLStringValue.Split(',');
                    Vector2 v2 = new Vector2(float.Parse(vals[0]), float.Parse(vals[1]));
                    return VFXValue.Create(v2);
                case "kTypeFloat3":
                case "kFloat3":
                    vals = XMLStringValue.Split(',');
                    Vector3 v3 = new Vector3(float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[2]));
                    return VFXValue.Create(v3);
                case "kTypeFloat4":
                case "kFloat4":
                    vals = XMLStringValue.Split(',');
                    Vector4 v4 = new Vector4(float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[2]), float.Parse(vals[3]));
                    return VFXValue.Create(v4);
                case "kTypeTexture2D":
                case "kTexture2D":
                    return VFXValue.Create(AssetDatabase.LoadAssetAtPath<Texture2D>(XMLStringValue));
                case "kTypeTexture3D":
                case "kTexture3D":
                    return VFXValue.Create(AssetDatabase.LoadAssetAtPath<Texture3D>(XMLStringValue));
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
                var systems = t.Elements("System");
                template.SystemInformation = new List<VFXEdSpawnTemplate.SysInfo>();

                foreach(XElement sys in systems)
                {
                    template.SystemInformation.Add(new VFXEdSpawnTemplate.SysInfo(float.Parse(sys.Attribute("SpawnRate").Value), uint.Parse(sys.Attribute("AllocationCount").Value), int.Parse(sys.Attribute("BlendMode").Value)));
                }
                // END TODO
            
                var nodes = t.Element("Nodes").Elements("Node");
                var datanodes = t.Element("Nodes").Elements("DataNode");

                var flowconnections = t.Element("Connections").Elements("FlowConnection");
                var dataconnections = t.Element("Connections").Elements("DataConnection");

                foreach(XElement n in nodes)
                {
                    template.AddContextNode(n.Attribute("Name").Value, n.Attribute("Context").Value, int.Parse(n.Attribute("SystemIndex").Value));

                    XElement contextParms = n.Element("Context");

                    foreach(XElement parm in contextParms.Elements("VFXParamValue"))
                    {
                        string nn = n.Attribute("Name").Value;
                        string pn = parm.Attribute("Name").Value;
                        string pt = parm.Attribute("Type").Value;

                        VFXValue value = CreateParamValue(pt, parm.Attribute("Value").Value);
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

                            VFXValue value = CreateParamValue(pt, parm.Attribute("Value").Value);
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

                            VFXValue value = CreateParamValue(pt, parm.Attribute("Value").Value);
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

        public void WriteParamValue(XmlWriter doc, string name,VFXValue paramValue)
        {
            doc.WriteStartElement("VFXParamValue");
            doc.WriteAttributeString("Name", name);

            doc.WriteAttributeString("Type", paramValue.ValueType.ToString());

            string value = "";
            switch (paramValue.ValueType)
            {
                case VFXValueType.kFloat: value = paramValue.Get<float>().ToString(); break;
                case VFXValueType.kInt: value = paramValue.Get<int>().ToString(); break;
                case VFXValueType.kUint: value = paramValue.Get<uint>().ToString(); break;
                case VFXValueType.kFloat2:
                    Vector2 v2 = paramValue.Get<Vector2>();
                    value = v2.x + "," + v2.y;
                    break;
                case VFXValueType.kFloat3:
                    Vector3 v3 = paramValue.Get<Vector3>();
                    value = v3.x + "," + v3.y + "," + v3.z;
                    break;
                case VFXValueType.kFloat4:
                    Vector4 v4 = paramValue.Get<Vector4>();
                    value = v4.x + "," + v4.y + "," + v4.z + "," + v4.w;
                    break;
                case VFXValueType.kTexture2D:
                    Texture2D t = paramValue.Get<Texture2D>();
                    value = AssetDatabase.GetAssetPath(t);
                    break;
                case VFXValueType.kTexture3D:
                    Texture3D t3 = paramValue.Get<Texture3D>();
                    value = AssetDatabase.GetAssetPath(t3);
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

                foreach(VFXEdSpawnTemplate.SysInfo info in template.SystemInformation)
                {
                    doc.WriteStartElement("System");
                    doc.WriteAttributeString("SpawnRate", info.SpawnRate.ToString());
                    doc.WriteAttributeString("AllocationCount", info.AllocationCount.ToString());
                    doc.WriteAttributeString("BlendMode", info.BlendMode.ToString());
                    doc.WriteEndElement();
                }

                doc.WriteStartElement("Nodes");
                foreach(KeyValuePair<string, ContextNodeInfo> kvp_node in template.ContextNodes)
                {
                    doc.WriteStartElement("Node");
                    doc.WriteAttributeString("Name", kvp_node.Key);
                    doc.WriteAttributeString("Context", kvp_node.Value.Context.ToString());
                    doc.WriteAttributeString("SystemIndex", kvp_node.Value.systemIndex.ToString());
                    // Context Parameters
                    doc.WriteStartElement("Context");
                    foreach(KeyValuePair<string, VFXValue> kvp_param in kvp_node.Value.ParameterOverrides)
                    {
                        // WRITE PARAM VALUE
                        WriteParamValue(doc, kvp_param.Key, kvp_param.Value);  
                    }
                    doc.WriteEndElement(); // End Context

                    foreach(KeyValuePair<string, NodeBlockInfo> kvp_nodeblock in kvp_node.Value.nodeBlocks)
                    {
                        doc.WriteStartElement("NodeBlock");
                        doc.WriteAttributeString("Name", kvp_nodeblock.Key);
                        doc.WriteAttributeString("BlockName", kvp_nodeblock.Value.BlockLibraryName);

                        foreach(KeyValuePair<string, VFXValue> kvp_param in kvp_nodeblock.Value.ParameterOverrides)
                        {
                            // WRITE PARAM VALUE
                            WriteParamValue(doc, kvp_param.Key, kvp_param.Value);
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

                        foreach(KeyValuePair<string, VFXValue> kvp_param in kvp_nodeblock.Value.ParameterOverrides)
                        {
                            // WRITE PARAM VALUE
                            WriteParamValue(doc, kvp_param.Key, kvp_param.Value);
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

            t.SystemInformation = new List<VFXEdSpawnTemplate.SysInfo>();
            var Systems = new List<VFXSystemModel>();

            foreach(CanvasElement e in canvas.selection)
            {
                
                if(e is VFXEdContextNode)
                {
                    VFXEdContextNode node = (e as VFXEdContextNode);
                    VFXSystemModel sysmodel = node.Model.GetOwner();

                    if (!Systems.Contains(sysmodel))
                    {
                        Systems.Add(sysmodel);
                        VFXEdSpawnTemplate.SysInfo info = new VFXEdSpawnTemplate.SysInfo(sysmodel.SpawnRate, sysmodel.MaxNb, (int)sysmodel.BlendingMode);
                        t.SystemInformation.Add(info);
                    }

                    int index = Systems.IndexOf(sysmodel);

                    t.AddContextNode(node.UniqueName, node.Model.Desc.Name,index);


                    // Context Node Parameters
                    for(int i = 0; i < node.Model.GetNbSlots(); i++)
                    {
                        t.SetContextNodeParameter(node.UniqueName, node.Model.GetSlot(i).Name, (node.Model.GetSlot(i).Value as VFXValue).Clone());
                    }

                    // Context Node Blocks
                    foreach(VFXEdProcessingNodeBlock block in node.NodeBlockContainer.nodeBlocks)
                    {
                        t.AddContextNodeBlock(node.UniqueName, block.UniqueName, block.LibraryName);
                        for (int i = 0; i < block.Model.GetNbSlots(); i++)
                        {
                            t.SetContextNodeBlockParameter(node.UniqueName, block.UniqueName, block.Model.GetSlot(i).Name, (block.Model.GetSlot(i).Value as VFXValue).Clone());
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
                            // ADD ONLY IF NOT INSIDE A EDITINGWIDGET IGNORE LIST
                            if(block.editingWidget != null)
                            {
                                if (!block.editingWidget.IgnoredParamNames.Contains(block.Params[i].m_Name))
                                    t.SetDataNodeBlockParameter(node.UniqueName, block.UniqueName, block.Params[i].m_Name, (block.Slots[i].Value as VFXValue).Clone());
                            }
                            else
                                t.SetDataNodeBlockParameter(node.UniqueName, block.UniqueName, block.Params[i].m_Name, (block.Slots[i].Value as VFXValue).Clone());
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
