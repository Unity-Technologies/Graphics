using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental
{
    public class VFXBlockProvider : IProvider
    {
        Vector2 m_mousePosition;
        VFXContextModel m_contextModel;
        VFXEdDataSource m_dataSource;
        VFXBlockModel m_blockModel;

        public class VFXBlockElement : VFXFilterWindow.Element
        {
            public VFXBlockDesc m_Desc;
            public Action<VFXBlockElement> m_SpawnCallback;

            public VFXBlockElement(int level, VFXBlockDesc desc, Action<VFXBlockElement> spawncallback)
            {
                this.level = level;
                content = new GUIContent(desc.Category.Replace("/"," ")+" : " + desc.Name, VFXEditor.styles.GetIcon(desc.Icon));
                m_Desc = desc;
                m_SpawnCallback = spawncallback;
            }
        }

        internal VFXBlockProvider(Vector2 mousePosition, VFXContextModel contextModel, VFXEdDataSource dataSource)
        {
            m_mousePosition = mousePosition;
            m_contextModel = contextModel;
            m_blockModel = null;
            m_dataSource = dataSource;
        }

        internal VFXBlockProvider(Vector2 mousePosition, VFXContextModel contextModel, VFXBlockModel blockModel, VFXEdDataSource dataSource)
        {
            m_mousePosition = mousePosition;
            m_contextModel = contextModel;
            m_blockModel = blockModel;
            m_dataSource = dataSource;
            
        }

        public void CreateComponentTree(List<VFXFilterWindow.Element> tree)
        {
            tree.Add(new VFXFilterWindow.GroupElement(0, "NodeBlocks"));

            var blocks = new List<VFXBlockDesc>(VFXEditor.BlockLibrary.GetBlocks());
            blocks.Sort((blockA, blockB) => {
                int res = blockA.Category.CompareTo(blockB.Category);
                return res != 0 ? res : blockA.Name.CompareTo(blockB.Name);
            });


            HashSet<string> categories = new HashSet<string>();

            foreach(VFXBlockDesc desc in blocks)
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

                tree.Add(new VFXBlockElement(i, desc, SpawnBlock));

            }
        }

        public void SpawnBlock(VFXBlockElement block)
        {
            int index;
            if(m_blockModel != null)
            {
                index = m_blockModel.GetOwner().GetIndex(m_blockModel);
                m_dataSource.Remove(m_blockModel);
            }
            else
            {
                index = m_dataSource.GetUI<VFXEdContextNode>(m_contextModel).NodeBlockContainer.GetDropIndex(m_mousePosition);
            }

            m_dataSource.Create(new VFXBlockModel(block.m_Desc), m_contextModel, index);
        }

        public bool GoToChild(VFXFilterWindow.Element element, bool addIfComponent)
        {
            if (element is VFXBlockElement)
            {
                ((VFXBlockElement)element).m_SpawnCallback.Invoke((VFXBlockElement)element);
                return true;
            }

            return false;
        }
    }
}
