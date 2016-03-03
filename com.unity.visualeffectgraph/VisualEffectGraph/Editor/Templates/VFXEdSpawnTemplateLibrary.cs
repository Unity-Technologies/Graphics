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

        public void ReloadLibrary()
        {
            Templates.Clear();
            string path = Application.dataPath + LibraryPath;
            XDocument doc = XDocument.Load(path);
            XElement lib = doc.Element("Library");
            var templates = lib.Elements("Template");
            foreach(XElement t in templates)
            {
                VFXEdSpawnTemplate template = VFXEdSpawnTemplate.Create(t.Attribute("Category").Value, t.Attribute("Name").Value);
                var nodes = t.Element("Nodes").Elements("Node");
                var flowconnections = t.Element("Connections").Elements("FlowConnection");

                foreach(XElement n in nodes)
                {
                    VFXEdContext c = VFXEdContext.None;
                    switch(n.Attribute("Context").Value)
                    {
                        case "Initialize" : c = VFXEdContext.Initialize; break;
                        case "Update" : c = VFXEdContext.Update; break;
                        case "Output" : c = VFXEdContext.Output; break;
                        case "Trigger" : c = VFXEdContext.Trigger; break;
                        case "None" :
                        default:
                            c = VFXEdContext.None; break;
                    }
                    template.AddNode(n.Attribute("Name").Value, c);

                    foreach(XElement nb in n.Elements("NodeBlock"))
                    {
                        template.AddNodeBlock(n.Attribute("Name").Value, nb.Attribute("Name").Value);

                        foreach(XElement parm in nb.Elements("VFXParamValue"))
                        {
                            string nn = n.Attribute("Name").Value;
                            string nbn = nb.Attribute("Name").Value;
                            string pn = parm.Attribute("Name").Value;
                            string[] vals;
                            switch(parm.Attribute("Type").Value)
                            {
                                case "kTypeFloat":
                                    template.SetNodeBlockParameter(nn, nbn, pn, VFXParamValue.Create(float.Parse(parm.Attribute("Value").Value)));
                                    break;
                                case "kTypeInt":
                                    template.SetNodeBlockParameter(nn, nbn, pn, VFXParamValue.Create(int.Parse(parm.Attribute("Value").Value)));
                                    break;
                                case "kTypeUint":
                                    template.SetNodeBlockParameter(nn, nbn, pn, VFXParamValue.Create(uint.Parse(parm.Attribute("Value").Value)));
                                    break;
                                case "kTypeFloat2":
                                    vals = parm.Attribute("Value").Value.Split(',');
                                    Vector2 v2 = new Vector2(float.Parse(vals[0]), float.Parse(vals[1]));
                                    template.SetNodeBlockParameter(nn, nbn, pn, VFXParamValue.Create(v2));
                                    break;
                                case "kTypeFloat3":
                                    vals = parm.Attribute("Value").Value.Split(',');
                                    Vector3 v3 = new Vector3(float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[2]));
                                    template.SetNodeBlockParameter(nn, nbn, pn, VFXParamValue.Create(v3));
                                    break;
                                case "kTypeFloat4":
                                    vals = parm.Attribute("Value").Value.Split(',');
                                    Vector4 v4 = new Vector4(float.Parse(vals[0]), float.Parse(vals[1]), float.Parse(vals[2]), float.Parse(vals[3]));
                                    template.SetNodeBlockParameter(nn, nbn, pn, VFXParamValue.Create(v4));
                                    break;
                                case "kTypeTexture2D":
                                    template.SetNodeBlockParameter(nn, nbn, pn, VFXParamValue.Create(AssetDatabase.LoadAssetAtPath<Texture2D>(parm.Attribute("Value").Value)));
                                    break;
                                case "kTypeTexture3D":
                                    template.SetNodeBlockParameter(nn, nbn, pn, VFXParamValue.Create(AssetDatabase.LoadAssetAtPath<Texture3D>(parm.Attribute("Value").Value)));
                                    break;
                                default:
                                    break;
                            }
                            
                        }
                    }

                }

                foreach(XElement fc in flowconnections)
                {
                    template.AddConnection(fc.Attribute("Previous").Value, fc.Attribute("Next").Value);
                }

                AddTemplate(template);
            }


            Debug.Log("OK! Reloaded!");
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

                doc.WriteStartElement("Nodes");
                foreach(KeyValuePair<string, NodeInfo> kvp_node in template.Nodes)
                {
                    doc.WriteStartElement("Node");
                    doc.WriteAttributeString("Name", kvp_node.Key);
                    doc.WriteAttributeString("Context", kvp_node.Value.Context.ToString());

                    foreach(KeyValuePair<string, NodeBlockInfo> kvp_nodeblock in kvp_node.Value.nodeBlocks)
                    {
                        doc.WriteStartElement("NodeBlock");
                        doc.WriteAttributeString("Name", kvp_nodeblock.Key);
                        doc.WriteAttributeString("BlockName", kvp_nodeblock.Value.BlockName);
                        foreach(KeyValuePair<string, VFXParamValue> kvp_param in kvp_nodeblock.Value.ParameterOverrides)
                        {
                            doc.WriteStartElement("VFXParamValue");
                            doc.WriteAttributeString("Name", kvp_param.Key);
                            VFXParam.Type type = kvp_param.Value.ValueType;
                            doc.WriteAttributeString("Type", type.ToString());

                            string value = "";
                            switch(type)
                            {
                                case VFXParam.Type.kTypeFloat: value = kvp_param.Value.GetValue<float>().ToString(); break;
                                case VFXParam.Type.kTypeInt: value = kvp_param.Value.GetValue<int>().ToString(); break;
                                case VFXParam.Type.kTypeUint: value = kvp_param.Value.GetValue<uint>().ToString(); break;
                                case VFXParam.Type.kTypeFloat2:
                                    Vector2 v2 = kvp_param.Value.GetValue<Vector2>();
                                    value = v2.x + "," + v2.y ;
                                    break;
                                case VFXParam.Type.kTypeFloat3:
                                    Vector3 v3 = kvp_param.Value.GetValue<Vector3>();
                                    value = v3.x + "," + v3.y+ "," + v3.z ;
                                    break;
                                case VFXParam.Type.kTypeFloat4:
                                    Vector4 v4 = kvp_param.Value.GetValue<Vector4>();
                                    value = v4.x + "," + v4.y + "," + v4.z+ "," + v4.w;
                                    break;
                                case VFXParam.Type.kTypeTexture2D:
                                    Texture2D t = kvp_param.Value.GetValue<Texture2D>();
                                    value = AssetDatabase.GetAssetPath(t) ;
                                    break;
                                case VFXParam.Type.kTypeTexture3D:
                                    Texture3D t3 = kvp_param.Value.GetValue<Texture3D>();
                                    value = AssetDatabase.GetAssetPath(t3) ;
                                    break;
                                default:
                                    break;
                            }
                            doc.WriteAttributeString("Value", value);
                            doc.WriteEndElement();
                            
                        }
                        
                        doc.WriteEndElement(); // End NodeBlock
                    }

                    doc.WriteEndElement(); // End Node
                }
                doc.WriteEndElement(); // End Nodes

                doc.WriteStartElement("Connections");
                foreach(FlowConnection c in template.Connections)
                {
                    doc.WriteStartElement("FlowConnection");
                    foreach(KeyValuePair<string,NodeInfo> kvp_node in template.Nodes )
                    {
                        if(kvp_node.Value == c.Previous) doc.WriteAttributeString("Previous", kvp_node.Key);
                        if(kvp_node.Value == c.Next) doc.WriteAttributeString("Next", kvp_node.Key);
                    }
                    
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
            Initialize();
            WriteLibrary();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        internal static VFXEdSpawnTemplate CreateTemplateFromSelection(VFXEdCanvas canvas, string category, string name)
        {
            VFXEdSpawnTemplate t = VFXEdSpawnTemplate.Create(category, name);
            
            foreach(CanvasElement e in canvas.selection)
            {
                if(e is VFXEdContextNode)
                {
                    VFXEdContextNode node = (e as VFXEdContextNode);
                    t.AddNode(node.UniqueName, node.context);
                    foreach(VFXEdProcessingNodeBlock block in node.NodeBlockContainer.nodeBlocks)
                    {
                        t.AddNodeBlock(node.UniqueName, block.name);
                        for (int i = 0 ;  i < block.Params.Length; i++)
                        {
                            t.SetNodeBlockParameter(node.UniqueName, block.name, block.Params[i].m_Name, block.ParamValues[i].Clone());
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
                        t.AddConnection(left, right);
                    }

                }
            }
            return t;
        }

        public void Initialize()
        {
            VFXEdSpawnTemplate fulltemplate = VFXEdSpawnTemplate.Create("Full", "Full Template");
            fulltemplate.AddNode("init", VFXEdContext.Initialize);
            fulltemplate.AddNode("update", VFXEdContext.Update);
            fulltemplate.AddNode("output", VFXEdContext.Output);

            fulltemplate.AddNodeBlock("init", "Set Lifetime (Random)");
            fulltemplate.AddNodeBlock("init", "Set Velocity (Spherical)");
            fulltemplate.AddNodeBlock("init", "Add Velocity (Constant)");
            fulltemplate.AddNodeBlock("init", "Set Size Constant (Square)");

            fulltemplate.AddNodeBlock("update", "Color Over Lifetime");
            fulltemplate.AddNodeBlock("update", "Apply Force");
            fulltemplate.AddNodeBlock("update", "Apply Drag");
            fulltemplate.AddNodeBlock("update", "Collision with Plane");
            fulltemplate.AddNodeBlock("update", "Age and Reap");
            fulltemplate.AddNodeBlock("update", "Apply Velocity to Positions");
            
            fulltemplate.SetNodeBlockParameter("init","Set Lifetime (Random)","minLifetime", VFXParamValue.Create(4.0f));
            fulltemplate.SetNodeBlockParameter("init","Set Lifetime (Random)","maxLifetime", VFXParamValue.Create(5.5f));
            fulltemplate.SetNodeBlockParameter("init","Set Velocity (Spherical)","angle", VFXParamValue.Create(new Vector2(80.0f,80.0f)));
            fulltemplate.SetNodeBlockParameter("init","Set Velocity (Spherical)","speed", VFXParamValue.Create(new Vector2(1.0f,1.0f)));
            fulltemplate.SetNodeBlockParameter("init","Add Velocity (Constant)","value", VFXParamValue.Create(new Vector3(50.0f,0.0f,0.0f)));
            fulltemplate.SetNodeBlockParameter("init","Set Size Constant (Square)","value", VFXParamValue.Create(1.5f));
            
            fulltemplate.SetNodeBlockParameter("update","Color Over Lifetime","start", VFXParamValue.Create(new Vector3(1.0f,0.0f,1.0f)));
            fulltemplate.SetNodeBlockParameter("update","Color Over Lifetime","end", VFXParamValue.Create(new Vector3(0.0f,1.0f,1.0f)));
            fulltemplate.SetNodeBlockParameter("update","Apply Force","force", VFXParamValue.Create(new Vector3(0.0f,-5.0f,0.0f)));
            fulltemplate.SetNodeBlockParameter("update","Apply Drag","multiplier", VFXParamValue.Create(0.02f));
            fulltemplate.SetNodeBlockParameter("update","Collision with Plane","normal", VFXParamValue.Create(new Vector3(0.0f,1.0f,0.0f)));
            fulltemplate.SetNodeBlockParameter("update","Collision with Plane","center", VFXParamValue.Create(new Vector3(0.0f,-1.0f,0.0f)));
            fulltemplate.SetNodeBlockParameter("update","Collision with Plane","elasticity", VFXParamValue.Create(0.95f));
            

            fulltemplate.AddConnection("init", "update");
            fulltemplate.AddConnection("update", "output");

            m_Templates.Add(fulltemplate);


            VFXEdSpawnTemplate init = VFXEdSpawnTemplate.Create("Simple", "Initialize");
            init.AddNode("init", VFXEdContext.Initialize);
            init.AddNodeBlock("init", "Set Lifetime (Constant)");
            init.AddNodeBlock("init", "Set Color (Constant)");
            init.AddNodeBlock("init", "Set Position (Point)");
            init.AddNodeBlock("init", "Set Velocity (Constant)");
            init.AddNodeBlock("init", "Set Size Constant (Square)");
            m_Templates.Add(init);


            VFXEdSpawnTemplate update = VFXEdSpawnTemplate.Create("Simple", "Update");
            update.AddNode("update", VFXEdContext.Update);
            update.AddNodeBlock("update", "Apply Force");
            update.AddNodeBlock("update", "Apply Drag");
            update.AddNodeBlock("update", "Age and Reap");
            update.AddNodeBlock("update", "Apply Velocity to Positions");
            m_Templates.Add(update);



            VFXEdSpawnTemplate output = VFXEdSpawnTemplate.Create("Simple", "Output");
            output.AddNode("output", VFXEdContext.Output);

            m_Templates.Add(output);
        }
    }
}
