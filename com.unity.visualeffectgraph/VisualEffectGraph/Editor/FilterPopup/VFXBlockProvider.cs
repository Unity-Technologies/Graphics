using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental;

namespace UnityEditor.VFX.UI
{
    abstract class VFXAbstractProvider<T> : VFXFilterWindow.IProvider
    {
        Action<T, Vector2> m_onSpawnDesc;

        protected class VFXBlockElement : VFXFilterWindow.Element
        {
            public T descriptor { get; private set; }

            public VFXBlockElement(int level, T desc, string category, string name)
            {
                this.level = level;
                content = new GUIContent(category.Replace("/", " ") + " : " + name /*, VFXEditor.styles.GetIcon(desc.Icon)*/);
                descriptor = desc;
            }
        }


        protected VFXAbstractProvider(Action<T, Vector2> onSpawnDesc) : base(null)
        {
            m_onSpawnDesc = onSpawnDesc;
        }

        protected abstract IEnumerable<T> GetDescriptors();
        protected abstract string GetName(T desc);
        protected abstract string GetCategory(T desc);

        public void CreateComponentTree(List<VFXFilterWindow.Element> tree)
        {
            tree.Add(new VFXFilterWindow.GroupElement(0, "Node"));
            var descriptors = GetDescriptors();

            var categories = new HashSet<string>();
            foreach (var desc in descriptors)
            {
                int depth = 0;
                var category = GetCategory(desc);
                if (!string.IsNullOrEmpty(category))
                {
                    var split = category.Split('/').Where(o => o != "").ToArray();
                    if (!categories.Contains(category))
                    {
                        var current = "";
                        while (depth < split.Length)
                        {
                            current += split[depth];
                            if (!categories.Contains(current))
                                tree.Add(new VFXFilterWindow.GroupElement(depth + 1, split[depth]));
                            depth++;
                            current += "/";
                        }
                        categories.Add(category);
                    }
                    else
                    {
                        depth = split.Length;
                    }
                    depth++;
                }

                tree.Add(new VFXBlockElement(depth, desc, category, GetName(desc)));
            }
        }

        public bool GoToChild(VFXFilterWindow.Element element, bool addIfComponent)
        {
            if (element is VFXBlockElement)
            {
                var blockElem = element as VFXBlockElement;
                m_onSpawnDesc(blockElem.descriptor, position);
                return true;
            }
            return false;
        }

        public Vector2 position
        {
            get; set;
        }
    }

    class VFXBlockProvider : VFXAbstractProvider<VFXModelDescriptor<VFXBlock>>
    {
        VFXContextPresenter m_ContextPresenter;
        public VFXBlockProvider(VFXContextPresenter context, Action<VFXModelDescriptor<VFXBlock>, Vector2> onAddBlock) : base(onAddBlock)
        {
            m_ContextPresenter = context;
        }

        protected override string GetCategory(VFXModelDescriptor<VFXBlock> desc)
        {
            return desc.name;
        }

        protected override string GetName(VFXModelDescriptor<VFXBlock> desc)
        {
            return desc.info.category;
        }

        protected override IEnumerable<VFXModelDescriptor<VFXBlock>> GetDescriptors()
        {
            var blocks = new List<VFXModelDescriptor<VFXBlock>>(VFXLibrary.GetBlocks());
            var filteredBlocks = blocks.Where(b => b.AcceptParent(m_ContextPresenter.model)).ToList();
            filteredBlocks.Sort((blockA, blockB) =>
                {
                    var infoA = blockA.info;
                    var infoB = blockB.info;
                    int res = infoA.category.CompareTo(infoB.category);
                    return res != 0 ? res : blockA.name.CompareTo(blockB.name);
                });
            return filteredBlocks;
        }
    }
}
