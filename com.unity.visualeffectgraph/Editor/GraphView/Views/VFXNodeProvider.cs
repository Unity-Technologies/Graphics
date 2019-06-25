//#define OLD_COPY_PASTE
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.VFX;
using UnityEngine.VFX;

using UnityObject = UnityEngine.Object;

namespace UnityEditor.VFX.UI
{
    class GroupNodeAdder
    {
    }

    abstract class SubGraphCache
    {

        protected SubGraphCache()
        {
        }

        string m_Filter;
        public struct Item
        {
            public string category;
            public string name;
            public string path;
            public object additionalInfos;
        }

        public struct AdditionalBlockInfo
        {
            public VFXContextType compatibleType;
            public VFXDataType compatibleData;
        }

        protected List<Item> m_Items = new List<Item>();
        protected bool m_UptoDate = false;

        public IEnumerable<Item> items { get {
                UpdateCache();
                return m_Items;
            } }

        protected abstract void UpdateCache();

        static Dictionary<Type, SubGraphCache> s_Caches = new Dictionary<Type, SubGraphCache>
        {
            { typeof(VisualEffectAsset), new SubGraphCache<VisualEffectAsset>()},
            { typeof(VisualEffectSubgraphBlock), new SubGraphCache<VisualEffectSubgraphBlock>()},
            { typeof(VisualEffectSubgraphOperator), new SubGraphCache<VisualEffectSubgraphOperator>()},
        };

        static void MarkChanged(Type type)
        {
            SubGraphCache cache;
            s_Caches.TryGetValue(type, out cache);
            if (cache != null)
                cache.m_UptoDate = false;
        }

        public static IEnumerable<Item> GetItems(Type type)
        {
            SubGraphCache cache;
            s_Caches.TryGetValue(type, out cache);
            if (cache != null)
                return cache.items;
            return Enumerable.Empty<Item>();
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
                if( ! path.StartsWith(VisualEffectAssetEditorUtility.templatePath))
                {
                    T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (asset != null)
                    {
                        VisualEffectResource res = asset.GetResource();

                        Item item = new Item() { name = asset.name, category = res.GetOrCreateGraph().categoryPath, path = path };
                        if (item.category == null)
                            item.category = "";

                        if ( typeof(T) == typeof(VisualEffectSubgraphBlock))
                        {
                            VFXBlockSubgraphContext blockContext = asset.GetResource().GetOrCreateGraph().children.OfType<VFXBlockSubgraphContext>().FirstOrDefault();

                            if (blockContext != null)
                            {
                                item.additionalInfos = new AdditionalBlockInfo() { compatibleType = blockContext.compatibleContextType, compatibleData = blockContext.ownedType };
                                m_Items.Add(item);
                            }
                        }
                        else
                            m_Items.Add(item);

                    }
                }
            }
            m_UptoDate = true;
        }
    }

    class VFXNodeProvider : VFXAbstractProvider<VFXNodeProvider.Descriptor>
    {
        public class Descriptor
        {
            public object modelDescriptor;
            public string category;
            public string name;
        }

        Func<Descriptor, bool> m_Filter;
        IEnumerable<Type> m_AcceptedTypes;
        VFXViewController m_Controller;

        public VFXNodeProvider(VFXViewController controller, Action<Descriptor, Vector2> onAddBlock, Func<Descriptor, bool> filter = null, IEnumerable<Type> acceptedTypes = null) : base(onAddBlock)
        {
            m_Filter = filter;
            m_AcceptedTypes = acceptedTypes;
            m_Controller = controller;
        }

        protected override string GetCategory(Descriptor desc)
        {
            return desc.category;
        }

        protected override string GetName(Descriptor desc)
        {
            return desc.name;
        }

        protected override string title
        {
            get {return "Node"; }
        }

        string ComputeCategory<T>(string type, VFXModelDescriptor<T> model) where T : VFXModel
        {
            if (model.info != null && model.info.category != null)
            {
                if (m_AcceptedTypes != null && m_AcceptedTypes.Count() == 1)
                {
                    return model.info.category;
                }
                else
                {
                    return string.Format("{0}/{1}", type, model.info.category);
                }
            }
            else
            {
                return type;
            }
        }

        protected override IEnumerable<Descriptor> GetDescriptors()
        {
            IEnumerable<Descriptor> descs = Enumerable.Empty<Descriptor>();

            if (m_AcceptedTypes == null || m_AcceptedTypes.Contains(typeof(VFXContext)))
            {
                var descriptorsContext = VFXLibrary.GetContexts().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = ComputeCategory("Context", o),
                        name = o.name
                    };
                }).OrderBy(o => o.category + o.name);

                descs = descs.Concat(descriptorsContext);
            }
            if (m_AcceptedTypes == null || m_AcceptedTypes.Contains(typeof(VFXOperator)))
            {
                var descriptorsOperator = VFXLibrary.GetOperators().Select(o =>
                {
                    return new Descriptor()
                    {
                        modelDescriptor = o,
                        category = ComputeCategory("Operator", o),
                        name = o.name
                    };
                });

                descriptorsOperator = descriptorsOperator.Concat(SubGraphCache.GetItems(typeof(VisualEffectSubgraphOperator)).Select(
                    t => new Descriptor()
                        {
                            modelDescriptor = t.path,
                            category = "Operator/Subgraph Operator/" + t.category,
                            name = t.name
                        }
                    ));

                descs = descs.Concat(descriptorsOperator.OrderBy(o => o.category + o.name));
            }
            if (m_AcceptedTypes == null || m_AcceptedTypes.Contains(typeof(VFXParameter)))
            {
                var parameterDescriptors = m_Controller.parameterControllers.Select(t =>
                    new Descriptor
                    {
                        modelDescriptor = t,
                        category = string.IsNullOrEmpty(t.model.category) ? "Parameter" : string.Format("Parameter/{0}", t.model.category),
                        name = t.exposedName
                    }
                    ).OrderBy(t => t.category);
                descs = descs.Concat(parameterDescriptors);
            }
            if (m_AcceptedTypes == null)
            {
                var systemFiles = System.IO.Directory.GetFiles(VisualEffectAssetEditorUtility.templatePath).Where(t=> Path.GetExtension(t) == VisualEffectResource.Extension).Select(t => t.Replace("\\", "/"));

                var systemDesc = systemFiles.Select(t => new Descriptor() { modelDescriptor = t.Replace(VisualEffectGraphPackageInfo.fileSystemPackagePath, VisualEffectGraphPackageInfo.assetPackagePath), category = "System", name = System.IO.Path.GetFileNameWithoutExtension(t) });

                descs = descs.Concat(systemDesc);
            }
            var groupNodeDesc = new Descriptor()
            {
                modelDescriptor = new GroupNodeAdder(),
                category = "Misc",
                name = "Group Node"
            };

            descs = descs.Concat(Enumerable.Repeat(groupNodeDesc, 1));

            if (m_Filter == null)
                return descs;
            else
                return descs.Where(t => m_Filter(t));
        }
    }
}
