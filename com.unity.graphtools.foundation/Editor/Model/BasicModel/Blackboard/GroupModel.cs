using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    public class GroupModel : GraphElementModel, IGroupModel, IRenamable
    {
        [SerializeReference]
        List<IGroupItemModel> m_Items = new List<IGroupItemModel>();

        /// <inheritdoc />
        [field: SerializeField]
        public string Title { get; set; }

        /// <inheritdoc />
        public virtual string DisplayTitle => Title;

        /// <inheritdoc />
        public IGroupModel ParentGroup { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<IGroupItemModel> Items => m_Items;

        IEnumerable<IGraphElementModel> IGraphElementContainer.GraphElementModels => Items;

        /// <inheritdoc />
        public override IGraphElementContainer Container => ParentGroup;

        /// <inheritdoc />
        public IEnumerable<IGraphElementModel> ContainedModels
        {
            get
            {
                return Items.SelectMany(t => Enumerable.Repeat(t, 1).Concat(t.ContainedModels));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupModel" /> class.
        /// </summary>
        public GroupModel()
        {
            m_Capabilities.AddRange(new[]
            {
                Overdrive.Capabilities.Deletable,
                Overdrive.Capabilities.Droppable,
                Overdrive.Capabilities.Selectable,
                Overdrive.Capabilities.Collapsible,
                Overdrive.Capabilities.Copiable,
                Overdrive.Capabilities.Renamable
            });
        }

        /// <inheritdoc />
        public IEnumerable<IGraphElementModel> InsertItem(IGroupItemModel itemModel, int index = int.MaxValue)
        {
            HashSet<IGraphElementModel> changedModels = new HashSet<IGraphElementModel>();

            IGroupModel current = this;
            while (current != null)
            {
                if (ReferenceEquals(current, itemModel))
                    return Enumerable.Empty<IGraphElementModel>();
                current = current.ParentGroup;
            }

            if (itemModel.ParentGroup != null)
                changedModels.UnionWith(itemModel.ParentGroup.RemoveItem(itemModel));

            changedModels.Add(this);
            itemModel.ParentGroup = this;
            if (index < 0)
                m_Items.Insert(0,itemModel);
            else if (index >= m_Items.Count)
                m_Items.Add(itemModel);
            else
                m_Items.Insert(index, itemModel);

            return changedModels;
        }

        /// <inheritdoc />
        public IEnumerable<IGraphElementModel> MoveItemsAfter(IReadOnlyList<IGroupItemModel> items, IGroupItemModel insertAfter)
        {
            if (insertAfter != null && !m_Items.Contains(insertAfter))
                return null;

            if (items.Contains((insertAfter)))
                return null;

            HashSet<IGraphElementModel> changedModels = new HashSet<IGraphElementModel>();
            foreach (var model in items)
            {
                if (model.ParentGroup != null)
                    changedModels.UnionWith(model.ParentGroup.RemoveItem(model));
            }

            // remove items from m_Items
            //   done by replacing m_Items with a copy that excludes items
            //   in most cases this is faster than doing many List.Remove
            var itemsCopy = new List<IGroupItemModel>(m_Items.Count);
            foreach (var item in m_Items)
            {
                if (!items.Contains(item))
                    itemsCopy.Add(item);
            }
            m_Items = itemsCopy;

            int insertIndex = m_Items.IndexOf(insertAfter);

            foreach (var model in items)
                changedModels.UnionWith(InsertItem(model, ++insertIndex));
            return changedModels;
        }

        /// <inheritdoc />
        public IEnumerable<IGraphElementModel> RemoveItem(IGroupItemModel itemModel)
        {
            HashSet<IGraphElementModel> changedModels = new HashSet<IGraphElementModel>();

            if (m_Items.Contains(itemModel))
            {
                itemModel.ParentGroup = null;

                m_Items.Remove(itemModel);

                changedModels.Add(this);
            }

            return changedModels;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            if (m_Items.Any(t => t == null))
            {
                m_Items = m_Items.Where(t => t != null).ToList();
            }
            foreach (var item in m_Items)
            {
                item.ParentGroup = this;
            }
        }

        public void RemoveElements(IReadOnlyCollection<IGraphElementModel> elementModels)
        {
            foreach (var element in elementModels)
                if (element is IGroupItemModel item)
                    RemoveItem(item);
        }

        public void Rename(string name)
        {
            Title = name;
        }

        /// <inheritdoc />
        public virtual void Repair()
        {
            m_Items.RemoveAll(t=>t == null);
            foreach (var item in m_Items.OfType<IGroupModel>())
            {
                item.Repair();
            }
        }
    }
}
