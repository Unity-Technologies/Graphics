using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX.UI
{
    class VFXBlockProvider : VFXFilterWindow.IProvider
    {
        VFXContextPresenter m_ContextPresenter;
        AddBlock m_onAddBlock;
        //VFXBlock m_blockModel;

        public class VFXBlockElement : VFXFilterWindow.Element
        {
            public VFXModelDescriptor<VFXBlock> m_BlockDesc;
            public AddBlock m_SpawnCallback;

            internal VFXBlockElement(int level, VFXModelDescriptor<VFXBlock> desc, AddBlock spawncallback)
            {
                this.level = level;
                content = new GUIContent(desc.info.category.Replace("/", " ") + " : " + desc.name/*, VFXEditor.styles.GetIcon(desc.Icon)*/);
                m_BlockDesc = desc;
                m_SpawnCallback = spawncallback;
            }
        }

        public delegate void AddBlock(int index, VFXBlock block);

        internal VFXBlockProvider(/*Vector2 mousePosition, */VFXContextPresenter contextModel, AddBlock onAddBlock)
        {
            //m_mousePosition = mousePosition;
            m_ContextPresenter = contextModel;
            //m_blockModel = null;
            m_onAddBlock = onAddBlock;
        }

        public void CreateComponentTree(List<VFXFilterWindow.Element> tree)
        {
            tree.Add(new VFXFilterWindow.GroupElement(0, "NodeBlocks"));

            var blocks = new List<VFXModelDescriptor<VFXBlock>>(VFXLibrary.GetBlocks());

            var filteredBlocks = blocks.Where(b => b.AcceptParent(m_ContextPresenter.model)).ToList();

            filteredBlocks.Sort((blockA, blockB) => {

                var infoA = blockA.info;
                var infoB = blockB.info;

                int res = infoA.category.CompareTo(infoB.category);
                return res != 0 ? res : blockA.name.CompareTo(blockB.name);
            });

            HashSet<string> categories = new HashSet<string>();

            foreach(var block in filteredBlocks)
            {
                int i = 0;

                var category = block.info.category;

                if (!categories.Contains(category) && category != "")
                {
                    string[] split = category.Split('/');
                    string current = "";

                    while(i < split.Length)
                    {
                        current += split[i];
                        if(!categories.Contains(current))
                            tree.Add(new VFXFilterWindow.GroupElement(i+1,split[i]));
                        i++;
                        current += "/";
                    }
                    categories.Add(category);
                }
                else
                {
                    i = category.Split('/').Length;
                }

                if (category != "")
                    i++;

                tree.Add(new VFXBlockElement(i, block, m_onAddBlock));

            }
        }
        
        public bool GoToChild(VFXFilterWindow.Element element, bool addIfComponent)
        {
            if (element is VFXBlockElement)
            {
                VFXBlockElement blockElem = element as VFXBlockElement;
                
                blockElem.m_SpawnCallback(-1,blockElem.m_BlockDesc.CreateInstance());
                return true;
            }

            return false;
        }
    }
}
