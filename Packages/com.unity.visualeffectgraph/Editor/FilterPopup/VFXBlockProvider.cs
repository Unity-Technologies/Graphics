using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;


namespace UnityEditor.VFX.UI
{
    abstract class VFXAbstractProvider<T> : VFXFilterWindow.IProvider
    {
        readonly Action<T, Vector2> m_onSpawnDesc;

        protected class VFXBlockElement : VFXFilterWindow.Element
        {
            public T descriptor { get; private set; }

            public VFXBlockElement(int level, T desc, string category, string name)
            {
                this.level = level;
                content = new GUIContent(name /*, VFXEditor.styles.GetIcon(desc.Icon)*/);
                descriptor = desc;
            }
        }

        protected VFXAbstractProvider(Action<T, Vector2> onSpawnDesc)
        {
            m_onSpawnDesc = onSpawnDesc;
        }

        protected abstract IEnumerable<T> GetDescriptors();
        protected abstract string GetName(T desc);
        protected abstract string GetCategory(T desc);

        protected abstract string title
        {
            get;
        }

        public void CreateComponentTree(List<VFXFilterWindow.Element> tree)
        {
            tree.Add(new VFXFilterWindow.GroupElement(0, title));
            var descriptors = GetDescriptors();

            var depth = 1;
            string prevCategory = "";
            var prevSplit = new string[0];
            var noCategory = new List<T>();

            foreach (var desc in descriptors)
            {
                var category = GetCategory(desc);
                if (string.IsNullOrEmpty(category))
                {
                    noCategory.Add(desc);
                    continue;
                }

                if (category != prevCategory)
                {
                    var split = category.Split('/').Where(o => o != "").ToArray();

                    for (int i = 0; i < split.Length; i++)
                    {
                        if (i >= prevSplit.Length || (i < prevSplit.Length && split[i] != prevSplit[i]))
                        {
                            depth = i + 1;
                            tree.Add(new VFXFilterWindow.GroupElement(depth, split[i]));
                        }
                    }

                    prevCategory = category;
                    prevSplit = split;
                }

                tree.Add(new VFXBlockElement(depth + 1, desc, category, GetName(desc)));
            }

            noCategory.ForEach(x => tree.Add(new VFXBlockElement(1, x, string.Empty, GetName(x))));
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

    class VFXBlockProvider : VFXAbstractProvider<VFXBlockProvider.Descriptor>
    {
        public abstract class Descriptor
        {
            public abstract string category { get; }
            public abstract string name { get; }
        }

        public class NewBlockDescriptor : Descriptor
        {
            public readonly VFXModelDescriptor<VFXBlock> newBlock;

            public NewBlockDescriptor(VFXModelDescriptor<VFXBlock> newBlock)
            {
                this.newBlock = newBlock;
            }

            public override string category { get { return newBlock.category; } }
            public override string name { get { return newBlock.name; } }
        }

        public class SubgraphBlockDescriptor : Descriptor
        {
            public readonly SubGraphCache.Item item;
            public SubgraphBlockDescriptor(SubGraphCache.Item item)
            {
                this.item = item;
            }

            public override string category { get { return item.category; } }
            public override string name { get { return item.name; } }
        }

        readonly VFXContextController m_ContextController;
        public VFXBlockProvider(VFXContextController context, Action<Descriptor, Vector2> onAddBlock) : base(onAddBlock)
        {
            m_ContextController = context;
        }

        protected override string GetCategory(VFXBlockProvider.Descriptor desc)
        {
            return desc.category;
        }

        protected override string GetName(VFXBlockProvider.Descriptor desc)
        {
            return desc.name;
        }

        protected override string title
        {
            get { return "Block"; }
        }

        protected override IEnumerable<VFXBlockProvider.Descriptor> GetDescriptors()
        {
            return VFXLibrary.GetBlocks()
                .Where(b => b.AcceptParent(m_ContextController.model))
                .Select(t => (Descriptor)new NewBlockDescriptor(t))
                .Concat(SubGraphCache.GetItems(typeof(VisualEffectSubgraphBlock))
                    .Where(t =>
                        (((SubGraphCache.AdditionalBlockInfo)t.additionalInfos).compatibleType &
                            m_ContextController.model.contextType) != 0 &&
                        (((SubGraphCache.AdditionalBlockInfo)t.additionalInfos).compatibleData &
                            m_ContextController.model.ownedType) != 0)
                    .Select(t => (Descriptor)new SubgraphBlockDescriptor(t)))
                .OrderBy(x => x.category)
                .ThenBy(x => x.name);
        }
    }
}
