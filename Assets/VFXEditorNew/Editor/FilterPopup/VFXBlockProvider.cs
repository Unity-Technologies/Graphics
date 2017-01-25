using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.UI
{
    public class VFXBlockProvider : VFXFilterWindow.IProvider
    {
        Vector2 m_mousePosition;
        VFXContextPresenter m_ContextPresenter;
        AddNodeBlock m_onAddNodeBlock;
        //VFXBlock m_blockModel;

        public class VFXBlockElement : VFXFilterWindow.Element
        {
            public VFXBlockDesc m_Desc;
            public AddNodeBlock m_SpawnCallback;

            internal VFXBlockElement(int level, VFXBlockDesc desc, AddNodeBlock spawncallback)
            {
                this.level = level;
                content = new GUIContent(desc.Category.Replace("/"," ")+" : " + desc.Name/*, VFXEditor.styles.GetIcon(desc.Icon)*/);
                m_Desc = desc;
                m_SpawnCallback = spawncallback;
            }
        }

        public delegate void AddNodeBlock(int index, VFXBlockDesc desc);



        internal VFXBlockProvider(/*Vector2 mousePosition, */VFXContextPresenter contextModel, AddNodeBlock onAddNodeBlock)
        {
            //m_mousePosition = mousePosition;
            m_ContextPresenter = contextModel;
            //m_blockModel = null;
            m_onAddNodeBlock = onAddNodeBlock;
        }

        public void CreateComponentTree(List<VFXFilterWindow.Element> tree)
        {
            tree.Add(new VFXFilterWindow.GroupElement(0, "NodeBlocks"));

            var blocks = new List<VFXBlockDesc>(VFXLibrary.GetBlocks());

            var filteredBlocks = blocks.Where(b => m_ContextPresenter.Model.Accept(b)).ToList();

            filteredBlocks.Sort((blockA, blockB) => {
                int res = blockA.Category.CompareTo(blockB.Category);
                return res != 0 ? res : blockA.Name.CompareTo(blockB.Name);
            });

            HashSet<string> categories = new HashSet<string>();

            foreach(VFXBlockDesc desc in filteredBlocks)
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

                tree.Add(new VFXBlockElement(i, desc, m_onAddNodeBlock));

            }
        }
        /*
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
        */
        public bool GoToChild(VFXFilterWindow.Element element, bool addIfComponent)
        {
            if (element is VFXBlockElement)
            {
                VFXBlockElement blockElem = element as VFXBlockElement;
                
                blockElem.m_SpawnCallback(-1,blockElem.m_Desc);
                return true;
            }

            return false;
        }
    }
}
