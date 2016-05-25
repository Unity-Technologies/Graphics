using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;


namespace UnityEditor.Experimental
{
    public class VFXDataNodeProvider : IProvider
    {
        Vector2 m_mousePosition;
        VFXEdContextNode m_contextNode;
        VFXEdDataSource m_dataSource;
        VFXEdCanvas m_canvas;

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


        internal VFXDataNodeProvider(Vector2 mousePosition, VFXEdDataSource dataSource, VFXEdCanvas canvas)
        {
            m_mousePosition = mousePosition;
            m_dataSource = dataSource;
            m_canvas = canvas;
        }

        public void CreateComponentTree(List<VFXFilterWindow.Element> tree)
        {
            tree.Add(new VFXFilterWindow.GroupElement(0, "Parameter Nodes"));

            tree.Add(new VFXDataNodeElement(1, "Add Empty Parameter Node", SpawnDataNode));

            HashSet<string> categories = new HashSet<string>();
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
                            tree.Add(new VFXFilterWindow.GroupElement(i+1,split[i]));
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

                tree.Add(new VFXDataNodeElement(i, desc, SpawnDataNode));

            }

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
        
        public bool GoToChild(VFXFilterWindow.Element element, bool addIfComponent)
        {
            if(element is VFXDataNodeElement)
            {
                ((VFXDataNodeElement)element).m_SpawnCallback.Invoke((VFXDataNodeElement)element);
                return true;
            }

            return false;
        }
    }
}
