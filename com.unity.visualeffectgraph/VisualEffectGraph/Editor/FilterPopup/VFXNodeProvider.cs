using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;


namespace UnityEditor.Experimental
{
    public class VFXNodeProvider : IProvider
    {
        Vector2 m_mousePosition;
        VFXEdContextNode m_contextNode;
        VFXEdDataSource m_dataSource;
        VFXEdCanvas m_canvas;

        public class VFXNodeElement : VFXFilterWindow.Element
        {
            public VFXContextDesc m_Desc;
            public Action<VFXNodeElement> m_SpawnCallback;

            public VFXNodeElement(int level, VFXContextDesc desc, Action<VFXNodeElement> spawncallback)
            {
                this.level = level;
                content = new GUIContent(VFXContextDesc.GetTypeName(desc.m_Type) + " : " + desc.Name);
                m_Desc = desc;
                m_SpawnCallback = spawncallback;
            }
        }

        public class VFXDataNodeElement : VFXFilterWindow.Element
        {
            public VFXDataBlockDesc m_Desc;
            public Action<VFXDataNodeElement> m_SpawnCallback;

            public VFXDataNodeElement(int level, string label, Action<VFXDataNodeElement> spawncallback)
            {
                this.level = level;
                content = new GUIContent(label, VFXEditor.styles.GetIcon("Config"));
                m_Desc = null;
                m_SpawnCallback = spawncallback;
            }

            public VFXDataNodeElement(int level, VFXDataBlockDesc desc, Action<VFXDataNodeElement> spawncallback)
            {
                this.level = level;
                content = new GUIContent(desc.Name,VFXEditor.styles.GetIcon(desc.Icon));
                m_Desc = desc;
                m_SpawnCallback = spawncallback;
            }
        }

        public class VFXNodeSetElement : VFXFilterWindow.Element
        {
            public VFXContextDesc m_Desc;
            public string[] m_DescNames;
            public Action<VFXNodeSetElement> m_SpawnCallback;

            public VFXNodeSetElement(int level, string label, string[] descNames, Action<VFXNodeSetElement> spawncallback)
            {
                this.level = level;
                content = new GUIContent(label, VFXEditor.styles.GetIcon("Effect"));
                m_DescNames = descNames;
                m_SpawnCallback = spawncallback;
            }
        }

        internal VFXNodeProvider(Vector2 mousePosition, VFXEdDataSource dataSource, VFXEdCanvas canvas)
        {
            m_mousePosition = mousePosition;
            m_dataSource = dataSource;
            m_canvas = canvas;
        }

        public void CreateComponentTree(List<VFXFilterWindow.Element> tree)
        {
            tree.Add(new VFXFilterWindow.GroupElement(0, "Nodes"));

            tree.Add(new VFXNodeSetElement(1, "Add Particle System", new string[] {"Initialize","Particle Update", "Billboard Output" }, SpawnNodeSet ));
            tree.Add(new VFXDataNodeElement(1, "Add Parameter Node", SpawnDataNode));

            //tree.Add(new VFXFilterWindow.GroupElement(1, "Events"));
            // TODO: Add Events here

            //tree.Add(new VFXFilterWindow.GroupElement(1, "Triggers"));
            // TODO : Add Triggers here

            tree.Add(new VFXFilterWindow.GroupElement(1, "Add Context Nodes..."));

            var contexts = new List<VFXContextDesc>(VFXEditor.ContextLibrary.GetContexts());
            contexts.Sort((blockA, blockB) => {
                int res = blockA.m_Type.CompareTo(blockB.m_Type);
                return res != 0 ? res : blockA.Name.CompareTo(blockB.Name);
            });

            HashSet<string> categories = new HashSet<string>();

            foreach(VFXContextDesc desc in contexts)
            {

                if(!categories.Contains(desc.m_Type.ToString()))
                {
                    categories.Add(desc.m_Type.ToString());
                    tree.Add(new VFXFilterWindow.GroupElement(2, VFXContextDesc.GetTypeName(desc.m_Type)));
                }

                tree.Add(new VFXNodeElement(3, desc, SpawnNode));
            }

            tree.Add(new VFXFilterWindow.GroupElement(1, "Add Parameter Node..."));
            categories.Clear();
            var dataBlocks = new List<VFXDataBlockDesc>(VFXEditor.BlockLibrary.GetDataBlocks());
            dataBlocks.Sort((blockA, blockB) => {
                int res = blockA.Category.CompareTo(blockB.Category);
                return res != 0 ? res : blockA.Name.CompareTo(blockB.Name);
            });

            foreach(VFXDataBlockDesc desc in dataBlocks)
            {
                int i = 0; 

                if(!categories.Contains(desc.Category) && desc.Category != "")
                {
                    string[] split = desc.Category.Split('/');
                    string current = "";

                    while(i < split.Length)
                    {
                        current += split[i];
                        if(!categories.Contains(current))
                            tree.Add(new VFXFilterWindow.GroupElement(i+2,split[i]));
                        i++;
                        current += "/";
                    }
                    categories.Add(desc.Category);
                }
                else
                {
                    i = desc.Category.Split('/').Length;
                }

                if (desc.Category != "")
                    i++;

                tree.Add(new VFXDataNodeElement(i+1, desc, SpawnDataNode));

            }

        }

        public void SpawnNode(VFXNodeElement node)
        { 
            VFXContextModel model = m_dataSource.CreateContext(node.m_Desc, m_canvas.MouseToCanvas(m_mousePosition)- new Vector2(VFXEditorMetrics.NodeDefaultWidth / 2, -20));

            if(model != null)
                m_canvas.ReloadData();
        }

        public void SpawnDataNode(VFXDataNodeElement node)
        {
            VFXDataNodeModel model = m_dataSource.CreateDataNode(m_canvas.MouseToCanvas(m_mousePosition));
            if(node.m_Desc != null)
            {
                var blockModel = new VFXDataBlockModel(node.m_Desc);
                m_dataSource.Create(blockModel, model);
            }

            if(model != null)
                m_canvas.ReloadData();

        }

        public void SpawnNodeSet(VFXNodeSetElement nodeset)
        {
            List<VFXContextModel> spawnedModels = new List<VFXContextModel>();

            for(int i = 0; i < nodeset.m_DescNames.Length; i++)
            {
                VFXContextModel model = m_dataSource.CreateContext(VFXEditor.ContextLibrary.GetContext(nodeset.m_DescNames[i]), m_canvas.MouseToCanvas(m_mousePosition));

                if(model != null)
                    spawnedModels.Add(model);
            }

            if(spawnedModels.Count > 1)
            {
                for(int i = 0; i < spawnedModels.Count -1 ; i++)
                {
                    m_dataSource.ConnectContext(spawnedModels[i], spawnedModels[i + 1]);
                }
            }
            
            VFXEdLayoutUtility.LayoutSystem(spawnedModels[0].GetOwner(), m_dataSource, m_canvas.MouseToCanvas(m_mousePosition) - new Vector2(VFXEditorMetrics.NodeDefaultWidth / 2, -20));

            m_canvas.ReloadData();
        }

        public bool GoToChild(VFXFilterWindow.Element element, bool addIfComponent)
        {
            if (element is VFXNodeElement)
            {
                ((VFXNodeElement)element).m_SpawnCallback.Invoke((VFXNodeElement)element);
                return true;
            }
            else if (element is VFXNodeSetElement)
            {
                ((VFXNodeSetElement)element).m_SpawnCallback.Invoke((VFXNodeSetElement)element);
                return true;
            }
            else if(element is VFXDataNodeElement)
            {
                ((VFXDataNodeElement)element).m_SpawnCallback.Invoke((VFXDataNodeElement)element);
                return true;
            }

            return false;
        }
    }
}
