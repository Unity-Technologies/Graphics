using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.VFX;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    abstract class SubGraphCache
    {
        string m_Filter;
        public struct Item
        {
            public string category;
            public string name;
            public string path;
            public string guid;
            public object additionalInfos;
        }

        public struct AdditionalBlockInfo
        {
            public VFXContextType compatibleType;
            public VFXDataType compatibleData;
        }

        protected List<Item> m_Items = new List<Item>();

        private IEnumerable<Item> items
        {
            get
            {
                UpdateCache();
                return m_Items;
            }
        }

        protected abstract void UpdateCache();

        static readonly Dictionary<Type, SubGraphCache> s_Caches = new()
        {
            { typeof(VisualEffectAsset), new SubGraphCache<VisualEffectAsset>()},
            { typeof(VisualEffectSubgraphBlock), new SubGraphCache<VisualEffectSubgraphBlock>()},
            { typeof(VisualEffectSubgraphOperator), new SubGraphCache<VisualEffectSubgraphOperator>()},
        };

        public static IEnumerable<Item> GetItems(Type type)
        {
            return s_Caches.TryGetValue(type, out var cache) ? cache.items : Enumerable.Empty<Item>();
        }
    }
    class SubGraphCache<T> : SubGraphCache where T : VisualEffectObject
    {
        protected override void UpdateCache()
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
            m_Items.Clear();
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith(VisualEffectAssetEditorUtility.templatePath))
                {
                    T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (asset != null)
                    {
                        VisualEffectResource res = asset.GetResource();

                        Item item = new Item { name = asset.name, category = res.GetOrCreateGraph().categoryPath, path = path, guid = guid};
                        if (item.category == null)
                            item.category = "";

                        if (typeof(T) == typeof(VisualEffectSubgraphBlock))
                        {
                            VFXBlockSubgraphContext blockContext = asset.GetResource().GetOrCreateGraph().children.OfType<VFXBlockSubgraphContext>().FirstOrDefault();

                            if (blockContext != null)
                            {
                                item.additionalInfos = new AdditionalBlockInfo { compatibleType = blockContext.compatibleContextType, compatibleData = blockContext.ownedType };
                                m_Items.Add(item);
                            }
                        }
                        else
                            m_Items.Add(item);
                    }
                }
            }
        }
    }

    class SubgraphVariant : Variant
    {
        private readonly string m_Guid;

        public SubgraphVariant(string name, string category, Type modelType, KeyValuePair<string, object>[] kvp, string guid)
            : base(name, category, modelType, kvp, null, null, true)
        {
            m_Guid = guid;
        }

        public override string GetUniqueIdentifier() => m_Guid;
    }

    class VFXNodeProvider : VFXAbstractProvider<VFXOperator>
    {
        readonly Func<IVFXModelDescriptor, bool> m_Filter;
        readonly IEnumerable<Type> m_AcceptedTypes;
        readonly VFXViewController m_Controller;

        public VFXNodeProvider(VFXViewController controller, Action<Variant, Vector2> onAddBlock, Func<IVFXModelDescriptor, bool> filter = null, IEnumerable<Type> acceptedTypes = null) : base(onAddBlock)
        {
            m_Filter = filter;
            m_AcceptedTypes = acceptedTypes;
            m_Controller = controller;
        }

#if VFX_HAS_UNIT_TEST
        public IEnumerable<IVFXModelDescriptor> GetDescriptorsForInternalTest()
        {
            return GetDescriptors();
        }
#endif

        public override IEnumerable<IVFXModelDescriptor> GetDescriptors()
        {
            var descs = new List<IVFXModelDescriptor>();

            if (m_AcceptedTypes == null || m_AcceptedTypes.Contains(typeof(VFXContext)))
            {
                descs.AddRange(VFXLibrary.GetContexts());
            }
            if (m_AcceptedTypes == null || m_AcceptedTypes.Contains(typeof(VFXOperator)))
            {
                descs.AddRange(VFXLibrary.GetOperators());
                descs.AddRange(SubGraphCache.GetItems(typeof(VisualEffectSubgraphOperator)).Select(x => new VFXModelDescriptor<VFXOperator>(new SubgraphVariant(
                    x.name,
                    x.category,
                    typeof(VisualEffectSubgraphOperator),
                    new []{ new KeyValuePair<string, object>("path", x.path)},
                    x.guid),
                    null)));
                descs.AddRange(m_Controller.graph.attributesManager.GetCustomAttributes().Select(x => new VFXModelDescriptor<VFXOperator>(new Variant(
                    $"Get {x.name}",
                    "Operator/Attribute/".AppendSeparator("Custom", 0),
                    typeof(VFXAttributeParameter),
                    new[] { new KeyValuePair<string, object>(nameof(VFXAttributeParameter.attribute), x.name) },
                    null,
                    null,
                    false), null)));
            }
            if (m_AcceptedTypes == null || m_AcceptedTypes.Contains(typeof(VFXParameter)))
            {
                var parameterVariants = m_Controller.parameterControllers.Select(t => new VFXModelDescriptor<VFXParameter>(
                    new VFXModelDescriptorParameters.ParameterVariant(
                        t.exposedName,
                        string.IsNullOrEmpty(t.model.category)
                            ? "Property"
                            : "Property/".AppendSeparator(t.model.category, 0),
                        t.portType), null));
                descs.AddRange(parameterVariants);
            }

            return m_Filter == null ? descs : descs.Where(t => m_Filter(t));
        }
    }
}
