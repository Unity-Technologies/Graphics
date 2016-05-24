using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEditor.Experimental.VFX;


namespace UnityEditor.Experimental
{
    public class VFXDataBlockProvider : IProvider
    {
        Vector2 m_mousePosition;
        VFXDataNodeModel m_dataNodeModel;
        VFXEdDataSource m_dataSource;

        public class VFXDataBlockElement : VFXFilterWindow.Element
        {
            public VFXDataBlockDesc m_Desc;
            public Action<VFXDataBlockElement> m_SpawnCallback;

            public VFXDataBlockElement(int level, VFXDataBlockDesc desc, Action<VFXDataBlockElement> spawncallback)
            {
                this.level = level;
                content = new GUIContent(desc.Category.Replace("/"," ")+" : " + desc.Name, VFXEditor.styles.GetIcon(desc.Icon));
                m_Desc = desc;
                m_SpawnCallback = spawncallback;
            }
        }


        internal VFXDataBlockProvider(Vector2 mousePosition, VFXDataNodeModel model, VFXEdDataSource dataSource)
        {
            m_mousePosition = mousePosition;
            m_dataNodeModel = model;
            m_dataSource = dataSource;
        }

        public void CreateComponentTree(List<VFXFilterWindow.Element> tree)
        {
            tree.Add(new VFXFilterWindow.GroupElement(0, "Parameter Blocks"));

            var blocks = new List<VFXDataBlockDesc>(VFXEditor.BlockLibrary.GetDataBlocks());
            blocks.Sort((blockA, blockB) => {
                int res = blockA.Category.CompareTo(blockB.Category);
                return res != 0 ? res : blockA.Name.CompareTo(blockB.Name);
            });

            HashSet<string> categories = new HashSet<string>();

            foreach(VFXDataBlockDesc desc in blocks)
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

                tree.Add(new VFXDataBlockElement(i, desc, SpawnDataBlock));

            }
        }

        public void SpawnDataBlock(VFXDataBlockElement block)
        {
            int index = m_dataSource.GetUI<VFXEdDataNode>(m_dataNodeModel).NodeBlockContainer.GetDropIndex(m_mousePosition);
            m_dataSource.Create(new VFXDataBlockModel(block.m_Desc), m_dataNodeModel, index);
        }

        public bool GoToChild(VFXFilterWindow.Element element, bool addIfComponent)
        {
            if (element is VFXDataBlockElement)
            {
                ((VFXDataBlockElement)element).m_SpawnCallback.Invoke((VFXDataBlockElement)element);
                return true;
            }

            return false;
        }
    }
}
