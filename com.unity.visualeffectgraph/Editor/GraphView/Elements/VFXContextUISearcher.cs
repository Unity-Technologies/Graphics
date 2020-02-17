using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Profiling;
using UnityEditor.Searcher;

using PositionType = UnityEngine.UIElements.Position;

namespace UnityEditor.VFX.UI
{
    partial class VFXContextUI : VFXNodeUI
    {

        List<SearcherItem> m_RootSearcherItems;
        class VFXContextSearcherItem : VFXSearcherItem
        {
            public VFXContextSearcherItem(VFXBlockProvider.Descriptor descriptor, string name, string help = "", List<SearcherItem> children = null)
            : base(name, descriptor.model, help, children)
            {
                m_Descriptor = descriptor;
            }
            VFXBlockProvider.Descriptor m_Descriptor;
            public VFXBlockProvider.Descriptor descriptor { get => m_Descriptor; }
        }
        class VFXContextSearcherAdapter : VFXSearcherAdapter
        {

            public VFXContextSearcherAdapter(string title, VFXView view) : base(title, view) { }



            public override void OnSelectionChanged(IEnumerable<SearcherItem> items)
            {
                base.OnSelectionChanged(items);
                if (items.OfType<VFXContextSearcherItem>().Any())
                {
                    var searcherItem = items.OfType<VFXContextSearcherItem>().First();

                    if (searcherItem.descriptor is VFXBlockProvider.NewBlockDescriptor contextDesc)
                    {
                        var newBlock = contextDesc.newBlock.CreateInstance();

                        VFXBlockController newBlockController = new VFXBlockController(newBlock, m_View.controller);
                        m_Controller = newBlockController;
                        newBlockController.ForceUpdate();
                        m_Node = new VFXBlockUI();
                        m_Node.settingsVisibility = VFXSettingAttribute.VisibleFlags.All;
                        m_Node.controller = newBlockController;
                        m_Node.style.position = PositionType.Relative;
                        m_Node.titleContainer.Insert(m_Node.childCount - 1, m_GlassPane);
                        m_NodeShape.Add(m_Node);
                        m_DragObject = contextDesc;
                        m_DragType = nameof(VFXBlockProvider.NewBlockDescriptor);
                    }
                }
            }
        }

        void InitilializeNewNodeSearcher()
        {
            m_RootSearcherItems = new List<SearcherItem>();

            var dict = new Dictionary<string, SearcherItem>();

            foreach (var desc in m_BlockProvider.descriptors)
            {
                SearcherItem categorySearchItem;

                if (dict.TryGetValue(desc.category, out categorySearchItem))
                {
                    categorySearchItem.AddChild(new VFXContextSearcherItem(desc, desc.name));
                }
                else
                {
                    string[] categories = desc.category.Split('/');
                    List<SearcherItem> currentList = m_RootSearcherItems;
                    Action<SearcherItem> addItemAction = item => m_RootSearcherItems.Add(item);

                    for (int i = 0; i < categories.Length; ++i)
                    {
                        if (!string.IsNullOrEmpty(categories[i]))
                        {
                            SearcherItem item = currentList.Find(t => t.Name == categories[i]);
                            if (item == null)
                            {
                                item = new SearcherItem(categories[i], categories[i]);
                                addItemAction(item);
                                dict[desc.category] = item;
                            }
                            currentList = item.Children;
                            addItemAction = t => item.AddChild(t);
                        }
                    }

                    addItemAction(new VFXContextSearcherItem(desc, desc.name));
                }
            }
        }

        VFXContextSearcherAdapter m_SearcherAdapter;

        class VFXContextSearcherDatabase : VFXSearcherDatabase
        {
            public VFXContextSearcherDatabase(IReadOnlyCollection<SearcherItem> db)
               : base(db)
            {
                MatchFilter = FilterNotVisible;
            }

            protected bool FilterNotVisible(string query, SearcherItem item)
            {
                if (string.IsNullOrEmpty(query))
                {
                    var contextItem = item as VFXContextSearcherItem;
                    if (contextItem != null && contextItem.descriptor is VFXBlockProvider.NewBlockDescriptor desc)
                    {
                        if (!desc.newBlock.visibleIfNotSearched)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

    }
}
