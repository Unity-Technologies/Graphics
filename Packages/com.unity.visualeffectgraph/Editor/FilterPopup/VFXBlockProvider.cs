using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor.VFX.Block;
using UnityEngine;


namespace UnityEditor.VFX.UI
{
    abstract class VFXAbstractProvider<T> : VFXFilterWindow.IProvider where T : VFXModel
    {
        readonly Action<Variant, Vector2> m_onSpawnDesc;

        protected VFXAbstractProvider(Action<Variant, Vector2> onSpawnDesc)
        {
            m_onSpawnDesc = onSpawnDesc;
        }

        public Vector2 position { get; set; }

        public abstract IEnumerable<IVFXModelDescriptor> GetDescriptors();
        public void AddNode(Variant descriptor)
        {
            m_onSpawnDesc(descriptor, position);
        }
    }

    class VFXBlockProvider : VFXAbstractProvider<VFXBlock>
    {
        readonly VFXContextController m_ContextController;

        public VFXBlockProvider(VFXContextController context, Action<Variant, Vector2> onAddBlock) : base(onAddBlock)
        {
            m_ContextController = context;
        }

        public override IEnumerable<IVFXModelDescriptor> GetDescriptors()
        {
            foreach (var descriptor in VFXLibrary.GetBlocks())
            {
                if (m_ContextController.model.AcceptChild(descriptor.model))
                {
                    yield return descriptor;
                }
            }

            foreach (var customAttribute in m_ContextController.model.GetGraph().attributesManager.GetCustomAttributes())
            {
                yield return new VFXModelDescriptor<VFXBlock>(new Variant(
                    $"Set {customAttribute.name}",
                    "Attribute/".AppendSeparator("Custom", 0),
                    typeof(SetAttribute),
                    new[] { new KeyValuePair<string, object>(nameof(SetAttribute.attribute), customAttribute.name) }), null);
            }

            foreach (var item in SubGraphCache.GetItems(typeof(VisualEffectSubgraphBlock)))
            {
                if (((SubGraphCache.AdditionalBlockInfo)item.additionalInfos).compatibleType.HasFlag(m_ContextController.model.contextType)
                    && ((SubGraphCache.AdditionalBlockInfo)item.additionalInfos).compatibleData.HasFlag(m_ContextController.model.ownedType))
                {
                    var variant = new SubgraphVariant(
                        item.name,
                        item.category,
                        typeof(VisualEffectSubgraphBlock), new[] { new KeyValuePair<string, object>("path", item.path) },
                        item.guid);
                    yield return new VFXModelDescriptor<VFXBlock>(variant, null);
                }
            }
        }
    }
}
