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
        public IGroupModel Group { get; set; }

        /// <inheritdoc />
        public IReadOnlyList<IGroupItemModel> Items => m_Items;

        IEnumerable<IGraphElementModel> IGraphElementContainer.GraphElementModels => Items;

        public override IGraphElementContainer Container => Group;

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
                Overdrive.Capabilities.Renamable
            });
        }

        /// <inheritdoc />
        public void InsertItem(IGroupItemModel itemModel, int index = -1)
        {
            if (itemModel.Group != null)
                itemModel.Group.RemoveItem(itemModel);

            itemModel.Group = this;
            if (index < 0 || index >= m_Items.Count)
                m_Items.Add(itemModel);
            else
                m_Items.Insert(index, itemModel);
        }

        /// <inheritdoc />
        public bool MoveItemsAfter(IReadOnlyList<IGroupItemModel> items, IGroupItemModel insertAfter)
        {
            int insertIndex = m_Items.IndexOf(insertAfter);

            if (insertAfter != null && insertIndex == -1)
                return false;

            if (items.Contains((insertAfter)))
                return false;

            foreach (var model in items)
                model.Group.RemoveItem(model);

            insertIndex = m_Items.IndexOf(insertAfter);

            foreach (var model in items)
                InsertItem(model, ++insertIndex);
            return true;
        }

        /// <inheritdoc />
        public void RemoveItem(IGroupItemModel itemModel)
        {
            itemModel.Group = null;

            m_Items.Remove(itemModel);
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
                item.Group = this;
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
    }
}
